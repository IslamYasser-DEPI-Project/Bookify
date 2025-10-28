using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bookify.DA.Enums;

namespace Bookify.DA.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public int BookingID { get; set; }
        public int PaymentTypeID { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;


        public Booking Booking { get; set; }
        public PaymentType PaymentType { get; set; }

    }
}
