using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models
{
    public class TriggeredAlert
    {
        public int Id { get; set; }
        public string ApplicationId { get; set; }
        public string Title { get; set; }
        public DateTime DateTriggered { get; set; }
        public string AlertId { get; set; }
        public int DayTriggered { get; set; }
        public string MonthTriggered { get; set; }
    }
}
