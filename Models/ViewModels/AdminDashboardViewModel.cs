namespace Podcast_MVC.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Summary counts
        public int PodcasterCount { get; set; }
        public int TotalPodcasts { get; set; }
        public int TotalEpisodes { get; set; }

        // Episode approval status for chart
        public int PendingEpisodes { get; set; }
        public int ApprovedEpisodes { get; set; }
        public int RejectedEpisodes { get; set; }

        // Users by role for chart
        public int AdminCount { get; set; }
        public int ListenerCount { get; set; }
    }
}
