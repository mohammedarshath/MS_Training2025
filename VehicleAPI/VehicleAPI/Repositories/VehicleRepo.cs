using Microsoft.EntityFrameworkCore;
using VehicleAPI.Contexts;
using VehicleAPI.Models;

namespace VehicleAPI.Repositories
{
    public class VehicleRepo : IVehicleRepo
    {
        private VehicleContext _vehicleContext;

        public VehicleRepo(VehicleContext vehicleContext) {
            _vehicleContext = vehicleContext;        
        
        }
        public async Task<Vehicle> AddVehicle(Vehicle vehicle)
        {
           var result= await _vehicleContext.Vehicles.AddAsync(vehicle);
            await _vehicleContext.SaveChangesAsync();
            return result.Entity;


        }

        public async Task<Vehicle> GetVehicle(string vehicleId)
        {
          var vehicle= await _vehicleContext.Vehicles.FirstOrDefaultAsync(v=>v.RegistrationId == vehicleId);
            if (vehicle == null)
            {
                return null;
            }
            return vehicle;
        }

        public async Task<IEnumerable<Vehicle>> GetVehicles()
        {
            return await _vehicleContext.Vehicles.ToListAsync();
        }

        public async Task<bool> RemoveVehicle(string vehicleId)
        {
            var status = false;
            var vehicle = await _vehicleContext.Vehicles.FirstOrDefaultAsync(v => v.RegistrationId == vehicleId);
            if (vehicle!=null)
            {
               _vehicleContext.Vehicles.Remove(vehicle);
                await _vehicleContext.SaveChangesAsync();
                status = true;
            }
            return status;

        }

        public async Task<Vehicle> UpdateVehicle(string RegNo, string color)
        {
            var vehicle = await _vehicleContext.Vehicles.FirstOrDefaultAsync(v => v.RegistrationId == RegNo);
            if (vehicle == null)
            {
                return null;
            }
            vehicle.Color = color;
            await _vehicleContext.SaveChangesAsync();
            return vehicle;
        }
    }
}
