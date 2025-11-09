namespace Podcast_MVC.Models.ViewModels
{
    public class PodcastWithEpisodesViewModel
    {
        public int PodcastID { get; set; }
        public string Title { get; set; }
        public List<EpisodeCardViewModel> Episodes { get; set; } = new();
    }
}
