using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.Application.DTOs.Responses
{
    public class AdminApprovalResponseDTO
    {
        public string UserId { get; set; } = string.Empty;
        public bool Approved { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
