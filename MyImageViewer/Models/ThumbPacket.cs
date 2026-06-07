using System.Drawing;

namespace MyImageViewer.Models
{
    /// <summary>
    /// Пакет данных для передачи информации о миниатюре из BackgroundWorker в UI поток
    /// </summary>
    public class ThumbPacket
    {
        /// <summary>
        /// Индекс элемента в списке
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Информация о файле изображения
        /// </summary>
        public ImageFile Image { get; set; }

        /// <summary>
        /// Загруженная миниатюра
        /// </summary>
        public Image Thumbnail { get; set; }
    }
}
