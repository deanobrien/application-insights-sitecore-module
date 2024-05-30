using DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Feature.ApplicationInsights.Models
{
    public class Alert
    {
        public Item Item { get; set; }
        public string Title { get; set; }
        public AlertType AlertType { get; set; }
        public string Url { get; set; }
        public string ExceptionString { get; set; }
        public string QueueName { get; set; }
        public string PipelineName { get; set; }
        public int? PipelineInactiveLimit { get; set; }
        public int? ExceptionSpikeLimit { get; set; }
        public string ApplicationId { get; set; }
        public int? DelayBetweenAlerts { get; set; }
        public bool isActive { get; set; }
        public bool isEmail { get; set; }
        public bool isSms { get; set; }
        public DateTime LastRun { get; set; }
        public DateTime NextRun { get; set; }
        public int? Threshold { get; set; }
        public int? PercentageThreshold { get; set; }
        public int? InactivityThresholdInMins { get; set; }
        public Item LinkedItem { get; set; }
        public List<Subscriber> Subscribers { get; set; }
        public int HoursSinceCustomEvent { get; set; }
        public bool EventDesired { get; set; }
        public string CustomEvent { get; internal set; }
        public string ServiceBusQueue { get; internal set; }
        public string ServiceBusConnection { get; internal set; }
        public int? ServiceBusQueueLimit { get; set; }
        public bool Enabled { get; internal set; }
    }
}
