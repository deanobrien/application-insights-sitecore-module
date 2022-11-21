using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Foundation.DataAccess.Models
{
    public class Table
    {
        public string name { get; set; }
        public Column[] columns { get; set; }
        public object[] rows { get; set; }
    }
}
