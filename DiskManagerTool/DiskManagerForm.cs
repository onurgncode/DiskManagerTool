using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using DiskYonetim.AppClass;
using EntitesLayer.Entity;

namespace DiskManagerTool
{

    public partial class DiskManagerForm : Form
    {
        private List<Disk> diskler = new List<Disk>();
        private string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DiskManagerLogs.csv");

        public DiskManagerForm()
        {
            InitializeComponent();

            // Event'lere abone ol
            DiskHash.DiskTakildi += DiskHash_DiskTakildi;
            DiskHash.DiskCikarildi += DiskHash_DiskCikarildi;

            // Form kapatılınca event'leri kaldır
            this.FormClosing += (s, e) =>
            {
                DiskHash.DiskTakildi -= DiskHash_DiskTakildi;
                DiskHash.DiskCikarildi -= DiskHash_DiskCikarildi;
            };

            // DataGridView ayarları
            SetupDataGridView();

            // Log dosyasını başlat (eğer yoksa)
            InitializeLogFile();

            // Disk izlemeyi başlat
            Task.Run(() => DiskHash.Baslat());

            // Form yüklendikten sonra mevcut diskleri yükle
            this.Load += (s, e) =>
            {
                LoadInitialDisks();
            };
        }

        private void InitializeLogFile()
        {
            if (!File.Exists(logFilePath))
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(logFilePath, false, Encoding.UTF8))
                    {
                        sw.WriteLine("İşlem Tarihi,İşlem Türü,Seri No,Disk Modeli,Disk Boyutu (MB),Hash Değeri,Erişim Türü");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Log dosyası oluşturulurken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SetupDataGridView()
        {
            // DataGridView sütunlarını tanımla
            dgvDiskler.AutoGenerateColumns = false;

            var colSeriNo = new DataGridViewTextBoxColumn
            {
                Name = "colSeriNo",
                HeaderText = "Seri No",
                DataPropertyName = "DiskSeriNo"
            };

            var colModel = new DataGridViewTextBoxColumn
            {
                Name = "colModel",
                HeaderText = "Disk Modeli",
                DataPropertyName = "DiskModel"
            };

            var colBoyut = new DataGridViewTextBoxColumn
            {
                Name = "colBoyut",
                HeaderText = "Disk Boyutu (MB)",
                DataPropertyName = "DiskBoyutMB"
            };

            var colTarih = new DataGridViewTextBoxColumn
            {
                Name = "colTarih",
                HeaderText = "Takılma Tarihi",
                DataPropertyName = "DiskTarih",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy HH:mm:ss" }
            };

            var colHash = new DataGridViewTextBoxColumn
            {
                Name = "colHash",
                HeaderText = "Hash Değeri",
                DataPropertyName = "Hash"
            };

            // Erişim türü için ComboBox sütunu ekle
            var colErisimTuru = new DataGridViewComboBoxColumn
            {
                Name = "colErisimTuru",
                HeaderText = "Erişim Türü",
                DataPropertyName = "ErisimTuru",
                ValueType = typeof(DiskErisimTuru),
                DataSource = Enum.GetValues(typeof(DiskErisimTuru))
            };

            // İşlem butonu için
            var colIslem = new DataGridViewButtonColumn
            {
                Name = "colIslem",
                HeaderText = "İşlem",
                Text = "Uygula",
                UseColumnTextForButtonValue = true
            };

            dgvDiskler.Columns.AddRange(new DataGridViewColumn[] {
                colSeriNo, colModel, colBoyut, colTarih, colHash, colErisimTuru, colIslem
            });

            // DataGridView olaylarını ekle
            dgvDiskler.CellContentClick += DgvDiskler_CellContentClick;
            dgvDiskler.CellValueChanged += DgvDiskler_CellValueChanged_1;
        }

        private void DgvDiskler_CellValueChanged_1(object sender, DataGridViewCellEventArgs e)
        {
            // Erişim türü değiştiğinde
            if (e.ColumnIndex == dgvDiskler.Columns["colErisimTuru"].Index && e.RowIndex >= 0)
            {
                // UI güncellemesi için değişimi kaydet
                var diskData = dgvDiskler.Rows[e.RowIndex].DataBoundItem;
                // Not: Burada erişim türü property'sini almak için dynamic kullanılıyor
                DiskErisimTuru secilenTur = (DiskErisimTuru)dgvDiskler.Rows[e.RowIndex].Cells["colErisimTuru"].Value;

                // İlgili diski bul ve erişim türünü güncelle
                string diskModel = dgvDiskler.Rows[e.RowIndex].Cells["colModel"].Value.ToString();
                Disk disk = diskler.FirstOrDefault(d => d.DiskModel == diskModel);
                if (disk != null)
                {
                    disk.ErisimTuru = secilenTur;
                }

                // İlgili diski ve yeni durumu logla
                LogDiskIslem("Erişim Değişti", diskModel, secilenTur.ToString());
            }
        }

        private void DgvDiskler_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Uygula butonuna tıklandığında
            if (e.ColumnIndex == dgvDiskler.Columns["colIslem"].Index && e.RowIndex >= 0)
            {
                DiskErisimTuru secilenTur = (DiskErisimTuru)dgvDiskler.Rows[e.RowIndex].Cells["colErisimTuru"].Value;
                string diskYolu = dgvDiskler.Rows[e.RowIndex].Cells["colModel"].Value.ToString();

                try
                {
                    UygulaErisimTuru(diskYolu, secilenTur);

                    // İşlemi logla
                    LogDiskIslem("Erişim Uygulandı", diskYolu, secilenTur.ToString());

                    // Kullanıcıya bilgi ver
                    ShowNotification($"{diskYolu} için {secilenTur} erişimi uygulandı", Color.Blue);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erişim türü uygulanırken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UygulaErisimTuru(string diskYolu, DiskErisimTuru erisimTuru)
        {
            if (string.IsNullOrEmpty(diskYolu) || !Directory.Exists(diskYolu))
            {
                throw new DirectoryNotFoundException($"Disk dizini bulunamadı: {diskYolu}");
            }

            // İlgili drivenın info'sunu al
            DriveInfo driveInfo = new DriveInfo(diskYolu);

            switch (erisimTuru)
            {
                case DiskErisimTuru.TamErisim:
                    SetDiskPermission(diskYolu, true, true);
                    break;

                case DiskErisimTuru.SaltOkunur:
                    SetDiskPermission(diskYolu, true, false);
                    break;

                case DiskErisimTuru.ErisimYok:
                    SetDiskPermission(diskYolu, false, false);
                    break;
            }
        }

        private void SetDiskPermission(string diskYolu, bool okuma, bool yazma)
        {
            try
            {
                // Bu metod Windows API veya uygun bir kütüphane kullanılarak genişletilebilir
                // Şu anki implementasyon örnek amaçlıdır

                DriveInfo driveInfo = new DriveInfo(diskYolu);

                // Gerçek bir uygulamada, burada Windows API çağrıları yapılmalı
                // veya icacls gibi komut satırı araçları kullanılmalı

                // Örnek olarak, sadece bildirim gösteriyoruz
                string mesaj = $"{diskYolu} için ";
                if (okuma && yazma)
                    mesaj += "tam erişim uygulandı.";
                else if (okuma && !yazma)
                    mesaj += "salt okuma izni uygulandı.";
                else
                    mesaj += "tüm erişimler engellendi.";

                // Gerçek uygulamada aşağıdaki kodlar gerekecek
                // Windows komut satırı kullanarak izinleri değiştirme örneği:
                /*
                string arguments = "";
                if (okuma && yazma)
                    arguments = $"/grant Everyone:(OI)(CI)F {diskYolu}";
                else if (okuma && !yazma)
                    arguments = $"/grant Everyone:(OI)(CI)R {diskYolu}";
                else
                    arguments = $"/deny Everyone:(OI)(CI)F {diskYolu}";
                
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "icacls",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                }
                */

                MessageBox.Show(mesaj, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Disk izinleri ayarlanırken hata: {ex.Message}");
            }
        }

        private void LoadInitialDisks()
        {
            // Form henüz yüklenmemiş olabilir, bu yüzden kontrolü ekleyin
            if (this.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    diskler = DiskHash.BagliDiskler.ToList();

                    // Mevcut disklere varsayılan erişim türü ata
                    foreach (var disk in diskler)
                    {
                        // Disk sınıfına ErisimTuru özelliği eklenmiş olmalı
                        SetDiskAccessProperty(disk, DiskErisimTuru.TamErisim);
                    }

                    RefreshDataGridView();
                });
            }
            else
            {
                // Handle henüz oluşturulmadı, doğrudan atama yap
                diskler = DiskHash.BagliDiskler.ToList();

                // Mevcut disklere varsayılan erişim türü ata
                foreach (var disk in diskler)
                {
                    SetDiskAccessProperty(disk, DiskErisimTuru.TamErisim);
                }

                // Form yüklendiğinde grid'i güncelle
                this.Load += (s, e) =>
                {
                    RefreshDataGridView();
                };
            }
        }

        private void DiskHash_DiskTakildi(object sender, DiskEventArgs e)
        {
            // Form henüz yüklenmemiş olabilir, bu yüzden kontrolü ekleyin
            if (this.IsHandleCreated)
            {
                // UI thread'ine geri dön
                this.Invoke((MethodInvoker)delegate
                {
                    // Yeni disk için varsayılan erişim türü
                    SetDiskAccessProperty(e.Disk, DiskErisimTuru.TamErisim);

                    diskler.Add(e.Disk);
                    RefreshDataGridView();

                    // Log kaydı oluştur
                    LogDiskIslem("Disk Takıldı", e.Disk.DiskModel, "TamErisim");

                    // Kullanıcıya bilgi ver
                    ShowNotification($"Yeni disk takıldı: {e.Disk.DiskSeriNo} ({e.Disk.DiskModel})", Color.Green);
                });
            }
            else
            {
                // Handle henüz oluşturulmadı, doğrudan atama yap
                SetDiskAccessProperty(e.Disk, DiskErisimTuru.TamErisim);
                diskler.Add(e.Disk);

                // Log kaydı oluştur (form yüklenmese bile)
                LogDiskIslem("Disk Takıldı", e.Disk.DiskModel, "TamErisim");

                // Form yüklendiğinde bildirim göster
                this.Load += (s, ev) =>
                {
                    ShowNotification($"Yeni disk takıldı: {e.Disk.DiskSeriNo} ({e.Disk.DiskModel})", Color.Green);
                };
            }
        }

        private void DiskHash_DiskCikarildi(object sender, DiskEventArgs e)
        {
            // Form henüz yüklenmemiş olabilir, bu yüzden kontrolü ekleyin
            if (this.IsHandleCreated)
            {
                // UI thread'ine geri dön
                this.Invoke((MethodInvoker)delegate
                {
                    // Disk çıkarılmadan önce logla
                    string erisimTuru = GetDiskAccessProperty(e.Disk).ToString();
                    LogDiskIslem("Disk Çıkarıldı", e.Disk.DiskModel, erisimTuru);

                    diskler.Remove(e.Disk);
                    RefreshDataGridView();

                    // Kullanıcıya bilgi ver
                    ShowNotification($"Disk çıkarıldı: {e.Disk.DiskSeriNo} ({e.Disk.DiskModel})", Color.Red);
                });
            }
            else
            {
                // Handle henüz oluşturulmadı
                // Disk çıkarılmadan önce logla (form yüklenmese bile)
                string erisimTuru = GetDiskAccessProperty(e.Disk).ToString();
                LogDiskIslem("Disk Çıkarıldı", e.Disk.DiskModel, erisimTuru);

                diskler.Remove(e.Disk);

                // Form yüklendiğinde bildirim göster
                this.Load += (s, ev) =>
                {
                    ShowNotification($"Disk çıkarıldı: {e.Disk.DiskSeriNo} ({e.Disk.DiskModel})", Color.Red);
                };
            }
        }

        private void SetDiskAccessProperty(Disk disk, DiskErisimTuru erisimTuru)
        {
            // Disk sınıfına ErisimTuru property'si eklendiği için doğrudan atama yapabiliriz
            disk.ErisimTuru = (EntitesLayer.Entity.DiskErisimTuru)erisimTuru;
        }

        // Disk nesnesinden erişim türünü almak için yardımcı metod
        private DiskErisimTuru GetDiskAccessProperty(Disk disk)
        {
            // Disk sınıfında ErisimTuru property'si var, direkt kullanabiliriz
            return (DiskErisimTuru)disk.ErisimTuru;
        }

        private void RefreshDataGridView()
        {
            // Diskleri MB formatında gösterecek şekilde dönüştür
            var disklerFormatli = diskler.Select(d =>
            {
                // Erişim türü özelliğini almaya çalış
                DiskErisimTuru erisimTuru = GetDiskAccessProperty(d);

                return new
                {
                    d.DiskSeriNo,
                    d.DiskModel,
                    DiskBoyutMB = (d.DiskBoyut / (1024 * 1024)).ToString("N0"),
                    d.DiskTarih,
                    d.Hash,
                    ErisimTuru = erisimTuru
                };
            }).ToList();

            // DataSource'u güncelle
            dgvDiskler.DataSource = null;
            dgvDiskler.DataSource = disklerFormatli;

            // En son satırı seç
            if (dgvDiskler.Rows.Count > 0)
            {
                dgvDiskler.FirstDisplayedScrollingRowIndex = dgvDiskler.Rows.Count - 1;
                dgvDiskler.Rows[dgvDiskler.Rows.Count - 1].Selected = true;
            }

            // Status bar'ı güncelle
            lblDiskSayisi.Text = $"Toplam {diskler.Count} disk bağlı";
        }





        private void ShowNotification(string message, Color color)
        {
            // Form henüz yüklenmemiş olabilir, bu yüzden kontrolü ekleyin
            if (this.IsHandleCreated)
            {
                if (this.InvokeRequired) // Formun kendisinin InvokeRequired özelliğini kullan
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        lblNotification.Text = message;
                        lblNotification.ForeColor = color;

                        // Timer'ı başlat
                        timerNotification.Start();
                    });
                }
                else
                {
                    lblNotification.Text = message;
                    lblNotification.ForeColor = color;

                    // Timer'ı başlat
                    timerNotification.Start();
                }
            }
            // Handle henüz oluşturulmadıysa, bildirimi gösterme
        }

        private void timerNotification_Tick(object sender, EventArgs e)
        {
            // 3 saniye sonra bildirim mesajını temizle
            lblNotification.Text = string.Empty;
            timerNotification.Stop();
        }

        // Disk işlemlerini CSV olarak logla
        private void LogDiskIslem(string islemTuru, string diskModel, string erisimTuru)
        {
            try
            {
                // İlgili diski bul
                Disk disk = diskler.FirstOrDefault(d => d.DiskModel == diskModel);
                if (disk == null && islemTuru != "Disk Çıkarıldı") // Çıkarılan disk listede olmayabilir
                {
                    return;
                }

                using (StreamWriter sw = new StreamWriter(logFilePath, true, Encoding.UTF8))
                {
                    string timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    string seriNo = disk?.DiskSeriNo ?? "Bilinmiyor";
                    string boyut = disk != null ? (disk.DiskBoyut / (1024 * 1024)).ToString("N0") : "0";
                    string hash = disk?.Hash ?? "Yok";

                    // CSV formatında log kaydı oluştur
                    sw.WriteLine($"{timestamp},{islemTuru},{seriNo},{diskModel},{boyut},{hash},{erisimTuru}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Log kaydı oluşturulurken hata: {ex.Message}");
                // Log kaydında hata olursa sessizce devam et
            }
        }

        // Diskleri CSV olarak dışa aktar
        private void btnDisklerExport_Click(object sender, EventArgs e)
        {
            try
            {
                if (diskler.Count == 0)
                {
                    MessageBox.Show("Dışa aktarılacak disk bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV Dosyası (*.csv)|*.csv";
                    sfd.FileName = $"DiskListesi_{DateTime.Now.ToString("yyyyMMdd")}.csv";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        StringBuilder sb = new StringBuilder();

                        // Başlık satırı
                        sb.AppendLine("Seri No,Disk Modeli,Disk Boyutu (MB),Takılma Tarihi,Hash Değeri,Erişim Türü");

                        // Veri satırları
                        foreach (var disk in diskler)
                        {
                            // Boyutu MB'a çevir ve formatlı yazdır
                            string boyutMB = (disk.DiskBoyut / (1024.0 * 1024.0)).ToString("N0");
                            string erisimTuru = GetDiskAccessProperty(disk).ToString();

                            sb.AppendLine($"{disk.DiskSeriNo},{disk.DiskModel},{boyutMB},{disk.DiskTarih.ToString("dd.MM.yyyy HH:mm:ss")},{disk.Hash},{erisimTuru}");
                        }

                        // Dosyaya yaz
                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);

                        MessageBox.Show("Disk listesi başarıyla dışa aktarıldı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dışa aktarma sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Log dosyasını görüntüle
        private void btnViewLogs_Click(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists(logFilePath))
                {
                    MessageBox.Show("Log dosyası bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Log dosyasını varsayılan CSV görüntüleyici ile aç
                System.Diagnostics.Process.Start(logFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Log dosyası açılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        
    }
}