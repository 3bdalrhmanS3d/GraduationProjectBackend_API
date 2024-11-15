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


        //  Update-Database -Context AppDbContext

        public AppDbContext()
        {

        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            optionsBuilder.UseSqlServer("Data Source=LAPTOP-3HFKTLSG\\SQL2022;Initial Catalog=GraduationProjectBackend_DB_API;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False");
            base.OnConfiguring(optionsBuilder);
        }

        //  User folder 
        public DbSet<User.Users> UsersT { get; set; }
        public DbSet<UserDetails> DetailsT { get; set; }
        public DbSet<UserVisitHistory> UserVisitHistoryT { get; set; }
        public DbSet<AccountVerification> AccountVerificationT { get; set; }
    }
}
