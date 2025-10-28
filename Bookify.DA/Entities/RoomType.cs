using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.DA.Entities
{
    public class RoomType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerNight { get; set; }
        public int Capacity { get; set; }

        public ICollection<Room> Rooms { get; set; }
    }
}
