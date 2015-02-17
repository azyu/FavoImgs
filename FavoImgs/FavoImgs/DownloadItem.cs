using System;

namespace FavoImgs
{
    public class DownloadItem
    {
        public DownloadItem(long tweetId, String screenName, Uri uri, String fileName)
        {
            TweetId = tweetId;
            ScreenName = screenName;
            Uri = uri;
            FileName = fileName;
        }

        public long TweetId { get; set; }
        public Uri Uri { get; set; }
        public String ScreenName { get; set; }
        public String FileName { get; set; }
    }
}
