using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VehicleAPI.Models;

namespace VehicleAPI.DTO
{
    public class VehicleDTO
    {
        
        public string RegistrationId { get; set; }
        
        public string Maker { get; set; }
        
        public DateTime DOR { get; set; }
        
        public string ChassisNo { get; set; }
        
        public string EngineNo { get; set; }
       
        public string Color { get; set; }
       
        public FuelType FuelType { get; set; }
    }
}
