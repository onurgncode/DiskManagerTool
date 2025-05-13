using EntitesLayer.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntitesLayer.DbContextFile
{
    public class MsContextDb: DbContext
    {
        public MsContextDb(DbContextOptions<MsContextDb> options) : base(options)
        {}
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Bağlantı dizesi doğrudan burada tanımlanıyor
            optionsBuilder.UseSqlServer("Server=.;Database=UrunDb;Trusted_Connection=True;");
        }
        public DbSet<Makine> makines { get; set; }
        public DbSet<Disk> disks { get; set; }
        public DbSet<Kabul> kabuls { get; set; }
        public DbSet<KabulDurum> kabulDurums { get; set; }
    }
}
