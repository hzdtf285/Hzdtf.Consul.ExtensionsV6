using Hzdtf.Utility;
using Microsoft.AspNetCore.Mvc;

namespace Hzdtf.Consul.ServiceExample.Controllers
{
    [ApiController]
    [Route("a/api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            return App.CurrConfig["test"];
        }
    }
}
