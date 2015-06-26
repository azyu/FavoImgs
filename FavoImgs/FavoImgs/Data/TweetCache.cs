using System;
using System.IO;
using System.Windows.Forms;

#if MONO
using Mono.Data.Sqlite;
using SqlCon = Mono.Data.Sqlite.SqliteConnection;
using SqlCmd = Mono.Data.Sqlite.SqliteCommand;
#else
using System.Data.SQLite;
using SqlCon = System.Data.SQLite.SQLiteConnection;
using SqlCmd = System.Data.SQLite.SQLiteCommand;
#endif

namespace FavoImgs.Data
{
    public class TweetCache
    {
        private static readonly string cachePath = Path.Combine(
             Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Cache.db");

        public static bool IsCreated()
        {
            return File.Exists(cachePath);
        }

        public static void Create()
        {
            try
            {
                if (!File.Exists(cachePath))
                    SqlCon.CreateFile(cachePath);

                string connstr = String.Format("Data Source={0};Version=3", cachePath);
                SqlCon conn = new SqlCon(connstr);
                conn.Open();

                SqlCmd cmd = new SqlCmd(conn);

                string query = String.Empty;

                query =
                    "CREATE TABLE MediaUris (" +
                    "Id bigint NOT NULL," +
                    "Uri nvarchar(256) NOT NULL," +
                    "State int NOT NULL," +
                    "PRIMARY KEY (Id, Uri));";

                cmd.CommandText = query;
                cmd.ExecuteNonQuery();

                conn.Close();
            }
            catch
            {
                throw;
            }
        }

        public static bool IsExist(long Id)
        {
            try
            {
                string connstr = String.Format("Data Source={0};Version=3", cachePath);
                SqlCon conn = new SqlCon(connstr);
                conn.Open();

                SqlCmd cmd = new SqlCmd(conn);

                string query = String.Empty;

                query = @"SELECT count(*) FROM [MediaUris] WHERE [Id] = @Id";
                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("@Id", Id);

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return (count != 0);
            }
            catch
            {
                throw;
            }
        }

        public static long GetLastestId()
        {
            try
            {
                string connstr = String.Format("Data Source={0};Version=3", cachePath);
                SqlCon conn = new SqlCon(connstr);
                conn.Open();

                SqlCmd cmd = new SqlCmd(conn);

                string query = String.Empty;

                query = @"SELECT Id FROM [MediaUris] order by Id DESC limit 1";
                cmd.CommandText = query;

                Int64 Id = Convert.ToInt64(cmd.ExecuteScalar());
                return Id;
            }
            catch
            {
                throw;
            }
        }

        public static bool ResetImageTakenState()
        {
            try
            {
                string connstr = String.Format("Data Source={0};Version=3", cachePath);
                SqlCon conn = new SqlCon(connstr);
                conn.Open();

                SqlCmd cmd = new SqlCmd(conn);

                string query = String.Empty;

                query = @"UPDATE [MediaUris] SET [State] = 0";
                cmd.CommandText = query;
                int rowcount = cmd.ExecuteNonQuery();

                return (rowcount != 0);
            }
            catch
            {
                throw;
            }
        }

        public static bool IsImageTaken(long Id, string uri)
        {
            try
            {
                string connstr = String.Format("Data Source={0};Version=3", cachePath);
                SqlCon conn = new SqlCon(connstr);
                conn.Open();

                SqlCmd cmd = new SqlCmd(conn);

                string query = String.Empty;

                query = @"SELECT [State] FROM [MediaUris] WHERE [Id] = @Id and [Uri] = @Uri";
                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("@Id", Id);
                cmd.Parameters.AddWithValue("@Uri", uri);

                int state = Convert.ToInt32(cmd.ExecuteScalar());
                return (state != 0);
            }
            catch
            {
                throw;
            }
        }

        public static long GetOldestId()
        {
            try
            {
                string connstr = String.Format("Data Source={0};Version=3", cachePath);
                SqlCon conn = new SqlCon(connstr);
                conn.Open();

                SqlCmd cmd = new SqlCmd(conn);

                string query = String.Empty;

                query = @"SELECT Id FROM [MediaUris] order by Id ASC limit 1";
                cmd.CommandText = query;

                Int64 Id = Convert.ToInt64(cmd.ExecuteScalar());
                return Id;
            }
            catch
            {
                throw;
            }
        }

        public static void Add(long Id, string uri)
        {
            try
            {
                string connstr = String.Format("Data Source={0};Version=3", cachePath);
                SqlCon conn = new SqlCon(connstr);
                conn.Open();

                SqlCmd cmd = new SqlCmd(conn);

                string query = String.Empty;

                query = @"INSERT INTO [MediaUris] ([Id], [Uri], [State])
                VALUES (@Id, @Uri, 1)";
                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("@Id", Id);
                cmd.Parameters.AddWithValue("@Uri", uri);

                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
