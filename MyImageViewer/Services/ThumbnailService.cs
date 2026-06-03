using System;
using System.Data.SQLite; // NuGet пакет
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using MyImageViewer.Data;

namespace MyImageViewer.Services
{
    public class ThumbnailService
    {
        private const int ThumbnailSize = 150; // Размер миниатюры

        // Получить превью из кеша или сгенерировать
        public Image GetThumbnail(string filePath)
        {
            long lastModified = new FileInfo(filePath).LastWriteTime.Ticks;

            // 1. Пробуем найти в базе
            using (var conn = DatabaseContext.GetConnection())
            {
                conn.Open();
                string sql = "SELECT Thumbnail, LastModified FROM Thumbnails WHERE FilePath = @path";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@path", filePath);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            long dbTime = (long)reader["LastModified"];
                            // Если файл не менялся, берем из базы
                            if (dbTime == lastModified)
                            {
                                byte[] imgBytes = (byte[])reader["Thumbnail"];
                                return ByteArrayToImage(imgBytes);
                            }
                        }
                    }
                }
            }

            // 2. Если в базе нет или устарел - создаем превью
            Image thumb = CreateThumbnail(filePath);
            if (thumb != null)
            {
                SaveToCache(filePath, thumb, lastModified);
            }
            return thumb;
        }

        private Image CreateThumbnail(string path)
        {
            try
            {
                using (Image original = Image.FromFile(path))
                {
                    // Пропорциональное масштабирование
                    int w, h;
                    if (original.Width > original.Height)
                    {
                        w = ThumbnailSize;
                        h = (int)(original.Height * ((float)ThumbnailSize / original.Width));
                    }
                    else
                    {
                        h = ThumbnailSize;
                        w = (int)(original.Width * ((float)ThumbnailSize / original.Height));
                    }

                    return new Bitmap(original, new Size(w, h));
                }
            }
            catch { return null; }
        }

        private void SaveToCache(string path, Image img, long modTime)
        {
            byte[] imgBytes = ImageToByteArray(img);
            using (var conn = DatabaseContext.GetConnection())
            {
                conn.Open();
                // INSERT OR REPLACE обновит запись, если путь уже есть
                string sql = "INSERT OR REPLACE INTO Thumbnails (FilePath, Thumbnail, LastModified) VALUES (@p, @img, @mod)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@p", path);
                    cmd.Parameters.AddWithValue("@img", imgBytes);
                    cmd.Parameters.AddWithValue("@mod", modTime);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Хелперы для конвертации Картинка <-> Байты
        private byte[] ImageToByteArray(Image img)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }

        private Image ByteArrayToImage(byte[] arr)
        {
            using (MemoryStream ms = new MemoryStream(arr))
            {
                return Image.FromStream(ms);
            }
        }
    }
}