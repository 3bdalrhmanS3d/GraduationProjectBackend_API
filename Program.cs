using GraduationProjectBackendAPI.Controllers.User;
using GraduationProjectBackendAPI.Models;
using GraduationProjectBackendAPI.Models.AppDBContext;
using GraduationProjectBackendAPI.Models.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography;
using System.Text;

namespace GraduationProjectBackendAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' is not set in appsettings.json.");
            }

            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.WithOrigins()
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromMinutes(30);
            });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "JwtBearer";
                options.DefaultChallengeScheme = "JwtBearer";
            })
            .AddJwtBearer("JwtBearer", jwtOptions =>
            {
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["JWT:ValidIss"],
                    ValidAudience = builder.Configuration["JWT:ValidAud"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"]))
                };
            });

            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                    Scheme = "Bearer",
                    Description = "Enter 'Bearer {token}' in the field below."
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
            builder.Services.AddSingleton<EmailQueueService>();
            builder.Services.AddHostedService<EmailQueueBackgroundService>();

            builder.Services.AddEndpointsApiExplorer();
  
            var app = builder.Build();
            await SeedAdminUserAsync(app.Services);

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseSession();
            app.UseAuthentication();
            app.UseCors("ReactAppPolicy");
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }


        private static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!await context.UsersT.AnyAsync(u => u.Role == UserRole.Admin))
            {
                var adminUser = new Users
                {
                    FullName = "Super Admin",
                    EmailAddress = "admin@platform.com",
                    PasswordHash = HashPassword("Admin@1234"),
                    CreatedAt = DateTime.UtcNow,
                    Role = UserRole.Admin,
                    ProfilePhoto = "/uploads/profile-pictures/defult_user.webp",
                    IsSystemProtected = true
                };

                context.UsersT.Add(adminUser);
                await context.SaveChangesAsync();

                var adminVerification = new AccountVerification
                {
                    UserId = adminUser.UserId,
                    Code = "000000",
                    CheckedOK = true,
                    Date = DateTime.UtcNow
                };

                context.AccountVerificationT.Add(adminVerification);
                await context.SaveChangesAsync();
            }
        }


        private static string HashPassword(string password)
        {
            byte[] salt = new byte[16];

            RandomNumberGenerator.Fill(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }
    }
}
