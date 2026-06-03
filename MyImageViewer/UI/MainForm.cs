using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MyImageViewer.Models;
using MyImageViewer.Services;

namespace MyImageViewer.UI
{
    public partial class MainForm : Form
    {
        private FileService _fileService;
        private ThumbnailService _thumbnailService;
        private List<ImageFile> _currentImages;
        private string _currentFolder;

        public MainForm()
        {
            InitializeComponent();
            InitializeServices();
        }

        private void InitializeServices()
        {
            _fileService = new FileService();
            _thumbnailService = new ThumbnailService();
            _currentImages = new List<ImageFile>();

            // Инициализация BackgroundWorker для загрузки миниатюр
            thumbWorker.DoWork += ThumbWorker_DoWork;
            thumbWorker.ProgressChanged += ThumbWorker_ProgressChanged;
            thumbWorker.RunWorkerCompleted += ThumbWorker_RunWorkerCompleted;

            // Загружаем папки при загрузке формы
            this.Load += MainForm_Load;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Инициализируем TreeView со всеми логическими дисками
                InitializeTreeView();

                // Подписываемся на событие раскрытия узлов для ленивой загрузки
                treeFolders.BeforeExpand += TreeFolders_BeforeExpand;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки папок: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Инициализировать TreeView со всеми логическими дисками компьютера
        /// Использует "ленивую загрузку" (Lazy Loading) для экономии ресурсов
        /// </summary>
        private void InitializeTreeView()
        {
            treeFolders.Nodes.Clear();

            try
            {
                // Получаем все логические диски (C:\, D:\, E:\ и т.д.)
                DriveInfo[] drives = DriveInfo.GetDrives();

                foreach (DriveInfo drive in drives)
                {
                    try
                    {
                        // Проверяем, что диск готов (не вставлен ли DVD и т.д.)
                        if (!drive.IsReady)
                            continue;

                        // Создаем узел для диска
                        string driveName = drive.Name; // Например "C:\"
                        TreeNode driveNode = new TreeNode(driveName)
                        {
                            Tag = driveName
                        };

                        // Добавляем пустой узел-заглушку для отображения "плюсика" расширения
                        // Это позволит TreeView показать, что можно раскрыть узел
                        driveNode.Nodes.Add(new TreeNode("...загрузка..."));

                        treeFolders.Nodes.Add(driveNode);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка инициализации диска {drive.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении логических дисков: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик события расширения узла в TreeView (BeforeExpand)
        /// Реализует "ленивую загрузку" папок при раскрытии узла
        /// </summary>
        private void TreeFolders_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            try
            {
                // Получаем путь из Tag узла
                string folderPath = e.Node.Tag?.ToString();
                if (string.IsNullOrEmpty(folderPath))
                    return;

                // Проверяем, не загружена ли уже папка (если больше чем 1 узел - значит уже загружена)
                // Пропускаем, если узел уже содержит реальные подпапки
                if (e.Node.Nodes.Count > 1 ||
                    (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text != "...загрузка..."))
                {
                    return;
                }

                // Очищаем узел от заглушки
                e.Node.Nodes.Clear();

                // Загружаем подпапки
                LoadSubfoldersLazy(e.Node, folderPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при расширении узла: {ex.Message}");
            }
        }

        /// <summary>
        /// Загрузить подпапки для узла (ленивая загрузка)
        /// Скрывает системные и скрытые папки
        /// </summary>
        private void LoadSubfoldersLazy(TreeNode parentNode, string parentPath)
        {
            try
            {
                // Проверяем, существует ли папка
                if (!_fileService.FolderExists(parentPath))
                    return;

                // Получаем все подпапки
                string[] subdirs = _fileService.GetSubdirectories(parentPath);

                foreach (string subdirPath in subdirs)
                {
                    try
                    {
                        // Получаем информацию о папке
                        DirectoryInfo dirInfo = new DirectoryInfo(subdirPath);

                        // Пропускаем системные и скрытые папки
                        if ((dirInfo.Attributes & FileAttributes.System) != 0 ||
                            (dirInfo.Attributes & FileAttributes.Hidden) != 0)
                        {
                            continue;
                        }

                        // Получаем имя папки
                        string folderName = dirInfo.Name;

                        // Создаем узел подпапки
                        TreeNode subNode = new TreeNode(folderName)
                        {
                            Tag = subdirPath
                        };

                        // Проверяем, есть ли в этой папке подпапки (без учета системных и скрытых)
                        if (HasAccessibleSubfolders(subdirPath))
                        {
                            // Добавляем заглушку, чтобы показать "плюсик" расширения
                            subNode.Nodes.Add(new TreeNode("...загрузка..."));
                        }

                        parentNode.Nodes.Add(subNode);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Пропускаем папки, к которым нет доступа
                        continue;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка загрузки подпапки {subdirPath}: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке подпапок {parentPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверить, есть ли в папке доступные подпапки (исключая системные и скрытые)
        /// </summary>
        private bool HasAccessibleSubfolders(string folderPath)
        {
            try
            {
                string[] subdirs = _fileService.GetSubdirectories(folderPath);

                foreach (string subdir in subdirs)
                {
                    try
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(subdir);

                        // Пропускаем системные и скрытые папки
                        if ((dirInfo.Attributes & FileAttributes.System) != 0 ||
                            (dirInfo.Attributes & FileAttributes.Hidden) != 0)
                        {
                            continue;
                        }

                        // Если нашли хотя бы одну доступную папку, возвращаем true
                        return true;
                    }
                    catch
                    {
                        continue;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Обработка выбора папки в TreeView
        /// </summary>
        private void treeFolders_AfterSelect(object sender, TreeViewEventArgs e)
        {
            _currentFolder = e.Node.Tag?.ToString();
            if (string.IsNullOrEmpty(_currentFolder))
                return;

            txtSearch.Clear();
            LoadImages(_currentFolder);
        }

        /// <summary>
        /// Загрузить изображения из папки
        /// </summary>
        private void LoadImages(string folderPath)
        {
            if (!_fileService.FolderExists(folderPath))
            {
                MessageBox.Show("Папка не найдена или недоступна.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            listThumbs.Clear();
            imgListThumbs.Images.Clear();
            _currentImages.Clear();

            // Запускаем загрузку в фоновом потоке
            if (!thumbWorker.IsBusy)
            {
                thumbWorker.RunWorkerAsync(folderPath);
                toolStripStatusLabel1.Text = "Загрузка миниатюр...";
            }
        }

        /// <summary>
        /// BackgroundWorker: загрузка миниатюр
        /// </summary>
        private void ThumbWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string folderPath = e.Argument as string;
            if (string.IsNullOrEmpty(folderPath))
                return;

            List<ImageFile> images = _fileService.LoadImagesFromFolder(folderPath, "");
            List<Image> thumbnails = new List<Image>();

            int total = images.Count;
            for (int i = 0; i < total; i++)
            {
                try
                {
                    Image thumb = _thumbnailService.GetThumbnail(images[i].FullPath);
                    if (thumb != null)
                    {
                        thumbnails.Add(thumb);
                    }
                    else
                    {
                        thumbnails.Add(null);
                    }

                    // Сообщаем о прогрессе
                    int progress = (int)((i + 1) * 100 / total);
                    thumbWorker.ReportProgress(progress, new { Images = images, Thumbnails = thumbnails });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки миниатюры: {ex.Message}");
                    thumbnails.Add(null);
                }
            }

            e.Result = new { Images = images, Thumbnails = thumbnails };
        }

        /// <summary>
        /// BackgroundWorker: обновление прогресса
        /// </summary>
        private void ThumbWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripStatusLabel1.Text = $"Загрузка миниатюр: {e.ProgressPercentage}%";
        }

        /// <summary>
        /// BackgroundWorker: завершение загрузки
        /// </summary>
        private void ThumbWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show($"Ошибка при загрузке: {e.Error.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            dynamic result = e.Result;
            List<ImageFile> images = result.Images;
            List<Image> thumbnails = result.Thumbnails;

            _currentImages = images;
            listThumbs.Clear();
            imgListThumbs.Images.Clear();

            for (int i = 0; i < images.Count; i++)
            {
                try
                {
                    Image thumb = thumbnails[i] ?? CreateDefaultThumbnail();
                    imgListThumbs.Images.Add(thumb);

                    ListViewItem item = new ListViewItem
                    {
                        ImageIndex = i,
                        Text = images[i].FileName,
                        Tag = images[i]
                    };

                    listThumbs.Items.Add(item);
                }
                catch
                {
                    continue;
                }
            }

            toolStripStatusLabel1.Text = $"Найдено изображений: {listThumbs.Items.Count}";
            UpdateStatusBar();
        }

        /// <summary>
        /// Создать изображение по умолчанию (когда миниатюра не загружена)
        /// </summary>
        private Image CreateDefaultThumbnail()
        {
            Bitmap bmp = new Bitmap(64, 64);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(Brushes.LightGray, 0, 0, 64, 64);
                g.DrawString("?", new Font("Arial", 20), Brushes.Gray, 20, 15);
            }
            return bmp;
        }

        /// <summary>
        /// Поиск по названию файла
        /// </summary>
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFolder))
                return;

            string searchText = txtSearch.Text.Trim();
            List<ImageFile> filteredImages = _fileService.LoadImagesFromFolder(_currentFolder, searchText);

            listThumbs.Clear();
            imgListThumbs.Images.Clear();

            for (int i = 0; i < filteredImages.Count; i++)
            {
                try
                {
                    Image thumb = _thumbnailService.GetThumbnail(filteredImages[i].FullPath) ?? CreateDefaultThumbnail();
                    imgListThumbs.Images.Add(thumb);

                    ListViewItem item = new ListViewItem
                    {
                        ImageIndex = i,
                        Text = filteredImages[i].FileName,
                        Tag = filteredImages[i]
                    };

                    listThumbs.Items.Add(item);
                }
                catch
                {
                    continue;
                }
            }

            _currentImages = filteredImages;
            toolStripStatusLabel1.Text = $"Найдено изображений: {listThumbs.Items.Count}";
        }

        /// <summary>
        /// Очистить поиск
        /// </summary>
        private void btnClearSearch_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
        }

        /// <summary>
        /// Двойной клик - открыть в полноэкранном просмотре
        /// </summary>
        private void listThumbs_DoubleClick(object sender, EventArgs e)
        {
            if (listThumbs.SelectedItems.Count == 0)
                return;

            ImageFile selectedImage = listThumbs.SelectedItems[0].Tag as ImageFile;
            if (selectedImage == null)
                return;

            ViewerForm viewerForm = new ViewerForm(_currentImages, _currentImages.IndexOf(selectedImage));
            viewerForm.ShowDialog(this);
        }

        /// <summary>
        /// Изменение выделения в списке
        /// </summary>
        private void listThumbs_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateStatusBar();
        }

        /// <summary>
        /// Обновить статус-бар с информацией о выделенном файле
        /// </summary>
        private void UpdateStatusBar()
        {
            if (listThumbs.SelectedItems.Count > 0)
            {
                ImageFile img = listThumbs.SelectedItems[0].Tag as ImageFile;
                if (img != null)
                {
                    string info = $"{img.FileName} ({img.Width}×{img.Height})";
                    string size = FormatFileSize(img.Size);
                    toolStripStatusLabel1.Text = info;
                    toolStripStatusLabel2.Text = size;
                }
            }
        }

        /// <summary>
        /// Форматировать размер файла
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

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
