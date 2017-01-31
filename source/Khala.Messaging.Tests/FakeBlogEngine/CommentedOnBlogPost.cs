using System;
using Khala.Messaging;

namespace FakeBlogEngine
{
    public class CommentedOnBlogPost : IPartitioned
    {
        public Guid PostId { get; set; }

        public Guid AuthorId { get; set; }

        public string Comment { get; set; }

        public DateTimeOffset CommentedAt { get; set; }

        string IPartitioned.PartitionKey => PostId.ToString();
    }
}
