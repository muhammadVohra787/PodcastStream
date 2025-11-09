using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podcast_MVC.Models
{
    public enum EpisodeStatus
    {
        Pending,
        Approved, 
        Rejected    
    }

    public class Episode
    {
        [Key]
        public int EpisodeID { get; set; }

        [Required]
        [ForeignKey("Podcast")]
        public int PodcastID { get; set; }

        [Required]
        public string Title { get; set; }

        public DateTime ReleaseDate { get; set; } = DateTime.UtcNow;

        public double Duration { get; set; }  // in minutes

        public int PlayCount { get; set; } = 0;

        public string? AudioFileURL { get; set; }

        // Navigation Property
        public Podcast? Podcast { get; set; }

        [Required]
        public EpisodeStatus Status { get; set; } = EpisodeStatus.Pending;

        public string getStatus ()
        {
            switch (Status)
            {
                case EpisodeStatus.Pending:
                    return "Pending";
                case EpisodeStatus.Approved:
                    return "Approved";
                case EpisodeStatus.Rejected:
                    return "Rejected";
                default:
                    return "Unknown";
            }
        }

    }
}
