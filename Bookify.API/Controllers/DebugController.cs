using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DebugController : ControllerBase
    {
        // Call with same Authorization: Bearer <token> header you use for Admin endpoints
        [HttpGet("whoami")]
        
        public IActionResult WhoAmI()
        {
            var claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();

            return Ok(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                Name = User.Identity?.Name,
                Claims = claims,
                IsInRoleAdmin = User.IsInRole("Admin")
            });
        }
    }
}