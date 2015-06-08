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

                case TweetSource.Search:
                    string dirname = String.Empty;

                    string[] optionStrings = options.Query.Split(' ');
                    if (optionStrings.Length > 0)
                        dirname = optionStrings[0];

                    retpath = Path.Combine(options.DownloadPath, "search", dirname);
                    break;
            }

            /*
            switch (convention)
            {
                default:
                case DirectoryNamingConvention.None:
                    retpath = options.DownloadPath;
                    break;

                case DirectoryNamingConvention.Date:
                    retpath = Path.Combine(options.DownloadPath, createdAt.LocalDateTime.ToString("yyyyMM"));
                    break;

                case DirectoryNamingConvention.ScreenName:
                    retpath = Path.Combine(options.DownloadPath, screenName);
                    break;

                case DirectoryNamingConvention.Date_ScreenName:
                    retpath = Path.Combine(options.DownloadPath, createdAt.LocalDateTime.ToString("yyyyMM"), screenName);
                    break;

                case DirectoryNamingConvention.ScreenName_Date:
                    retpath = Path.Combine(options.DownloadPath, screenName, createdAt.LocalDateTime.ToString("yyyyMM"));
                    break;
            }
            */

            return retpath;
        }
    }
}
