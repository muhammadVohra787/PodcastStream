namespace Podcast_MVC.Models.ViewModels
{
    public class EpisodeCardViewModel
    {
        public int EpisodeID { get; set; }
        public string Title { get; set; }
        public string PodcastTitle { get; set; }

        public int PodcastId { get; set; }
        public int PlayCount { get; set; }
        public DateTime ReleaseDate { get; set; }
    }
}
