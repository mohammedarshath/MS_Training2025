using Confluent.Kafka;
using System.Text.Json;
using VehicleInfoConsumer.DTOs;
using VehicleInfoConsumer.Repository;

namespace VehicleInfoConsumer.Listeners
{
    public class VehicleInfoHandler : IMessageHandler
    {
        private readonly IVehicleRepo repo;

        public VehicleInfoHandler(IVehicleRepo repo)
        {
            this.repo = repo;
        }
        public Task<bool> HandleAsync(string topic, string key, string value, Headers headers, TopicPartitionOffset topicPartitionOffset, CancellationToken ct)
        {
            // Example: deserialize and do something
            try
            {
                
                var vehicle = JsonSerializer.Deserialize<VehicleReadDTO>(value) ?? null;
                Console.WriteLine($"[VehicleInfoHandler] topic={topic} key={key} id={vehicle.RegNo} model={vehicle.model} dor={vehicle.dor}");
                // todo: call your domain logic / DB, etc.
                VehicleBson vehicleBson = new VehicleBson
                {
                    RegNo = vehicle.RegNo,
                    Model = vehicle.model,
                    DOR = vehicle.dor,
                    ChassisNo = vehicle.chassisNo,
                    EngineNo = vehicle.engineNo,
                    Color = vehicle.color,
                    FuelType = vehicle.fuelType,
                    Key = headers.ToString(),
                    PartitionOffset = topicPartitionOffset.Offset.ToString()
                };
                repo.AddVechicle(vehicleBson);

                return Task.FromResult(true); // commit
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VehicleInfoHandler] Deserialize/handle failed: {ex.Message}");
                // return false to avoid committing (will retry). Be careful with poison pills.
                return Task.FromResult(true); // demo: commit anyway to avoid infinite retries
            }
        }
    }
}
