using Bookify.Application.DTOs.Responses;
using Bookify.Application.Interfaces;
using Bookify.DA.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.Application.Services.Admin_Services
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppDbContext _dbContext;

        public AdminService(UserManager<IdentityUser> userManager, AppDbContext dbContext)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<IEnumerable<UserInfoDTO>> GetAllUsersAsync()
        {
            // fetching users
            var users = await _userManager.Users.ToListAsync();
            var list = new List<UserInfoDTO>();

            foreach (var user in users)
            {
                // Get roles 
                var roles = await _userManager.GetRolesAsync(user) ?? new List<string>();

                // Get claims
                var claims = await _userManager.GetClaimsAsync(user);
                var pending = claims.Any(c => c.Type == "AdminRequest" && c.Value == "Pending");

                // Add to result response list
                list.Add(new UserInfoDTO
                {
                    Id = user.Id ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = roles,
                    IsAdminRequestPending = pending
                });
            }

            return list;
        }

        public async Task<AdminApprovalResponseDTO> ApproveAdminByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new AdminApprovalResponseDTO { Approved = false, Message = "User not found." };

            var pendingClaim = (await _userManager.GetClaimsAsync(user))
                .FirstOrDefault(c => c.Type == "AdminRequest" && c.Value == "Pending");

            if (pendingClaim == null)
                return new AdminApprovalResponseDTO { Approved = false, Message = "This user has no pending admin request." };

            // Remove pending claim
            await _userManager.RemoveClaimAsync(user, pendingClaim);

            // Add Admin role
            await _userManager.AddToRoleAsync(user, "Admin");

            // Update DB table
            var approvalRequest = await _dbContext.AdminApprovalRequests
                .FirstOrDefaultAsync(r => r.UserId == user.Id && !r.IsApproved);

            if (approvalRequest != null)
            {
                approvalRequest.IsApproved = true;
                approvalRequest.ApprovedAt = DateTime.UtcNow;
                _dbContext.AdminApprovalRequests.Update(approvalRequest);
                await _dbContext.SaveChangesAsync();
            }

            return new AdminApprovalResponseDTO
            {
                UserId = user.Id,
                Approved = true,
                Message = "Admin request approved."
            };
        }
        public async Task<AdminApprovalResponseDTO> RejectAdminByEmailAsync(string email)
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new AdminApprovalResponseDTO { Approved = false, Message = "User not found." };

            // Check for pending claim
            var pendingClaim = (await _userManager.GetClaimsAsync(user))
                .FirstOrDefault(c => c.Type == "AdminRequest" && c.Value == "Pending");

            if (pendingClaim == null)
                return new AdminApprovalResponseDTO { Approved = false, Message = "No pending admin request found." };

            // Remove pending claim
            await _userManager.RemoveClaimAsync(user, pendingClaim);

            // Update DB table if exists
            var approvalRequest = await _dbContext.AdminApprovalRequests
                .FirstOrDefaultAsync(r => r.UserId == user.Id && !r.IsApproved);

            if (approvalRequest != null)
            {
                _dbContext.AdminApprovalRequests.Remove(approvalRequest);
                await _dbContext.SaveChangesAsync();
            }

            return new AdminApprovalResponseDTO
            {
                UserId = user.Id,
                Approved = false,
                Message = "Admin request rejected."
            };
        }

    }
}
