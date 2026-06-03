using System;
using System.Collections.Generic;
using System.Text;

namespace MyImageViewer.Models
{
    public class ImageFile
    {
        // Полный путь к файлу на диске (например: C:\Images\photo.jpg)
        public string FullPath { get; set; }

        // Только имя файла (например: photo.jpg)
        public string FileName { get; set; }

        // Ширина изображения в пикселях
        public int Width { get; set; }

        // Высота изображения в пикселях
        public int Height { get; set; }

        // Размер файла в байтах
        public long Size { get; set; }

        // Конструктор для удобного создания объекта
        public ImageFile(string fullPath)
        {
            FullPath = fullPath;
            FileName = Path.GetFileName(fullPath);

            // Сразу получаем размер файла
            FileInfo fi = new FileInfo(fullPath);
            Size = fi.Length;
        }
    }
}
