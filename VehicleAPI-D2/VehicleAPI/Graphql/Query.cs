using Microsoft.EntityFrameworkCore;
using VehicleAPI.Contexts;
using VehicleAPI.Models;

namespace VehicleAPI.Graphql
{
    public class Query
    {
        [UsePaging]
        [UseProjection]
        [UseFiltering]
        [UseSorting]

        public IQueryable<Vehicle> GetVehicles([Service] VehicleContext context) =>
            context.Vehicles.AsNoTracking();
        [UseProjection]
        public Task<Vehicle> GetVehiclesById([Service] VehicleContext context, string id) =>
            context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v =>v.RegistrationId == id);

    }
}
