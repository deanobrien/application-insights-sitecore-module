using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;

namespace DeanOBrien.Feature.ApplicationInsights.Models
{
    public class SummaryOfExceptions
    {
        public SummaryOfExceptions(string applicationId, string timespan = "7d", List<GroupedException> dailyLogs = null, List<GroupedException> hourlyLogs = null, bool sumHourly = false)
        {
            this.DailyLogs = dailyLogs;
            if (dailyLogs != null && dailyLogs.Count > 0)
            {
                this.DailySummary = dailyLogs
                    .GroupBy(x => x.DateCreated.ToString("dd-MM-yyyy"))
                    .Select(y => new DateCount { Date = y.Key, Count = y.Sum(z => z.Count) }).ToList();
            }
            else 
            {
                this.DailyLogs = new List<GroupedException>();
            }
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

            this.HoursToReview = hours;
            DateTime dateFrom = DateTime.Now.AddHours((double)-hours);
            this.DailyLogs = (ICollection<GroupedException>)this.DailyLogs.Reverse<GroupedException>().ToList<GroupedException>();
            this.HourlyLogs = (ICollection<GroupedException>)this.HourlyLogs.Reverse<GroupedException>().ToList<GroupedException>();

            var sumHourlyExceptions = new Dictionary<string, int>();
            if (sumHourly)
            {
                foreach (var log in this.HourlyLogs)
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
        }
        public Dictionary<string, int> SumHourlyExceptions { get; set; } 
        public ICollection<GroupedException> DailyLogs { get; set; }
        public ICollection<DateCount> DailySummary { get; set; }

        public ICollection<GroupedException> HourlyLogs { get; set; }
        public ICollection<DateCount> HourlySummary { get; set; }

        public int HoursToReview { get; set; }

        public IEnumerable<string> DailyLabels
        {
            get
            {
                List<string> stringList = new List<string>();
                DateTime dateTime1 = DateTime.Now;
                dateTime1 = dateTime1.AddHours((double)-(this.HoursToReview));
                for (DateTime dateTime2 = dateTime1.Date; dateTime2 <= DateTime.Now.Date; dateTime2 = dateTime2.AddDays(1.0))
                    stringList.Add(string.Format("{0} {1}", (object)dateTime2.DayOfWeek.ToString(), (object)dateTime2.Day.ToString()));
                return (IEnumerable<string>)stringList;
            }
        }
        public IEnumerable<string> Labels
        {
            get
            {
                List<string> stringList = new List<string>();
                for (DateTime dateTime = DateTime.Now.AddHours((double)-(this.HoursToReview)); dateTime < DateTime.Now.AddHours(1.0); dateTime = dateTime.AddHours(1.0))
                    stringList.Add(dateTime.Hour.ToString());
                return (IEnumerable<string>)stringList;
            }
        }
        public IEnumerable<string> DailyValues
        {
            get
            {
                DateCount[] array = null;
                List<string> stringList = new List<string>();
                bool hasValues = (this.DailySummary != null && this.DailySummary.Count > 0);
                if (hasValues) array = this.DailySummary.ToArray<DateCount>();
                
                DateTime dateTimeFrom = DateTime.Now.AddHours((double)-(this.HoursToReview)).Date;
                int index = 0;
                for (; dateTimeFrom < DateTime.Now; dateTimeFrom = dateTimeFrom.AddDays(1.0))
                {
                    bool found = false;
                    if (hasValues)
                    {
                        for (int x = 0; x < array.Count(); x++)
                        {
                            Log.Info($"Attempting to parse ({array[x].Date})", this);
                            if (DateTime.Parse(array[x].Date, CultureInfo.CreateSpecificCulture("en-GB")) == dateTimeFrom)
                            {
                                stringList.Add(array[x].Count.ToString());
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found) stringList.Add("0");
                    ++index;
                }
                return (IEnumerable<string>)stringList;
            }
        }


        public IEnumerable<string> Values
        {
            get
            {
                DateCount[] array = null;
                List<string> stringList = new List<string>();

                bool hasValues = (this.HourlySummary != null && this.HourlySummary.Count > 0);
                if (hasValues) array = this.HourlySummary.ToArray();

                DateTime dateTimeFrom = DateTime.Now.AddHours((double)-(this.HoursToReview));
                int index = 0;
                for (; dateTimeFrom < DateTime.Now.AddHours(1.0); dateTimeFrom = dateTimeFrom.AddHours(1.0))
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
    }
    public class DateCount
    {
        public DateCount() { }
        public DateCount(int count, string date)
        {
            Count = count;
            Date = date;
        }


        public int Count { get; set; }

        public string Date { get; set; }
    }
}