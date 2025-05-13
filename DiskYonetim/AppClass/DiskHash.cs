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
        private static List<Disk> bagliDiskler = new();

        // Dışarıdan erişim için event tanımlayalım
        public static event EventHandler<DiskEventArgs> DiskTakildi;
        public static event EventHandler<DiskEventArgs> DiskCikarildi;

        // Bağlı diskleri döndüren property
        public static List<Disk> BagliDiskler => bagliDiskler.ToList();

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
                    // Diskler listesinden çıkarılanı bul ve listeden kaldır
                    var cikarilacakDisk = bagliDiskler.FirstOrDefault(d => d.DiskModel.StartsWith(disk));
                    if (cikarilacakDisk != null)
                    {
                        bagliDiskler.Remove(cikarilacakDisk);
                        // Event'i tetikle
                        DiskCikarildi?.Invoke(null, new DiskEventArgs { Disk = cikarilacakDisk });
                    }

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
                var driveInfo = new DriveInfo(diskYolu);
                string diskSeriNo = driveInfo.VolumeLabel;
                string diskModel = driveInfo.RootDirectory.FullName;
                long diskBoyut = driveInfo.TotalSize;

                Disk disk = new Disk()
                {
                    DiskBoyut = diskBoyut,
                    DiskModel = diskModel,
                    DiskSeriNo = diskSeriNo,
                    DiskTarih = DateTime.Now
                };

                string raw = $"{diskSeriNo}-{diskModel}-{diskBoyut}";
                string hash = SHA256Uret(raw, disk);

                // Hash değerini disk nesnesine ekle
                disk.Hash = hash;

                // Listeye ekle
                bagliDiskler.Add(disk);

                // Event'i tetikle
                DiskTakildi?.Invoke(null, new DiskEventArgs { Disk = disk });

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Yeni disk takıldı: {diskSeriNo} ({diskModel}) | Boyut: {diskBoyut / (1024 * 1024)} MB");
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
            }
        }
    }

    // Event veri sınıfı
    public class DiskEventArgs : EventArgs
    {
        public Disk Disk { get; set; }
    }
}