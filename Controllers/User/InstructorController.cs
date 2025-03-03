using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GraduationProjectBackendAPI.Controllers.User
{
    [Route("api/instructor")]
    [ApiController]
    [Authorize(Roles = "Instructor")]
    public class InstructorController : Controller
    {

        [HttpGet("dashboard")]
        public IActionResult GetInstructorDashboard()
        {
            return Ok(new { message = "Welcome to Instructor Dashboard!" });
        }
    }
}
