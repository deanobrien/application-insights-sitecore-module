using DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Feature.ApplicationInsights.Models
{
    public class SummaryForAIOverview
    {
        private string id;
        public List<GroupedExceptionOverTime> HourlyLogs;

        public SummaryForAIOverview(string id, string timespan, List<GroupedException> hourlyLogs)
        {
            this.id = id;

            int hours = 1;
            if (timespan.Contains("h"))
            {
                int.TryParse(timespan.Replace("h", ""), out hours);
            }
            else if (timespan.Contains("d"))
            {
                int days = 1;
                int.TryParse(timespan.Replace("d", ""), out days);
                hours = days * 24;
            }
            this.TimeSpan = $"{hours}h";

            var sumHourlyExceptions = new Dictionary<string, int>();
            if (hourlyLogs != null && hourlyLogs.Count > 0)
            {
                foreach (var log in hourlyLogs)
                {
                    if (!sumHourlyExceptions.ContainsKey(log.ProblemId))
                    {
                        sumHourlyExceptions.Add(log.ProblemId, log.Count);
                        continue;
                    }
                    sumHourlyExceptions.TryGetValue(log.ProblemId, out var currentCount);
                    sumHourlyExceptions[log.ProblemId] = currentCount + log.Count;
                }
            }
            sumHourlyExceptions.OrderByDescending(x => x.Value);
            SumHourlyExceptions = sumHourlyExceptions;

            this.ExceptionsInTimeSpan = new List<GroupedExceptionOverTime>();
            foreach (KeyValuePair<string, int> entry in SumHourlyExceptions)
            {
                var first = hourlyLogs.Where(x => x.ProblemId == entry.Key).FirstOrDefault();
                var groupedExceptionOverTime = new GroupedExceptionOverTime(first, hourlyLogs.Where(x => x.ProblemId == entry.Key).ToList(), hours, entry.Value);
                this.ExceptionsInTimeSpan.Add(groupedExceptionOverTime);
            }
        }

        public Application Application { get; internal set; }
        public string TimeSpan { get; internal set; }
        private Dictionary<string, int> SumHourlyExceptions { get; set; }
        public string ErrorMessage { get; internal set; }
        public List<GroupedExceptionOverTime> ExceptionsInTimeSpan { get; set; }
    }
}
