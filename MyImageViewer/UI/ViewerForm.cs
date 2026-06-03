using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MyImageViewer.Models;
using MyImageViewer.Services;

namespace MyImageViewer.UI
{
    public partial class ViewerForm : Form
    {
        private List<ImageFile> _images;
        private int _currentIndex;
        private ImageService _imageService;
        private Bitmap _currentBitmap;
        private PictureBox _pictureBox;

        public ViewerForm(List<ImageFile> images, int startIndex = 0)
        {
            _images = images;
            _currentIndex = startIndex;
            _imageService = new ImageService();

            InitializeComponent();
            SetupUI();
            LoadImage(_currentIndex);
        }

        private void InitializeComponent()
        {
            _pictureBox = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)_pictureBox).BeginInit();
            SuspendLayout();

            // PictureBox
            _pictureBox.Dock = DockStyle.Fill;
            _pictureBox.Location = new Point(0, 0);
            _pictureBox.Name = "_pictureBox";
            _pictureBox.Size = new Size(800, 600);
            _pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            _pictureBox.TabIndex = 0;
            _pictureBox.TabStop = false;
            _pictureBox.BackColor = Color.Black;

            // ViewerForm
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 600);
            Controls.Add(_pictureBox);
            Name = "ViewerForm";
            Text = "Image Viewer - Fullscreen";
            WindowState = FormWindowState.Maximized;
            KeyDown += ViewerForm_KeyDown;
            MouseClick += ViewerForm_MouseClick;

            ((System.ComponentModel.ISupportInitialize)_pictureBox).EndInit();
            ResumeLayout(false);
        }

        private void SetupUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
        }

        /// <summary>
        /// Загрузить изображение по индексу
        /// </summary>
        private void LoadImage(int index)
        {
            if (index < 0 || index >= _images.Count)
                return;

            _currentIndex = index;
            ImageFile imgFile = _images[index];

            try
            {
                // Освобождаем старое изображение
                _currentBitmap?.Dispose();

                // Загружаем новое
                _currentBitmap = _imageService.LoadImage(imgFile.FullPath);

                if (_currentBitmap != null)
                {
                    _pictureBox.Image = _currentBitmap;
                    UpdateTitle();
                }
                else
                {
                    MessageBox.Show("Не удалось загрузить изображение.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Обновить заголовок окна с информацией о текущем файле
        /// </summary>
        private void UpdateTitle()
        {
            if (_currentBitmap == null)
                return;

            ImageFile img = _images[_currentIndex];
            string dims = $"{_currentBitmap.Width} × {_currentBitmap.Height}";
            this.Text = $"{img.FileName} ({dims}) - [{_currentIndex + 1}/{_images.Count}]";
        }

        /// <summary>
        /// Навигация с помощью клавиатуры
        /// </summary>
        private void ViewerForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Right:
                case Keys.Down:
                case Keys.Space:
                    NextImage();
                    e.Handled = true;
                    break;

                case Keys.Left:
                case Keys.Up:
                case Keys.Back:
                    PreviousImage();
                    e.Handled = true;
                    break;

                case Keys.Home:
                    LoadImage(0);
                    e.Handled = true;
                    break;

                case Keys.End:
                    LoadImage(_images.Count - 1);
                    e.Handled = true;
                    break;

                case Keys.Escape:
                case Keys.Q:
                    this.Close();
                    e.Handled = true;
                    break;

                case Keys.R:
                    RotateImage(90);
                    e.Handled = true;
                    break;

                case Keys.D:
                    DeleteCurrentImage();
                    e.Handled = true;
                    break;

                case Keys.F:
                    if (e.Control)
                    {
                        ShowImageInfo();
                        e.Handled = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// Навигация с помощью мыши
        /// </summary>
        private void ViewerForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Левая половина - предыдущее, правая половина - следующее
                if (e.X < this.Width / 2)
                {
                    PreviousImage();
                }
                else
                {
                    NextImage();
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Правая кнопка - контекстное меню (можно расширить)
                this.Close();
            }
        }

        private void NextImage()
        {
            if (_currentIndex < _images.Count - 1)
            {
                LoadImage(_currentIndex + 1);
            }
        }

        private void PreviousImage()
        {
            if (_currentIndex > 0)
            {
                LoadImage(_currentIndex - 1);
            }
        }

        private void RotateImage(int angle)
        {
            if (_currentBitmap == null)
                return;

            try
            {
                _currentBitmap?.Dispose();
                _currentBitmap = _imageService.RotateImage(_currentBitmap, angle);
                _pictureBox.Image = _currentBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поворота: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteCurrentImage()
        {
            if (DialogResult.Yes == MessageBox.Show("Удалить этот файл?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                try
                {
                    ImageFile img = _images[_currentIndex];
                    System.IO.File.Delete(img.FullPath);
                    _images.RemoveAt(_currentIndex);

                    if (_images.Count > 0)
                    {
                        if (_currentIndex >= _images.Count)
                            _currentIndex = _images.Count - 1;

                        LoadImage(_currentIndex);
                    }
                    else
                    {
                        MessageBox.Show("Нет больше изображений.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ShowImageInfo()
        {
            if (_currentBitmap == null)
                return;

            ImageFile img = _images[_currentIndex];
            string info = $"Файл: {img.FileName}\n" +
                         $"Путь: {img.FullPath}\n" +
                         $"Размер: {img.Width} × {img.Height} px\n" +
                         $"Размер файла: {FormatFileSize(img.Size)}\n" +
                         $"Позиция: {_currentIndex + 1}/{_images.Count}";

            MessageBox.Show(info, "Информация об изображении", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _currentBitmap?.Dispose();
                _pictureBox?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
