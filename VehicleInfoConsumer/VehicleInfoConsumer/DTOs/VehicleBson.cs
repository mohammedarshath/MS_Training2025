

using MongoDB.Bson.Serialization.Attributes;

namespace VehicleInfoConsumer.DTOs
{
  
    public class VehicleBson
    {
        [BsonId]
        public string RegNo { get; set; }
        public string Model { get; set; }
        public DateTime DOR { get; set; }
        public string ChassisNo { get; set; }
        public string EngineNo { get; set; }
        public string Color { get; set; }
        public FuelType FuelType {  get; set; }
        public string Key {  get; set; }
        public string PartitionOffset { get; set; }


    }

}
