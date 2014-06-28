using System;

namespace FavoImgs
{
    public class DownloadItem
    {
        public DownloadItem(long tweetId, Uri uri, String fileName)
        {
            TweetId = tweetId;
            Uri = uri;
            FileName = fileName;
        }

        public long TweetId { get; set; }
        public Uri Uri { get; set; }
        public String FileName { get; set; }
    }
}
