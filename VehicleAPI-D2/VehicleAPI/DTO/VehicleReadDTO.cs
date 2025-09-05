using VehicleAPI.Models;

namespace VehicleAPI.DTO
{
   public record VehicleReadDTO(string RegNo,string model, DateTime dor,  string chassisNo,string engineNo, string color, FuelType fuelType);
}
