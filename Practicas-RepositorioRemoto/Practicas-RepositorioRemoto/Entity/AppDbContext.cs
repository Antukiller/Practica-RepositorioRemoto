using Microsoft.EntityFrameworkCore;
using Practicas_RepositorioRemoto.Models;

namespace Practicas_RepositorioRemoto.Entity;

public class AppDbContext : DbContext {
    public DbSet<User> Users => Set<User>();
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>(entity => {
            entity.ToTable("tbl_user");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.UserName)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Phone)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Website)
                .IsRequired()
                .HasMaxLength(250);

            entity.OwnsOne(e => e.Address, address => {
                address.ToJson();
                address.OwnsOne(e => e.Geo);
            });

            entity.OwnsOne(e => e.Company, company => {
                company.ToJson();
            });

            entity.Property(e => e.CreateAt);
            entity.Property(e => e.UpdateAt);
            entity.Property(e => e.DeleteAt);
            entity.Property(e => e.IsDeleted);
        });
    }
}