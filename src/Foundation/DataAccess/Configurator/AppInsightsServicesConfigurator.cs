using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights.Configurator
{
    public class AppInsightsServicesConfigurator : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IAppInsightsApi, AppInsightsApi>();
            serviceCollection.AddScoped<ILogStore, SqlLogStore>();
        }
    }
}