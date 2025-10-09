using DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Feature.ApplicationInsights.Models
{
    public class GroupedExceptionOverTime : GroupedException
    {
        private GroupedException first;
        private List<GroupedException> groupedExceptions;

        public GroupedExceptionOverTime(GroupedException groupedException, List<GroupedException> hourlyLogs, int hours = 2, int count = 0)
        {
            this.ApplicationId = groupedException.ApplicationId;
            this.ProblemId = groupedException.ProblemId;
            this.ProblemIdBase64 = groupedException.ProblemIdBase64;
            this.Method = groupedException.Method;
            this.OuterMethod = groupedException.OuterMethod;
            this.OuterMessage = groupedException.OuterMessage;
            this.InnerMostMessage = groupedException.InnerMostMessage;
            this.InnerMostMessageBase64 = groupedException.InnerMostMessageBase64;
            this.Id = groupedException.Id;
            this.AppInsightType = groupedException.AppInsightType;
            this.Assembly = groupedException.Assembly;
            this.Count = count;

            this.HourlyLogs = hourlyLogs;
            if (hourlyLogs != null && hourlyLogs.Count > 0)
            {
                this.HourlySummary = hourlyLogs
                    .GroupBy(x => x.DateCreated.ToString("dd-MM-yyyy HH"))
                    .Select(y => new DateCount { Date = y.Key, Count = y.Sum(z => z.Count) }).ToList();
            }
            else
            {
                this.HourlyLogs = new List<GroupedException>();
            }

            this.HoursToReview = hours + 1;
        }

        private ICollection<GroupedException> HourlyLogs { get; set; }
        private ICollection<DateCount> HourlySummary { get; set; }
        private int HoursToReview { get; set; }
        private IEnumerable<string> Labels
        {
            get
            {
                List<string> stringList = new List<string>();
                for (DateTime dateTime = DateTime.Now.AddHours((double)-(this.HoursToReview - 1)); dateTime < DateTime.Now; dateTime = dateTime.AddHours(1.0))
                    stringList.Add(dateTime.Hour.ToString());
                return (IEnumerable<string>)stringList;
            }
        }

        private IEnumerable<string> Values
        {
            get
            {
                DateCount[] array = null;
                List<string> stringList = new List<string>();

                bool hasValues = (this.HourlySummary != null && this.HourlySummary.Count > 0);
                if (hasValues) array = this.HourlySummary.ToArray();

                DateTime dateTimeFrom = DateTime.Now.AddHours((double)-(this.HoursToReview - 1));
                int index = 0;
                for (; dateTimeFrom < DateTime.Now; dateTimeFrom = dateTimeFrom.AddHours(1.0))
                {
                    if (hasValues && index <= ((IEnumerable<DateCount>)array).Count() - 1 && array[index].Date == dateTimeFrom.ToString("dd-MM-yyyy HH"))
                    {
                        stringList.Add(array[index].Count.ToString());
                        ++index;
                    }
                    else
                        stringList.Add("0");
                }
                return (IEnumerable<string>)stringList;
            }
        }
        public ICollection<DateCount> ExceptionsCountOverTime
        {
            get
            {
                DateCount[] array = null;
                var stringList = new List<DateCount>();

                bool hasValues = (this.HourlySummary != null && this.HourlySummary.Count > 0);
                if (hasValues) array = this.HourlySummary.ToArray();

                DateTime dateTimeFrom = DateTime.Now.AddHours((double)-(this.HoursToReview - 1));
                int index = 0;
                for (; dateTimeFrom < DateTime.Now.AddHours(1.0); dateTimeFrom = dateTimeFrom.AddHours(1.0))
                {
                    if (hasValues && index <= ((IEnumerable<DateCount>)array).Count() - 1 && array[index].Date == dateTimeFrom.ToString("dd-MM-yyyy HH"))
                    {
                        stringList.Add(new DateCount(array[index].Count, dateTimeFrom.ToString("dd-MM-yyyy HH")));
                        ++index;
                    }
                    else
                        stringList.Add(new DateCount(0,dateTimeFrom.ToString("dd-MM-yyyy HH")));
                }
                return stringList;
            }
        }
    }
}
