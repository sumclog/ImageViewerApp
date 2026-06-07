using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace MyImageViewer.Services
{
    public class ImageService
    {
        /// <summary>
        /// Загрузить изображение из файла с автоматической коррекцией ориентации (EXIF)
        /// </summary>
        public Bitmap LoadImage(string filePath)
        {
            try
            {
                // Загружаем как Bitmap
                Bitmap bmp = new Bitmap(filePath);

                // Сразу проверяем ориентацию (EXIF)
                FixOrientation(bmp);

                return bmp;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Получить размеры изображения в текстовом формате
        /// </summary>
        public string GetImageDimensions(Bitmap image)
        {
            if (image == null) 
                return "Нет изображения";

            return $"{image.Width} × {image.Height} px";
        }

        /// <summary>
        /// Получить информацию о файле изображения
        /// </summary>
        public string GetImageInfo(string filePath)
        {
            try
            {
                FileInfo fi = new FileInfo(filePath);
                string sizeStr = FormatFileSize(fi.Length);
                return $"{Path.GetFileName(filePath)} ({sizeStr})";
            }
            catch
            {
                return "Информация недоступна";
            }
        }

        /// <summary>
        /// Повернуть изображение на указанный угол
        /// </summary>
        public Bitmap RotateImage(Bitmap image, int angle)
        {
            if (image == null) 
                return null;

            // Создаем копию, чтобы не повредить оригинал
            Bitmap rotated = new Bitmap(image);

            // Определяем тип поворота
            RotateFlipType type = RotateFlipType.RotateNoneFlipNone;

            if (angle == 90) 
                type = RotateFlipType.Rotate90FlipNone;
            else if (angle == 180) 
                type = RotateFlipType.Rotate180FlipNone;
            else if (angle == 270) 
                type = RotateFlipType.Rotate270FlipNone;
            else if (angle == -90)
                type = RotateFlipType.Rotate270FlipNone;

            rotated.RotateFlip(type);
            return rotated;
        }

        /// <summary>
        /// Масштабировать изображение на весь экран (сохраняя пропорции)
        /// </summary>
        public Bitmap ScaleImageToFit(Bitmap image, int maxWidth, int maxHeight)
        {
            if (image == null)
                return null;

            float scale = Math.Min((float)maxWidth / image.Width, (float)maxHeight / image.Height);
            int newWidth = (int)(image.Width * scale);
            int newHeight = (int)(image.Height * scale);

            return new Bitmap(image, new Size(newWidth, newHeight));
        }

        /// <summary>
        /// Обработка EXIF (ориентация)
        /// Часто фото с телефона имеют неправильную ориентацию
        /// </summary>
        private void FixOrientation(Bitmap img)
        {
            if (img == null || img.PropertyIdList.Length == 0) 
                return;

            try
            {
                int orientationId = 0x0112; // ID свойства Orientation в EXIF
                if (img.PropertyIdList.Contains(orientationId))
                {
                    PropertyItem pItem = img.GetPropertyItem(orientationId);
                    int orientation = pItem.Value[0];

                    switch (orientation)
                    {
                        case 2: img.RotateFlip(RotateFlipType.RotateNoneFlipX); break;
                        case 3: img.RotateFlip(RotateFlipType.Rotate180FlipNone); break;
                        case 4: img.RotateFlip(RotateFlipType.RotateNoneFlipY); break;
                        case 5: img.RotateFlip(RotateFlipType.Rotate90FlipX); break;
                        case 6: img.RotateFlip(RotateFlipType.Rotate90FlipNone); break;
                        case 7: img.RotateFlip(RotateFlipType.Rotate270FlipX); break;
                        case 8: img.RotateFlip(RotateFlipType.Rotate270FlipNone); break;
                    }
                    // Сбрасываем ориентацию, чтобы не повернуть дважды
                    img.RemovePropertyItem(orientationId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обработки EXIF: {ex.Message}");
            }
        }

        /// <summary>
        /// Форматировать размер файла в читаемый вид
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
        public Size GetImageSize(string filePath)
        {
            try
            {
                using (Image img = Image.FromFile(filePath))
                {
                    return new Size(img.Width, img.Height);
                }
            }
            catch
            {
                return Size.Empty;
            }
        }
    }
}
