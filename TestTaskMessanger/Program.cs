using TestTaskMessanger.Hubs;
using Microsoft.EntityFrameworkCore;
using TestTaskMessanger.Dbl.Data;
using TestTaskMessanger.Dbl.Repository;
using TestTaskMessanger.Utils;

namespace TestTaskMessanger
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<MesMemoryCache>();
            builder.Services.AddSignalR(opt =>
            {
                opt.KeepAliveInterval = TimeSpan.FromMinutes(1);
            });

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            builder.Services.AddDbContext<MessangerDbContext>(opt => opt.UseNpgsql(configuration.GetConnectionString("default")));
            builder.Services.AddTransient<IMessangerRepository, MessangerRepository>();

            var app = builder.Build();
            app.MapControllers();
            app.MapHub<MessangerHub>("/Messanger");

            app.Run();
        }
    }
}