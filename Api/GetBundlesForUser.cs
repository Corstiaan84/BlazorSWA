using BlazorApp.Shared;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Api
{
  public class GetBundlesForUser
  {
    private readonly ILogger _logger;
    private readonly CosmosClient _cosmosClient;

    public GetBundlesForUser(ILoggerFactory loggerFactory, CosmosClient cosmosClient)
    {
      _logger = loggerFactory.CreateLogger<GetBundlesForUser>();
      _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
    }

    [Function("GetBundlesForUser")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user")] HttpRequestData req, string vanityUrl)
    {
      try
      {
        // Get the cosmos container
        var container = _cosmosClient.GetContainer("TheUrlist", "linkbundles");

        ClientPrincipal principal = null;

        if (req.Headers.TryGetValues("x-ms-client-principal", out var header))
        {
          var data = header.FirstOrDefault();
          var decoded = Convert.FromBase64String(data);
          var json = Encoding.UTF8.GetString(decoded);
          principal = JsonSerializer.Deserialize<ClientPrincipal>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
          if (principal != null)
          {

            // Hash the username using the Hasher class
            Hasher hasher = new Hasher();
            string username = hasher.HashString(principal.UserDetails);

            string provider = principal.IdentityProvider;

            // get any link bundles from the database where the username and provider match
            var query = new QueryDefinition("SELECT c.id, c.vanityUrl, c.description, c.links FROM c WHERE c.userId = @username AND c.provider = @provider")
                            .WithParameter("@username", username)
                            .WithParameter("@provider", provider);

            var response = await container.GetItemQueryIterator<LinkBundle>(query).ReadNextAsync();

            // If there are no link bundles, return a 404
            if (!response.Any())
            {
              return req.CreateResponse(HttpStatusCode.NotFound);
            }

            // return the response as JSON
            var res = req.CreateResponse();
            await res.WriteAsJsonAsync(response);

            return res;
          }
          else
          {
            // If there is no client principal, return a 401
            var res = req.CreateResponse(HttpStatusCode.Unauthorized);
            await res.WriteAsJsonAsync(new { error = "Unauthorized" });

            return res;
          }
        }
        else
        {
          // If there is no client principal, return a 401
          var res = req.CreateResponse(HttpStatusCode.Unauthorized);
          await res.WriteAsJsonAsync(new { error = "Unauthorized" });

          return res;
        }
      }
      catch (Exception ex)
      {
        var res = req.CreateResponse();
        await res.WriteAsJsonAsync(new { error = ex.Message }, HttpStatusCode.InternalServerError);

        return res;
      }
    }
  }
}