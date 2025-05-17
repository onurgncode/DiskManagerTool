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
            // DataGridView sütunlarını temizle
            dgvDiskler.Columns.Clear();
            dgvDiskler.AutoGenerateColumns = false;

            // Temel sütunları ekle
            dgvDiskler.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colSeriNo",
                HeaderText = "Seri No",
                DataPropertyName = "DiskSeriNo",
                Width = 100
            });

            dgvDiskler.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colModel",
                HeaderText = "Disk Modeli",
                DataPropertyName = "DiskModel",
                Width = 120
            });

            dgvDiskler.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colBoyut",
                HeaderText = "Disk Boyutu (MB)",
                DataPropertyName = "DiskBoyutMB",
                Width = 100
            });

            dgvDiskler.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTarih",
                HeaderText = "Takılma Tarihi",
                DataPropertyName = "DiskTarih",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy HH:mm:ss" }
            });

            dgvDiskler.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colHash",
                HeaderText = "Hash Değeri",
                DataPropertyName = "Hash",
                Width = 150
            });

            dgvDiskler.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colErisimTuru",
                HeaderText = "Erişim Türü",
                DataPropertyName = "ErisimTuru",
                Width = 100,
                ReadOnly = true
            });

            // Tam Erişim Butonu
            var tamErisimColumn = new DataGridViewButtonColumn
            {
                Name = "colTamErisim",
                HeaderText = "Tam Erişim",
                Text = "Tam",
                UseColumnTextForButtonValue = true,
                Width = 60,
                FlatStyle = FlatStyle.Flat
            };
            dgvDiskler.Columns.Add(tamErisimColumn);

            // Salt Okunur Butonu
            var saltOkunurColumn = new DataGridViewButtonColumn
            {
                Name = "colSaltOkunur",
                HeaderText = "Salt Okunur",
                Text = "Salt",
                UseColumnTextForButtonValue = true,
                Width = 60,
                FlatStyle = FlatStyle.Flat
            };
            dgvDiskler.Columns.Add(saltOkunurColumn);

            // Erişim Yok Butonu
            var erisimYokColumn = new DataGridViewButtonColumn
            {
                Name = "colErisimYok",
                HeaderText = "Erişim Yok",
                Text = "Engelle",
                UseColumnTextForButtonValue = true,
                Width = 60,
                FlatStyle = FlatStyle.Flat
            };
            dgvDiskler.Columns.Add(erisimYokColumn);

            // Olayları ekle
            dgvDiskler.CellContentClick += DgvDiskler_CellContentClick;

            // Görünüm ayarları
            dgvDiskler.EnableHeadersVisualStyles = false;
            dgvDiskler.ColumnHeadersDefaultCellStyle.BackColor = Color.LightBlue;
            dgvDiskler.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgvDiskler.ColumnHeadersDefaultCellStyle.Font = new Font(dgvDiskler.Font, FontStyle.Bold);
            dgvDiskler.RowHeadersVisible = false;
            dgvDiskler.AllowUserToAddRows = false;

            // Satırlara alternatif renk verme
            dgvDiskler.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;
            dgvDiskler.DefaultCellStyle.SelectionBackColor = Color.LightSteelBlue;
            dgvDiskler.DefaultCellStyle.SelectionForeColor = Color.Black;
        }

        private void RefreshDataGridView()
        {
            try
            {
                // Grid'i temizle
                dgvDiskler.Rows.Clear();

                // Her disk için yeni bir satır oluştur
                foreach (var disk in diskler)
                {
                    int rowIndex = dgvDiskler.Rows.Add();
                    DataGridViewRow row = dgvDiskler.Rows[rowIndex];

                    // Temel bilgileri doldur
                    row.Cells["colSeriNo"].Value = disk.DiskSeriNo;
                    row.Cells["colModel"].Value = disk.DiskModel;
                    row.Cells["colBoyut"].Value = (disk.DiskBoyut / (1024 * 1024)).ToString("N0");
                    row.Cells["colTarih"].Value = disk.DiskTarih;
                    row.Cells["colHash"].Value = disk.Hash;
                    row.Cells["colErisimTuru"].Value = disk.ErisimTuru.ToString();

                    // Aktif erişim türüne göre buton renklerini ayarla
                    StyleButtonColumns(row, disk.ErisimTuru);
                }

                // Son satırı seç
                if (dgvDiskler.Rows.Count > 0)
                {
                    dgvDiskler.FirstDisplayedScrollingRowIndex = Math.Max(0, dgvDiskler.Rows.Count - 1);
                    dgvDiskler.Rows[dgvDiskler.Rows.Count - 1].Selected = true;
                }

                // Status bar'ı güncelle
                lblDiskSayisi.Text = $"Toplam {diskler.Count} disk bağlı";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Disk listesi yenilenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Buton hücrelerinin görünümünü erişim türüne göre değiştir
        private void StyleButtonColumns(DataGridViewRow row, DiskErisimTuru erisimTuru)
        {
            // Tüm butonları normal stilde başlat
            if (row.Cells["colTamErisim"] is DataGridViewButtonCell tamErisimCell)
            {
                tamErisimCell.Style.BackColor = Color.LightGray;
                tamErisimCell.Style.ForeColor = Color.Black;
            }

            if (row.Cells["colSaltOkunur"] is DataGridViewButtonCell saltOkunurCell)
            {
                saltOkunurCell.Style.BackColor = Color.LightGray;
                saltOkunurCell.Style.ForeColor = Color.Black;
            }

            if (row.Cells["colErisimYok"] is DataGridViewButtonCell erisimYokCell)
            {
                erisimYokCell.Style.BackColor = Color.LightGray;
                erisimYokCell.Style.ForeColor = Color.Black;
            }

            // Aktif olan erişim türünü vurgula
            switch (erisimTuru)
            {
                case DiskErisimTuru.TamErisim:
                    if (row.Cells["colTamErisim"] is DataGridViewButtonCell activeTamCell)
                    {
                        activeTamCell.Style.BackColor = Color.Green;
                        activeTamCell.Style.ForeColor = Color.White;
                    }
                    break;
                case DiskErisimTuru.SaltOkunur:
                    if (row.Cells["colSaltOkunur"] is DataGridViewButtonCell activeSaltCell)
                    {
                        activeSaltCell.Style.BackColor = Color.Orange;
                        activeSaltCell.Style.ForeColor = Color.White;
                    }
                    break;
                case DiskErisimTuru.ErisimYok:
                    if (row.Cells["colErisimYok"] is DataGridViewButtonCell activeErisimYokCell)
                    {
                        activeErisimYokCell.Style.BackColor = Color.Red;
                        activeErisimYokCell.Style.ForeColor = Color.White;
                    }
                    break;
            }
        }

        private void DgvDiskler_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Erişim butonu tıklandığında
            if (e.RowIndex >= 0)
            {
                DiskErisimTuru erisimTuru = DiskErisimTuru.TamErisim; // Varsayılan
                bool isErisimButonu = false;

                if (e.ColumnIndex == dgvDiskler.Columns["colTamErisim"].Index)
                {
                    erisimTuru = DiskErisimTuru.TamErisim;
                    isErisimButonu = true;
                }
                else if (e.ColumnIndex == dgvDiskler.Columns["colSaltOkunur"].Index)
                {
                    erisimTuru = DiskErisimTuru.SaltOkunur;
                    isErisimButonu = true;
                }
                else if (e.ColumnIndex == dgvDiskler.Columns["colErisimYok"].Index)
                {
                    erisimTuru = DiskErisimTuru.ErisimYok;
                    isErisimButonu = true;
                }

                if (isErisimButonu)
                {
                    try
                    {
                        // Disk bilgilerini al
                        string diskSeriNo = dgvDiskler.Rows[e.RowIndex].Cells["colSeriNo"].Value.ToString();
                        string diskModel = dgvDiskler.Rows[e.RowIndex].Cells["colModel"].Value.ToString();

                        // İlgili diski bul
                        Disk disk = diskler.FirstOrDefault(d => d.DiskSeriNo == diskSeriNo);
                        if (disk == null)
                        {
                            MessageBox.Show("Belirtilen disk bulunamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Eğer aynı erişim türüne tekrar tıklandıysa işlem yapma
                        if (disk.ErisimTuru == erisimTuru)
                        {
                            ShowNotification($"{diskModel} zaten {erisimTuru} modunda", Color.Blue);
                            return;
                        }

                        // Erişim türünü değiştir
                        disk.ErisimTuru = erisimTuru;

                        // Erişim türünü uygula
                        UygulaErisimTuru(disk.DiskModel, erisimTuru);

                        // Buton stilini güncelle
                        StyleButtonColumns(dgvDiskler.Rows[e.RowIndex], erisimTuru);

                        // Erişim türü hücresini güncelle
                        dgvDiskler.Rows[e.RowIndex].Cells["colErisimTuru"].Value = erisimTuru.ToString();

                        // İşlemi logla
                        LogDiskIslem("Erişim Değiştirildi", disk.DiskModel, erisimTuru.ToString());

                        // Kullanıcıya bilgi ver
                        ShowNotification($"{diskModel} için {erisimTuru} erişimi uygulandı", GetStatusColorForAccessType(erisimTuru));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erişim türü uygulanırken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Erişim türüne göre renk döndür
        private Color GetStatusColorForAccessType(DiskErisimTuru erisimTuru)
        {
            switch (erisimTuru)
            {
                case DiskErisimTuru.TamErisim:
                    return Color.Green;
                case DiskErisimTuru.SaltOkunur:
                    return Color.Orange;
                case DiskErisimTuru.ErisimYok:
                    return Color.Red;
                default:
                    return Color.Blue;
            }
        }

        private void UygulaErisimTuru(string diskYolu, DiskErisimTuru erisimTuru)
        {
            if (string.IsNullOrEmpty(diskYolu))
            {
                throw new ArgumentException("Disk yolu boş olamaz!");
            }

            try
            {
                // DiskPermissionManager sınıfını kullanarak gerçek disk izinlerini uygula
                bool basarili = DiskYonetim.AppClass.DiskPermissionManager.UygulaErisimTuru(diskYolu, erisimTuru);

                if (!basarili)
                {
                    // İzin değişikliği başarısız oldu, kullanıcıya bilgi ver
                    throw new Exception($"{diskYolu} için {erisimTuru} erişimi uygulanamadı");
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda kullanıcıya bilgi ver
                throw new Exception($"Disk izinleri ayarlanırken hata: {ex.Message}");
            }
        }

        private void SetDiskPermission(string diskYolu, bool okuma, bool yazma)
        {
            try
            {
                // Bu metod Windows API veya uygun bir kütüphane kullanılarak genişletilebilir
                // Şu anki implementasyon örnek amaçlıdır

                // Örnek olarak, sadece bildirim gösteriyoruz
                string mesaj = $"{diskYolu} için ";
                if (okuma && yazma)
                    mesaj += "tam erişim uygulandı.";
                else if (okuma && !yazma)
                    mesaj += "salt okuma izni uygulandı.";
                else
                    mesaj += "tüm erişimler engellendi.";

                // Gerçek uygulamada Windows API çağrıları yapılmalı
                Console.WriteLine(mesaj);

                // Bildirim göster ama MessageBox kullanma (akışı bozmasın)
                // MessageBox.Show(mesaj, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Disk izinleri ayarlanırken hata: {ex.Message}");
            }
        }

        private void LoadInitialDisks()
        {
            try
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
                            disk.ErisimTuru = DiskErisimTuru.TamErisim;
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
                        disk.ErisimTuru = DiskErisimTuru.TamErisim;
                    }

                    // Form yüklendiğinde grid'i güncelle
                    this.Load += (s, e) =>
                    {
                        RefreshDataGridView();
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Başlangıç diskleri yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DiskHash_DiskTakildi(object sender, DiskEventArgs e)
        {
            try
            {
                // Form henüz yüklenmemiş olabilir, bu yüzden kontrolü ekleyin
                if (this.IsHandleCreated)
                {
                    // UI thread'ine geri dön
                    this.Invoke((MethodInvoker)delegate
                    {
                        // Yeni disk için varsayılan erişim türü
                        e.Disk.ErisimTuru = DiskErisimTuru.TamErisim;

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
                    e.Disk.ErisimTuru = DiskErisimTuru.TamErisim;
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
            catch (Exception ex)
            {
                Console.WriteLine($"Disk takılma olayında hata: {ex.Message}");
            }
        }

        private void DiskHash_DiskCikarildi(object sender, DiskEventArgs e)
        {
            try
            {
                // Form henüz yüklenmemiş olabilir, bu yüzden kontrolü ekleyin
                if (this.IsHandleCreated)
                {
                    // UI thread'ine geri dön
                    this.Invoke((MethodInvoker)delegate
                    {
                        // Disk çıkarılmadan önce logla
                        string erisimTuru = e.Disk.ErisimTuru.ToString();
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
                    string erisimTuru = e.Disk.ErisimTuru.ToString();
                    LogDiskIslem("Disk Çıkarıldı", e.Disk.DiskModel, erisimTuru);

                    diskler.Remove(e.Disk);

                    // Form yüklendiğinde bildirim göster
                    this.Load += (s, ev) =>
                    {
                        ShowNotification($"Disk çıkarıldı: {e.Disk.DiskSeriNo} ({e.Disk.DiskModel})", Color.Red);
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Disk çıkarılma olayında hata: {ex.Message}");
            }
        }

        private void ShowNotification(string message, Color color)
        {
            try
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Bildirim gösterirken hata: {ex.Message}");
            }
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
                            string erisimTuru = disk.ErisimTuru.ToString();

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