using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.IO;
using EntitesLayer.Entity;

namespace DiskYonetim.AppClass
{
    public class MakineHash
    {
        /// <summary>
        /// Makine bilgilerini alır ve konsola yazdırır
        /// </summary>
        public static async Task Baslat()
        {
            try
            {
                Console.WriteLine("Makine bilgileri alınıyor...");

                var makine = await Task.Run(() =>
                {
                    string macAdresi = MacAdresiGetir();
                    string makineAdi = Environment.MachineName;
                    DateTime tarih = DateTime.Now;

                    var yeniMakine = new Makine
                    {
                        MacAdresi = macAdresi,
                        MakineAdi = makineAdi,
                        MakineTarih = tarih
                    };

                    // Hash değerini oluştur
                    yeniMakine.MakineHash = HashOlustur(yeniMakine);

                    return yeniMakine;
                });

                // Bilgileri yazdır
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n========= Makine Bilgileri =========");
                Console.WriteLine($"Makine Adı    : {makine.MakineAdi}");
                Console.WriteLine($"MAC Adresi    : {makine.MacAdresi}");
                Console.WriteLine($"Hash Değeri   : {makine.MakineHash}");
                Console.WriteLine($"Tarih         : {makine.MakineTarih}");
                Console.WriteLine("==================================\n");
                Console.ResetColor();

                // Burada veritabanı işlemleri yapılabilir
                // Örneğin: await DatabaseService.MakineKaydet(makine);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Hata oluştu: {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Verilen makine bilgilerinden benzersiz hash oluşturur
        /// </summary>
        private static string HashOlustur(Makine makine)
        {
            try
            {
                // Disk ID'yi al
                string diskKimligi = DiskKimligiGetir();

                // Tüm özellikleri birleştir
                string birlesikBilgiler = $"{makine.MacAdresi}|{makine.MakineAdi}|{diskKimligi}";

                // SHA256 hash oluştur
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] veriBytes = Encoding.UTF8.GetBytes(birlesikBilgiler);
                    byte[] hashBytes = sha256.ComputeHash(veriBytes);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Hash oluşturulurken bir hata oluştu: " + ex.Message);
            }
        }

        /// <summary>
        /// Sistemdeki aktif ağ adaptörünün MAC adresini getirir
        /// </summary>
        private static string MacAdresiGetir()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ag => ag.OperationalStatus == OperationalStatus.Up &&
                           (ag.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                            ag.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                    .Select(ag => BitConverter.ToString(ag.GetPhysicalAddress().GetAddressBytes()))
                    .FirstOrDefault() ?? "MAC_YOK";
            }
            catch (Exception ex)
            {
                throw new Exception("MAC adresi alınırken bir hata oluştu: " + ex.Message);
            }
        }

        /// <summary>
        /// İşletim sistemine göre benzersiz disk kimliği getirir
        /// </summary>
        private static string DiskKimligiGetir()
        {
            try
            {
                // İşletim sistemine göre farklı yaklaşımlar kullan
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows için C: sürücüsünün volume ID'sini al
                    var surucu = new DriveInfo("C");
                    return $"WIN-{surucu.VolumeLabel}-{surucu.TotalSize}";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Linux için machine-id dosyasını kullan
                    if (File.Exists("/etc/machine-id"))
                    {
                        return $"LNX-{File.ReadAllText("/etc/machine-id").Trim()}";
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // macOS için ürün UUID'sini kullan
                    if (File.Exists("/sys/class/dmi/id/product_uuid"))
                    {
                        return $"MAC-{File.ReadAllText("/sys/class/dmi/id/product_uuid").Trim()}";
                    }
                }

                // Eğer yukarıdakiler başarısız olursa, kök dizininin boyutunu kullan
                var kokSurucu = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
                return $"DISK-{kokSurucu.TotalSize}";
            }
            catch (Exception ex)
            {
                throw new Exception("Disk kimliği alınırken bir hata oluştu: " + ex.Message);
            }
        }
    }
}