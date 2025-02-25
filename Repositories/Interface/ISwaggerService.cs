using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;

namespace Unanet_POC.Repositories.Interface
{
    public interface ISwaggerService
    {
        (bool Success, IActionResult Result) GetRequiredValues(JsonElement swaggerJson, string endpointName);
    }
}
