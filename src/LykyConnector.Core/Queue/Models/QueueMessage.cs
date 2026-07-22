namespace LykyConnector.Core.Queue.Models;

public enum QueueStatus
{
    Pending = 0,
    Processing = 1,
    Success = 2,
    Failed = 3,
    DeadLetter = 4
}

public class QueueMessage
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string? OutTradeNo { get; set; }
    public int TryCount { get; set; }
    public int MaxRetry { get; set; } = 5;
    public QueueStatus Status { get; set; } = QueueStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NextRetryAt { get; set; }
    public string? LastError { get; set; }
    public DateTime? DoneAt { get; set; }
}
