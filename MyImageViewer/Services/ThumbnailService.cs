using System;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using MyImageViewer.Data;

namespace MyImageViewer.Services
{
    public class ThumbnailService
    {
        private const int ThumbnailSize = 150;

        public Image GetThumbnail(string filePath)
        {
            // Проверяем существование файла
            if (!File.Exists(filePath)) return null;

            long lastModified = new FileInfo(filePath).LastWriteTime.Ticks;

            try
            {
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
                                long dbTime = Convert.ToInt64(reader["LastModified"]);
                                // Если файл не менялся
                                if (dbTime == lastModified)
                                {
                                    byte[] imgBytes = (byte[])reader["Thumbnail"];
                                    return ByteArrayToImage(imgBytes);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Если ошибка БД, просто идем дальше генерировать превью
                System.Diagnostics.Debug.WriteLine($"Ошибка чтения из БД: {ex.Message}");
            }

            // 2. Если в базе нет или устарел/ошибка - создаем превью
            Image thumb = CreateThumbnail(filePath);
            if (thumb != null)
            {
                try
                {
                    SaveToCache(filePath, thumb, lastModified);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сохранения в БД: {ex.Message}");
                }
            }
            return thumb;
        }

        private Image CreateThumbnail(string path)
        {
            try
            {
                using (Image original = Image.FromFile(path))
                {
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

                    // Создаем новую битмап
                    Bitmap bmp = new Bitmap(w, h);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(original, 0, 0, w, h);
                    }
                    return bmp;
                }
            }
            catch
            {
                return null;
            }
        }

        private void SaveToCache(string path, Image img, long modTime)
        {
            try
            {
                byte[] imgBytes = ImageToByteArray(img);
                using (var conn = DatabaseContext.GetConnection())
                {
                    conn.Open();
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка кеширования: {ex.Message}");
            }
        }

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
            try
            {
                using (MemoryStream ms = new MemoryStream(arr))
                {
                    // ВАЖНО: Image.FromStream требует открытый поток.
                    // Но MemoryStream закрывается в using.
                    // Поэтому создаем КОПИЮ Bitmap, которая не зависит от потока.
                    return new Bitmap(ms);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}