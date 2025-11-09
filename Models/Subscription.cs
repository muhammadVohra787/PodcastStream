using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podcast_MVC.Models
{
    public class Subscription
    {
        [Key]
        public int SubscriptionID { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserID { get; set; }

        [Required]
        [ForeignKey("Podcast")]
        public int PodcastID { get; set; }

        public DateTime SubscribedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public User? User { get; set; }
        public Podcast? Podcast { get; set; }
    }
}
