using LykyConnector.Core.Queue.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LykyConnector.Core.Queue;

public interface IProcessor
{
    string Type { get; }
    Task<bool> HandleAsync(QueueMessage message, CancellationToken ct);
}

public class QueueConsumer : BackgroundService
{
    private readonly IMessageQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueueConsumer> _logger;

    public QueueConsumer(
        IMessageQueue queue,
        IServiceProvider serviceProvider,
        ILogger<QueueConsumer> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("队列消费调度启动");

        var processors = _serviceProvider.GetServices<IProcessor>()
            .ToDictionary(p => p.Type, p => p);

        _logger.LogInformation("已注册 {Count} 个消息处理器: {Types}",
            processors.Count, string.Join(", ", processors.Keys));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(processors, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "队列消费异常");
            }

            await Task.Delay(2000, stoppingToken);
        }

        _logger.LogInformation("队列消费调度已停止");
    }

    private async Task ProcessBatchAsync(
        Dictionary<string, IProcessor> processors,
        CancellationToken ct)
    {
        for (int i = 0; i < 20; i++)
        {
            if (ct.IsCancellationRequested) break;

            var message = await _queue.DequeueAsync(ct);
            if (message == null) break;

            if (!processors.TryGetValue(message.Type, out var processor))
            {
                _logger.LogWarning("未注册的处理器 Type={Type}", message.Type);
                await _queue.MarkFailedAsync(message.Id, $"未注册的处理器: {message.Type}");
                continue;
            }

            try
            {
                var success = await processor.HandleAsync(message, ct);
                if (success)
                {
                    await _queue.MarkSuccessAsync(message.Id);
                }
                else
                {
                    await _queue.MarkFailedAsync(message.Id, "处理器返回失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理消息失败 Type={Type} Id={Id}", message.Type, message.Id);
                await _queue.MarkFailedAsync(message.Id, ex.Message);
            }
        }
    }
}
