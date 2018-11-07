using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Coolblue.Utilities.ApplicationHealth;
using Coolblue.Utilities.ApplicationHealth.AspNetCore;
using Coolblue.Utilities.MonitoringEvents;
using Coolblue.Utilities.MonitoringEvents.Datadog;
using Coolblue.Utilities.MonitoringEvents.SimpleInjector;
using Coolblue.Utilities.RequestResponseLogging.AspNetCore;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

using SimpleInjector;
using SimpleInjector.Integration.AspNetCore.Mvc;
using SimpleInjector.Lifestyles;

using StatsdClient;

using Swashbuckle.AspNetCore.Swagger;
using TDDWorkShop.Api.Infrastructure;

using PhilosophicalMonkey;
using TDDWorkShop.Persistence.Oracle;
using Coolblue.Sinks.Splunk;
using ChimpLab.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Coolblue.CorrelationId.AspNetCore;
using Coolblue.CorrelationId;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.PlatformAbstractions;
using TDDWorkShop.Api.Controllers;

namespace TDDWorkShop.Api
{
    public class Startup
    {
        public static string AppName => "ProductsApi";

        private ILogger _logger;
        private MonitoringEvents _monitoringEvents;
        private WebServiceSettings _settings;
        private readonly Container _container = new Container();
        private PersistenceAdapter _persistenceAdapter;
        private readonly IHostingEnvironment _hostingEnvironment;

        public Action<Container> RegisterOverrides { get; set; } = c => { return; };

        public Startup(IHostingEnvironment env)
        {
            _hostingEnvironment = env;
            ConfigureSettings();
        }

        // ReSharper disable once UnusedMember.Global
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCorrelationId();
        
            services
                .AddMvcCore()
                .AddVersionedApiExplorer(
                    options =>
                    {
                        options.GroupNameFormat = "'v'VVV";
                    });

            services.AddMvc()
                    //todo : Replace with controller name
                    .AddApplicationPart(typeof(ProductsController).Assembly)
                    .AddMvcOptions(opt => opt.OutputFormatters.RemoveType<StringOutputFormatter>());

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new MediaTypeApiVersionReader("version");
                options.ApiVersionSelector = new CurrentImplementationApiVersionSelector( options );
            });

            if (!_hostingEnvironment.IsProduction())
            {
                services.AddSwaggerGen(
                    options =>
                    {
                        // resolve the IApiVersionDescriptionProvider service
                        // note: that we have to build a temporary service provider here because one has not been created yet
                        var provider = services.BuildServiceProvider()
                            .GetRequiredService<IApiVersionDescriptionProvider>();

                        // add a swagger document for each discovered API version
                        // note: you might choose to skip or document deprecated API versions differently
                        foreach (var description in provider.ApiVersionDescriptions)
                        {
                            options.SwaggerDoc(description.GroupName,
                                CreateInfoForApiVersion(description));
                        }

                        // add a custom operation filter which sets default values
                        options.OperationFilter<SwaggerDefaultValues>();

                        // integrate xml comments
                        options.IncludeXmlComments(XmlCommentsFilePath);
                    });
            }
            services.AddSingleton<IControllerActivator>(new SimpleInjectorControllerActivator(_container));

            services.UseSimpleInjectorAspNetRequestScoping(_container);
        }

        public void Configure(IApplicationBuilder app,
                              IApiVersionDescriptionProvider provider)
        {
            ConfigureLoggingAndMetrics();

            ConfigurePersistenceAdapter();
            ConfigureSimpleInjector(app);
            ConfigureMiddleware(app, provider);
        }

        private void ConfigureSettings()
        {
            var configuration = GetConfiguration();

            _settings = new WebServiceSettings(configuration);
        }

        protected virtual IConfigurationRoot GetConfiguration()
        {
            var personalGlobalConfigKey = $"{AppName}_global_config_file";
            var personalConfigKey = $"{AppName}_config_file";
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddJsonFileFromEnvironmentVariable(personalGlobalConfigKey, optional: true)
                .AddJsonFileFromEnvironmentVariable(personalConfigKey, optional: true)
                .AddEnvironmentVariables();

            var config = builder.Build();

            return config;
        }

        private void ConfigureLoggingAndMetrics()
        {
            ConfigureSerilog();
            ConfigureDatadog();

            _monitoringEvents = new MonitoringEvents(_logger, new DatadogMetrics());

            _monitoringEvents.ApplicationStart();
        }

        private void ConfigureSerilog()
        {
            var splunkConnectionInfo =
                new SplunkUdpSinkConnectionInfo(hostname: _settings.SplunkHost, port: _settings.SplunkUdpPort);

            var configuration = new LoggerConfiguration()
                .MinimumLevel.Is(_settings.GlobalMinimumLogLevel)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message,-30:lj} {Properties:j}{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Literate)
                .WriteTo.Trace()
                .WriteTo.SplunkViaUdp(splunkConnectionInfo, restrictedToMinimumLevel: _settings.SplunkMinimumLogLevel)
                .Enrich.FromLogContext();

            if (!string.IsNullOrEmpty(_settings.SeqUrl))
            {
                configuration.WriteTo.Seq(_settings.SeqUrl);
            }

            _logger = configuration.CreateLogger();
        }

        private void ConfigureDatadog()
        {
            DogStatsd.Configure(new StatsdConfig
            {
                Prefix = AppName,
                StatsdServerName = "localhost"
            });
        }

        private void ConfigurePersistenceAdapter()
        {
            _persistenceAdapter = new PersistenceAdapter(_settings.PersistenceAdapterSettings, _monitoringEvents);
        }

        private void ConfigureSimpleInjector(IApplicationBuilder app)
        {
            _container.Options.PropertySelectionBehavior = new MonitoringEventsPropertySelectionBehavior();
            _container.Options.DefaultLifestyle = new AsyncScopedLifestyle();

            _container.RegisterInstance(_settings);
            _container.RegisterInstance(_monitoringEvents);

            _persistenceAdapter.Register(_container);
            _container.RegisterCollection(_persistenceAdapter.GetHealthTests());

            _container.Register<IProductsMeasurementUseCase>(()=> new ProductMeasurementUseCase());

            _container.RegisterMvcControllers(app);

            _container.Register(() => new ExceptionHandlingMiddleware(_monitoringEvents, !_hostingEnvironment.IsProduction()));

            RegisterOverrides(_container);
        }

        private void ConfigureMiddleware(IApplicationBuilder app,
                                         IApiVersionDescriptionProvider provider)
        {
            app.UseCorrelationId();

            app.Use((context, next) => _container.GetInstance<ExceptionHandlingMiddleware>().Invoke(context, next));

            app.UseApplicationHealthEndpoints(_container.GetAllInstances<IHealthTest>().ToList(),
                                              Reflect.OnTypes.GetAssembly(typeof(Startup)), applicationStartTime: DateTimeOffset.Now);

            app.UseMvc();
						app.UseStaticFiles();

            if (!_hostingEnvironment.IsProduction())
            {
                app.UseSwagger();

                app.UseSwaggerUI(options =>
                {
                    foreach ( var description in provider.ApiVersionDescriptions )
                    {
                        options.SwaggerEndpoint(
                            $"/swagger/{description.GroupName}/swagger.json",
                            description.GroupName.ToUpperInvariant() );
                    }

                });
            }
            app.UseRequestResponseLogging(AppName, _monitoringEvents, !_hostingEnvironment.IsProduction());
        }

        private static string XmlCommentsFilePath
        {
            get
            {
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var fileName = typeof( Startup ).GetTypeInfo().Assembly.GetName().Name + ".xml";
                return Path.Combine( basePath, fileName );
            }
        }

        private static Info CreateInfoForApiVersion( ApiVersionDescription description )
        {
            var info = new Info
            {
                Title = $"Domain API {description.ApiVersion}",
                Version = description.ApiVersion.ToString(),
                Description = "API Versioning example.",
                TermsOfService = "Copyright",
                License = new License { Name = "Private" }
            };

            if ( description.IsDeprecated )
            {
                info.Description += " This API version has been deprecated.";
            }

            return info;
        }
    }
}
