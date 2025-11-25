using Bookify.Application.DTOs;
using Bookify.Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.Application.Interfaces
{
    public interface IAdminService
    {
        Task<IEnumerable<UserInfoDTO>> GetAllUsersAsync();
        Task<AdminApprovalResponseDTO> ApproveAdminByEmailAsync(string email);
        Task<AdminApprovalResponseDTO> RejectAdminByEmailAsync(string email);
    }
}
