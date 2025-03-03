using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GraduationProjectBackendAPI.Controllers.User
{
    [Route("api/user")]
    [ApiController]
    [Authorize(Roles = "RegularUser, Instructor, Admin")]
    public class UserController : Controller
    {
        
        [HttpGet("dashboard")]
        public IActionResult GetUserDashboard()
        {
            return Ok(new { message = "Welcome to User Dashboard!" });
        }

    }
}
