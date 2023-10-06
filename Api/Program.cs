using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;
using System.Text.Json.Serialization;
using System.Text.Json;
using Api.Services;

namespace ApiIsolated
{
    public class Program
    {
        public static void Main()
        {
            // Setup custom serializer to use System.Text.Json
            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            CosmosSystemTextJsonSerializer cosmosSystemTextJsonSerializer = new CosmosSystemTextJsonSerializer(jsonSerializerOptions);
            CosmosClientOptions cosmosClientOptions = new CosmosClientOptions()
            {
                ApplicationName = "SystemTextJson",
                Serializer = cosmosSystemTextJsonSerializer
            };

            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IDataService, CosmosDataService>();
                    services.AddSingleton<CosmosClient>(sp => new CosmosClient(
                        context.Configuration["CosmosDb:Endpoint"], 
                        context.Configuration["CosmosDb:Key"],
                        cosmosClientOptions));
                })
                .ConfigureFunctionsWorkerDefaults()
                .Build();

            host.Run();
        }
    }
}