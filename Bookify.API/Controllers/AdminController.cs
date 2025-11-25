using Bookify.Application.DTOs;
using Bookify.DA.Data;
using Bookify.DA.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(AppDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [HttpPost("approve-admin")]
        public async Task<IActionResult> ApproveAdmin([FromBody] ApproveAdminDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { error = "Email is required." });

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound(new { error = "User not found." });

            var claims = await _userManager.GetClaimsAsync(user);
            var pending = claims.FirstOrDefault(c => c.Type == "AdminRequest" && c.Value == "Pending");
            if (pending == null)
                return NotFound(new { error = "No pending admin request found for that email." });

            // assign Admin role
            var addRoleResult = await _userManager.AddToRoleAsync(user, "Admin");
            if (!addRoleResult.Succeeded)
                return BadRequest(new { errors = addRoleResult.Errors.Select(e => e.Description) });

            // remove pending claim
            var removeClaimResult = await _userManager.RemoveClaimAsync(user, pending);
            if (!removeClaimResult.Succeeded)
            {
                // role assigned but claim removal failed; report but keep role
                return Ok(new { message = "User assigned Admin role but pending flag removal failed." });
            }

            return Ok(new { message = "User approved and assigned Admin role." });
        }

        // New endpoint: list users with their roles and pending admin request flag
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = _userManager.Users.ToList();
            var list = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var claims = await _userManager.GetClaimsAsync(user);
                var pending = claims.Any(c => c.Type == "AdminRequest" && c.Value == "Pending");

                list.Add(new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    Roles = roles, // string[]
                    IsAdminRequestPending = pending
                });
            }

            return Ok(list);
        }
    }
}
