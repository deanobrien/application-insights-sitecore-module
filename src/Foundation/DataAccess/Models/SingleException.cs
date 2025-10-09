using Sitecore.Web.UI.HtmlControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights.Models
{
    public class SingleException
    {
        public SingleException() {}
        
        public SingleException(object[] array, bool detailed = false)
        {
            Details = RemoveComments(RemoveId(array[16].ToString()));
        }


        public int Id { get; set; }
        public string Details { get; set; }
        public string Path {
            get {
                int startPos = Details.IndexOf("src");
                int endPos = Details.IndexOf(".cshtml");
                int extSize = 7;
                if (endPos == -1)
                {
                    endPos = Details.IndexOf(".cs");
                    extSize = 3;
                }
                if (startPos == -1 || endPos == -1) return string.Empty;

                int length = endPos - startPos;
                string sub = Details.Substring(startPos, length+ extSize);
                return sub;
            }
        }
        public int Line
        {
            get
            {
                string sub = string.Empty;

                int startPos = Details.IndexOf("\"line\":") +7;
                int endPos = Details.IndexOf(",\"fileName\"");

                if (startPos != -1 && endPos != -1)
                {
                    int length = endPos - startPos;
                    sub = Details.Substring(startPos, length);
                    return Convert.ToInt32(sub);
                }
                return 0;
            }
        }
        private static string RemoveId(string input)
        {
            string sub = string.Empty;
            int startPos = input.LastIndexOf("id\":\"")+5;
            int endPos = input.LastIndexOf("\",\"parsed");
            
            if (startPos != -1 && endPos != -1)
            {
                int length = endPos - startPos;
                sub = input.Substring(startPos, length);
            }
            return input.Replace(sub,"xxx");
        }
        private static string RemoveComments(string input)
        {
            string response = string.Empty;
            response = input.Replace("<!--", "");
            response = input.Replace("-->", "");
            return response;
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
