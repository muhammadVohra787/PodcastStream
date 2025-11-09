namespace Podcast_MVC.Models.ViewModels
{
    public class ViewEpisodeViewModel
    {
        public int EpisodeID { get; set; }
        public string Title { get; set; }
        public string PodcastTitle { get; set; }
        public string PodcastDescription { get; set; }
        public double Duration { get; set; }
        public string AudioFileURL { get; set; }
        public int PodcastId { get; set; }
        public int PlayCount { get; set; }
        public string CreatorName { get; set; }
        public int CreatorEpisodeCount { get; set; }
        public DateTime ReleaseDate { get; set; }

        public bool IsSubcribed { get; set; }

        public List<Comment> Comments { get; set; }
    }
}
