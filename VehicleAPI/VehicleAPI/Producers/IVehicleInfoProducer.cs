using VehicleAPI.DTO;
using VehicleAPI.Models;

namespace VehicleAPI.Producers
{
    public interface IVehicleInfoProducer
    {
        Task<string> ProduceAsync(string topic, string key, VehicleReadDTO vehicle, CancellationToken ct = default);
    }
}
