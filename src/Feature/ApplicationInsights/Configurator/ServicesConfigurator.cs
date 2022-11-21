//using DeanOBrien.Feature.ApplicationInsights.Extensions;
using DeanOBrien.Feature.ApplicationInsights.Controllers;
using DeanOBrien.Foundation.DataAccess.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;

namespace DeanOBrien.Feature.ApplicationInsights.Configurator
{
    public class ServicesConfigurator : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<ApplicationInsightsController>();
        }
    }
}
