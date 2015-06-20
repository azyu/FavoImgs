using FavoImgs.Data;
using System;
using System.IO;

namespace FavoImgs
{
    class PathHelper
    {
        public static string GetSubDirectoryName(Options options)
        {
            string retpath = String.Empty;

            switch (options.TweetSource)
            {
                default:
                case TweetSource.Favorites:
                    retpath = Path.Combine(options.DownloadPath, "favorites", options.ScreenName);
                    break;

                case TweetSource.Lists:
                    retpath = Path.Combine(options.DownloadPath, "lists", options.ScreenName, options.Slug);
                    break;

                case TweetSource.Tweets:
                    retpath = Path.Combine(options.DownloadPath, "tweets", options.ScreenName);
                    break;

                case TweetSource.Hashtag:
                    retpath = Path.Combine(options.DownloadPath, "hashtags", options.Hashtag);
                    break;

                /*
                case TweetSource.Search:
                    string dirname = String.Empty;

                    string[] optionStrings = options.Query.Split(' ');
                    if (optionStrings.Length > 0)
                        dirname = optionStrings[0];

                    retpath = Path.Combine(options.DownloadPath, "search", dirname);
                    break;
                 */
            }

            return retpath;
        }
    }
}
