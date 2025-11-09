using static Podcast_MVC.Models.ViewModels.EpisodeCardViewModel;
using static Podcast_MVC.Models.ViewModels.PodcastWithEpisodesViewModel;


namespace Podcast_MVC.Models.ViewModels
{
    public class ListenerDashboardViewModel
    {
        public string SearchQuery { get; set; }
        public string SearchType { get; set; } = "episode";

        // Featured
        public List<EpisodeCardViewModel> TopEpisodes { get; set; } = new();
        public List<EpisodeCardViewModel> RecentEpisodes { get; set; } = new();

        // Unified list of podcasts + episodes
        public List<PodcastWithEpisodesViewModel> Podcasts { get; set; } = new();
    }
}
