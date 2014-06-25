using System;
using System.IO;
using System.Xml.Serialization;

namespace FavoImgs.Data
{
    public static class XmlFile
    {
        public static T Read<T>(string filePath) where T : new()
        {
            if (String.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            T obj = new T();
            StreamReader file = null;

            try
            {
                file = new System.IO.StreamReader(filePath);

                var serializer = new XmlSerializer(typeof(T));
                obj = (T)serializer.Deserialize(file);
            }
            finally
            {
                if (file != null)
                    file.Close();
            }

            return obj;
        }

        public static void Write<T>(this T obj, string filePath) where T : new()
        {
            if (String.IsNullOrEmpty(filePath))
                throw new FileNotFoundException("Invalid file path", filePath);

            try
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? String.Empty;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            catch
            {
                throw;
            }

            StreamWriter file = null;
            var serializer = new XmlSerializer(typeof(T));

            try
            {
                file = new System.IO.StreamWriter(filePath);
                serializer.Serialize(file, obj);
            }
            finally
            {
                if (file != null)
                    file.Close();
            }
        }
    }
}
