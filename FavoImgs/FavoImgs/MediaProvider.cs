using CoreTweet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace FavoImgs
{
    interface IMediaProvider
    {
        List<Uri> GetUri(Uri uri);
    }

    class TwitterMp4 : IMediaProvider
    {
        public List<Uri> GetUri(Uri uri)
        {
            List<Uri> retval = new List<Uri>();

            string htmlCode = String.Empty;
            try
            {
                var htmlwc = new WebClient();
                htmlCode = htmlwc.DownloadString(uri);
            }
            catch
            {
                throw;
            }

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);

            var nodes = doc.DocumentNode.SelectNodes("//source");
            if (nodes == null)
                return retval;

            foreach (var link in nodes)
            {
                if (!link.Attributes.Any(x => x.Name == "type" && x.Value == "video/mp4"))
                    continue;

                var attributes = link.Attributes.Where(x => x.Name == "video-src").ToList();
                foreach (var att in attributes)
                {
                    retval.Add(new Uri(att.Value));
                }
            }

            return retval;
        }
    }

    class TwitPic : IMediaProvider
    {
        public List<Uri> GetUri(Uri uri)
        {
            List<Uri> retval = new List<Uri>();

            Uri newUrl = new Uri(String.Format("{0}/full", uri.ToString()));
            string htmlCode = String.Empty;
            try
            {
                var htmlwc = new WebClient();
                htmlCode = htmlwc.DownloadString(newUrl);
            }
            catch
            {
                throw;
            }

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);

            
            var nodes = doc.DocumentNode.SelectNodes("//*[@id='media-full']/img");
            if (nodes == null)
                return retval;

            foreach (var link in nodes)
            {
                var attributes = link.Attributes.Where(x => x.Name == "src").ToList();
                foreach (var att in attributes)
                {
                    retval.Add(new Uri(att.Value));
                }
            }

            return retval;
        }
    }

    class Yfrog : IMediaProvider
    {
        public List<Uri> GetUri(Uri uri)
        {
            List<Uri> retval = new List<Uri>();

            Uri newUrl = new Uri(String.Format("http://twitter.yfrog.com/z/{0}", uri.Segments.Last()));
            string htmlCode = String.Empty;
            try
            {
                var htmlwc = new WebClient();
                htmlCode = htmlwc.DownloadString(newUrl);
            }
            catch
            {
                throw;
            }

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);

            var nodes = doc.DocumentNode.SelectNodes("//*[@id='the-image']/a/img");
            if (nodes == null)
                return retval;

            foreach (var link in nodes)
            {
                var attributes = link.Attributes.Where(x => x.Name == "src").ToList();
                foreach (var att in attributes)
                {
                    retval.Add(new Uri(att.Value));
                }
            }

            return retval;
        }
    }

}

