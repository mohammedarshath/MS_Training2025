using VehicleAPI.Models;

namespace VehicleAPI.DTO
{
   public record VehicleReadDTO(string RegNo,string model,string color, string chassisNo,string engineNo, DateTime dor, FuelType fuelType);
}
