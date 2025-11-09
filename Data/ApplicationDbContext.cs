using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Podcast_MVC.Models;

namespace Podcast_MVC.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Podcast> Podcasts { get; set; }
        public DbSet<Episode> Episodes { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Episode cascade when Podcast is deleted
            modelBuilder.Entity<Episode>()
                .HasOne(e => e.Podcast)
                .WithMany(p => p.Episodes)
                .HasForeignKey(e => e.PodcastID)
                .OnDelete(DeleteBehavior.Cascade);

            // Subscription cascade when Podcast is deleted
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Podcast)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PodcastID)
                .OnDelete(DeleteBehavior.Cascade);

            // Subscription stays if User is deleted (optional)
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.User)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(s => s.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
