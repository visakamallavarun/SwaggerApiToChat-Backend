using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Unanet_POC.Models.DTO;
using Unanet_POC.Repositories.Interface;

namespace Unanet_POC.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class SwaggerController : ControllerBase
    {
        private readonly ISwaggerService _swaggerService;

        public SwaggerController(ISwaggerService swaggerService)
        {
            _swaggerService = swaggerService;
        }


        [HttpPost("get-required-values")]
        public IActionResult GetRequiredValues([FromBody] SwaggerRequest request)
        {
            var (success, result) = _swaggerService.GetRequiredValues(request.SwaggerJson, request.EndpointName);
            return result;
        }
    }

}
