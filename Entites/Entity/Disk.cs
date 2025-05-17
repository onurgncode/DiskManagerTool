using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntitesLayer.Entity
{


    public class Disk
    {
        public string DiskSeriNo { get; set; }
        public string DiskModel { get; set; }
        public long DiskBoyut { get; set; }
        public DateTime DiskTarih { get; set; }
        public string Hash { get; set; }
        public DiskErisimTuru ErisimTuru { get; set; } 
    }
}