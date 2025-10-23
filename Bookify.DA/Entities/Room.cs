using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.DA.Entities
{
    public class Room
    {
        public int RoomID { get; set; }
        public int HotelID { get; set; }
        public int RoomTypeID { get; set; }
        public string RoomNumber { get; set; }
        public string Status { get; set; }


        public Hotel Hotel { get; set; }
        public RoomType RoomType { get; set; }
        public ICollection<Booking> Bookings { get; set; }
    }
}
