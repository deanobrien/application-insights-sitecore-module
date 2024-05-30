using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models
{
    public class GroupedException
    {
        public GroupedException() {}
        
        public GroupedException(object[] array, bool detailed = false)
        {
            ProblemId = array[0].ToString();
            ProblemIdBase64 = Base64Encode(array[0].ToString());
            OuterType = array[1].ToString();
            Type = array[2].ToString();
            InnerMostType = array[3].ToString();
            OuterAssembly = array[4].ToString();
            Assembly = array[5].ToString();
            OuterMethod = array[6].ToString();
            Method = array[7].ToString();
            if (detailed)
            {
                OuterMessage = array[8].ToString();
                InnerMostMessage = array[9].ToString();
                InnerMostMessageBase64 = Base64Encode(array[9].ToString());
                Count = Convert.ToInt32(array[10]);
            }
            else 
            {
                Count = Convert.ToInt32(array[8]);
            }
        }


        public int Id { get; set; }
        public string ApplicationId { get; set; }
        public AppInsightType AppInsightType { get; set; }
        public string ProblemId { get; set; }
        public string ProblemIdBase64 { get; set; }
        public string OuterMessage { get; set; }
        public string InnerMostMessage { get; set; }
        public string InnerMostMessageBase64 { get; set; }
        public string OuterType { get; set; }
        public string Type { get; set; }
        public string InnerMostType { get; set; }
        public string OuterAssembly { get; set; }
        public string Assembly { get; set; }
        public string OuterMethod { get; set; }
        public string Method { get; set; }
        public int Count { get; set; }
        public DateTime DateCreated { get; set; }
        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
