using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TDDWorkShop.Api
{
    public class Program
    {
        public static void Main()
        {
            var configuration = GetHostingConfiguration();

            var host = new WebHostBuilder()
                .UseConfiguration(configuration)
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            //if (WindowsServiceDetection.IsRunningAsWindowsService())
            //    host.RunAsService();
            //else
                host.Run();
        }

        private static IConfigurationRoot GetHostingConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("hosting.json", optional: true)
                .AddEnvironmentVariables();

            return builder.Build();
        }
    }
}