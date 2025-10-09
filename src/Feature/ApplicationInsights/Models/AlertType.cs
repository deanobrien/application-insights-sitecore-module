using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Feature.ApplicationInsights.Models
{
    public enum AlertType
    {
        ExceptionContainsString,
        ExceptionSpike,
        CronTaskInactivity,
        ScheduledTaskInactivity,
        CustomEvent,
        WebpageDown,
        ServiceBusQueueExceeds,
        AIAlert
    }
}
