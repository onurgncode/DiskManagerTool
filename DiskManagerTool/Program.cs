

using EntitesLayer.DbContextFile;
using Microsoft.EntityFrameworkCore;

namespace DiskManagerTool
{
    internal static class Program
    {


        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var optionsBuilder = new DbContextOptionsBuilder<MsContextDb>();
            optionsBuilder.UseSqlServer("Server=.;Database=DiskMT;Trusted_Connection=True;");
            var context = new MsContextDb(optionsBuilder.Options);
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}