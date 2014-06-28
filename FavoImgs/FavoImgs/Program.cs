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
using System.Windows.Forms;

namespace FavoImgs
{
    public class DownloadItem
    {
        public DownloadItem(Uri uri, String fileName)
        {
            Uri = uri;
            FileName = fileName;
        }

        public Uri Uri { get; set; }
        public String FileName { get; set; }
    }

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
            Console.WriteLine("http://github.com/azyu/FavoImgs");
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

                    if (IsImageFile(uri.ToString()))
                    {
                        downloadItems.Add(new DownloadItem(uri, uri.Segments.Last()));
                    }
                    else
                    {
                        if (uri.ToString().Contains("twitter.com"))
                        {
                            string htmlCode = String.Empty;
                            try
                            {
                                var htmlwc = new WebClient();
                                htmlCode = htmlwc.DownloadString(uri);
                            }
                            catch (WebException)
                            {
                                continue;
                            }

                            var doc = new HtmlAgilityPack.HtmlDocument();
                            doc.LoadHtml(htmlCode);

                            var nodes = doc.DocumentNode.SelectNodes("//source");
                            if (nodes == null) continue;

                            foreach (var link in nodes)
                            {
                                if (!link.Attributes.Any(x => x.Name == "type" && x.Value == "video/mp4"))
                                    continue;

                                var attributes = link.Attributes.Where(x => x.Name == "video-src").ToList();
                                foreach (var att in attributes)
                                {
                                    var attUri = new Uri(att.Value);
                                    downloadItems.Add(new DownloadItem(attUri, attUri.Segments.Last()));
                                }
                            }
                        }
                        else if(uri.ToString().Contains("twitpic.com"))
                        {
                            string htmlCode = String.Empty;
                            try
                            {
                                var htmlwc = new WebClient();
                                htmlCode = htmlwc.DownloadString(uri + "/full");
                            }
                            catch (WebException)
                            {
                                continue;
                            }

                            var doc = new HtmlAgilityPack.HtmlDocument();
                            doc.LoadHtml(htmlCode);

                            var nodes = doc.DocumentNode.SelectNodes("//*[@id='media-full']/img");
                            if (nodes == null) continue;

                            foreach (var link in nodes)
                            {
                                var attributes = link.Attributes.Where(x => x.Name == "src").ToList();
                                foreach (var att in attributes)
                                {
                                    var attUri = new Uri(att.Value);
                                    downloadItems.Add(new DownloadItem(attUri, attUri.Segments.Last()));
                                }
                            }
                        }
                        else if(uri.ToString().Contains("yfrog.com"))
                        {
                            Uri newUrl = new Uri(String.Format("http://twitter.yfrog.com/z/{0}", uri.Segments.Last()));
                            string htmlCode = String.Empty;
                            try
                            {
                                var htmlwc = new WebClient();
                                htmlCode = htmlwc.DownloadString(newUrl);
                            }
                            catch (WebException)
                            {
                                continue;
                            }

                            var doc = new HtmlAgilityPack.HtmlDocument();
                            doc.LoadHtml(htmlCode);

                            var nodes = doc.DocumentNode.SelectNodes("//*[@id='the-image']/a/img");
                            if (nodes == null) continue;

                            foreach (var link in nodes)
                            {
                                var attributes = link.Attributes.Where(x => x.Name == "src").ToList();
                                foreach (var att in attributes)
                                {
                                    var attUri = new Uri(att.Value);
                                    downloadItems.Add(new DownloadItem(attUri, attUri.Segments.Last()));
                                }
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

                    downloadItems.Add(new DownloadItem(uri, uri.Segments.Last()));
                }
            }
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

            for (int i = 0; i < 10; ++i)
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

                    /*
                    if (!TweetCache.IsExist(twt.Id))
                        TweetCache.Add(twt);
                    else if (TweetCache.IsImageTaken(twt.Id))
                    {
                        Console.WriteLine(" - already taken image. pass...\n");
                        continue;
                    }
                    */

                    string dir = GetSubDirectoryName(
                        Settings.Current.DownloadPath,
                        Settings.Current.DirectoryNamingConvention,
                        twt.CreatedAt, twt.User.ScreenName);

                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    bool isAllDownloaded = true;

                    var downloadItems = new List<DownloadItem>();

                    GetMediaUris(twt, ref downloadItems);

                    var tempPath = Path.GetTempPath();

                    for (int j = 0; j < downloadItems.Count; ++j)
                    {
                        try
                        {
                            WebClient wc = new WebClient();
                            string tempFilePath = Path.Combine(tempPath, downloadItems[j].FileName);
                            string realFilePath = Path.Combine(downloadPath, downloadItems[j].FileName);

                            Console.WriteLine(" - Downloading... {0}", downloadItems[j].Uri);
                            wc.DownloadFile(downloadItems[j].Uri, tempFilePath);

                            // 탐색기 섬네일 캐시 문제로 인하여 임시 폴더에서 파일을 받은 다음, 해당 폴더로 이동
                            File.Move(tempFilePath, realFilePath);
                        }
                        catch (Exception ex)
                        {
                            isAllDownloaded = false;
                            Console.WriteLine(ex.Message);
                        }
                    }

                    // if( isAllDownloaded )
                        // TweetCache.SetImageTaken(twt.Id);
    
                    Console.WriteLine();
                }

                Console.WriteLine("Limit: {0}/{1}, Reset: {2}",
                    favorites.RateLimit.Remaining,
                    favorites.RateLimit.Limit,
                    favorites.RateLimit.Reset.LocalDateTime);

            }

            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();

            Settings.Current.Save();
            return 0;
        }
    }
}
