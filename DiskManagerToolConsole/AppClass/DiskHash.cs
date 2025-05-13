using EntitesLayer.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DiskManagerToolConsole.AppClass
{
    public class DiskHash
    {
        public static async Task Dinleme() // disk takılıdğında dinleme methodu 
        {
            await Task.Run(() =>
            {
                using (ManagementEventWatcher GirisDinleme = new ManagementEventWatcher(
                               new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_DiskDrive'")))
                //using (ManagementEventWatcher CikisDinleme = new ManagementEventWatcher(
                          // new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_DiskDrive'")))
                {
                    GirisDinleme.EventArrived += async (s, e) => await DİskUretme(e.NewEvent["TargetInstance"] as ManagementBaseObject);
                    //CikisDinleme.EventArrived += async (s, e) => await OnDiskRemovedAsync(e.NewEvent["TargetInstance"] as ManagementBaseObject);

                    GirisDinleme.Start();
                    //CikisDinleme.Start();

                    Console.WriteLine("Disk takma/çıkarma olayları dinleniyor...");
                    Console.ReadLine(); // Program kapanmasın diye
                }
            });

        }
        private static async Task DİskUretme(ManagementBaseObject disk)
        {
            if (disk == null) return;

            await Task.Run(() =>
            {
                string diskSeriNo = disk["SerialNumber"]?.ToString();
                string diskmodel = disk["Model"]?.ToString();
                long boyut = Convert.ToInt64(disk["Size"]);


                Disk entities = new Disk
                {
                    DiskSeriNo = int.Parse(diskSeriNo),
                    DiskModel = diskmodel,
                    DiskBoyut = boyut
                };

                
                string diskId = GenarateDiskID(entities);

                
                Console.WriteLine($"Yeni disk takıldı: {diskmodel}, ID: {diskId}");
            });
        }


        public static string GenarateDiskID(Disk Entityes)
        {
            string rawData = $"{Entityes.DiskSeriNo}-{Entityes.DiskModel}-{Entityes.DiskBoyut}";
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                string DiskHashID = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                Entityes.DiskHash = DiskHashID;
                return $"{Entityes.DiskModel} : {Entityes.DiskHash}";
            }
        }





    } 

}

