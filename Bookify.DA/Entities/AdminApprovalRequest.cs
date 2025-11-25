using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.DA.Entities { 
 public class AdminApprovalRequest
        {
            public int Id { get; set; }
            public string UserId { get; set; } = default!;
            public string Email { get; set; } = default!;
            public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
            public bool IsApproved { get; set; } = false;
            public string? ApprovedByUserId { get; set; }
            public DateTime? ApprovedAt { get; set; }
        }
    }

