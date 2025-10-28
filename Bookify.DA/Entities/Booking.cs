using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bookify.DA.Enums;

namespace Bookify.DA.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public int CustomerID { get; set; }
        public int RoomID { get; set; }
        public DateTime BookingDate { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;



        public Customer Customer { get; set; }
        public Room Room { get; set; }
        public Payment Payment { get; set; }

    }
}
