using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;

namespace RentalManagement.Models;

public class MainContext : DbContext
{
    public MainContext() { }

    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlServer(
            "server=cyldbms1.cylee.com;initial catalog=WBTEST;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Encrypt=False"
        );

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Client>()
            .HasMany(e => e.Contracts)
            .WithOne(e => e.Client)
            .HasForeignKey(e => e.ClientId)
            .IsRequired(false);
        modelBuilder
            .Entity<Property>()
            .HasMany(e => e.Contracts)
            .WithMany(e => e.Properties);
        modelBuilder
            .Entity<Property>()
            .HasData(new Property{
                 Id = 1,
                 Name = "Property 1",
                 Location = "Location 1"
            });
        modelBuilder
            .Entity<Property>()
            .HasData(new Property{
                 Id = 2,
                 Name = "Property 11",
                 Location = "Location 1",
                 ParentPropertyId = 1
            });
    }

    public DbSet<SYS_User> SYS_User { get; set; }

    public DbSet<Property> Property { get; set; }
    public DbSet<Client> Client { get; set; }
    public DbSet<Contract> Contract { get; set; }
}
