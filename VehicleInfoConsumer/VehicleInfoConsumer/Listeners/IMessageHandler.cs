using Confluent.Kafka;

namespace VehicleInfoConsumer.Listeners
{
    public interface IMessageHandler
    {
        /// <summary>
        /// Return true to commit offset, false to skip commit (will retry on next poll).
        /// Throw for “poison” messages only if you want to drop/park them by policy.
        /// </summary>
        Task<bool> HandleAsync(string topic, string key, string value, Headers headers, CancellationToken ct);
    }
}
