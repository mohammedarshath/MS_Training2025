using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CircuitBreaker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UsersController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("jsonApi");
            var response = await client.GetAsync("users", cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return Content(json, "application/json");
        }
    }
}
