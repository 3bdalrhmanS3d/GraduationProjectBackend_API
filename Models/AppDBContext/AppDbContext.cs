using Microsoft.EntityFrameworkCore;

namespace GraduationProjectBackendAPI.Models.AppDBContext
{
    public class AppDbContext : DbContext
    {
        // Add-Migration init0 -Context AppDbContext

        // اخر ابديت حل كان رقم 
        // 0  abdo saad  

        // note
        // لما تعمل المايجريشن وقبل ما تعمل ابديت 
        // اي 
        // onDelete: ReferentialAction.Cascade);
        // حولها الى
        // onDelete: ReferentialAction.NoAction);


        // Update-Database -Context AppDbContext

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        // User folder 
        public DbSet<User.Users> UsersT { get; set; }
        public DbSet<UserDetails> DetailsT { get; set; }
        public DbSet<UserVisitHistory> UserVisitHistoryT { get; set; }
        public DbSet<AccountVerification> AccountVerificationT { get; set; }
    }
}
