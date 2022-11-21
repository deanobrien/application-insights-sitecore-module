using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DeanOBrien.Feature.ApplicationInsights.Models
{
    public class LinkedApplicationJson
    {
        public List<LinkedApplicationNode> nodes { get; set; }
        public List<LinkedApplicationEdge> edges { get; set; }
        public List<string> ports { get; set; }
        public List<string> groups { get; set; }
    }
}