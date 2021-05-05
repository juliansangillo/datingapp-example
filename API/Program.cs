using System;
using System.Threading.Tasks;
using API.Data;
using API.Entities.DB;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace API {
	public class Program {
		public static async Task Main(string[] args) {
			IHost host = CreateHostBuilder(args).Build();
			using IServiceScope scope = host.Services.CreateScope();
			IServiceProvider services = scope.ServiceProvider;
            DataContext context = services.GetRequiredService<DataContext>();
            UserManager<AppUser> userManager = services.GetRequiredService<UserManager<AppUser>>();
            RoleManager<AppRole> roleManager = services.GetRequiredService<RoleManager<AppRole>>();
            await context.Database.MigrateAsync();
            await Seed.SeedUsers(userManager, roleManager);

            await host.RunAsync();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder => {
					webBuilder.UseStartup<Startup>();
				});
	}
}
