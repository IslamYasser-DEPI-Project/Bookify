using Bookify.Application.DTOs.Requests;
using Bookify.Application.DTOs.Requests;
using Bookify.Application.DTOs.Responses;
using Bookify.Application.Interfaces;
using Bookify.Application.Services.Admin_Services;
using Bookify.DA.Data;
using Bookify.DA.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAdminService _adminService;
        public AdminController(AppDbContext dbContext, UserManager<IdentityUser> userManager , IAdminService adminService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

        }



        //for listing users with (roles and pending admin request status)
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _adminService.GetAllUsersAsync();
            return Ok(users);
        }



        [HttpPost("approve-admin")]
        public async Task<IActionResult> ApproveAdmin([FromBody] ApproveAdminDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { error = "Email is required." });

            var result = await _adminService.ApproveAdminByEmailAsync(dto.Email);
            return Ok(result);
        }

        
        [HttpPost("reject-admin")]
        public async Task<IActionResult> RejectAdmin([FromBody] ApproveAdminDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { error = "Email is required." });

            var result = await _adminService.RejectAdminByEmailAsync(dto.Email);
            return Ok(result);
        }
    }
}
