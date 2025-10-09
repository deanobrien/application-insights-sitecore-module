using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights.ApplicationInsights
{
    public interface IGenAIService
    {
        string Call(List<Tuple<string, string>> prompts, string userPrompt, string context);
    }
}
