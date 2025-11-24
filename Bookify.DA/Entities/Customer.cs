using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Bookify.DA.Entities
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        // Changed to string to match IdentityUser.Id and renamed to UserId (conventional)
        public string UserId { get; set; }

        // Use IdentityUser from ASP.NET Identity
        public IdentityUser User { get; set; }

        // Initialize collection to avoid null refs
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
