using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Podcast_MVC.Models
{
    public class User : IdentityUser
    {
        [Required]
        public string FullName { get; set; }
        // Navigation Properties
        public ICollection<Podcast>? Podcasts { get; set; }
        public ICollection<Subscription>? Subscriptions { get; set; }

    }
}
