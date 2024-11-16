using GraduationProjectBackendAPI.Models.AppDBContext;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace GraduationProjectBackendAPI.Controllers.User
{
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AppDbContext _context;

        public TokenBlacklistMiddleware(RequestDelegate next, AppDbContext context)
        {
            _next = next;
            _context = context;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                var isBlacklisted = _context.BlacklistTokensT.Any(t => t.Token == token && t.ExpiryDate > DateTime.UtcNow);

                if (isBlacklisted)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Token is invalid or expired.");
                    return;
                }

                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);

                    if (jwtToken == null || jwtToken.ValidTo <= DateTime.UtcNow)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Token is expired.");
                        return;
                    }
                }
                catch
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid token.");
                    return;
                }
            }

            await _next(context);
        }
    }
}
