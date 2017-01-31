using System;
using Khala.Messaging;

namespace FakeBlogEngine
{
    public class BlogPostCreated : IPartitioned
    {
        public Guid PostId { get; set; }

        public string Content { get; set; }

        public DateTimeOffset PostedAt { get; set; }

        string IPartitioned.PartitionKey => PostId.ToString();
    }
}
