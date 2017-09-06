namespace FakeBlogEngine
{
    using System;
    using Khala.Messaging;

    public sealed class BlogPostCreated : IPartitioned
    {
        public Guid PostId { get; set; }

        public string Content { get; set; }

        public DateTimeOffset PostedAt { get; set; }

        string IPartitioned.PartitionKey => PostId.ToString();
    }
}
