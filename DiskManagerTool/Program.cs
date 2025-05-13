using System;
using System.Windows.Forms;
using DiskYonetim.AppClass;

namespace DiskManagerTool
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Windows Form uygulamasýný baþlat
            Application.Run(new DiskManagerForm());
        }
    }
}