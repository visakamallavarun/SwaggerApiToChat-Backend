using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Unanet_POC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SwaggerController : ControllerBase
    {
        [HttpPost("get-required-values")]
        public IActionResult GetRequiredValues([FromBody] JsonElement swaggerJson, [FromQuery] string endpointName)
        {
            try
            {
                if (swaggerJson.ValueKind == JsonValueKind.Undefined)
                {
                    return BadRequest("Swagger JSON is required.");
                }

                if (string.IsNullOrEmpty(endpointName))
                {
                    return BadRequest("Endpoint name is required.");
                }

                // Find the endpoint containing the given name
                if (!swaggerJson.TryGetProperty("paths", out JsonElement paths))
                {
                    return NotFound("Paths section not found in Swagger JSON.");
                }

                var matchingEndpoint = paths.EnumerateObject()
                    .FirstOrDefault(p => p.Name.Contains(endpointName, StringComparison.OrdinalIgnoreCase));

                if (matchingEndpoint.Equals(default(JsonProperty)))
                {
                    return NotFound($"No endpoint found matching '{endpointName}'.");
                }

                string fullEndpointName = matchingEndpoint.Name;
                var methods = matchingEndpoint.Value.EnumerateObject();
                var method = methods.FirstOrDefault();
                if (method.Equals(default(JsonProperty)))
                {
                    return NotFound($"No HTTP methods found for endpoint '{fullEndpointName}'.");
                }

                // Find the request body schema
                if (!method.Value.TryGetProperty("requestBody", out JsonElement requestBody) ||
                    !requestBody.TryGetProperty("content", out JsonElement content) ||
                    !content.TryGetProperty("application/json", out JsonElement appJson) ||
                    !appJson.TryGetProperty("schema", out JsonElement schemaRefElement))
                {
                    return NotFound($"No request body schema found for endpoint '{fullEndpointName}'.");
                }

                if (!schemaRefElement.TryGetProperty("$ref", out JsonElement schemaRefJson))
                {
                    return NotFound($"No schema reference found for endpoint '{fullEndpointName}'.");
                }

                string schemaRef = schemaRefJson.GetString();
                if (string.IsNullOrEmpty(schemaRef))
                {
                    return NotFound($"Schema reference is empty for endpoint '{fullEndpointName}'.");
                }

                // Extract the schema name from $ref
                string schemaName = schemaRef.Split('/').Last();
                if (!swaggerJson.TryGetProperty("components", out JsonElement components) ||
                    !components.TryGetProperty("schemas", out JsonElement schemas) ||
                    !schemas.TryGetProperty(schemaName, out JsonElement schema))
                {
                    return NotFound($"Schema '{schemaName}' not found.");
                }

                // Extract required properties
                if (!schema.TryGetProperty("properties", out JsonElement properties))
                {
                    return NotFound("No properties found in the schema.");
                }

                var requiredValues = properties.EnumerateObject().Select(p => p.Name).ToList();

                return Ok(new { Endpoint = fullEndpointName, Schema = schemaName, RequiredValues = requiredValues });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
