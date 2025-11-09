namespace Podcast_MVC.Models.ViewModels
{
    public class EpisodePodcasterViewModel
    {
        public Episode Episode { get; set; }
        public List<Comment> Comments { get; set; } = new();
    }
}
