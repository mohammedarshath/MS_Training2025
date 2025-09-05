using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TradingApp.DTOs;
using TradingApp.Repositories;

namespace TradingApp.Controllers
{
    [ApiVersion("1.0")]
    [ApiVersion("1.1")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [EnableCors]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;

        public RolesController(IRoleRepository roleRepository) {
            _roleRepository = roleRepository;
        }

        [HttpGet]

        public async Task<IEnumerable<RoleReadDTO>> GetAllRoles()
        {
            var data = await _roleRepository.GetAllRoles();

            var result = data.Select(r => new RoleReadDTO(
           r.RoleId, r.RoleName, r.Users.Select(u => u.Username)
       ));
            return result;
        }


        [HttpPost]
        public  async  Task<IActionResult> AddRole([FromBody] RoleCreateDto roleCreateDTO)
        {
            if (roleCreateDTO == null || string.IsNullOrWhiteSpace(roleCreateDTO.Name))
            {
                return BadRequest("Invalid role data.");
            }
            var newRole = new TradingApp.Models.Role
            {
                RoleName = roleCreateDTO.Name
            };
            var createdRole = await _roleRepository.AddRoleAsync(newRole);
            var roleReadDTO = new RoleReadDTO(
                createdRole.RoleId,
                createdRole.RoleName,
                createdRole.Users.Select(u => u.Username)
            );
            return CreatedAtAction(nameof(GetAllRoles), new { id = createdRole.RoleId }, roleReadDTO);
        }

        [HttpPut("{id:long}/{name}")]
        public async Task<IActionResult> UpdateRole(long id, string name)
        {
            if (name == null || string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Invalid role data.");
            }
            var updatedRole = await _roleRepository.UpdateRoleAsync(id, name);
            if (updatedRole == null)
            {
                return NotFound();
            }
            var roleReadDTO = new RoleReadDTO(
                updatedRole.RoleId,
                updatedRole.RoleName,
                updatedRole.Users.Select(u => u.Username)
            );
            return Ok(roleReadDTO);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteRole(long id)
        {
            var success = await _roleRepository.DeleteRoleAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }

    }
}
