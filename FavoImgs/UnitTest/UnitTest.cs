using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void MimeHelperTest()
        {
            string actual = FavoImgs.MimeHelper.GetFileExtension("image/jpeg");

            Assert.AreEqual(".jpg", actual);
        }

        [TestMethod]
        public void PathHelper_FavoritesTest()
        {
            FavoImgs.Options options = new FavoImgs.Options();

            options.DownloadPath = @"D:\Download";
            options.TweetSource = FavoImgs.TweetSource.Favorites;
            options.ScreenName = "happy_naru";
            string actual = FavoImgs.PathHelper.GetSubDirectoryName(options);

            Assert.AreEqual(@"D:\Download\favorites\happy_naru", actual);
        }

        [TestMethod]
        public void PathHelper_ListsTest()
        {
            FavoImgs.Options options = new FavoImgs.Options();

            options.DownloadPath = @"D:\Download";
            options.TweetSource = FavoImgs.TweetSource.Lists;
            options.ScreenName = "iiotoko";
            options.Slug = "yaranaika";
            string actual = FavoImgs.PathHelper.GetSubDirectoryName(options);

            Assert.AreEqual(@"D:\Download\lists\iiotoko\yaranaika", actual);
        }

        [TestMethod]
        public void PathHelper_TweetsTest()
        {
            FavoImgs.Options options = new FavoImgs.Options();

            options.DownloadPath = @"D:\Download";
            options.TweetSource = FavoImgs.TweetSource.Tweets;
            options.ScreenName = "nozoeri";
            string actual = FavoImgs.PathHelper.GetSubDirectoryName(options);

            Assert.AreEqual(@"D:\Download\tweets\nozoeri", actual);
        }
    }
}
