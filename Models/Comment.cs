using Amazon.DynamoDBv2.DataModel;
using System;

namespace Podcast_MVC.Models
{
    [DynamoDBTable("PodcastEpisodesComments")]
    public class Comment
    {
        // Partition Key: EpisodeID (so all comments for same episode are grouped)
        [DynamoDBHashKey]
        public int EpisodeID { get; set; }

        // Sort Key: CommentID (unique per comment)
        [DynamoDBRangeKey]
        public string CommentID { get; set; } = Guid.NewGuid().ToString();

        [DynamoDBProperty]
        public int PodcastID { get; set; }

        [DynamoDBProperty]
        public string UserID { get; set; }

        [DynamoDBProperty]
        public string Text { get; set; }

        [DynamoDBProperty]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string UserName { get; set; }
        }
}
