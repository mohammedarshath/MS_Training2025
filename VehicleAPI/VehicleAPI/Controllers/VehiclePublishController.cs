using AutoMapper;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using VehicleAPI.DTO;
using VehicleAPI.Producers;
using VehicleAPI.Repositories;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace VehicleAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [EnableCors]
    [ApiController]
    public class VehiclePublishController : ControllerBase
    {
        private IVehicleRepo _vehicleRepo;
        private IConfiguration _configuration;
        private IVehicleInfoProducer _vehicleInfoProducer;
        private readonly IMapper _mapper;
        public VehiclePublishController(IVehicleRepo vehicleRepo, IConfiguration configuration,IMapper mapper,IVehicleInfoProducer vehicleInfoProducer)
        {
            _vehicleRepo = vehicleRepo;
            _configuration = configuration;
            _mapper = mapper;
            _vehicleInfoProducer = vehicleInfoProducer;
        }

        // GET api/<VehiclePublishController>/5
        [HttpGet("{regNo}")]
        public async Task<ActionResult> Get(string regNo)
        {
            var vehicle = await _vehicleRepo.GetVehicle(regNo);
            VehicleReadDTO vehicleInfo = _mapper.Map<VehicleReadDTO>(vehicle);
            var topicName = _configuration["kafka:Topic"];
            var key = vehicleInfo.RegNo;
            var result = await _vehicleInfoProducer.ProduceAsync(topicName, key, vehicleInfo);

            return Ok(result);
        }


    }
}
