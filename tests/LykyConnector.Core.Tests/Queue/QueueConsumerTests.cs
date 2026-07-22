using LykyConnector.Core.Queue;
using LykyConnector.Core.Queue.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace LykyConnector.Core.Tests.Queue;

public class QueueConsumerTests
{
    [Fact]
    public async Task Consumer_DispatchesToCorrectProcessor()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_cons_{Guid.NewGuid():N}.db");
        var queue = new MessageQueue(dbPath);

        try
        {
            await queue.EnqueueAsync(new QueueMessage
            {
                Type = "PushForward",
                Payload = """{"test":true}""",
                OutTradeNo = "T001"
            });

            var processor = new Mock<IProcessor>();
            processor.Setup(p => p.Type).Returns("PushForward");
            processor.Setup(p => p.HandleAsync(It.IsAny<QueueMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var services = new ServiceCollection();
            services.AddSingleton<IMessageQueue>(queue);
            services.AddSingleton(processor.Object);
            services.AddLogging(b => b.AddConsole());

            var provider = services.BuildServiceProvider();
            var consumer = new QueueConsumer(
                queue,
                provider,
                provider.GetRequiredService<ILogger<QueueConsumer>>());

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var task = consumer.StartAsync(cts.Token);

            await Task.Delay(3000, CancellationToken.None);
            cts.Cancel();

            await task;

            var pending = await queue.GetPendingCountAsync();
            Assert.Equal(0, pending);

            processor.Verify(
                p => p.HandleAsync(
                    It.Is<QueueMessage>(m => m.Type == "PushForward"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}
