using Group6WebProject.Models;
using Microsoft.EntityFrameworkCore;

namespace Group6WebProject.Data;
    
public class ApplicationDbContext : DbContext
{
    public DbSet<MemberPreferences> MemberPreferences { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

}

