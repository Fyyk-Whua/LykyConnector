using LykyConnector.Core.Queue;
using LykyConnector.Core.Queue.Models;
using Microsoft.Data.Sqlite;

namespace LykyConnector.Core.Tests.Queue;

public class MessageQueueTests : IDisposable
{
    private readonly string _testDb;

    public MessageQueueTests()
    {
        _testDb = Path.Combine(Path.GetTempPath(), $"test_lyky_queue_{Guid.NewGuid():N}.db");
    }

    [Fact]
    public async Task Enqueue_ThenDequeue_ReturnsSameMessage()
    {
        var queue = new MessageQueue(_testDb);
        var msg = new QueueMessage
        {
            Type = "PushForward",
            Payload = """{"out_trade_no":"123"}""",
            OutTradeNo = "123"
        };

        var id = await queue.EnqueueAsync(msg);
        Assert.True(id > 0);

        var dequeued = await queue.DequeueAsync();
        Assert.NotNull(dequeued);
        Assert.Equal("PushForward", dequeued!.Type);
        Assert.Equal("123", dequeued.OutTradeNo);
    }

    [Fact]
    public async Task MarkSuccess_StatusBecomesSuccess()
    {
        var queue = new MessageQueue(_testDb);
        var id = await queue.EnqueueAsync(new QueueMessage { Type = "Test", Payload = "{}" });

        var msg = await queue.DequeueAsync();
        await queue.MarkSuccessAsync(msg!.Id);

        var pending = await queue.GetPendingCountAsync();
        Assert.Equal(0, pending);
    }

    [Fact]
    public async Task MarkFailed_RetriesThenDeadLetter()
    {
        var queue = new MessageQueue(_testDb);
        var msg = new QueueMessage { Type = "Test", Payload = "{}", MaxRetry = 1 };
        var id = await queue.EnqueueAsync(msg);

        var dequeued = await queue.DequeueAsync();
        Assert.NotNull(dequeued);
        await queue.MarkFailedAsync(dequeued!.Id, "failure");

        var deadLetters = await queue.GetDeadLettersAsync();
        Assert.Single(deadLetters);
        Assert.Equal(QueueStatus.DeadLetter, deadLetters[0].Status);
        Assert.Equal("failure", deadLetters[0].LastError);
    }

    [Fact]
    public async Task RequeueDeadLetter_ReturnsToPending()
    {
        var queue = new MessageQueue(_testDb);
        var msg = new QueueMessage { Type = "Test", Payload = "{}", MaxRetry = 1 };
        var id = await queue.EnqueueAsync(msg);

        var dq = await queue.DequeueAsync();
        await queue.MarkFailedAsync(dq!.Id, "error");

        await queue.RequeueDeadLetterAsync(id);

        var pending = await queue.GetPendingCountAsync();
        Assert.Equal(1, pending);
    }

    [Fact]
    public async Task Dequeue_Empty_ReturnsNull()
    {
        var queue = new MessageQueue(_testDb);
        var result = await queue.DequeueAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task ResetProcessingOnStartup_SetsProcessingToPending()
    {
        var queue = new MessageQueue(_testDb);
        var id = await queue.EnqueueAsync(new QueueMessage { Type = "Test", Payload = "{}" });
        await queue.DequeueAsync();

        var queue2 = new MessageQueue(_testDb);
        await queue2.ResetProcessingOnStartup();

        var msg = await queue2.DequeueAsync();
        Assert.NotNull(msg);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_testDb)) File.Delete(_testDb);
    }
}
