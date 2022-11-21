using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DeanOBrien.Feature.ApplicationInsights.Models
{
    public class LinkedApplicationEdge
    {
        public string source { get; set; }
        public string target { get; set; }
        public string label { get; set; }
        public List<LinkedApplicationEdgeData> data { get; set; }
    }
}