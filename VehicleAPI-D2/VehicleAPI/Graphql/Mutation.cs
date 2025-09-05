using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using VehicleAPI.Contexts;
using VehicleAPI.DTO;
using VehicleAPI.Models;

namespace VehicleAPI.Graphql
{
    public class Mutation
    {

        public async Task<Vehicle> CreateVehicle([Service] VehicleContext db, VehicleDTO input)
        {
            var vehicle = new Vehicle
            {
                RegistrationId = input.RegistrationId,
                Maker = input.Maker,
                DOR = input.DOR,
                ChassisNo = input.ChassisNo,
                EngineNo = input.EngineNo,
                Color = input.Color,
                FuelType = input.FuelType
               
            };
            await db.Vehicles.AddAsync(vehicle);
            await db.SaveChangesAsync();
            return vehicle;
        }


        public async Task<Vehicle> UpdateVehicle([Service] VehicleContext db, string id, string color)
        {
            var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.RegistrationId == id);
            if (vehicle is null)
            {
                throw new Exception("Vehicle not found");
            }

            vehicle.Color = color;
            
            await db.SaveChangesAsync();
            return vehicle;
        }

        public async Task<bool> DeleteVehicle([Service] VehicleContext db, string id)
        {
            var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.RegistrationId == id);
            if (vehicle is null)
            {
                throw new Exception("Vehicle not found");
            }
            db.Vehicles.Remove(vehicle);
            await db.SaveChangesAsync();
            return true;
        }

    }
}
