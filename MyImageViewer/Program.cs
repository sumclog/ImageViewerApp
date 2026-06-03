using MyImageViewer.Data;
using MyImageViewer.UI;

namespace MyImageViewer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Инициализируем базу данных перед запуском приложения
            DatabaseContext.Initialize();

            Application.Run(new MainForm());
        }
    }
}