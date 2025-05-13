using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntitesLayer.Entity
{
    public class KabulDurum
    {
        public int DurumID { get; set; }
        public bool Engellenmis { get; set; }
        public bool Okunur { get; set; }
        public bool Yazilabilir { get; set; }

    }
}
