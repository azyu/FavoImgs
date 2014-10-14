using CoreTweet;
using FavoImgs.Data;
using FavoImgs.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace FavoImgs
{
    enum TweetSource
    {
        Invalid = 0,
        Tweets,
        Favorites,
        Lists,
    };

    class Program
    {
        private static void WriteException(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" - {0}", ex.Message);
            Console.ResetColor();
        }

        private static void Initialize()
        {
            try
            {
                if (!Data.TweetCache.IsCreated())
                    Data.TweetCache.Create();
            }
            catch
            {
                throw;
            }
        }

        private static string ShowTweet(CoreTweet.Status tweet)
        {
            return String.Format("{0} (@{1})  -- {2}\n{3}",
                tweet.User.Name, tweet.User.ScreenName, tweet.CreatedAt.LocalDateTime, tweet.Text);
        }

        private static void ShowAppInfo()
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;

            Console.WriteLine("FavoImgs {0}, Copyright (c) 2014, Azyu (@_uyza_)", version);
            Console.WriteLine("http://github.com/azyu/FavoImgs");
            Console.WriteLine("============================================================");
        }

        private static string ShowFolderBrowserDialog()
        {
            FolderBrowserDialog b = new FolderBrowserDialog();
            b.Description = Strings.SelectFolderToSave;

            if (b.ShowDialog() == DialogResult.OK)
                return b.SelectedPath;

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "FavoImgs");
        }

        private static void CheckDownloadPath()
        {
            if (String.IsNullOrEmpty(Settings.Current.DownloadPath))
            {
                string downloadPath = ShowFolderBrowserDialog();
                Settings.Current.DownloadPath = downloadPath;
            }

            Console.WriteLine(" [] {0}: {1}\n", Strings.DownloadPath, Settings.Current.DownloadPath);
        }


        private static bool IsImageFile(string uri)
        {
            string pattern = @"^.*\.(jpg|JPG|jpeg|JPEG|gif|GIF|png|PNG)$";
            return Regex.IsMatch(uri, pattern);
        }

        private static string ModifyImageUri(string uri)
        {
            string retval = String.Empty;

            // Twitter image
            if (uri.IndexOf("twimg.com") > 0)
            {
                retval = uri + ":orig";
            }

            return retval;
        }

        static CoreTweet.Tokens GetTwitterToken(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
        {
            Tokens tokens = null;

            if (String.IsNullOrEmpty(accessToken) || String.IsNullOrEmpty(accessTokenSecret))
            {
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

        private static CoreTweet.Core.ListedResponse<Status> GetTweets(
            Tokens tokens, Options options, Dictionary<string, object> arguments)
        {
            CoreTweet.Core.ListedResponse<Status> tweets = null;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" [] {0}", Strings.GetTweetsFromTwitter);
            Console.ResetColor();

            switch (options.TweetSource)
            {
                case TweetSource.Favorites:
                    arguments.Add("screen_name", options.ScreenName);
                    tweets = tokens.Favorites.List(arguments);
                    break;

                case TweetSource.Tweets:
                    if (options.ExcludeRetweets)
                    {
                        arguments.Add("include_rts", "false");
                    }

                    arguments.Add("screen_name", options.ScreenName);
                    tweets = tokens.Statuses.UserTimeline(arguments);
                    break;

                case TweetSource.Lists:
                    if (options.ExcludeRetweets)
                    {
                        arguments.Add("include_rts", "false");
                    }

                    arguments.Add("slug", options.Slug);
                    arguments.Add("owner_screen_name", options.ScreenName);
                    tweets = tokens.Lists.Statuses(arguments);
                    break;
            }

            return tweets;
        }

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                ShowAppInfo();

                Initialize();
                Settings.Load();
                Statistics.Initialize();
            }
            catch (Exception ex)
            {
                WriteException(ex);
                Console.ReadLine();
                return 0;
            }

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
            catch
            {
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
                
                if(String.IsNullOrEmpty(options.ScreenName))
                    options.ScreenName = myInfo.ScreenName;
            }
            catch (Exception ex)
            {
                WriteException(ex);
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
                    WriteException(ex);
                    return 1;
                }
            }

            long maxId = 0;
            if (options.Continue)
            {
                try
                {
                    maxId = TweetCache.GetOldestId();
                }
                catch (Exception ex)
                {
                    WriteException(ex);
                }
            }

            int left = 5;
            if (options.GetThemAll)
                left = Int32.MaxValue;

            bool bRunning = true;

            while (bRunning)
            {
                const int TWEET_COUNT_PER_API = 200;

                Dictionary<string, object> arguments = new Dictionary<string, object>();
                arguments.Add("count", TWEET_COUNT_PER_API);
                if (maxId != 0)
                    arguments.Add("max_id", maxId - 1);

                CoreTweet.Core.ListedResponse<Status> tweets = null;

                try
                {
                    tweets = GetTweets(tokens, options, arguments);
                }

                catch (WebException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(" [] {0}. {1}", ex.Message, Strings.TryAgain);
                    Console.ResetColor();

                    continue;
                }
                catch (TwitterException ex)
                {
                    // rate limit exceeded
                    if (ex.Status == (HttpStatusCode)429)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(" [] {0}", Strings.APIRateLimitExceeded);
                        Console.ResetColor();

                        Thread.Sleep(60 * 1000);
                        continue;
                    }
                    else
                    {
                        WriteException(ex);
                        Console.ReadLine();
                        return 1;
                    }
                }
                catch (Exception ex)
                {
                    WriteException(ex);
                    Console.ReadLine();
                    return 1;
                }

                foreach (var twt in tweets)
                {
                    if (maxId == 0)
                        maxId = twt.Id;
                    else
                        maxId = Math.Min(maxId, twt.Id);

                    string twtxt = ShowTweet(twt);
                    Console.WriteLine(twtxt);

                    string finalPath = PathHelper.GetSubDirectoryName(options);

                    if (!Directory.Exists(finalPath))
                        Directory.CreateDirectory(finalPath);

                    var downloadItems = new List<DownloadItem>();

                    try
                    {
                        TweetHelper.GetMediaUris(twt, ref downloadItems);
                    }
                    catch (Exception ex)
                    {
                        WriteException(ex);
                    }

                    var tempPath = Path.GetTempPath();

                    Statistics.Current.DownloadCount += downloadItems.Count;
                    for (int j = 0; j < downloadItems.Count; ++j)
                    {
                        if (TweetCache.IsImageTaken(downloadItems[j].TweetId, downloadItems[j].Uri.ToString()))
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine(" - {0} ({1})", downloadItems[j].Uri, Strings.AlreadyDownloaded);
                            Console.ResetColor();

                            continue;
                        }

                        string tempFilePath = Path.Combine(tempPath, downloadItems[j].FileName);
                        string realFilePath = Path.Combine(finalPath, downloadItems[j].FileName);
                        long tweetId = downloadItems[j].TweetId;
                        Uri uri = downloadItems[j].Uri;

                        DownloadFile(tempFilePath, realFilePath, tweetId, uri);
                    }

                    Statistics.Current.TweetCount += 1;
                    Console.WriteLine();
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(" [] API: {0}/{1}, Reset: {2}\n",
                    tweets.RateLimit.Remaining,
                    tweets.RateLimit.Limit,
                    tweets.RateLimit.Reset.LocalDateTime);
                Console.ResetColor();

                --left;

                if (left == 0 || tweets.Count == 0)
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

        private static bool ApplyOptions(Options options)
        {
            options.TweetSource = TweetSource.Favorites;

            if (!String.IsNullOrEmpty(options.ScreenName))
            {
                Console.WriteLine(" [Option] ScreenName: {0}", options.ScreenName);
            }

            if (!String.IsNullOrEmpty(options.Source))
            {
                var source = options.Source.ToLower();
                if (source == "list")
                {
                    options.TweetSource = TweetSource.Lists;

                    if (String.IsNullOrEmpty(options.Slug))
                    {
                        Console.WriteLine(" - Error: Missing required option 'Slug'!");
                        return false;
                    }

                    Console.WriteLine(" [Option] Source: List ({0})", options.Slug);
                }
                else if (source == "tweets")
                {
                    options.TweetSource = TweetSource.Tweets;
                    Console.WriteLine(" [Option] Source: Tweets");
                }
            }

            if (options.Continue)
            {
                Console.WriteLine(" [Option] Continue download from the oldest favorite tweet");
            }

            if (options.ResetDownloadPath)
            {
                Settings.Current.DownloadPath = String.Empty;
                Console.WriteLine(" [Option] Reset default download path");
            }

            if (options.GetThemAll)
            {
                Console.WriteLine(" [Option] Get them all!");
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

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(" - {0}", uri);
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

                Console.ResetColor();

                // 탐색기 섬네일 캐시 문제로 인하여 임시 폴더에서 파일을 받은 다음, 해당 폴더로 이동
                File.Move(tempFilePath, realFilePath);

                TweetCache.Add(tweetId, uri.ToString());

                Statistics.Current.DownloadedCount += 1;
            }
            catch (Exception ex)
            {
                WriteException(ex);
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }
    }
}