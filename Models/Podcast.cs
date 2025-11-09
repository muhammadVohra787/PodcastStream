using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podcast_MVC.Models
{
    public class Podcast
    {
        [Key]
        public int PodcastID { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }

        [BindNever]
        [ForeignKey("Creator")]
        public string? CreatorID { get; set; } = string.Empty; // prevent null issues

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public User? Creator { get; set; }
        public ICollection<Episode>? Episodes { get; set; }
        public ICollection<Subscription>? Subscriptions { get; set; }

        public override string ToString()
        {
            return $"PodcastID: {PodcastID}, Title: {Title}, Description: {Description}, CreatorID: {CreatorID}, CreatedDate: {CreatedDate}";
        }
    }
}
