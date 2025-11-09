using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Podcast_MVC.Data;
using Podcast_MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Podcast_MVC.Scripts
{
    public static class DatabaseInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            string[] roles = { "Admin", "Podcaster", "Listener" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            var admin = await CreateUser(userManager, "admin@pd.com", "Admin User", "Password@123", "Admin");
            var podcaster1 = await CreateUser(userManager, "podcaster1@pd.com", "Podcaster One", "Password@123", "Podcaster");
            var podcaster2 = await CreateUser(userManager, "podcaster2@pd.com", "Podcaster Two", "Password@123", "Podcaster");
            var listener1 = await CreateUser(userManager, "listener1@pd.com", "Listener One", "Password@123", "Listener");
            var listener2 = await CreateUser(userManager, "listener2@pd.com", "Listener Two", "Password@123", "Listener");

            var podcasts = new List<Podcast>
            {
                new() { Title = "Digital Horizons", Description = "Exploring future tech and digital innovation", CreatorID = podcaster1.Id },
                new() { Title = "Curious Minds", Description = "Weekly deep dives into fascinating scientific topics", CreatorID = podcaster2.Id },
                new() { Title = "Market Moves", Description = "Breaking down trends in business and finance", CreatorID = podcaster1.Id },
                new() { Title = "Echoes of the Past", Description = "Uncovering hidden events that shaped history", CreatorID = podcaster2.Id },
                new() { Title = "Wellness Wave", Description = "Practical advice for better health and balance", CreatorID = podcaster1.Id }
            };


            foreach (var p in podcasts)
            {
                if (!await context.Podcasts.AnyAsync(x => x.Title == p.Title))
                    context.Podcasts.Add(p);
            }

            await context.SaveChangesAsync();

            var allPodcasts = await context.Podcasts.ToListAsync();
            int counter = 1;
            foreach (var podcast in allPodcasts)
            {
                if (!await context.Episodes.AnyAsync(e => e.PodcastID == podcast.PodcastID))
                {
                    var random = new Random();
                    context.Episodes.Add(new Episode
                    {
                        PodcastID = podcast.PodcastID,
                        Title = $"Episode 1: {podcast.Title}",
                        ReleaseDate = DateTime.UtcNow.AddDays(-counter),
                        Duration = 00.34,
                        PlayCount = random.Next(0, 20),
                        AudioFileURL = $"sample/item{counter}.mp3",
                        Status = EpisodeStatus.Approved
                    });
                    counter++;
                }
            }

            await context.SaveChangesAsync();

            var allEpisodes = await context.Episodes.ToListAsync();
            var subscribers = new[] { listener1, listener2 };
            foreach (var podcast in allPodcasts)
            {
                foreach (var listener in subscribers)
                {
                    if (!await context.Subscriptions.AnyAsync(s => s.UserID == listener.Id && s.PodcastID == podcast.PodcastID))
                    {
                        context.Subscriptions.Add(new Subscription
                        {
                            UserID = listener.Id,
                            PodcastID = podcast.PodcastID
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task<User> CreateUser(UserManager<User> userManager, string email, string fullName, string password, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                    throw new Exception($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

                await userManager.AddToRoleAsync(user, role);
            }
            else if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            return user;
        }
    }
}
