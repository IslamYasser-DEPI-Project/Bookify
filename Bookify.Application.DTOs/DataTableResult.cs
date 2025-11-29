using System.Collections.Generic;

namespace Bookify.Application.DTOs
{
    public class DataTableResult<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public IEnumerable<T> Data { get; set; } = new List<T>();
    }
}