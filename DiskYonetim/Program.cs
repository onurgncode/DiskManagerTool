using DiskYonetim.AppClass;

internal class Program
{
    private static void Main(string[] args)
    {
        // ilk çalışcak olan makinme hashleme eğer hashlenmemişse çalıştır
        // Sonra Disk Dinleme Çalışacak
        // Takılan Disk Daha önce Hashlenmişmi veritabanında varmı bunu sorgulayacak 
        // Eğer yoksa yeni hash oluşturup yöneticiye gönderecek
        // varsa Kontrol izinlerine bakacak ve ona göre çalışacak
        DiskHash.Baslat();
        Console.ReadKey();
    }
}