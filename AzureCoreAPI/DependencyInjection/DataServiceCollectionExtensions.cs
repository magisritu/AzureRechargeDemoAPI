using AzureCoreAPI.Infrastucture;
using AzureCoreAPI.Repository;
using AzureCoreAPI.Service;
using Microsoft.EntityFrameworkCore;

namespace AzureCoreAPI.DependencyInjection
{
    internal static class DataServiceCollectionExtensions
    {
        public static IServiceCollection AddDataServices(this IServiceCollection services,
            ConfigurationManager configuration)
        {
            //services.AddOptions<SqlDatabaseConfiguration>()
            //    .Bind(configuration.GetSection("DocumentApiConfig:SqlDatabaseConfig"))
            //    .ValidateDataAnnotations()
            //    .ValidateOnStart();

            //var sqlDatabaseConfiguration = configuration
            //    .GetSection("DocumentApiConfig:SqlDatabaseConfig")
            //    .Get<SqlDatabaseConfiguration>();

            services.AddDbContext<MyDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddScoped<IWeatherRepository, WeatherRepository>();
            services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();

            return services;
        }
    }
}
