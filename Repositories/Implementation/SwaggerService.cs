using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Unanet_POC.Repositories.Interface;

namespace Unanet_POC.Repositories.Implementation
{
    public class SwaggerService : ISwaggerService
    {
        public (bool Success, IActionResult Result) GetRequiredValues(JsonElement swaggerJson, string endpointName)
        {
            try
            {
                if (swaggerJson.ValueKind == JsonValueKind.Undefined)
                {
                    return (false, new BadRequestObjectResult("Swagger JSON is required."));
                }

                if (string.IsNullOrEmpty(endpointName))
                {
                    return (false, new BadRequestObjectResult("Endpoint name is required."));
                }

                if (!swaggerJson.TryGetProperty("paths", out JsonElement paths))
                {
                    return (false, new NotFoundObjectResult("Paths section not found in Swagger JSON."));
                }

                var matchingEndpoint = paths.EnumerateObject()
                    .FirstOrDefault(p => p.Name.Contains(endpointName, StringComparison.OrdinalIgnoreCase));

                if (matchingEndpoint.Equals(default(JsonProperty)))
                {
                    return (false, new NotFoundObjectResult($"No endpoint found matching '{endpointName}'."));
                }

                string fullEndpointName = matchingEndpoint.Name;
                var methods = matchingEndpoint.Value.EnumerateObject();
                var method = methods.FirstOrDefault();
                if (method.Equals(default(JsonProperty)))
                {
                    return (false, new NotFoundObjectResult($"No HTTP methods found for endpoint '{fullEndpointName}'."));
                }

                if (!method.Value.TryGetProperty("requestBody", out JsonElement requestBody) ||
                    !requestBody.TryGetProperty("content", out JsonElement content) ||
                    !content.TryGetProperty("application/json", out JsonElement appJson) ||
                    !appJson.TryGetProperty("schema", out JsonElement schemaRefElement))
                {
                    return (false, new NotFoundObjectResult($"No request body schema found for endpoint '{fullEndpointName}'."));
                }

                if (!schemaRefElement.TryGetProperty("$ref", out JsonElement schemaRefJson))
                {
                    return (false, new NotFoundObjectResult($"No schema reference found for endpoint '{fullEndpointName}'."));
                }

                string schemaRef = schemaRefJson.GetString();
                if (string.IsNullOrEmpty(schemaRef))
                {
                    return (false, new NotFoundObjectResult($"Schema reference is empty for endpoint '{fullEndpointName}'."));
                }

                string schemaName = schemaRef.Split('/').Last();
                if (!swaggerJson.TryGetProperty("components", out JsonElement components) ||
                    !components.TryGetProperty("schemas", out JsonElement schemas) ||
                    !schemas.TryGetProperty(schemaName, out JsonElement schema))
                {
                    return (false, new NotFoundObjectResult($"Schema '{schemaName}' not found."));
                }

                if (!schema.TryGetProperty("properties", out JsonElement properties))
                {
                    return (false, new NotFoundObjectResult("No properties found in the schema."));
                }

                var requiredValues = properties.EnumerateObject().Select(p => p.Name).ToList();

                return (true, new OkObjectResult(new { Endpoint = fullEndpointName, Schema = schemaName, RequiredValues = requiredValues }));
            }
            catch (Exception ex)
            {
                return (false, new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 });
            }
        }
    }
}
