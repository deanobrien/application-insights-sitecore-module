using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.ApplicationInsights.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new TestHarness();
            Task.Run(() => app.Run()).Wait();
        }
    }
}
