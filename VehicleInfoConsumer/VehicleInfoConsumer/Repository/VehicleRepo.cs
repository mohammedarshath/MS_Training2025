using MongoDB.Driver;
using VehicleInfoConsumer.DTOs;

namespace VehicleInfoConsumer.Repository
{
    public class VehicleRepo : IVehicleRepo
    {
        private readonly IConfiguration _configuration;
        private IMongoCollection<VehicleBson> _MongoCollection;
        public VehicleRepo(IConfiguration configuration)
        {

            _configuration = configuration;

            var mongoClient = new MongoClient(_configuration["ConnectionString"]);

            var database = mongoClient.GetDatabase(_configuration["DatabaseName"]);

            _MongoCollection = database.GetCollection<VehicleBson>(
             _configuration["VehiclesCollectionName"]);


        }

        public void AddVechicle(VehicleBson vehicleBson)
        {
            _MongoCollection.InsertOne(vehicleBson);
        }
    }
}
