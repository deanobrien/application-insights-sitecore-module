using Sitecore.Web.UI.WebControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models
{
    public class CustomEventSummary
    {
        public CustomEventSummary(object[] array)
        {
            Name = array[0].ToString();
            Number = Convert.ToInt32(array[1]);
        }
        public string Name { get; set; }
        public int Number { get; set; }
    }
}
