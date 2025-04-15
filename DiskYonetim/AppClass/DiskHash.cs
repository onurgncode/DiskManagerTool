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

                // Çıkarılan diskleri bul
                var cikarilanDiskler = oncekiDiskler.Except(mevcutDiskler).ToList();
                foreach (var disk in cikarilanDiskler)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"UYARI: {disk} diski çıkartıldı!");
                    Console.ResetColor();
                }

                // En son önceki disk listesini güncelle
                oncekiDiskler = mevcutDiskler;

                await Task.Delay(2000); // 2 saniyede bir kontrol et
            }
        }

        private static async Task DiskBilgisiYaz(string diskYolu)
        {
            await Task.Run(() =>
            {
                var disk = new DriveInfo(diskYolu);
                string DiskSeriNo = disk.VolumeLabel;
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
                string hash = SHA256Uret(raw, diskolustur);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Yeni disk takıldı: {DiskSeriNo} ({DiskModel}) | Boyut: {DiskBoyut / (1024 * 1024)} MB");
                Console.WriteLine($"Disk Hash: {hash}");
                Console.ResetColor();
            });
        }

        private static string SHA256Uret(string data, Disk disk)
        {
            using (SHA256 sha = SHA256.Create())
            {
                var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                // Bu kısımda disk verisi veritabanına gönderilecekse, buraya ekleme yapılabilir.
                // Örn: DiskDb.Add(disk);
            }
        }
    }
}