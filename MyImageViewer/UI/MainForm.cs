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
        // Флаг наличия нативной SQLite (interop) библиотеки
        private bool _dbNativeAvailable = true;
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

            // Проверяем наличие нативной SQLite (SQLite.Interop.dll).
            // Если DLL отсутствует, отключаем использование DB-кеша (если FileService поддерживает это).
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string interopPath1 = Path.Combine(baseDir, "SQLite.Interop.dll");
                string interopPath2 = Path.Combine(baseDir, IntPtr.Size == 8 ? "x64" : "x86", "SQLite.Interop.dll");

                if (!File.Exists(interopPath1) && !File.Exists(interopPath2))
                {
                    _dbNativeAvailable = false;
                    System.Diagnostics.Debug.WriteLine("SQLite native interop DLL not found. Database caching will be disabled to avoid repeated DllNotFoundExceptions.");

                    // Попытка отключить кеш БД в FileService через отражение, если соответствующее свойство существует.
                    try
                    {
                        var prop = _fileService.GetType().GetProperty("UseDatabaseCache");
                        if (prop != null && prop.CanWrite)
                        {
                            prop.SetValue(_fileService, false);
                        }
                    }
                    catch
                    {
                        // Ничего не делаем — безопасно игнорируем, если свойства нет.
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking SQLite native DLL: {ex.Message}");
            }

            // Инициализация ImageList с оптимальным размером
            imgListThumbs.ImageSize = new Size(120, 120);
            imgListThumbs.ColorDepth = ColorDepth.Depth32Bit;

            // Привязываем ImageList к ListView
            listThumbs.LargeImageList = imgListThumbs;

            // Инициализация BackgroundWorker для загрузки миниатюр
            thumbWorker.DoWork += ThumbWorker_DoWork;
            thumbWorker.ProgressChanged += ThumbWorker_ProgressChanged;
            thumbWorker.RunWorkerCompleted += ThumbWorker_RunWorkerCompleted;

            // Загружаем папки при загрузке формы
            this.Load += MainForm_Load;

            // Очищаем ресурсы при закрытии
            this.FormClosing += MainForm_FormClosing;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Освобождаем предпросмотр
            if (pictureBoxPreview.Image != null)
            {
                pictureBoxPreview.Image.Dispose();
                pictureBoxPreview.Image = null;
            }
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
        /// Инициализировать TreeView с "Избранным" и дисками
        /// </summary>
        private void InitializeTreeView()
        {
            treeFolders.Nodes.Clear();

            try
            {
                // 1. Добавляем раздел "Избранное" со специальными папками
                TreeNode favoritesNode = new TreeNode("Избранное")
                {
                    Tag = null
                };

                // Рабочий стол
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (Directory.Exists(desktopPath))
                {
                    TreeNode desktopNode = new TreeNode("Рабочий стол")
                    {
                        Tag = desktopPath
                    };
                    if (HasAccessibleSubfolders(desktopPath))
                    {
                        desktopNode.Nodes.Add(new TreeNode("...загрузка..."));
                    }
                    favoritesNode.Nodes.Add(desktopNode);
                }

                // Загрузки
                string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (Directory.Exists(downloadsPath))
                {
                    TreeNode downloadsNode = new TreeNode("Загрузки")
                    {
                        Tag = downloadsPath
                    };
                    if (HasAccessibleSubfolders(downloadsPath))
                    {
                        downloadsNode.Nodes.Add(new TreeNode("...загрузка..."));
                    }
                    favoritesNode.Nodes.Add(downloadsNode);
                }

                // Изображения
                string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                if (Directory.Exists(picturesPath))
                {
                    TreeNode picturesNode = new TreeNode("Изображения")
                    {
                        Tag = picturesPath
                    };
                    if (HasAccessibleSubfolders(picturesPath))
                    {
                        picturesNode.Nodes.Add(new TreeNode("...загрузка..."));
                    }
                    favoritesNode.Nodes.Add(picturesNode);
                }

                // Музыка
                string musicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                if (Directory.Exists(musicPath))
                {
                    TreeNode musicNode = new TreeNode("Музыка")
                    {
                        Tag = musicPath
                    };
                    if (HasAccessibleSubfolders(musicPath))
                    {
                        musicNode.Nodes.Add(new TreeNode("...загрузка..."));
                    }
                    favoritesNode.Nodes.Add(musicNode);
                }

                treeFolders.Nodes.Add(favoritesNode);

                // 2. Добавляем раздел "Этот компьютер" с дисками
                TreeNode computerNode = new TreeNode("Этот компьютер")
                {
                    Tag = null
                };

                DriveInfo[] drives = DriveInfo.GetDrives();

                foreach (DriveInfo drive in drives)
                {
                    try
                    {
                        if (!drive.IsReady)
                            continue;

                        string driveName = drive.Name;
                        string driveLabel = $"Диск {drive.Name.TrimEnd('\\')}";
                        if (!string.IsNullOrEmpty(drive.VolumeLabel))
                        {
                            driveLabel = $"{drive.VolumeLabel} ({drive.Name.TrimEnd('\\')})";
                        }

                        TreeNode driveNode = new TreeNode(driveLabel)
                        {
                            Tag = driveName
                        };

                        // Проверяем, есть ли подпапки
                        if (HasAccessibleSubfolders(driveName))
                        {
                            driveNode.Nodes.Add(new TreeNode("...загрузка..."));
                        }

                        computerNode.Nodes.Add(driveNode);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка инициализации диска {drive.Name}: {ex.Message}");
                    }
                }

                treeFolders.Nodes.Add(computerNode);

                // Расширяем "Избранное" по умолчанию
                if (favoritesNode.Nodes.Count > 0)
                {
                    favoritesNode.Expand();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при инициализации TreeView: {ex.Message}");
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

            listThumbs.Items.Clear();
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
        /// BackgroundWorker: загрузка миниатюр (стриминговый режим)
        /// Загружает изображения по одному и сообщает о прогрессе
        /// </summary>
        private void ThumbWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string folderPath = e.Argument as string;
            if (string.IsNullOrEmpty(folderPath))
                return;

            List<ImageFile> images = _fileService.LoadImagesFromFolder(folderPath, "");
            int total = images.Count;

            _currentImages = images; // Сохраняем список для поиска

            for (int i = 0; i < total; i++)
            {
                if (thumbWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                ImageFile img = images[i];

                try
                {
                    // Загружаем миниатюру одного файла
                    Image thumb = _thumbnailService.GetThumbnail(img.FullPath);

                    // Отправляем ОДИН элемент в UI поток
                    thumbWorker.ReportProgress(i * 100 / total, new ThumbPacket
                    {
                        Index = i,
                        Image = img,
                        Thumbnail = thumb ?? CreateDefaultThumbnail()
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки миниатюры для {img.FileName}: {ex.Message}");

                    // Отправляем элемент с default thumbnail при ошибке
                    thumbWorker.ReportProgress(i * 100 / total, new ThumbPacket
                    {
                        Index = i,
                        Image = img,
                        Thumbnail = CreateDefaultThumbnail()
                    });
                }
            }

            e.Result = total;
        }

        /// <summary>
        /// BackgroundWorker: обновление прогресса (добавляем элементы по одному)
        /// </summary>
        private void ThumbWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var packet = e.UserState as ThumbPacket;
            if (packet == null)
            {
                toolStripStatusLabel1.Text = $"Загрузка: {e.ProgressPercentage}%";
                return;
            }

            // Добавляем картинку в ImageList
            imgListThumbs.Images.Add(packet.Thumbnail);

            // Создаём item сразу
            ListViewItem item = new ListViewItem
            {
                ImageIndex = imgListThumbs.Images.Count - 1,
                Text = packet.Image.FileName,
                Tag = packet.Image
            };

            listThumbs.Items.Add(item);

            toolStripStatusLabel1.Text = $"Загрузка: {packet.Index + 1} / ...";
        }

        /// <summary>
        /// BackgroundWorker: завершение загрузки
        /// </summary>
        private void ThumbWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                toolStripStatusLabel1.Text = "Загрузка отменена";
                return;
            }

            if (e.Error != null)
            {
                MessageBox.Show($"Ошибка при загрузке: {e.Error.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                listThumbs.EndUpdate();
                return;
            }

            // EndUpdate после завершения загрузки
            int count = listThumbs.Items.Count;
            toolStripStatusLabel1.Text = $"Готово: {count} изображений";
            UpdateStatusBar();
        }

        /// <summary>
        /// Создать изображение по умолчанию (когда миниатюра не загружена)
        /// </summary>
        private Image CreateDefaultThumbnail()
        {
            int size = imgListThumbs?.ImageSize.Width ?? 120;
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(Brushes.LightGray, 0, 0, size, size);
                using (Font f = new Font("Arial", Math.Max(12, size/5)))
                {
                    SizeF textSize = g.MeasureString("?", f);
                    g.DrawString("?", f, Brushes.Gray, (size - textSize.Width)/2, (size - textSize.Height)/2);
                }
            }
            return bmp;
        }

        /// <summary>
        /// Поиск по названию файла (быстрый поиск в памяти через LINQ)
        /// </summary>
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (_currentImages == null || _currentImages.Count == 0)
                return;

            string searchText = txtSearch.Text.Trim().ToLower();

            // Фильтруем уже загруженные изображения из памяти через LINQ
            List<ImageFile> filteredImages = string.IsNullOrEmpty(searchText)
                ? _currentImages
                : _currentImages.Where(img => img.FileName.ToLower().Contains(searchText)).ToList();

            // BeginUpdate для оптимизации
            listThumbs.BeginUpdate();

            // Очищаем ListView и ImageList
            listThumbs.Items.Clear();
            imgListThumbs.Images.Clear();

            // Добавляем отфильтрованные изображения
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

            // EndUpdate после обновления
            listThumbs.EndUpdate();

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
            // Загружаем размеры только один раз
            if (selectedImage.Width == 0 || selectedImage.Height == 0)
            {
                ImageService imageService = new ImageService();

                Size size = imageService.GetImageSize(selectedImage.FullPath);

                selectedImage.Width = size.Width;
                selectedImage.Height = size.Height;
            }
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
            ShowPreviewInPanel();
        }

        /// <summary>
        /// Показать предпросмотр выбранного изображения в pictureBoxPreview
        /// </summary>
        private void ShowPreviewInPanel()
        {
            if (listThumbs.SelectedItems.Count == 0)
            {
                pictureBoxPreview.Image = null;
                return;
            }

            try
            {
                ImageFile selectedImage = listThumbs.SelectedItems[0].Tag as ImageFile;
                if (selectedImage == null)
                {
                    pictureBoxPreview.Image = null;
                    return;
                }

                // Освобождаем старое изображение
                if (pictureBoxPreview.Image != null)
                {
                    pictureBoxPreview.Image.Dispose();
                }

                // Загружаем полное изображение (не миниатюру)
                ImageService imageService = new ImageService();
                Bitmap fullImage = imageService.LoadImage(selectedImage.FullPath);

                if (fullImage != null)
                {
                    pictureBoxPreview.Image = fullImage;
                    UpdateStatusBar();
                }
                else
                {
                    pictureBoxPreview.Image = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки предпросмотра: {ex.Message}");
                pictureBoxPreview.Image = null;
            }
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
                    string info;

                    if (img.Width > 0 && img.Height > 0)
                    {
                        info = $"{img.FileName} ({img.Width}×{img.Height})";
                    }
                    else
                    {
                        info = img.FileName;
                    }
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

        private void MainForm_Load_1(object sender, EventArgs e)
        {

        }

        private void thumbWorker_DoWork_1(object sender, DoWorkEventArgs e)
        {
            string folderPath = e.Argument as string;
            if (string.IsNullOrEmpty(folderPath))
                return;

            List<ImageFile> images = _fileService.LoadImagesFromFolder(folderPath, "");
            int total = images.Count;

            for (int i = 0; i < total; i++)
            {
                if (thumbWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                ImageFile img = images[i];

                try
                {
                    Image thumb = _thumbnailService.GetThumbnail(img.FullPath);

                    // отправляем ОДИН элемент, а не список
                    thumbWorker.ReportProgress(i * 100 / total, new ThumbPacket
                    {
                        Index = i,
                        Image = img,
                        Thumbnail = thumb ?? CreateDefaultThumbnail()
                    });
                }
                catch
                {
                    thumbWorker.ReportProgress(i * 100 / total, new ThumbPacket
                    {
                        Index = i,
                        Image = img,
                        Thumbnail = CreateDefaultThumbnail()
                    });
                }
            }
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }
    }
}
