
using Confluent.Kafka;
using GreenDonut;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Text.Json;
using VehicleAPI.DTO;
using static Confluent.Kafka.ConfigPropertyNames;

namespace VehicleAPI.Producers
{
    public sealed class KafkaProducerOptions
    {
        public string BootstrapServers { get; init; } = "kafka:9092";
        public string Acks { get; init; } = "all";
        public bool EnableIdempotence { get; init; } = true;
        public string CompressionType { get; init; } = "gzip";
        public int LingerMs { get; init; } = 5;
        public int BatchSize { get; init; } = 65536;
    }

    public class VehicleInfoProducer : IVehicleInfoProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<VehicleInfoProducer> _logger;
       

        public VehicleInfoProducer(IOptions<KafkaProducerOptions> options, ILogger<VehicleInfoProducer> logger)
        {
            _logger = logger;
            var cfg = options.Value;

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = cfg.BootstrapServers,
                // Replace this line:
                // Acks = cfg.Acks,
                // With the following:
                Acks = Enum.Parse<Acks>(cfg.Acks, ignoreCase: true),
                
                EnableIdempotence = cfg.EnableIdempotence,
                CompressionType = Enum.Parse<CompressionType>(cfg.CompressionType, ignoreCase: true),
                LingerMs = cfg.LingerMs,
                BatchSize = cfg.BatchSize,
                // Recommended reliability settings
                MessageSendMaxRetries = 5,
                RetryBackoffMs = 100,
                SocketKeepaliveEnable = true
            };

            _producer = new ProducerBuilder<string, string>(producerConfig)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Reason}", e.Reason))
                .SetStatisticsHandler((_, s) => _logger.LogDebug("Kafka stats: {Stats}", s))
                .Build();
        }

        public async Task<string> ProduceAsync(string topic, string key, VehicleReadDTO vehicle, CancellationToken ct = default)
        {
            string value = JsonSerializer.Serialize(vehicle);


            var msg = new Message<string, string> { 
            Key= key,
            Value= value

            };

            try
            {
                // Produce with delivery report
                var dr = await _producer.ProduceAsync(topic, msg, ct).ConfigureAwait(false);
                if (dr.Status != PersistenceStatus.Persisted)
                {
                    _logger.LogWarning("Message not fully persisted: {Status}", dr.Status);
                    return await Task.FromResult("Not Published.....");

                }
                else
                {
                    _logger.LogInformation("Produced to {TopicPartitionOffset}", dr.TopicPartitionOffset);
                    Debug.WriteLine($"Delivery Timestamp:{dr.Timestamp.UtcDateTime}");
                    _producer.Flush(TimeSpan.FromSeconds(60));
                    return await Task.FromResult($"Delivery Timestamp:{dr.Timestamp.UtcDateTime}");

                }
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Failed to produce to {Topic}: {Reason}", topic, ex.Error.Reason);
                return await Task.FromResult("Not Published....."+ex.Message);
            }
        }

        public void Dispose() => _producer.Flush(TimeSpan.FromSeconds(10));
    }
}
