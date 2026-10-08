using Microsoft.AspNetCore.Mvc;
using System.DirectoryServices.AccountManagement;

namespace prepchartApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, "hfmg.net"))
            {
                
                if (pc.ValidateCredentials(req.email, req.password))
                {
                    UserPrincipal user = UserPrincipal.FindByIdentity(pc, req.email);
                    if (user != null)
                    {
                        string email_address = user.EmailAddress;
                        User p = new User
                        {
                            Account = email_address,
                            Name = user.Name,
                            Mail = email_address,
                            Password = req.password,
                            Role = "",
                        };
                        
                        return Ok(p);
                    }
                    return Ok("Unauthorised");
                }
            }
            return Ok(null);


        }
    }

    public class LoginRequest
    {
        public string email { get; set; }
        public string password { get; set; }
    }

    public class User
    {
        public int Id { get; set; }

        public string Account { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Mail { get; set; } = null!;

        public string? Role { get; set; }

        public string? Password { get; set; }

        public DateTime? Created { get; set; }
    }
}


