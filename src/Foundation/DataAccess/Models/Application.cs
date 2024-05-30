using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models
{
    public class Application
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime NextHourly { get; set; }
        public DateTime NextDaily { get; set; }
        public string ApplicationInsightsId { get; set; }
        public string ApplicationInsightsKey { get; set; }
    }
}
