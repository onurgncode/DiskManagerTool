using System;
using System.IO;
using System.Security.AccessControl;
using System.Management;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DiskYonetim.AppClass
{
    public static class DiskPermissionManager
    {
        #region Windows API Declarations

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr SecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        // Windows API constantları
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;
        private const uint FSCTL_LOCK_VOLUME = 0x00090018;
        private const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;
        private const int ERROR_ACCESS_DENIED = 5;

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        #endregion

        /// <summary>
        /// Disk için izinleri uygular (Tam Erişim, Salt Okunur veya Erişim Yok)
        /// </summary>
        /// <param name="diskYolu">Disk harfi veya yolu (örn: "C:\")</param>
        /// <param name="erisimTuru">Uygulanacak erişim türü</param>
        /// <returns>İşlem başarılı ise true, değilse false</returns>
        public static bool UygulaErisimTuru(string diskYolu, EntitesLayer.Entity.DiskErisimTuru erisimTuru)
        {
            try
            {
                if (string.IsNullOrEmpty(diskYolu))
                    throw new ArgumentException("Disk yolu boş olamaz!");

                // Disk harfini düzgün formatta al (son karakter \ olmalı)
                if (!diskYolu.EndsWith("\\"))
                    diskYolu += "\\";

                // DriveInfo ile disk harfi olup olmadığını kontrol et
                DriveInfo drive = new DriveInfo(diskYolu.Substring(0, 1));

                switch (erisimTuru)
                {
                    case EntitesLayer.Entity.DiskErisimTuru.TamErisim:
                        return AyarlaTamErisim(diskYolu);

                    case EntitesLayer.Entity.DiskErisimTuru.SaltOkunur:
                        return AyarlaSaltOkunur(diskYolu);

                    case EntitesLayer.Entity.DiskErisimTuru.ErisimYok:
                        return AyarlaErisimYok(diskYolu);

                    default:
                        throw new ArgumentException("Geçersiz erişim türü");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erişim izni ayarlanırken hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disk için tam erişim izinlerini ayarlar
        /// </summary>
        private static bool AyarlaTamErisim(string diskYolu)
        {
            try
            {
                // Disk kilitli veya korumalı ise kilitlerini kaldır
                RemoveDiskProtection(diskYolu);

                // NTFS ise dosya sistemi izinlerini ayarla
                if (IsDiskNTFS(diskYolu))
                {
                    // Tüm dosya sistemindeki izinleri tam erişime çevir
                    DirectoryInfo dirInfo = new DirectoryInfo(diskYolu);
                    DirectorySecurity dirSecurity = dirInfo.GetAccessControl();

                    // Tam erişim için ACL kuralı oluştur
                    FileSystemAccessRule fullAccessRule = new FileSystemAccessRule(
                        "Everyone",
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow);

                    // Yazma ve okuma erişimi için kuralları ekle
                    dirSecurity.AddAccessRule(fullAccessRule);

                    // Kuralları uygula
                    dirInfo.SetAccessControl(dirSecurity);
                }

                // Tam erişim için registrydeki veya politika ayarlarını temizle
                // (Bu kısım gerçek senaryoda registry veya WMI aracılığıyla yapılabilir)

                Console.WriteLine($"{diskYolu} için tam erişim izinleri ayarlandı");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tam erişim ayarlanırken hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disk için salt okunur izinleri ayarlar
        /// </summary>
        private static bool AyarlaSaltOkunur(string diskYolu)
        {
            try
            {
                // Disk sürücüsü bilgisini kontrol et
                DriveInfo driveInfo = new DriveInfo(diskYolu.Substring(0, 1));

                if (IsDiskNTFS(diskYolu))
                {
                    // Salt okunur yapmak için dosya sistemi izinlerini ayarla
                    DirectoryInfo dirInfo = new DirectoryInfo(diskYolu);
                    DirectorySecurity dirSecurity = dirInfo.GetAccessControl();

                    // Salt okunur için ACL kuralları oluştur 
                    FileSystemAccessRule readRule = new FileSystemAccessRule(
                        "Everyone",
                        FileSystemRights.ReadAndExecute | FileSystemRights.ListDirectory,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow);

                    FileSystemAccessRule writeRule = new FileSystemAccessRule(
                        "Everyone",
                        FileSystemRights.Write | FileSystemRights.Modify | FileSystemRights.Delete,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Deny);

                    // Eski kuralları temizle ve yeni kuralları ekle
                    dirSecurity.PurgeAccessRules(new System.Security.Principal.SecurityIdentifier("S-1-1-0")); // Everyone için temizle
                    dirSecurity.AddAccessRule(readRule);  // Okuma izni ver
                    dirSecurity.AddAccessRule(writeRule); // Yazma izni reddet

                    // Kuralları uygula
                    dirInfo.SetAccessControl(dirSecurity);

                    // Ayrıca temel diskler için readonly attribute ayarla
                    SetAttributesReadOnly(diskYolu);
                }
                else
                {
                    // FAT veya diğer dosya sistemleri için alternatif yaklaşım
                    // Burada Windows API kullanılabilir
                    SetAttributesReadOnly(diskYolu);
                }

                Console.WriteLine($"{diskYolu} için salt okunur izinler ayarlandı");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Salt okunur erişim ayarlanırken hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disk için erişim yok izinlerini ayarlar
        /// </summary>
        private static bool AyarlaErisimYok(string diskYolu)
        {
            try
            {
                // Taşınabilir diskler için donanım kilitleme dene
                string driveLetter = diskYolu.Substring(0, 1);
                bool isRemovable = IsRemovableDrive(driveLetter);

                if (isRemovable)
                {
                    // Çıkarılabilir disk için donanım erişimini engelle
                    return LockDisk(diskYolu);
                }

                // Sabit diskler için yazılım kilitleme
                if (IsDiskNTFS(diskYolu))
                {
                    // Tüm erişimleri reddet
                    DirectoryInfo dirInfo = new DirectoryInfo(diskYolu);
                    DirectorySecurity dirSecurity = dirInfo.GetAccessControl();

                    // Erişim yok için ACL kuralı
                    FileSystemAccessRule denyRule = new FileSystemAccessRule(
                        "Everyone",
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Deny);

                    // Tüm izinleri reddet
                    dirSecurity.PurgeAccessRules(new System.Security.Principal.SecurityIdentifier("S-1-1-0"));
                    dirSecurity.AddAccessRule(denyRule);

                    // Kuralları uygula
                    dirInfo.SetAccessControl(dirSecurity);
                }

                Console.WriteLine($"{diskYolu} için erişim engellendi");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erişim engelleme sırasında hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Diski kilitleyerek erişimi engeller (özellikle çıkarılabilir diskler için)
        /// </summary>
        private static bool LockDisk(string diskYolu)
        {
            string devicePath = $"\\\\.\\{diskYolu.Substring(0, 1)}:";
            IntPtr handle = CreateFile(devicePath,
                                      GENERIC_READ | GENERIC_WRITE,
                                      FILE_SHARE_READ | FILE_SHARE_WRITE,
                                      IntPtr.Zero,
                                      OPEN_EXISTING,
                                      FILE_ATTRIBUTE_NORMAL,
                                      IntPtr.Zero);

            if (handle == INVALID_HANDLE_VALUE)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Console.WriteLine($"Disk kilitleme hatası: {errorCode}");
                return false;
            }

            try
            {
                // Diski kilitleyip kilidi tut
                uint bytesReturned;
                bool success = DeviceIoControl(handle, FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

                if (!success)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Console.WriteLine($"Disk kilitleme hatası: {errorCode}");
                    CloseHandle(handle);
                    return false;
                }

                // Diski ayır (dismount) ki işletim sistemi artık erişemesin
                success = DeviceIoControl(handle, FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

                // Kilitlemeyi sürdür, bu noktada handle'ı kapatmayın
                // Not: Gerçek uygulamada bu handle'ı bir servise taşıyıp orada tutmak gerekir

                return success;
            }
            catch
            {
                CloseHandle(handle);
                throw;
            }
        }

        /// <summary>
        /// Disk üzerindeki koruma veya kilitleri kaldırır
        /// </summary>
        private static void RemoveDiskProtection(string diskYolu)
        {
            // Bu kısım mevcut tüm kilitleri kaldırma işlemini yapabilir
            // Gerçek bir uygulamada, disk kilidi için tuttuğunuz tüm handle'ları kapatmanız gerekir
        }

        /// <summary>
        /// Disk dosya sisteminin NTFS olup olmadığını kontrol eder
        /// </summary>
        private static bool IsDiskNTFS(string diskYolu)
        {
            try
            {
                DriveInfo drive = new DriveInfo(diskYolu.Substring(0, 1));
                return drive.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Belirtilen disk harfinin çıkarılabilir disk olup olmadığını kontrol eder
        /// </summary>
        private static bool IsRemovableDrive(string driveLetter)
        {
            try
            {
                DriveInfo drive = new DriveInfo(driveLetter);
                return drive.DriveType == DriveType.Removable;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Windows Explorer arayüzünü kullanarak salt okunur özniteliğini ayarlar
        /// </summary>
        private static void SetAttributesReadOnly(string path)
        {
            try
            {
                // Komut satırı kullanarak öznitelikleri ayarla
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "attrib",
                    Arguments = $"+r {path}*.*",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = Process.Start(startInfo);
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Öznitelik ayarlanırken hata: {ex.Message}");
            }
        }
    }
}