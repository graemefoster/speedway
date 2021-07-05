using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Speedway.Api.Extensions;

namespace Speedway.Policy
{
    public class Startup
    {
        private readonly IHostEnvironment _hostEnvironment;

        public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry();

            services
                .AddMicrosoftIdentityWebApiAuthentication(Configuration);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKeyAuthentication", null);

            services.AddControllers();
            services.Configure<PolicyApiSettings>(Configuration.GetSection("PolicyApiSettings"));

            if (_hostEnvironment.IsDevelopment() &&
                (Environment.GetEnvironmentVariable("APPSETTING_WEBSITE_SITE_NAME") == null))
            {
                IdentityModelEventSource.ShowPII = true;
                services.AddSingleton<IAzureCredentialProvider, LocalAzureCredentialProvider>();
            }
            else
            {
                services.AddSingleton<IAzureCredentialProvider, LocalAzureCredentialProvider>();
            }

            typeof(Startup).Assembly.RegisterValidationServices(services);
            services.AddMediatR(typeof(Startup).Assembly);
            services.AddMvcCore(options => { options.Filters.Add<SpeedwayExceptionFilter>(); });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}