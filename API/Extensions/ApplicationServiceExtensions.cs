using System;
using API.Data;
using API.Helpers;
using API.Interfaces;
using API.Services;
using API.Settings;
using API.SignalR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace API.Extensions {
    public static class ApplicationServiceExtensions {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config) {
            services.AddDbContext<DataContext>(options => {
                string dbHost = config["Database:Host"];
                string dbUser = config["Database:UserId"];
                string dbPassword = config["Database:Password"];
                string dbName = config["Database:Name"];

                NpgsqlConnectionStringBuilder connection = new NpgsqlConnectionStringBuilder() {
                    Host = dbHost,
                    Username = dbUser,
                    Password = dbPassword,
                    Database = dbName,
                    SslMode = SslMode.Disable
                };
                connection.Pooling = true;

                options.UseNpgsql(connection.ConnectionString);
            });
            services.Configure<CloudinarySettings>(config.GetSection("CloudinarySettings"));
            services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);
            services.AddSingleton<PresenceTracker>();
            services.AddScoped<LogUserActivity>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPhotoService, PhotoService>();

            return services;
        }
    }
}