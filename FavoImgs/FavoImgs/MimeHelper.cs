namespace FavoImgs
{
    public class MimeHelper
    {
        public static string GetFileExtension(string mimeType)
        {
            string result;
            object value;

            var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"MIME\Database\Content Type\" + mimeType, false);
            value = key != null ? key.GetValue("Extension", null) : null;
            result = value != null ? value.ToString() : string.Empty;

            return result;
        }
    }
}