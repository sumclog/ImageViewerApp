using MyImageViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MyImageViewer.Services
{
    public class FileService
    {
        // Поддерживаемые форматы изображений
        private readonly string[] _imageExtensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.tiff" };

        /// <summary>
        /// Получить все изображения из папки
        /// </summary>
        public string[] GetImageFiles(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return new string[0];

                // Ищем все поддерживаемые форматы
                List<string> allImages = new List<string>();
                foreach (string pattern in _imageExtensions)
                {
                    allImages.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
                }

                return allImages.OrderBy(f => Path.GetFileName(f)).ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка доступа к папке: {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// Загрузить список изображений в объекты ImageFile с фильтром
        /// </summary>
        public List<ImageFile> LoadImagesFromFolder(string folderPath, string searchFilter = "")
        {
            List<ImageFile> images = new List<ImageFile>();
            string[] filePaths = GetImageFiles(folderPath);

            foreach (string path in filePaths)
            {
                try
                {
                    string fileName = Path.GetFileName(path);

                    if (!string.IsNullOrWhiteSpace(searchFilter) &&
                        !fileName.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ImageFile imgFile = new ImageFile(path);

                    // Размеры будем получать позже
                    imgFile.Width = 0;
                    imgFile.Height = 0;

                    images.Add(imgFile);
                }
                catch
                {
                    continue;
                }
            }

            return images;
        }

        /// <summary>
        /// Получить все подпапки в указанной директории
        /// </summary>
        public string[] GetSubdirectories(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return new string[0];

                return Directory.GetDirectories(path);
            }
            catch
            {
                return new string[0];
            }
        }

        /// <summary>
        /// Проверить, существует ли папка
        /// </summary>
        public bool FolderExists(string path)
        {
            return Directory.Exists(path);
        }
    }
}
