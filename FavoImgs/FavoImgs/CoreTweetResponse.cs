using CoreTweet;
using System.Collections.Generic;

namespace FavoImgs
{
    class CoreTweetResponse
    {
        public List<Status> Tweets { get; set; }

        public string Json { get; set; }
        public RateLimit RateLimit { get; set; }
    }
}
