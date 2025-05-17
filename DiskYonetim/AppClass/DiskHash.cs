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
                try
                {
                    var driveInfo = new DriveInfo(diskYolu);

                    // Daha güvenilir disk seri numarası almak için
                    string diskSeriNo = GetDiskSerialNumber(diskYolu);
                    if (string.IsNullOrEmpty(diskSeriNo))
                    {
                        // Eğer seri numarası alınamazsa volumeLabel kullan veya benzersiz bir değer oluştur
                        diskSeriNo = !string.IsNullOrEmpty(driveInfo.VolumeLabel) ?
                                    driveInfo.VolumeLabel :
                                    $"Disk_{Guid.NewGuid().ToString().Substring(0, 8)}";
                    }

                    string diskModel = driveInfo.RootDirectory.FullName;
                    long diskBoyut = driveInfo.TotalSize;

                    Disk disk = new Disk()
                    {
                        DiskBoyut = diskBoyut,
                        DiskModel = diskModel,
                        DiskSeriNo = diskSeriNo,
                        DiskTarih = DateTime.Now,
                        ErisimTuru = DiskErisimTuru.TamErisim
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Disk bilgisi alınırken hata: {ex.Message}");
                }
            });
        }

        private static string GetDiskSerialNumber(string driveLetter)
        {
            try
            {
                // WMI kullanarak gerçek fiziksel disk seri numarasını al
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT * FROM Win32_LogicalDisk WHERE DeviceID='{driveLetter.TrimEnd('\\')}:'"))
                {
                    foreach (var disk in searcher.Get())
                    {
                        // Disk fiziksel ID'sini al
                        string deviceID = disk["DeviceID"].ToString();

                        // Fiziksel diski bul
                        using (var physicalSearcher = new System.Management.ManagementObjectSearcher(
                            "SELECT * FROM Win32_DiskDrive"))
                        {
                            foreach (var drive in physicalSearcher.Get())
                            {
                                string model = drive["Model"].ToString();
                                string serialNumber = drive["SerialNumber"]?.ToString() ?? "";

                                // Eğer bu fiziksel disk mantıksal disk ile eşleşiyorsa
                                using (var partitionSearcher = new System.Management.ManagementObjectSearcher(
                                    $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{drive["DeviceID"]}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition"))
                                {
                                    foreach (var partition in partitionSearcher.Get())
                                    {
                                        using (var logicalSearcher = new System.Management.ManagementObjectSearcher(
                                            $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass = Win32_LogicalDiskToPartition"))
                                        {
                                            foreach (var logical in logicalSearcher.Get())
                                            {
                                                if (logical["DeviceID"].ToString() == deviceID)
                                                {
                                                    // Seri numarası ve model bilgisini birleştirerek benzersiz bir tanımlayıcı oluştur
                                                    return !string.IsNullOrEmpty(serialNumber) ?
                                                           serialNumber :
                                                           $"{model}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
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