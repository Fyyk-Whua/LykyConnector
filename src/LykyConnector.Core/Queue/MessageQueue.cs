using System.Text.Json;
using LykyConnector.Core.Queue.Models;
using Microsoft.Data.Sqlite;

namespace LykyConnector.Core.Queue;

public interface IMessageQueue
{
    Task<long> EnqueueAsync(QueueMessage message);
    Task<QueueMessage?> DequeueAsync(CancellationToken ct = default);
    Task MarkSuccessAsync(long id);
    Task MarkFailedAsync(long id, string error);
    Task<long> GetPendingCountAsync();
    Task<List<QueueMessage>> GetDeadLettersAsync(int limit = 100);
    Task RequeueDeadLetterAsync(long id);
}

public class MessageQueue : IMessageQueue
{
    private readonly string _connectionString;

    public MessageQueue(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeTable();
    }

    private void InitializeTable()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS queue (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                type TEXT NOT NULL,
                payload TEXT NOT NULL,
                out_trade_no TEXT,
                try_count INTEGER NOT NULL DEFAULT 0,
                max_retry INTEGER NOT NULL DEFAULT 5,
                status INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                next_retry_at TEXT,
                last_error TEXT,
                done_at TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_queue_status ON queue(status);
            CREATE INDEX IF NOT EXISTS idx_queue_next_retry ON queue(next_retry_at);
            CREATE INDEX IF NOT EXISTS idx_queue_out_trade_no ON queue(out_trade_no);
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task<long> EnqueueAsync(QueueMessage message)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO queue (type, payload, out_trade_no, try_count, max_retry,
                status, created_at, next_retry_at)
            VALUES (@type, @payload, @out_trade_no, 0, @max_retry,
                @status, @created_at, @next_retry_at);
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("@type", message.Type);
        cmd.Parameters.AddWithValue("@payload", message.Payload);
        cmd.Parameters.AddWithValue("@out_trade_no", (object?)message.OutTradeNo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@max_retry", message.MaxRetry);
        cmd.Parameters.AddWithValue("@status", (int)message.Status);
        cmd.Parameters.AddWithValue("@created_at", message.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@next_retry_at",
            (object?)message.NextRetryAt?.ToString("O") ?? DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        return (long)result!;
    }

    public async Task<QueueMessage?> DequeueAsync(CancellationToken ct = default)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        using var transaction = conn.BeginTransaction();

        using var selectCmd = conn.CreateCommand();
        selectCmd.Transaction = transaction;
        selectCmd.CommandText = """
            SELECT id, type, payload, out_trade_no, try_count, max_retry,
                   status, created_at, next_retry_at, last_error, done_at
            FROM queue
            WHERE status = 0
              AND (next_retry_at IS NULL OR next_retry_at <= @now)
            ORDER BY id ASC
            LIMIT 1
            """;
        selectCmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));

        using var reader = await selectCmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            await transaction.RollbackAsync(ct);
            return null;
        }

        var message = ReadMessage(reader);
        reader.Close();

        using var updateCmd = conn.CreateCommand();
        updateCmd.Transaction = transaction;
        updateCmd.CommandText = """
            UPDATE queue SET status = 1 WHERE id = @id AND status = 0
            """;
        updateCmd.Parameters.AddWithValue("@id", message.Id);

        var updated = await updateCmd.ExecuteNonQueryAsync(ct);
        if (updated == 0)
        {
            await transaction.RollbackAsync(ct);
            return null;
        }

        await transaction.CommitAsync(ct);
        return message;
    }

    public async Task MarkSuccessAsync(long id)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE queue SET status = 2, done_at = @done_at WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@done_at", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkFailedAsync(long id, string error)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        using var selectCmd = conn.CreateCommand();
        selectCmd.CommandText = """
            SELECT try_count, max_retry FROM queue WHERE id = @id
            """;
        selectCmd.Parameters.AddWithValue("@id", id);

        using var reader = await selectCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return;

        var tryCount = reader.GetInt32(0) + 1;
        var maxRetry = reader.GetInt32(1);
        reader.Close();

        if (tryCount >= maxRetry)
        {
            using var deadCmd = conn.CreateCommand();
            deadCmd.CommandText = """
                UPDATE queue SET status = 4, try_count = @try_count,
                    last_error = @error, done_at = @done_at
                WHERE id = @id
                """;
            deadCmd.Parameters.AddWithValue("@try_count", tryCount);
            deadCmd.Parameters.AddWithValue("@error", error);
            deadCmd.Parameters.AddWithValue("@done_at", DateTime.UtcNow.ToString("O"));
            deadCmd.Parameters.AddWithValue("@id", id);
            await deadCmd.ExecuteNonQueryAsync();
        }
        else
        {
            var backoffSec = 10 * Math.Pow(2, tryCount);
            var nextRetry = DateTime.UtcNow.AddSeconds(backoffSec);

            using var retryCmd = conn.CreateCommand();
            retryCmd.CommandText = """
                UPDATE queue SET status = 0, try_count = @try_count,
                    last_error = @error, next_retry_at = @next_retry
                WHERE id = @id
                """;
            retryCmd.Parameters.AddWithValue("@try_count", tryCount);
            retryCmd.Parameters.AddWithValue("@error", error);
            retryCmd.Parameters.AddWithValue("@next_retry", nextRetry.ToString("O"));
            retryCmd.Parameters.AddWithValue("@id", id);
            await retryCmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<long> GetPendingCountAsync()
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM queue WHERE status IN (0, 1)";
        var result = await cmd.ExecuteScalarAsync();
        return (long)result!;
    }

    public async Task<List<QueueMessage>> GetDeadLettersAsync(int limit = 100)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, type, payload, out_trade_no, try_count, max_retry,
                   status, created_at, next_retry_at, last_error, done_at
            FROM queue WHERE status = 4
            ORDER BY created_at DESC LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@limit", limit);

        var messages = new List<QueueMessage>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(ReadMessage(reader));
        }
        return messages;
    }

    public async Task RequeueDeadLetterAsync(long id)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE queue SET status = 0, try_count = 0,
                next_retry_at = NULL, last_error = NULL
            WHERE id = @id AND status = 4
            """;
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ResetProcessingOnStartup()
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE queue SET status = 0 WHERE status = 1";
        await cmd.ExecuteNonQueryAsync();
    }

    private static QueueMessage ReadMessage(SqliteDataReader reader)
    {
        return new QueueMessage
        {
            Id = reader.GetInt64(0),
            Type = reader.GetString(1),
            Payload = reader.GetString(2),
            OutTradeNo = reader.IsDBNull(3) ? null : reader.GetString(3),
            TryCount = reader.GetInt32(4),
            MaxRetry = reader.GetInt32(5),
            Status = (QueueStatus)reader.GetInt32(6),
            CreatedAt = DateTime.Parse(reader.GetString(7)),
            NextRetryAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
            LastError = reader.IsDBNull(9) ? null : reader.GetString(9),
            DoneAt = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10))
        };
    }
}
