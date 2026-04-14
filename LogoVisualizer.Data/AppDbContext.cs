using LogoVisualizer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LogoVisualizer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<PrintZone> PrintZones => Set<PrintZone>();
    public DbSet<PrintTechnique> PrintTechniques => Set<PrintTechnique>();
    public DbSet<PrintZoneTechnique> PrintZoneTechniques => Set<PrintZoneTechnique>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite PK for the many-to-many join table
        modelBuilder.Entity<PrintZoneTechnique>()
            .HasKey(pzt => new { pzt.PrintZoneId, pzt.PrintTechniqueId });

        modelBuilder.Entity<PrintZoneTechnique>()
            .HasOne(pzt => pzt.PrintZone)
            .WithMany(pz => pz.AllowedTechniques)
            .HasForeignKey(pzt => pzt.PrintZoneId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PrintZoneTechnique>()
            .HasOne(pzt => pzt.PrintTechnique)
            .WithMany(pt => pt.PrintZoneTechniques)
            .HasForeignKey(pzt => pzt.PrintTechniqueId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .Property(p => p.Title).HasMaxLength(500).IsRequired();

        modelBuilder.Entity<Product>()
            .Property(p => p.ImagePath).HasMaxLength(1000).IsRequired();

        modelBuilder.Entity<PrintZone>()
            .Property(pz => pz.Name).HasMaxLength(200).IsRequired();

        modelBuilder.Entity<PrintTechnique>()
            .Property(pt => pt.Name).HasMaxLength(200).IsRequired();

        modelBuilder.Entity<PrintTechnique>()
            .HasIndex(pt => pt.Name).IsUnique();

        // Seed the standard print techniques
        modelBuilder.Entity<PrintTechnique>().HasData(
            new PrintTechnique { Id = 1, Name = "Screen Print",  Description = "Silkscreen/serigrafi-tryk" },
            new PrintTechnique { Id = 2, Name = "Embroidery",    Description = "Broderi" },
            new PrintTechnique { Id = 3, Name = "Sublimation",   Description = "Sublimationstryk (kræver lyst syntetisk stof)" },
            new PrintTechnique { Id = 4, Name = "Engraving",     Description = "Laser- eller mekanisk gravering" },
            new PrintTechnique { Id = 5, Name = "DTG",           Description = "Direct-to-garment tryk" },
            new PrintTechnique { Id = 6, Name = "Pad Print",     Description = "Tampontryk — velegnet til små flader" },
            new PrintTechnique { Id = 7, Name = "Digital Print", Description = "Digitalt tryk / inkjet" }
        );
    }
}
