using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingApp.Contexts;
using TradingApp.DTOs;
using TradingApp.Models;
using TradingApp.Repositories;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TradingApp.Controllers
{
    [ApiVersion("1.0")]
    [ApiVersion("1.1")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [EnableCors]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        // GET: Users
        public async Task<IEnumerable<UserReadDto>> GetUsers()
        {
            var data = await _userRepository.GetAllUsers();

            var result = data.Select(u => new UserReadDto(
           u.UserId, u.Username, u.Email,
           u.Roles
       ));
            return result;
        }

        [HttpGet("{id:long}")]

        // GET: Users/Details/5
        public async Task<IActionResult> GetUserById(long id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var data = await _userRepository.GetUserByIdAsync(id);

            if (data == null)
            {
                return NotFound();
            }

            return Ok(new UserReadDto(
             data.UserId, data.Username, data.Email, data.Roles

         ));
        }

        [HttpPost]

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.


        public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var entity = new User { Username = dto.UserName, Email = dto.Email };
            var result = await _userRepository.AddUserAsync(entity);
            return CreatedAtAction("GetUsers", new { id = result.UserId }, entity);

        }

        [HttpPut("{id}/{email}")]
        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(long id, string email)
        {


            var data = await _userRepository.UpdateUserAsync(id, email);
            if (data == null)
            {
                return NotFound();
            }
            return Ok(new UserReadDto(
            data.UserId, data.Username, data.Email, data.Roles

        ));
        }

        [HttpDelete("{id:long}")]

        public async Task<ActionResult> Delete(long id)
        {
            var data = await _userRepository.DeleteUserAsync(id);
            if (data)
            {
                return Ok("User Deleted");
            }
            else
            {
                return NotFound();

            }
        }
    }
}
