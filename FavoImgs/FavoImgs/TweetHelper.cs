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
        /// 해당 트윗에 포함된 모든 이미지 / 동영상을 찾아냄
        /// </summary>
        public static void GetMediaUris(CoreTweet.Status twt, ref List<DownloadItem> downloadItems)
        {
            // Twitter Video
            if (twt.ExtendedEntities != null)
            {
                foreach (var eachMedia in twt.ExtendedEntities.Media)
                {
                    if (eachMedia.VideoInfo != null)
                    {
                        foreach (var eachVideoVariant in eachMedia.VideoInfo.Variants)
                        {
                            Uri uri = eachVideoVariant.Url;
                            downloadItems.Add(new DownloadItem(twt.Id, twt.User.ScreenName, uri, uri.Segments.Last()));
                        }
                    }
                }
            }

            else if (twt.Entities.Media != null)
            {
                foreach (var url in twt.Entities.Media)
                {
                    Uri uri = url.MediaUrl;

                    IMediaProvider mediaProvider = null;

                    if (IsImageFile(uri.ToString()))
                    {
                        Uri newUri = new Uri(ModifyImageUri(uri.ToString()));

                        downloadItems.Add(new DownloadItem(twt.Id, twt.User.ScreenName, newUri, uri.Segments.Last()));
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
                                    downloadItems.Add(new DownloadItem(twt.Id, twt.User.ScreenName, eachUri, filename));
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

            if (twt.Entities.Urls != null)
            {
                foreach (var url in twt.Entities.Urls)
                {
                    Uri uri = new Uri(url.ExpandedUrl);

                    IMediaProvider mediaProvider = null;

                    if (IsImageFile(uri.ToString()))
                    {
                        downloadItems.Add(new DownloadItem(twt.Id, twt.User.ScreenName, uri, uri.Segments.Last()));
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
                                    downloadItems.Add(new DownloadItem(twt.Id, twt.User.ScreenName, eachUri, filename));
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

                    downloadItems.Add(new DownloadItem(twt.Id, twt.User.ScreenName, newUri, uri.Segments.Last()));
                }
            }
        }

        /// <summary>
        /// 이미지 주소를 통해 어떤 서비스의 이미지인지 찾아냄
        /// </summary>
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
            else if (uri.ToString().Contains("vine.co"))
            {
                mediaProvider = new Vine();
            }

            return mediaProvider;
        }

        private static bool IsImageFile(string uri)
        {
            string pattern = @"^.*\.(jpg|JPG|jpeg|JPEG|gif|GIF|png|PNG)$";
            return Regex.IsMatch(uri, pattern);
        }

        /// <summary>
        /// 이미지 주소를 수정할 필요가 있을 경우 수정
        /// </summary>
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