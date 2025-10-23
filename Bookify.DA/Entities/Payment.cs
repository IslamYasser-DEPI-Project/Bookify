using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.DA.Entities
{
    public class Payment
    {
        public int PaymentID { get; set; }
        public int BookingID { get; set; }
        public int PaymentTypeID { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string PaymentStatus { get; set; }


        public Booking Booking { get; set; }
        public PaymentType PaymentType { get; set; }

    }
}
