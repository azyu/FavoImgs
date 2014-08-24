using CoreTweet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FavoImgs
{
    internal class TweetHelper
    {
        /// <summary>
        /// 해당 트윗에 포함된 모든 이미지의 url을 찾아냄
        /// </summary>
        public static void GetMediaUris(CoreTweet.Status twt, ref List<DownloadItem> downloadItems)
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
                            catch
                            {
                                throw;
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

        private static IMediaProvider GetMediaProvider(Uri uri)
        {
            IMediaProvider mediaProvider = null;

            if (uri.ToString().Contains("twitter.com"))
            {
                mediaProvider = new TwitterMp4();
            }
            else if (uri.ToString().Contains("twitpic.com"))
            {
                mediaProvider = new TwitPic();
            }
            else if (uri.ToString().Contains("yfrog.com"))
            {
                mediaProvider = new Yfrog();
            }
            else if (uri.ToString().Contains("tistory.com/image"))
            {
                mediaProvider = new Tistory();
            }
            else if (uri.ToString().Contains("tistory.com/original"))
            {
                mediaProvider = new Tistory();
            }
            else if (uri.ToString().Contains("p.twipple.jp"))
            {
                mediaProvider = new Twipple();
            }

            return mediaProvider;
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
    }
}