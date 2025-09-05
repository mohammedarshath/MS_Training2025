
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using VehicleInfoConsumer.Listeners;

namespace VehicleInfoConsumer.Services
{
    public sealed class VehicleBackgroundService : BackgroundService
    {
        private readonly ILogger<VehicleBackgroundService> _logger;
        private readonly IMessageHandler _handler;
        private readonly KafkaConsumerOptions _opts;

        // simple in-memory metrics
        public long Consumed { get; private set; }
        public long Succeeded { get; private set; }
        public long Failed { get; private set; }
        public DateTimeOffset? LastMessageAt { get; private set; }
        public volatile bool Paused = false;

        public VehicleBackgroundService(
            IOptions<KafkaConsumerOptions> options,
            IMessageHandler handler,
            ILogger<VehicleBackgroundService> logger)
        {
            _opts = options.Value;
            _handler = handler;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _opts.BootstrapServers,
                GroupId = _opts.GroupId,
                EnableAutoCommit = _opts.EnableAutoCommit,
                AutoOffsetReset = Enum.Parse<AutoOffsetReset>(_opts.AutoOffsetReset, true),
                SessionTimeoutMs = _opts.SessionTimeoutMs,
                MaxPollIntervalMs = _opts.MaxPollIntervalMs,
                FetchMinBytes = _opts.FetchMinBytes,
                FetchWaitMaxMs = _opts.FetchWaitMaxMs,
                AllowAutoCreateTopics = true
            };

            using var consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Reason}", e.Reason))
                .SetStatisticsHandler((_, s) => _logger.LogDebug("Kafka stats: {Stats}", s))
                .SetPartitionsAssignedHandler((c, parts) =>
                {
                    _logger.LogInformation("Partitions assigned: {Parts}", string.Join(",", parts));
                })
                .SetPartitionsRevokedHandler((c, parts) =>
                {
                    _logger.LogWarning("Partitions revoked: {Parts}", string.Join(",", parts));
                })
                .Build();

            consumer.Subscribe(_opts.Topic);
            _logger.LogInformation("Subscribed to {Topic} as group {Group}", _opts.Topic, _opts.GroupId);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (Paused)
                    {
                        await Task.Delay(200, stoppingToken);
                        continue;
                    }

                    ConsumeResult<string, string>? cr = null;
                    try
                    {
                        cr = consumer.Consume(stoppingToken); // blocks up to broker wait config
                    }
                    catch (ConsumeException cex)
                    {
                        _logger.LogError(cex, "ConsumeException: {Reason}", cex.Error.Reason);
                        continue;
                    }

                    if (cr is null) continue;

                    Consumed++;
                    LastMessageAt = DateTimeOffset.UtcNow;

                    bool commit = false;
                    try
                    {
                        commit = await _handler.HandleAsync(cr.Topic, cr.Message.Key!, cr.Message.Value!, cr.Message.Headers, cr.TopicPartitionOffset, stoppingToken);
                        if (commit && !_opts.EnableAutoCommit)
                        {
                            consumer.Commit(cr);
                        }
                        Succeeded++;
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Failed++;
                        _logger.LogError(ex, "Handler failed for {TopicPartitionOffset}", cr.TopicPartitionOffset);
                        // Optional: send to DLT here with a producer; for demo we commit to avoid poison-pill loop.
                        if (!_opts.EnableAutoCommit)
                        {
                            consumer.Commit(cr);
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    consumer.Close(); // graceful leave-group
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during consumer.Close()");
                }
            }
        }
    }
}
