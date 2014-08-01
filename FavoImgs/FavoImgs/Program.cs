using CoreTweet;
using FavoImgs.Data;
using FavoImgs.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace FavoImgs
{
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

		private static IMediaProvider GetMediaProvider(Uri uri)
		{
			IMediaProvider mediaProvider = null;

			if (uri.ToString ().Contains ("twitter.com")) {
				mediaProvider = new TwitterMp4 ();
			} else if (uri.ToString ().Contains ("twitpic.com")) {
				mediaProvider = new TwitPic ();
			} else if (uri.ToString ().Contains ("yfrog.com")) {
				mediaProvider = new Yfrog ();
			} else if (uri.ToString ().Contains ("tistory.com/image")) {
				mediaProvider = new Tistory ();
			} else if (uri.ToString ().Contains ("tistory.com/original")) {
				mediaProvider = new Tistory ();
			} else if (uri.ToString ().Contains ("p.twipple.jp")) {
				mediaProvider = new Twipple ();
			}

			return mediaProvider;
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
            b.Description = "Select folder to save...";

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

            Console.WriteLine("[] Download Path: {0}\n", Settings.Current.DownloadPath);
        }

        private static string GetSubDirectoryName(string basePath, DirectoryNamingConvention convention, DateTimeOffset createdAt, string screenName)
        {
            string retpath = String.Empty;
            switch (convention)
            {
                default:
                case DirectoryNamingConvention.None:
                    retpath = basePath;
                    break;

                case DirectoryNamingConvention.Date:
                    retpath = Path.Combine(basePath, createdAt.LocalDateTime.ToString("yyyyMMdd"));
                    break;

                case DirectoryNamingConvention.ScreenName:
                    retpath = Path.Combine(basePath, screenName);
                    break;

                case DirectoryNamingConvention.Date_ScreenName:
                    retpath = Path.Combine(basePath, createdAt.LocalDateTime.ToString("yyyyMMdd"), screenName);
                    break;

                case DirectoryNamingConvention.ScreenName_Date:
                    retpath = Path.Combine(basePath, screenName, createdAt.LocalDateTime.ToString("yyyyMMdd"));
                    break;
            }

            return retpath;
        }

        private static bool IsImageFile(string uri)
        {
            string pattern = @"^.*\.(jpg|JPG|gif|GIF|png|PNG)$";
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

        private static void GetMediaUris(CoreTweet.Status twt, ref List<DownloadItem> downloadItems)
        {
            if (twt.Entities.Urls != null)
            {
                foreach (var url in twt.Entities.Urls)
                {
                    Uri uri = url.ExpandedUrl;
                    
                    IMediaProvider mediaProvider = null;

                    if (IsImageFile(uri.ToString()))
                    {
                        downloadItems.Add(new DownloadItem(twt.Id, uri, uri.Segments.Last()));
                    }
                    else
                    {
						mediaProvider = GetMediaProvider(uri);

                        if (mediaProvider != null)
                        {
                            try
                            {
                                List<Uri> mediaUris = mediaProvider.GetUri(uri);

                                foreach (var eachUri in mediaUris)
                                {
                                    string filename = eachUri.Segments.Last();
                                    downloadItems.Add(new DownloadItem(twt.Id, eachUri, filename));
                                }
                            }
                            catch(Exception ex)
                            {
                                WriteException(ex);
                            }
                        }
                    }
                }
            }

            if (twt.ExtendedEntities != null && twt.ExtendedEntities.Media != null)
            {
                foreach (var media in twt.ExtendedEntities.Media)
                {
                    Uri uri = media.MediaUrl;

                    if (!IsImageFile(uri.ToString()))
                        continue;

                    Uri newUri = new Uri(ModifyImageUri(uri.ToString()));

                    downloadItems.Add(new DownloadItem(twt.Id, newUri, uri.Segments.Last()));
                }
            }
        }

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                ShowAppInfo();

                Initialize();
                Settings.Load();
            }
            catch(Exception ex)
            {
                WriteException(ex);
                Console.ReadLine();
                return 0;
            }

            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
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

                Console.WriteLine();
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
                Console.WriteLine("Cannot read OAuth Token!");
                Console.ReadLine();
                return 1;
            }

            Tokens tokens = null;
            try
            {
                tokens = GetTwitterToken(consumerKey, consumerSecret, accessToken, accessTokenSecret);
            }
            catch (Exception ex)
            {
                WriteException(ex);
                Console.ReadLine();
                return 1;
            }

            string downloadPath = Settings.Current.DownloadPath;
            if (!Directory.Exists(downloadPath))
                Directory.CreateDirectory(downloadPath);

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
            
            while(bRunning)
            {
                Dictionary<string, object> arguments = new Dictionary<string, object>();
                arguments.Add("count", 200);
                if (maxId != 0)
                    arguments.Add("max_id", maxId - 1);

                CoreTweet.Core.ListedResponse<Status> favorites = null;

                try
                {
                    favorites = tokens.Favorites.List(arguments);
                }
                catch(TwitterException ex)
                {
                    // rate limit exceeded
                    if(ex.Status == (HttpStatusCode)429)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(" [] Rate limit exceeded. Try again after 60 seconds.");
                        Console.ResetColor();
                    }

                    Thread.Sleep(60 * 1000);
                    continue;
                }
                catch (Exception ex)
                {
                    WriteException(ex);
                    Console.ReadLine();
                    return 1;
                }

                foreach (var twt in favorites)
                {
                    if (maxId == 0)
                        maxId = twt.Id;
                    else
                        maxId = Math.Min(maxId, twt.Id);

                    string twtxt = ShowTweet(twt);
                    Console.WriteLine(twtxt);

                    string dir = GetSubDirectoryName(
                        Settings.Current.DownloadPath,
                        Settings.Current.DirectoryNamingConvention,
                        twt.CreatedAt, twt.User.ScreenName);

                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    var downloadItems = new List<DownloadItem>();

                    GetMediaUris(twt, ref downloadItems);

                    var tempPath = Path.GetTempPath();

                    for (int j = 0; j < downloadItems.Count; ++j)
                    {
                        if (TweetCache.IsImageTaken(downloadItems[j].TweetId, downloadItems[j].Uri.ToString()))
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine(" - {0} (already downloaded. skip...)", downloadItems[j].Uri);
                            Console.ResetColor();

                            continue;
                        }

                        string tempFilePath = Path.Combine(tempPath, downloadItems[j].FileName);
                        string realFilePath = Path.Combine(downloadPath, downloadItems[j].FileName);

                        try
                        {
                            WebClient wc = new WebClient();

                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.WriteLine(" - {0}", downloadItems[j].Uri);
                            wc.DownloadFile(downloadItems[j].Uri, tempFilePath);
                            Console.ResetColor();

                            // 탐색기 섬네일 캐시 문제로 인하여 임시 폴더에서 파일을 받은 다음, 해당 폴더로 이동
                            File.Move(tempFilePath, realFilePath);

                            TweetCache.Add(downloadItems[j].TweetId, downloadItems[j].Uri.ToString());
                        }
                        catch (Exception ex)
                        {
                            WriteException(ex);
                            if (File.Exists(tempFilePath))
                                File.Delete(tempFilePath);
                        }
                    }

                    Console.WriteLine();
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(" [] Limit: {0}/{1}, Reset: {2}\n",
                    favorites.RateLimit.Remaining,
                    favorites.RateLimit.Limit,
                    favorites.RateLimit.Reset.LocalDateTime);
                Console.ResetColor();

                --left;

                if (left == 0 || favorites.Count == 0)
                    bRunning = false;
            }

            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();

            Settings.Current.Save();
            return 0;
        }
    }
}
