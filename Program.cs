using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ExtractorFacturasAzure.Data;
using ExtractorFacturasAzure.Services;
using Microsoft.Azure.Functions.Worker; 
using System.Text.Json; 

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration["ConnectionStrings:DefaultConnection"];
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddHttpClient();
        services.AddScoped<IFacturaService, FacturaService>();

        services.Configure<WorkerOptions>(workerOptions =>
        {
            var settings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            workerOptions.Serializer = new Azure.Core.Serialization.JsonObjectSerializer(settings);
        });
    })
    .Build();

host.Run();