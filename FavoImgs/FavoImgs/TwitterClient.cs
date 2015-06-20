using CoreTweet;
using FavoImgs.Data;
using FavoImgs.Resources;
using FavoImgs.Security;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace FavoImgs
{
    enum GroupBy
    {
        None = 0,
        ScreenName, // Default
    };

    enum TweetSource
    {
        Invalid = 0,
        Tweets,
        Favorites,
        Lists,
        Hashtag,
        // Search,
    };

    class TwitterClient
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string ShowTweet(CoreTweet.Status tweet)
        {
            return String.Format("{0} (@{1})  -- {2}\n{3}",
                tweet.User.Name, tweet.User.ScreenName, tweet.CreatedAt.LocalDateTime, tweet.Text);
        }

        private string ShowFolderBrowserDialog()
        {
            FolderBrowserDialog b = new FolderBrowserDialog();
            b.Description = Strings.SelectFolderToSave;

            if (b.ShowDialog() == DialogResult.OK)
                return b.SelectedPath;

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "FavoImgs");
        }

        private void CheckDownloadPath()
        {
            if (String.IsNullOrEmpty(Settings.Current.DownloadPath))
            {
                string downloadPath = ShowFolderBrowserDialog();
                Settings.Current.DownloadPath = downloadPath;
            }

            Console.WriteLine(" [] {0}: {1}\n", Strings.DownloadPath, Settings.Current.DownloadPath);
        }

        private bool IsImageFile(string uri)
        {
            string pattern = @"^.*\.(jpg|JPG|jpeg|JPEG|gif|GIF|png|PNG)$";
            return Regex.IsMatch(uri, pattern);
        }

        private string ModifyImageUri(string uri)
        {
            string retval = String.Empty;

            // Twitter image
            if (uri.IndexOf("twimg.com") > 0)
            {
                retval = uri + ":orig";
            }

            return retval;
        }

        private CoreTweet.Tokens GetTwitterToken(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
        {
            Tokens tokens = null;

            if (String.IsNullOrEmpty(accessToken) || String.IsNullOrEmpty(accessTokenSecret))
            {
                Console.WriteLine(Strings.NeedAuthentication);
                logger.Info(Strings.NeedAuthentication);

                var session = OAuth.Authorize(consumerKey, consumerSecret);
                var url = session.AuthorizeUri;
                Process.Start(url.ToString());

                string pin = String.Empty;
                Console.Write("ENTER PIN: ");
                pin = Console.ReadLine();

                try
                {
                    tokens = session.GetTokens(pin);
                }
                catch
                {
                    throw;
                }

                Settings.Current.AccessToken = RijndaelEncryption.EncryptRijndael(tokens.AccessToken);
                Settings.Current.AccessTokenSecret = RijndaelEncryption.EncryptRijndael(tokens.AccessTokenSecret);
                Settings.Current.Save();
            }
            else
            {
                tokens = Tokens.Create(consumerKey, consumerSecret, accessToken, accessTokenSecret);
            }

            return tokens;
        }

        private CoreTweetResponse GetResponse(
            Tokens tokens, Options options, Dictionary<string, object> arguments)
        {
            CoreTweetResponse response = new CoreTweetResponse();

            ConsoleHelper.WriteColoredLine(ConsoleColor.Yellow, " [] {0}", Strings.GetTweetsFromTwitter);
            logger.Info(Strings.GetTweetsFromTwitter);

            switch (options.TweetSource)
            {
                case TweetSource.Favorites:
                    {
                        arguments.Add("screen_name", options.ScreenName);
                        var result = tokens.Favorites.List(arguments);
                        response.Tweets = result.ToList();
                        response.RateLimit = result.RateLimit;
                    }
                    break;

                case TweetSource.Tweets:
                    {
                        if (options.ExcludeRetweets)
                        {
                            arguments.Add("include_rts", "false");
                        }

                        arguments.Add("screen_name", options.ScreenName);
                        var result = tokens.Statuses.UserTimeline(arguments);
                        response.Tweets = result.ToList();
                        response.RateLimit = result.RateLimit;
                    }
                    break;

                case TweetSource.Lists:
                    {
                        if (options.ExcludeRetweets)
                        {
                            arguments.Add("include_rts", "false");
                        }

                        arguments.Add("slug", options.Slug);
                        arguments.Add("owner_screen_name", options.ScreenName);
                        var result = tokens.Lists.Statuses(arguments);
                        response.Tweets = result.ToList();
                        response.RateLimit = result.RateLimit;
                    }
                    break;


                case TweetSource.Hashtag:
                    {
                        String query = String.Format("{0} filter:images -filter:retweets", options.Hashtag);
                        arguments.Add("q", query);
                        var result = tokens.Search.Tweets(arguments);
                        response.Tweets = result.ToList();
                        response.RateLimit = result.RateLimit;
                    }

                    /*
                case TweetSource.Search:
                {
                    arguments.Add("q", options.Query);
                    var result = tokens.Search.Tweets(arguments);
                    response.Tweets = result.ToList();
                    response.RateLimit = result.RateLimit;
                }
                     * */
                    break;
            }

            return response;
        }

        public int Run(string[] args)
        {
            logger.Info("Arguments: {0}", String.Join(" ", args));

            var options = new Options();

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                if (!ApplyOptions(options))
                    return 0;
            }
            else
            {
                return 0;
            }

            CheckDownloadPath();

            string consumerKey = Settings.Current.ConsumerKey;
            string consumerSecret = Settings.Current.ConsumerSecret;
            string accessToken = Settings.Current.AccessToken;
            string accessTokenSecret = Settings.Current.AccessTokenSecret;

            try
            {
                if (!String.IsNullOrEmpty(accessToken))
                    accessToken = RijndaelEncryption.DecryptRijndael(accessToken);

                if (!String.IsNullOrEmpty(accessTokenSecret))
                    accessTokenSecret = RijndaelEncryption.DecryptRijndael(accessTokenSecret);
            }
            catch (Exception ex)
            {
                logger.ErrorException(Strings.CannotReadOAuthToken, ex);
                Console.WriteLine("{0}", Strings.CannotReadOAuthToken);
                Console.ReadLine();
                return 1;
            }

            Tokens tokens = null;
            UserResponse myInfo = null;

            try
            {
                tokens = GetTwitterToken(consumerKey, consumerSecret, accessToken, accessTokenSecret);
                myInfo = tokens.Account.VerifyCredentials();

                if (String.IsNullOrEmpty(options.ScreenName))
                    options.ScreenName = myInfo.ScreenName;
            }
            catch (Exception ex)
            {
                logger.ErrorException(ex.Message, ex);
                ConsoleHelper.WriteException(ex);
                Console.ReadLine();
                return 1;
            }

            if (String.IsNullOrEmpty(options.DownloadPath))
            {
                options.DownloadPath = Settings.Current.DownloadPath;
            }

            if (!Directory.Exists(options.DownloadPath))
            {
                try
                {
                    Directory.CreateDirectory(options.DownloadPath);
                    if (!Directory.Exists(options.DownloadPath))
                    {
                        Console.WriteLine("{0}", Strings.CannotCreateDownloadFolder);
                        return 1;
                    }

                }
                catch (Exception ex)
                {
                    logger.ErrorException(ex.Message, ex);
                    ConsoleHelper.WriteException(ex);
                    return 1;
                }
            }


            const int TWEET_COUNT_PER_API = 200;

            long maxId = 0;

            // 200 x 16 = 3200
            int left = 16;

            bool bRunning = true;
            while (bRunning)
            {
                Dictionary<string, object> arguments = new Dictionary<string, object>();
                arguments.Add("count", TWEET_COUNT_PER_API);
                if (maxId != 0)
                    arguments.Add("max_id", maxId - 1);

                CoreTweetResponse response = null;

                try
                {
                    response = GetResponse(tokens, options, arguments);
                    Console.WriteLine(" [] {0}: {1}", Strings.NumberOfFetchedTweets, response.Tweets.Count);
                    logger.Info("{0}: {1}", Strings.NumberOfFetchedTweets, response.Tweets.Count);
                }

                catch (WebException ex)
                {
                    ConsoleHelper.WriteColoredLine(ConsoleColor.Yellow, " [] {0}. {1}", ex.Message, Strings.TryAgain);
                    continue;
                }
                catch (TwitterException ex)
                {
                    // rate limit exceeded
                    if (ex.Status == (HttpStatusCode)429)
                    {
                        ConsoleHelper.WriteColoredLine(ConsoleColor.Yellow, " [] {0}", Strings.APIRateLimitExceeded);
                        Thread.Sleep(60 * 1000);
                        continue;
                    }
                    else
                    {
                        logger.ErrorException(ex.Message, ex);
                        ConsoleHelper.WriteException(ex);
                        Console.ReadLine();
                        return 1;
                    }
                }
                catch (Exception ex)
                {
                    logger.ErrorException(ex.Message, ex);
                    ConsoleHelper.WriteException(ex);
                    Console.ReadLine();
                    return 1;
                }

                foreach (var twt in response.Tweets)
                {
                    DownloadFilesFromTweet(options, twt);

                    if (maxId == 0)
                        maxId = twt.Id;
                    else
                        maxId = Math.Min(maxId, twt.Id);

                    Statistics.Current.TweetCount += 1;
                }

                ConsoleHelper.WriteColoredLine(ConsoleColor.Yellow,
                    " [] API: {0}/{1}, Reset: {2}\n",
                    response.RateLimit.Remaining, response.RateLimit.Limit, response.RateLimit.Reset.LocalDateTime);

                --left;

                if (left == 0 || response.Tweets.Count == 0)
                    bRunning = false;
            }

            Console.WriteLine("{0}", Strings.WorkComplete);
            Console.WriteLine();

            Console.WriteLine(" - Tweet(s): {0}", Statistics.Current.TweetCount);
            Console.WriteLine(" - Media Url(s): {0}", Statistics.Current.DownloadCount);
            Console.WriteLine(" - Downloaded file(s): {0}", Statistics.Current.DownloadedCount);

            Settings.Current.Save();
            return 0;
        }

        private void DownloadFilesFromTweet(Options options, Status twt)
        {
            string twtxt = ShowTweet(twt);
            Console.WriteLine(twtxt);

            string downloadRootPath = PathHelper.GetSubDirectoryName(options);

            if (!Directory.Exists(downloadRootPath))
                Directory.CreateDirectory(downloadRootPath);

            var downloadItems = new List<DownloadItem>();

            try
            {
                TweetHelper.GetMediaUris(twt, ref downloadItems);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteException(ex);
            }

            var tempPath = Path.GetTempPath();

            Statistics.Current.DownloadCount += downloadItems.Count;
            for (int j = 0; j < downloadItems.Count; ++j)
            {
                if (TweetCache.IsImageTaken(downloadItems[j].TweetId, downloadItems[j].Uri.ToString()))
                {
                    ConsoleHelper.WriteColoredLine(ConsoleColor.DarkRed, " - {0} ({1})", downloadItems[j].Uri, Strings.AlreadyDownloaded);
                    continue;
                }

                string tempFilePath = Path.Combine(tempPath, downloadItems[j].FileName);
                string targetFilePath = String.Empty;
                switch (options.GroupBy)
                {
                    default:
                    case GroupBy.None:
                        targetFilePath = downloadItems[j].FileName;
                        break;

                    case GroupBy.ScreenName:
                        if (options.TweetSource != TweetSource.Tweets)
                        {
                            var upperFilePath = Path.Combine(downloadRootPath, twt.User.ScreenName);
                            if (!Directory.Exists(upperFilePath))
                                Directory.CreateDirectory(upperFilePath);

                            targetFilePath = Path.Combine(twt.User.ScreenName, downloadItems[j].FileName);
                        }
                        else
                        {
                            targetFilePath = downloadItems[j].FileName;
                        }
                        break;
                }

                string realFilePath = Path.Combine(downloadRootPath, targetFilePath);
                long tweetId = downloadItems[j].TweetId;
                Uri uri = downloadItems[j].Uri;

                DownloadFile(tempFilePath, realFilePath, tweetId, uri);
            }

            Console.WriteLine();
        }

        private static bool ApplyOptions(Options options)
        {
            options.GroupBy = GroupBy.ScreenName;
            options.TweetSource = TweetSource.Favorites;

            if (!String.IsNullOrEmpty(options.ScreenName))
            {
                Console.WriteLine(" [Option] ScreenName: {0}", options.ScreenName);
            }

            if (!String.IsNullOrEmpty(options.Group))
            {
                var source = options.Group.ToLower();

                switch (source)
                {
                    case "none":
                        options.GroupBy = GroupBy.None;
                        break;

                    default:
                    case "screen_name":
                        options.GroupBy = GroupBy.ScreenName;
                        break;
                }
            }

            if (!String.IsNullOrEmpty(options.Source))
            {
                var source = options.Source.ToLower();
                if (source == "list")
                {
                    options.TweetSource = TweetSource.Lists;

                    if (String.IsNullOrEmpty(options.Slug))
                    {
                        const string msg = " - Error: Missing required option 'Slug'!";

                        Console.WriteLine(msg);
                        logger.Error(msg);

                        return false;
                    }

                    Console.WriteLine(" [Option] Source: List ({0})", options.Slug);
                }
                else if (source == "tweets")
                {
                    options.TweetSource = TweetSource.Tweets;
                    Console.WriteLine(" [Option] Source: Tweets");
                }
                else if (source == "hashtag")
                {
                    options.TweetSource = TweetSource.Hashtag;

                    if (String.IsNullOrEmpty(options.Hashtag))
                    {
                        string msg = String.Format(" - Error: {0}", Strings.HashtagMissing);

                        Console.WriteLine(msg);
                        logger.Error(msg);

                        return false;
                    }

                    if (options.Hashtag[0] != '#')
                    {
                        string msg = String.Format(" - Error: {0}", Strings.HashtagNotBeginsWithSharp);

                        Console.WriteLine(msg);
                        logger.Error(msg);

                        return false;
                    }

                    if (options.Hashtag.Contains(" "))
                    {
                        string msg = String.Format(" - Error: {0}", Strings.HashtagInvalid);

                        Console.WriteLine(msg);
                        logger.Error(msg);

                        return false;
                    }

                    Console.WriteLine(" [Option] Source: Hashtag ({0})", options.Hashtag);
                }
                /*
                else if (source == "search")
                {
                    options.TweetSource = TweetSource.Search;

                    if (String.IsNullOrEmpty(options.Query))
                    {
                        const string msg = " - Error: Missing required option 'query'!";

                        Console.WriteLine(msg);
                        logger.Error(msg);

                        return false;
                    }

                    Console.WriteLine(" [Option] Source: Search");
                }
                */
                else
                {
                    options.TweetSource = TweetSource.Tweets;
                    Console.WriteLine(" [Option] Source: Favorites");
                }
            }

            if (options.ResetDownloadPath)
            {
                Settings.Current.DownloadPath = String.Empty;
                Console.WriteLine(" [Option] Reset default download path");
            }

            if (options.ExcludeRetweets)
            {
                Console.WriteLine(" [Option] Exclude retweets");
            }

            if (!String.IsNullOrEmpty(options.DownloadPath))
            {
                Settings.Current.DownloadPath = options.DownloadPath;
            }

            Console.WriteLine();
            return true;
        }

        private static void DownloadFile(string tempFilePath, string realFilePath, long tweetId, Uri uri)
        {
            try
            {
                WebClient wc = new WebClient();

                ConsoleHelper.WriteColoredLine(ConsoleColor.DarkCyan, " - {0}", uri);
                wc.DownloadFile(uri, tempFilePath);

                // 확장자가 붙지 않았을 경우, Content-Type으로 추론
                if (!Path.HasExtension(tempFilePath))
                {
                    string extension = MimeHelper.GetFileExtension(wc.ResponseHeaders["Content-Type"]);
                    string newFilePath = String.Format("{0}{1}", tempFilePath, extension);

                    File.Move(tempFilePath, newFilePath);
                    tempFilePath = newFilePath;
                    realFilePath = String.Format("{0}{1}", realFilePath, extension);
                }


                // 탐색기 섬네일 캐시 문제로 인하여 임시 폴더에서 파일을 받은 다음, 해당 폴더로 이동
                File.Move(tempFilePath, realFilePath);

                TweetCache.Add(tweetId, uri.ToString());

                Statistics.Current.DownloadedCount += 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteException(ex);
                logger.InfoException(String.Format("{0} - {1}", ex.Message, uri.ToString()), ex);

                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }
    }
}