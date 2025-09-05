using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using VehicleAPI.DTO;
using VehicleAPI.Models;
using VehicleAPI.Repositories;
using static System.Runtime.InteropServices.JavaScript.JSType;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace VehicleAPI.Controllers
{
    [ApiVersion("1.0")]  
    [Route("api/v{version:apiVersion}/[controller]")]
    [EnableCors]
    [ApiController]
    public class VehiclesController : Controller
    {
        private IVehicleRepo _vehicleRepo;

        public VehiclesController(IVehicleRepo vehicleRepo)
        {
            _vehicleRepo = vehicleRepo;
        }



        // GET: api/<VehiclesController>
        [HttpGet]
        public async Task<IEnumerable<VehicleReadDTO>> Get()
        {
            var vehicle = await _vehicleRepo.GetVehicles();
            var result = vehicle.Select(v => new VehicleReadDTO(
            v.RegistrationId,v.Maker,v.Color,v.ChassisNo,v.EngineNo,v.DOR,v.FuelType
       ));
            return result;
        }

        // GET api/<VehiclesController>/5
        [HttpGet("{regNo}")]
        public async Task<ActionResult> Get(string regNo)
        {
            var vehicle= await _vehicleRepo.GetVehicle(regNo);
            return Ok(new VehicleReadDTO(
              vehicle.RegistrationId, vehicle.Maker, vehicle.Color, vehicle.ChassisNo, vehicle.EngineNo, vehicle.DOR, vehicle.FuelType

         ));
        }

        // POST api/<VehiclesController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] VehicleDTO vehicleDTO)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var entity = new Vehicle
            {
                RegistrationId = vehicleDTO.RegistrationId,
                Maker = vehicleDTO.Maker,
                DOR = vehicleDTO.DOR,
                ChassisNo = vehicleDTO.ChassisNo,
                EngineNo = vehicleDTO.EngineNo,
                Color = vehicleDTO.Color,
                FuelType = vehicleDTO.FuelType
            };
            var vehicle=await _vehicleRepo.AddVehicle(entity);
            return Ok(new VehicleReadDTO(
             vehicle.RegistrationId, vehicle.Maker, vehicle.Color, vehicle.ChassisNo, vehicle.EngineNo, vehicle.DOR, vehicle.FuelType

        ));


        }

        // PUT api/<VehiclesController>/5
        [HttpPut("{regNo}/{color}")]
        public async Task<IActionResult> Put(string regNo,string color)
        {
            var vehicle = await _vehicleRepo.UpdateVehicle(regNo, color);
            return Ok(new VehicleReadDTO(
                         vehicle.RegistrationId, vehicle.Maker, vehicle.Color, vehicle.ChassisNo, vehicle.EngineNo, vehicle.DOR, vehicle.FuelType));

        }

        // DELETE api/<VehiclesController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
           var result= await _vehicleRepo.RemoveVehicle(id);
            if (result)
                return Ok("Vehicle Removed");
            else
                return NotFound();

        }
    }
}
