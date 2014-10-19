using FavoImgs.Data;
using System;
using System.Reflection;

namespace FavoImgs
{
    class Program
    {
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