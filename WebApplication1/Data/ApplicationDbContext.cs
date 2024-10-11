using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data;
    
public class ApplicationDbContext : DbContext
{
    public DbSet<MemberPreferences> MemberPreferences { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

}

