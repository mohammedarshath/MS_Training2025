using VehicleInfoConsumer.DTOs;

namespace VehicleInfoConsumer.Repository
{
    public interface IVehicleRepo
    {
        void AddVechicle(VehicleBson vehicleBson);
    }
}
