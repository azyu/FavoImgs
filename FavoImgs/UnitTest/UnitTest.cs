using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FavoImgs
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void MimeHelperTest()
        {
            string actual = MimeHelper.GetFileExtension("image/jpeg");

            Assert.AreEqual(".jpg", actual);
        }

        [TestMethod]
        public void PathHelper_FavoritesTest()
        {
            Options options = new Options();

            options.DownloadPath = @"D:\Download";
            options.TweetSource = TweetSource.Favorites;
            options.ScreenName = "happy_naru";
            string actual = PathHelper.GetSubDirectoryName(options);

            Assert.AreEqual(@"D:\Download\favorites\happy_naru", actual);
        }

        [TestMethod]
        public void PathHelper_ListsTest()
        {
            Options options = new Options();

            options.DownloadPath = @"D:\Download";
            options.TweetSource = TweetSource.Lists;
            options.ScreenName = "iiotoko";
            options.Slug = "yaranaika";
            string actual = PathHelper.GetSubDirectoryName(options);

            Assert.AreEqual(@"D:\Download\lists\iiotoko\yaranaika", actual);
        }

        [TestMethod]
        public void PathHelper_TweetsTest()
        {
            Options options = new Options();

            options.DownloadPath = @"D:\Download";
            options.TweetSource = TweetSource.Tweets;
            options.ScreenName = "nozoeri";
            string actual = PathHelper.GetSubDirectoryName(options);

            Assert.AreEqual(@"D:\Download\tweets\nozoeri", actual);
        }

        [TestMethod]
        public void PathHelper_HashtagTest()
        {
            Options options = new Options();

            options.DownloadPath = @"D:\Download";
            options.TweetSource = TweetSource.Hashtag;
            options.Hashtag = "#東條希生誕祭2015";
            string actual = PathHelper.GetSubDirectoryName(options);

            Assert.AreEqual(@"D:\Download\hashtags\#東條希生誕祭2015", actual);
        }

        [TestMethod]
        public void MediaProvider_TwippleTest()
        {
            Uri uri = new Uri("http://p.twipple.jp/2d1sn");

            PrivateType pt = new PrivateType(typeof(TweetHelper));

            IMediaProvider mediaProvider = (IMediaProvider)pt.InvokeStatic("GetMediaProvider", new object[] { uri });
            Assert.AreEqual(typeof(Twipple), mediaProvider.GetType());
        }

        [TestMethod]
        public void MediaProvider_VineTest()
        {
            Uri uri = new Uri("https://mtc.cdn.vine.co/r/videos_h264high/28D3E01F201216824363924803584_SW_WEBM_1433087743394462be3b271.mp4");

            PrivateType pt = new PrivateType(typeof(TweetHelper));

            IMediaProvider mediaProvider = (IMediaProvider)pt.InvokeStatic("GetMediaProvider", new object[] { uri });
            Assert.AreEqual(typeof(Vine), mediaProvider.GetType());
        }
    }
}
