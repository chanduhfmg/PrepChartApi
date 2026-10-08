using Microsoft.AspNetCore.Mvc;

namespace prepchartApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get([FromQuery] string name)
        {
            var sampleData = new[] { "Hello", "from", "API" };
            if (name == "akash")
                    {
                        var access = " Granted " + name;
                        return Ok(access);
                    }
            return Ok(name);
        }
    }
}
