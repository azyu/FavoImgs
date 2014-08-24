namespace FavoImgs
{
    internal class Statistics
    {
        public static Statistics Current { get; set; }

        public static void Initialize()
        {
            Current = new Statistics();
        }

        public int TweetCount { get; set; }
        public int DownloadCount { get; set; }
        public int DownloadedCount { get; set; }
    }
}