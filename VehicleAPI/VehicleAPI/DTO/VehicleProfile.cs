using AutoMapper;
using VehicleAPI.Models;

namespace VehicleAPI.DTO
{
    public class VehicleProfile : Profile
    {
        public VehicleProfile()
        {
            // Entity -> DTO
            CreateMap<Vehicle, VehicleReadDTO>().ConstructUsing(src => new VehicleReadDTO(
                src.RegistrationId,
                src.Maker,
                src.DOR,
                src.ChassisNo,
                src.EngineNo,
                src.Color,
                src.FuelType
            ));

            // DTO -> Entity
            CreateMap<VehicleDTO, Vehicle>();
        }
    }
}
