using DeanOBrien.Foundation.DataAccess.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;

namespace DeanOBrien.Foundation.DataAccess.Configurator
{
    public class ServicesConfigurator : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IAppInsightsApi, AppInsightsApi>();
            serviceCollection.AddScoped<ILogStore, SqlLogStore>();
        }
    }
}