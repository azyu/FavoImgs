using System;
using System.IO;
using System.Xml.Serialization;

namespace FavoImgs.Data
{
    [Serializable]
    public enum DirectoryNamingConvention
    {
        [XmlEnumAttribute(Name = "None")]
        None = 0,

        [XmlEnumAttribute(Name = "Date")]
        Date,

        [XmlEnumAttribute(Name = "ScreenName")]
        ScreenName,

        [XmlEnumAttribute(Name = "Date_ScreenName")]
        Date_ScreenName,

        [XmlEnumAttribute(Name = "ScreenName_Date")]
        ScreenName_Date,
    }

    public class Settings
    {
        private static readonly string filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FavoImgs",
            "Settings.xml");

        public static Settings GetDefaultSettings()
        {
            return new Settings
            {
                ConsumerKey = "A22Di4+GfBVUKToPK1IHWXIAFTXrBVURULDxA8B5AL8=",
                ConsumerSecret = "r9i+A4CfFJPu8dqI9mw68wWMcUhJm1kK4uXxryLKHBPOykPmbOYlpkD/aNyp/Oej3X+vl+UHKdESoBvfJ1oELw==",
                DownloadPath = String.Empty,
                DirectoryNamingConvention = DirectoryNamingConvention.None,
            };
        }

        public static Settings Current { get; set; }
        public static void Load()
        {
            try
            {
                Current = XmlFile.Read<Settings>(filePath);
            }
            catch
            {
                Current = GetDefaultSettings();
            }
        }

        public void Save()
        {
            try
            {
                this.Write(filePath);
            }
            catch
            {
            }
        }

        public string AccessToken { get; set; }
        public string AccessTokenSecret { get; set; }
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string DownloadPath { get; set; }
        public DirectoryNamingConvention DirectoryNamingConvention { get; set; }
    }
}
