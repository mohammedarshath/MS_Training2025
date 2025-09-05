using AutoMapper;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IMapper _mapper;

        public VehiclesController(IVehicleRepo vehicleRepo,IMapper mapper)
        {
            _vehicleRepo = vehicleRepo;
            _mapper = mapper;
        }



        // GET: api/<VehiclesController>
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleReadDTO>>> Get()
        {
            var vehicles = await _vehicleRepo.GetVehicles();
            return Ok(_mapper.Map<IEnumerable<VehicleReadDTO>>(vehicles));
        }

        // GET api/<VehiclesController>/5
        [HttpGet("{regNo}")]
        public async Task<ActionResult> Get(string regNo)
        {
            var vehicle= await _vehicleRepo.GetVehicle(regNo);
            return Ok(_mapper.Map<VehicleReadDTO>(vehicle));
        }

        // POST api/<VehiclesController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] VehicleDTO vehicleDTO)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var entity = _mapper.Map<Vehicle>(vehicleDTO);
            var vehicle=await _vehicleRepo.AddVehicle(entity);
            return Ok(_mapper.Map<VehicleReadDTO>(vehicle));


        }

        // PUT api/<VehiclesController>/5
        [HttpPut("{regNo}/{color}")]
        public async Task<IActionResult> Put(string regNo,string color)
        {
            var vehicle = await _vehicleRepo.UpdateVehicle(regNo, color);
            return Ok(_mapper.Map<VehicleReadDTO>(vehicle));
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
