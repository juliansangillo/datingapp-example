using System;
using API.Extensions;
using API.Middleware;
using API.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace API {
	public class Startup {
        private readonly IConfiguration config;
        public Startup(IWebHostEnvironment env) {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsEnvironment("Localhost") || env.IsDevelopment()) {
                builder.AddUserSecrets<Startup>();
            }

            builder.AddEnvironmentVariables();

            config = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddApplicationServices(config);
            services.AddControllers();
            services.AddCors();
            services.AddIdentityServices(config);
            services.AddSignalR();
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo {
                    Version = "v1",
                    Title = config["Swagger:Title"],
                    Description = config["Swagger:Description"],
                    Contact = new OpenApiContact {
                        Name = config["Swagger:Contact:Name"],
                        Email = config["Swagger:Contact:Email"],
                        Url = new Uri(config["Swagger:Contact:Url"])
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app) {
            app.UseMiddleware<ExceptionMiddleware>();
            
            bool useSwagger = bool.Parse(config["Swagger:Enabled"]);
            if (useSwagger) {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{config["Swagger:Title"]} v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(policy => 
                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithOrigins("https://localhost:4200")
            );

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapHub<PresenceHub>("hubs/presence");
                endpoints.MapHub<MessageHub>("hubs/message");
                endpoints.MapFallbackToController("Index", "Root");
            });
        }
    }
}
