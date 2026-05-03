using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Node.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Node.Data.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<MemberProfile> MemberProfiles => Set<MemberProfile>();
    public DbSet<AstrologyProfile> AstrologyProfiles => Set<AstrologyProfile>();
    public DbSet<Swipe> Swipes => Set<Swipe>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<BlockedUser> BlockedUsers => Set<BlockedUser>();
    public DbSet<LanguagePreference> LanguagePreferences => Set<LanguagePreference>();
    public DbSet<CookieConsentLog> CookieConsentLogs => Set<CookieConsentLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<MemberProfile>()
            .HasOne(p => p.User)
            .WithOne(u => u.MemberProfile)
            .HasForeignKey<MemberProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AstrologyProfile>()
            .HasOne(a => a.User)
            .WithOne()
            .HasForeignKey<AstrologyProfile>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

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

        builder.Entity<Match>()
            .HasIndex(m => new { m.User1Id, m.User2Id })
            .IsUnique();

        builder.Entity<ChatMessage>()
            .HasOne(c => c.Match)
            .WithMany(m => m.ChatMessages)
            .HasForeignKey(c => c.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ChatMessage>()
            .HasOne(c => c.SenderUser)
            .WithMany()
            .HasForeignKey(c => c.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);

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

        builder.Entity<BlockedUser>()
            .HasOne(b => b.BlockedUserAccount)
            .WithMany()
            .HasForeignKey(b => b.BlockedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BlockedUser>()
            .HasOne(b => b.BlockedByAdmin)
            .WithMany()
            .HasForeignKey(b => b.BlockedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}