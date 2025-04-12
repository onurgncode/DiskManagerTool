using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using EntitesLayer.Entity;

namespace DiskYonetim.AppClass
{
    public class DiskHash
    {
        private static List<string> oncekiDiskler = new();

        public static async Task Baslat()
        {
            Console.WriteLine("Disk izleme başladı...");

            while (true)
            {
                var mevcutDiskler = DriveInfo.GetDrives()
                                             .Where(d => d.IsReady) 
                                             .Select(d => d.Name)
                                             .ToList();

                // Yeni takılan diskleri bul
                var yeniDiskler = mevcutDiskler.Except(oncekiDiskler).ToList();

                foreach (var disk in yeniDiskler)
                {
                    await DiskBilgisiYaz(disk);
                }

                oncekiDiskler = mevcutDiskler;

                await Task.Delay(2000); // 2 saniyede bir kontrol et

                // Furkan 
            }
        }
        
        private static async Task DiskBilgisiYaz(string diskYolu)
        {
            await Task.Run(() =>
            {
                
                var disk = new DriveInfo(diskYolu);
                int DiskSeriNo = Convert.ToInt32(disk.VolumeLabel);
                string DiskModel = disk.RootDirectory.FullName;
                long DiskBoyut = disk.TotalSize;
                Disk diskolustur = new Disk()
                {
                    DiskBoyut = DiskBoyut,
                    DiskModel = DiskModel,
                    DiskSeriNo = DiskSeriNo,
                    DiskTarih = DateTime.Now
                };



                string raw = $"{DiskSeriNo}-{DiskModel}-{DiskBoyut}";
                string hash = SHA256Uret(raw,diskolustur);

                Console.WriteLine($"Yeni disk takıldı: {DiskSeriNo} ({DiskModel}) | Boyut: {DiskBoyut / (1024 * 1024)} MB");
                Console.WriteLine($"Disk Hash: {hash}");
            });
            
        }

        private static string SHA256Uret(string data,Disk disk)
        {
            using (SHA256 sha = SHA256.Create())
            {
                var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                // Yasir Bu kısmı veritabanına ekleyecek oluşturulacağını
                // disk i DiskDb gönderecek
            }
        }
    }

//main
}
