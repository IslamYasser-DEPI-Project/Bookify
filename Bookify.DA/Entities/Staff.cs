using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.DA.Entities
{
    public class Staff
    {
        public int StaffID { get; set; }
        public int HotelID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public int UserID { get; set; }


        public Hotel Hotel { get; set; }
        public User User { get; set; }

    }
}
