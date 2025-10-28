using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bookify.DA.Enums;

namespace Bookify.DA.Entities
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public UserRole Role { get; set; } 
        public bool IsActive { get; set; }
        public Customer Customer { get; set; }

    }
}
