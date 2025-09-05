using VehicleAPI.Models;

namespace VehicleAPI.Repositories
{
    public interface IVehicleRepo
    {
        Task<Vehicle> AddVehicle(Vehicle vehicle);
        Task<bool> RemoveVehicle(string vehicleId);
        Task<Vehicle> GetVehicle(string vehicleId);
        Task<IEnumerable<Vehicle>> GetVehicles();
        Task<Vehicle> UpdateVehicle(string RegNo, string color);         



    }
}
