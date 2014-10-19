using CoreTweet;
using FavoImgs.Data;
using FavoImgs.Resources;
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


        [STAThread]
        static int Main(string[] args)
        {
            TwitterClient tc = new TwitterClient();

            try
            {
                ShowAppInfo();

                Initialize();
                Settings.Load();
                Statistics.Initialize();

                return tc.Run(args);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteException(ex);
                Console.ReadLine();
                return 0;
            }
        }
    }
}