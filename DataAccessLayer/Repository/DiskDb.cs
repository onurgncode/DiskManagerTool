using EntitesLayer.DbContextFile;
using EntitesLayer.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repository
{
    public class DiskDb
    {
        private readonly MsContextDb _diskDb;

        public DiskDb(MsContextDb diskDb)
        {
            _diskDb = diskDb;
        }

        public void DiskKaydet(List<Disk> disk)
        {
            foreach (var x in disk)
            {
                _diskDb.disks.Add(x);
                _diskDb.SaveChanges();
            }
        }
    }
}
