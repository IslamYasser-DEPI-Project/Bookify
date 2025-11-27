using Bookify.Application.DTOs.Requests;
using Bookify.Application.Interfaces;
using Bookify.DA.Data;
using Bookify.DA.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.Application.Services.Registeration_Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;

        public AccountService(UserManager<IdentityUser> userManager, IConfiguration configuration, AppDbContext dbContext)
        {
            _userManager = userManager;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        public async Task<(bool Success, IEnumerable<string> Errors, string Message)> RegisterAsync(RegisterDto registerDto)
        {
            if (registerDto.Password != registerDto.ConfirmPassword)
                return (false, new[] { "Passwords do not match." }, string.Empty);

            // Determine requested role (default to Customer)
            var requestedRole = string.IsNullOrWhiteSpace(registerDto.Role)
                ? "Customer"
                : registerDto.Role.Trim();

            var allowedRoles = new[] { "Admin", "Customer" };

            if (!allowedRoles.Any(r => string.Equals(r, requestedRole, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, new[] { "Invalid role specified. Allowed values: Admin, Customer." }, string.Empty);
            }

            
            var user = new IdentityUser { UserName = registerDto.Email, Email = registerDto.Email };
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
                return (false, result.Errors.Select(e => e.Description), string.Empty);

            // If customer -> assign immediately and create Customer profile row
            if (string.Equals(requestedRole, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    return (false, roleResult.Errors.Select(e => e.Description), string.Empty);
                }

                //customer record linked to identity
                var customer = new Customer
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    Name = user.UserName ?? user.Email ?? string.Empty,
                    Phone = string.Empty
                };

                _dbContext.Customers.Add(customer);
                await _dbContext.SaveChangesAsync();

                return (true, Enumerable.Empty<string>(), "Registration successful");
            }

            // If Admin requested -> do NOT assign Admin role yet.
            // Mark user with a pending claim so admins can approve later.
            var pendingClaim = new Claim("AdminRequest", "Pending");
            var addClaimResult = await _userManager.AddClaimAsync(user, pendingClaim);

            if (!addClaimResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return (false, addClaimResult.Errors.Select(e => e.Description), string.Empty);
            }

            
            var approvalRequest = new AdminApprovalRequest
            {
                UserId = user.Id,
                Email = user.Email!,
                RequestedAt = DateTime.UtcNow,
                IsApproved = false
            };

            _dbContext.AdminApprovalRequests.Add(approvalRequest);
            await _dbContext.SaveChangesAsync();

            return (true, Enumerable.Empty<string>(), "Waiting for approval");

        }

        public async Task<(bool Success, string Token, string Error)> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
                return (false, null, "Invalid login.");

            var passwordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!passwordValid)
                return (false, null, "Invalid login.");

            // build claims
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Include roles if any
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expireMinutes = 60;
            if (int.TryParse(_configuration["Jwt:ExpireMinutes"], out var parsed))
                expireMinutes = parsed;

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return (true, tokenString, null);
        }
    }
}
