using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleAPI.Models
{
    public enum FuelType
    {
        PETROL,DIESEL,EV,GAS
    }

    [Table("Vehicle")]
    public class Vehicle
    {
        [Key]
        [Column("RegistationNo")]
        public string RegistrationId { get; set; }
        [Required]
        [Column("Maker")]
        [RegularExpression("^[a-zA-Z]{5,25}$",
           ErrorMessage = "Maker Should be in alphabets within the range of 5,25")]
        public string Maker {  get; set; }
        [Required]
        [Column("DOR")]
        [DataType(DataType.Date)]
        public DateTime DOR {  get; set; }
        [Required]
        [RegularExpression("^[a-zA-Z0-9]{5,10}$",
           ErrorMessage = "ChassisNo Should be in alphabets within the range of 3,25")]
        [Column("ChassisNo")]
        public string ChassisNo {  get; set; }
        [Required]
        [RegularExpression("^[a-zA-Z0-9]{5,10}$",
           ErrorMessage = "EngineNo Should be in alphabets within the range of 3,25")]
        [Column("EngineNo")]
        public string EngineNo {  get; set; }
        [Required]
        [Column("Color")]
        [RegularExpression("^[a-zA-Z]{3,10}$",
           ErrorMessage = "ChassisNo Should be in alphabets within the range of 3,25")]
        public string Color { get; set; }
        [Column("FuelType")]
        
        public FuelType FuelType { get; set; }

    }
}
