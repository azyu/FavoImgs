using FavoImgs.Data;
using System;
using System.IO;

namespace FavoImgs
{
    class PathHelper
    {
        public static string GetSubDirectoryName(string basePath, DirectoryNamingConvention convention, DateTimeOffset createdAt, string screenName)
        {
            string retpath = String.Empty;
            switch (convention)
            {
                default:
                case DirectoryNamingConvention.None:
                    retpath = basePath;
                    break;

                case DirectoryNamingConvention.Date:
                    retpath = Path.Combine(basePath, createdAt.LocalDateTime.ToString("yyyyMM"));
                    break;

                case DirectoryNamingConvention.ScreenName:
                    retpath = Path.Combine(basePath, screenName);
                    break;

                case DirectoryNamingConvention.Date_ScreenName:
                    retpath = Path.Combine(basePath, createdAt.LocalDateTime.ToString("yyyyMM"), screenName);
                    break;

                case DirectoryNamingConvention.ScreenName_Date:
                    retpath = Path.Combine(basePath, screenName, createdAt.LocalDateTime.ToString("yyyyMM"));
                    break;
            }

            return retpath;
        }
    }
}
