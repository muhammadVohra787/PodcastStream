namespace Podcast_MVC.Models.ViewModels
{
    public class EpisodeApprovalViewModel
    {
        public int EpisodeID { get; set; }
        public string Title { get; set; }
        public string PodcastTitle { get; set; }

        public string CreatorEmail { get; set; }
        public string CreatorName { get; set; }
        public double Duration { get; set; }
        public DateTime ReleaseDate { get; set; }
    }
}
