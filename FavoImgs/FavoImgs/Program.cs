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
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace FavoImgs
{
    class Program
    {
        private static readonly string dataPath = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FavoImgs");

        private static void Initialize()
        {
            try
            {
                InitializeDataDirectory();

                if (!Data.TweetCache.IsCreated())
                    Data.TweetCache.Create();
            }
            catch
            {
                throw;
            }
        }

        private static void InitializeDataDirectory()
        {
            if (String.IsNullOrEmpty(dataPath))
                throw new DirectoryNotFoundException();

            try
            {
                if (!Directory.Exists(dataPath))
                    Directory.CreateDirectory(dataPath);
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
            // Console.WriteLine("http://github.com/azyu/FavoImgs");
            Console.WriteLine("============================================================");
            Console.WriteLine();
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
            switch(convention)
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
                retval =  uri + ":orig";
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

        [STAThread]
        static int Main(string[] args)
        {
            ShowAppInfo();
            Initialize();
            
            Settings.Load();
            CheckDownloadPath();

            string consumerKey = Settings.Current.ConsumerKey;
            string consumerSecret = Settings.Current.ConsumerSecret;
            string accessToken = Settings.Current.AccessToken;
            string accessTokenSecret = Settings.Current.AccessTokenSecret;

            try
            {
                if (!String.IsNullOrEmpty(consumerKey))
                    consumerKey = RijndaelEncryption.DecryptRijndael(consumerKey);

                if (!String.IsNullOrEmpty(consumerSecret))
                    consumerSecret = RijndaelEncryption.DecryptRijndael(consumerSecret);

                if (!String.IsNullOrEmpty(accessToken))
                    accessToken = RijndaelEncryption.DecryptRijndael(accessToken);

                if (!String.IsNullOrEmpty(accessTokenSecret))
                    accessTokenSecret = RijndaelEncryption.DecryptRijndael(accessTokenSecret);
            }
            catch
            {
                Console.WriteLine("Cannot read OAuth Token!");
                return 1;
            }

            Tokens tokens = null;
            try
            {
                tokens = GetTwitterToken(consumerKey, consumerSecret, accessToken, accessTokenSecret);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }

            string downloadPath = Settings.Current.DownloadPath;
            if (!Directory.Exists(downloadPath))
                Directory.CreateDirectory(downloadPath);

            long maxId = 0;

            for (int i = 0; i < 5; ++i)
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
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
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

                    if (!TweetCache.IsExist(twt.Id))
                        TweetCache.Add(twt);
                    else if (TweetCache.IsImageTaken(twt.Id))
                    {
                        Console.WriteLine(" - already taken image. pass...\n");
                        continue;
                    }

                    string dir = GetSubDirectoryName(
                        Settings.Current.DownloadPath,
                        Settings.Current.DirectoryNamingConvention,
                        twt.CreatedAt, twt.User.ScreenName);

                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    if( twt.Entities.Urls != null )
                    {
                        foreach (var url in twt.Entities.Urls)
                        {
                            WebClient wc = new WebClient();
                            Uri uri = url.ExpandedUrl;

                            if (!IsImageFile(uri.ToString()))
                                continue;

                            Console.WriteLine(" - Downloading... {0} (Url)", uri.ToString());

                            try
                            {
                                wc.DownloadFile(uri, Path.Combine(dir, uri.Segments.Last()));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }

                    if( twt.Entities.Media != null )
                    {
                        foreach (var media in twt.Entities.Media)
                        {
                            WebClient wc = new WebClient();
                            Uri uri = media.MediaUrl;

                            if (!IsImageFile(uri.ToString()))
                                continue;

                            Console.WriteLine(" - Downloading... {0} (Twitter image)", uri.ToString());

                            try
                            {
                                string newuri = ModifyImageUri(uri.ToString());
                                wc.DownloadFile(newuri, Path.Combine(dir, uri.Segments.Last()));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }

                    TweetCache.SetImageTaken(twt.Id);
                    Console.WriteLine();
                }

                Console.WriteLine("Limit: {0}/{1}, Reset: {2}",
                    favorites.RateLimit.Remaining,
                    favorites.RateLimit.Limit,
                    favorites.RateLimit.Reset.LocalDateTime.ToString());

            }

            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();

            Settings.Current.Save();
            return 0;
        }
    }
}
