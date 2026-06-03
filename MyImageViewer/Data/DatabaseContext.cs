using System;
using System.Data.SQLite; // Не забудь using
using System.IO;

namespace MyImageViewer.Data
{
    public class DatabaseContext
    {
        // Путь к файлу базы данных (будет лежать рядом с exe)
        private const string DbName = "ImageCache.db";
        private const string ConnectionString = $"Data Source={DbName};Version=3;";

        public static void Initialize()
        {
            if (!File.Exists(DbName))
            {
                SQLiteConnection.CreateFile(DbName);
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();
                    string sql = @"
                        CREATE TABLE Thumbnails (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                            FilePath TEXT UNIQUE,      -- Путь к файлу (уникальный ключ)
                            Thumbnail BLOB,            -- Сама картинка в байтах
                            LastModified INTEGER       -- Дата изменения файла (для проверки актуальности)
                        )";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(ConnectionString);
        }
    }
}