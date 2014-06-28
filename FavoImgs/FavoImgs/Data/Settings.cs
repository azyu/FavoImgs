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
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Settings.xml");

        public static Settings GetDefaultSettings()
        {
            return new Settings
            {
                ConsumerKey = "Q2ClVMX9YvWAiWRfJbIbZr0DO",
                ConsumerSecret = "cHXGkKc5gmWaYFvA6Zvr56GQLzrus2pOYblupP6rI2famIlhgD",
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
