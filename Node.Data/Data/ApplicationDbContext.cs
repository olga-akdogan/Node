using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Node.Data.Models;
using Node.Data.Models.Enums;

namespace Node.Data.Data;

/// <summary>
/// De databankcontext van het project, gebaseerd op IdentityDbContext
/// zodat de Identity-tabellen en de eigen tabellen in
/// dezelfde databank leven.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<NatalChart> NatalCharts => Set<NatalChart>();
    public DbSet<Placement> Placements => Set<Placement>();
    public DbSet<Swipe> Swipes => Set<Swipe>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<CookieConsentLog> CookieConsentLogs => Set<CookieConsentLog>();
    public DbSet<PartnerPreference> PartnerPreferences => Set<PartnerPreference>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Horoscoop: 1-op-1 met de gebruiker; verdwijnt mee met het account.
        builder.Entity<NatalChart>()
            .HasOne(n => n.User)
            .WithOne(u => u.NatalChart)
            .HasForeignKey<NatalChart>(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tekens als tekst opslaan zodat de databank leesbaar blijft tijdens de demo.
        builder.Entity<NatalChart>().Property(n => n.SunSign).HasConversion<string>().HasMaxLength(20);
        builder.Entity<NatalChart>().Property(n => n.MoonSign).HasConversion<string>().HasMaxLength(20);
        builder.Entity<NatalChart>().Property(n => n.AscendantSign).HasConversion<string>().HasMaxLength(20);

        // Posities: veel per horoscoop, elk hemellichaam maximaal één keer.
        builder.Entity<Placement>()
            .HasOne(p => p.NatalChart)
            .WithMany(n => n.Placements)
            .HasForeignKey(p => p.NatalChartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Placement>()
            .HasIndex(p => new { p.NatalChartId, p.Body })
            .IsUnique();

        builder.Entity<Placement>().Property(p => p.Body).HasConversion<string>().HasMaxLength(20);
        builder.Entity<Placement>().Property(p => p.Sign).HasConversion<string>().HasMaxLength(20);

        // Swipes: Restrict i.p.v. Cascade omdat SQL Server geen dubbele
        // cascade-paden naar dezelfde tabel (AspNetUsers) toelaat.
        builder.Entity<Swipe>()
            .HasOne(s => s.SwiperUser)
            .WithMany()
            .HasForeignKey(s => s.SwiperUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Swipe>()
            .HasOne(s => s.TargetUser)
            .WithMany()
            .HasForeignKey(s => s.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Eén swipe per combinatie swiper/doelwit.
        builder.Entity<Swipe>()
            .HasIndex(s => new { s.SwiperUserId, s.TargetUserId })
            .IsUnique();

        builder.Entity<Match>()
            .HasOne(m => m.User1)
            .WithMany()
            .HasForeignKey(m => m.User1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Match>()
            .HasOne(m => m.User2)
            .WithMany()
            .HasForeignKey(m => m.User2Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Eén match per paar (User1Id < User2Id wordt in de servicelaag afgedwongen).
        builder.Entity<Match>()
            .HasIndex(m => new { m.User1Id, m.User2Id })
            .IsUnique();

        builder.Entity<Match>().Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

        builder.Entity<Report>()
            .HasOne(r => r.ReporterUser)
            .WithMany()
            .HasForeignKey(r => r.ReporterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Report>()
            .HasOne(r => r.ReportedUser)
            .WithMany()
            .HasForeignKey(r => r.ReportedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Consentlog mag blijven bestaan als het account verdwijnt (bewijslast),
        // de verwijzing naar de gebruiker wordt dan leeggemaakt.
        builder.Entity<CookieConsentLog>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // HasDefaultValue: lets the migration add this new, required column
        // to a table that already has (seeded) users in it.
        builder.Entity<ApplicationUser>().Property(u => u.Gender)
            .HasConversion<string>().HasMaxLength(20).HasDefaultValue(Gender.Male);

        // Partner preference: removed together with the account; multiple genders per user are allowed.
        builder.Entity<PartnerPreference>()
            .HasOne(p => p.User)
            .WithMany(u => u.PartnerPreferences)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PartnerPreference>().Property(p => p.Gender).HasConversion<string>().HasMaxLength(20);

        builder.Entity<PartnerPreference>()
            .HasIndex(p => new { p.UserId, p.Gender })
            .IsUnique();
    }
}
