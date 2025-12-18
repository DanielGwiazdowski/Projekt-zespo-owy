using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using static Projekt_zespołowy.MainWindow;

#nullable enable
using IOPath = System.IO.Path;
using System.Net.Mail;
using System.Net;

namespace Projekt_zespołowy
{
    public partial class AdminWindow : Window
    {
        private string? wybraneZdjecie;
        private readonly ObservableCollection<Produkt> produkty = new();
        private readonly string _connectionString = "Data Source=bazaAPH.db;Version=3;";

        public AdminWindow()
        {


            InitializeComponent();

            // Kategorie przykładowe
            KategoriaElementu.Items.Add("uklad_elektryczny");
            KategoriaElementu.Items.Add("uklad_hamulcowy");
            KategoriaElementu.Items.Add("uklad_napedowy");
            KategoriaElementu.Items.Add("oleje");
            KategoriaElementu.Items.Add("filtry");
            KategoriaElementu.Items.Add("sprzegla");
            KategoriaElementu.Items.Add("kola_dwumasowe");

            ListaElementow.ItemsSource = produkty;

            ZaladujProdukty();
            ZaladujZamowienia();

        }

        private ObservableCollection<ZamowienieAdmin> Zamowienia
        = new ObservableCollection<ZamowienieAdmin>(); 

        private void ZaladujProdukty()
        {
            produkty.Clear();

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id_produktu, nazwa, opis, kategoria, producent, cena_netto, zdjecie FROM produkty ORDER BY id_produktu DESC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                produkty.Add(new Produkt
                {
                    Id = Convert.ToInt32(reader["id_produktu"]),
                    Nazwa = reader["nazwa"]?.ToString() ?? "",
                    Opis = reader["opis"]?.ToString() ?? "",
                    Producent = reader["producent"]?.ToString() ?? "",
                    Kategoria = reader["kategoria"]?.ToString() ?? "",
                    Cena = reader["cena_netto"] != DBNull.Value ? Convert.ToDecimal(reader["cena_netto"]) : 0,
                    Zdjecie = reader["zdjecie"]?.ToString() ?? ""
                });
            }
        }

        private void ZaladujZamowienia()
        {
            Zamowienia.Clear();

            using (var con = new SQLiteConnection("Data Source=bazaAPH.db;Version=3;"))
            {
                con.Open();

                using (var cmd = new SQLiteCommand(
                    @"SELECT 
                id_zamowienia, 
                data_zamowienia, 
                status, 
                suma_brutto 
              FROM zamowienia
              ORDER BY id_zamowienia DESC", con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Zamowienia.Add(new ZamowienieAdmin
                        {
                            Id = reader.GetInt32(0),
                            Data = reader.GetString(1),
                            Status = reader.GetString(2),
                            Suma = reader.GetDouble(3)
                        });
                    }
                }
            }

            ListaZamowien.ItemsSource = Zamowienia;
        }

        // HANDLER DO OTWIERANIA OKNA Z UŻYTKOWNIKAMI
        private void OtworzListeUzytkownikow_Click(object sender, RoutedEventArgs e)
        {
            UserListWindow userWindow = new UserListWindow();
            userWindow.Show();
        }

        private void WybierzZdjecie_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                Filter = "Obrazy|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };

            if (dlg.ShowDialog() == true)
            {
                wybraneZdjecie = dlg.FileName;

                BitmapImage bmp = new();
                bmp.BeginInit();
                bmp.UriSource = new Uri(wybraneZdjecie);
                bmp.EndInit();

                PodgladZdjecia.Source = bmp;
            }
        }

        private void DodajElement_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NazwaElementu.Text))
            {
                MessageBox.Show("Podaj nazwę.", "Błąd");
                return;
            }

            if (string.IsNullOrWhiteSpace(OpisElementu.Text))
            {
                MessageBox.Show("Podaj opis.", "Błąd");
                return;
            }

            if (string.IsNullOrWhiteSpace(ProducentElementu.Text))
            {
                MessageBox.Show("Podaj producenta.", "Błąd");
                return;
            }

            if (!decimal.TryParse(CenaElementu.Text, out decimal cena))
            {
                MessageBox.Show("Podaj poprawną cenę netto.", "Błąd");
                return;
            }

            if (wybraneZdjecie is null)
            {
                MessageBox.Show("Wybierz zdjęcie.", "Błąd");
                return;
            }

            if (KategoriaElementu.SelectedItem is null)
            {
                MessageBox.Show("Wybierz kategorię.", "Błąd");
                return;
            }

            string fileName = IOPath.GetFileName(wybraneZdjecie);
            string dest = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", fileName);
            Directory.CreateDirectory(IOPath.GetDirectoryName(dest)!);
            File.Copy(wybraneZdjecie, dest, true);
            string relativePath = "/images/" + fileName;

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
    INSERT INTO produkty (nazwa, opis, producent, kategoria, cena_netto, zdjecie)
    VALUES (@nazwa, @opis, @producent, @kategoria, @cena, @zdjecie)";
            cmd.Parameters.AddWithValue("@nazwa", NazwaElementu.Text);
            cmd.Parameters.AddWithValue("@opis", OpisElementu.Text);
            cmd.Parameters.AddWithValue("@producent", ProducentElementu.Text);
            cmd.Parameters.AddWithValue("@kategoria", KategoriaElementu.SelectedItem!.ToString());
            cmd.Parameters.AddWithValue("@cena", cena);
            cmd.Parameters.AddWithValue("@zdjecie", relativePath);

            cmd.ExecuteNonQuery();

            ZaladujProdukty();

            NazwaElementu.Text = "";
            OpisElementu.Text = "";
            ProducentElementu.Text = "";
            CenaElementu.Text = "";
            PodgladZdjecia.Source = null;
            KategoriaElementu.SelectedIndex = -1;
            wybraneZdjecie = null;

            MessageBox.Show("Dodano produkt do bazy.");
        }

        private void UsunElement_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is null) return;

            int id = Convert.ToInt32(fe.Tag);

            var produkt = produkty.FirstOrDefault(x => x.Id == id);
            if (produkt == null) return;

            if (MessageBox.Show($"Usunąć {produkt.Nazwa}?", "Potwierdź", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM produkty WHERE id_produktu=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            produkty.Remove(produkt);
        }
        private async void GenerujRaportSprzedazy_Click(object sender, RoutedEventArgs e)
        {
            if (DataOd.SelectedDate == null || DataDo.SelectedDate == null)
            {
                MessageBox.Show("Wybierz zakres dat.");
                return;
            }

            DateTime od = DataOd.SelectedDate.Value;
            DateTime doo = DataDo.SelectedDate.Value.AddDays(1);

            decimal suma = 0;
            int liczba = 0;

            try
            {
                using var con = new SQLiteConnection(_connectionString); // używamy tej samej bazy co reszta
                await con.OpenAsync();

                string sql = @"
            SELECT SUM(suma_brutto) AS SumaKwot,
                   COUNT(*) AS LiczbaZamowien
            FROM zamowienia
            WHERE datetime(data_zamowienia) >= datetime(@od)
              AND datetime(data_zamowienia) < datetime(@do)
        ";

                using var cmd = new SQLiteCommand(sql, con);
                cmd.Parameters.AddWithValue("@od", od.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@do", doo.ToString("yyyy-MM-dd HH:mm:ss"));

                using var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    suma = reader["SumaKwot"] != DBNull.Value ? Convert.ToDecimal(reader["SumaKwot"]) : 0;
                    liczba = reader["LiczbaZamowien"] != DBNull.Value ? Convert.ToInt32(reader["LiczbaZamowien"]) : 0;
                }

                PodsumowanieRaportu.Text =
                    $"💰 Suma sprzedaży brutto: {suma:C}\n" +
                    $"🧾 Liczba zamówień: {liczba}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas generowania raportu: " + ex.Message);
            }
        }

        public class ZamowienieAdmin
        {
            public int Id { get; set; }
            public string Data { get; set; }
            public string Status { get; set; }
            public double Suma { get; set; }
        }


        // ===== ID WYBRANEGO ZAMÓWIENIA =====
        private int SelectedOrderId = 0;

        // ===== WYBÓR ZAMÓWIENIA =====
        private void ListaZamowien_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListaZamowien.SelectedItem is ZamowienieAdmin zam)
            {
                SelectedOrderId = zam.Id;
            }
        }

        // ===== ZMIANA STATUSU =====

        private void ZmienStatusZamowienia_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedOrderId == 0)
            {
                MessageBox.Show("Nie wybrano zamówienia");
                return;
            }

            if (StatusCombo.SelectedItem == null)
            {
                MessageBox.Show("Wybierz status");
                return;
            }

            string newStatus =
                (StatusCombo.SelectedItem as ComboBoxItem).Content.ToString();

            try
            {
                string dbPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "bazaAPH.db"
                );

                using (var con = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    con.Open();

                    using (var cmd = new SQLiteCommand(
                        "UPDATE zamowienia SET status = @s WHERE id_zamowienia = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@s", newStatus);
                        cmd.Parameters.AddWithValue("@id", SelectedOrderId);

                        int rows = cmd.ExecuteNonQuery();

                        if (rows == 0)
                        {
                            MessageBox.Show("❌ Status NIE został zmieniony (brak rekordu)");
                            return;
                        }
                    }
                }

                // 🔥 WYSYŁKA MAILA
                SendStatusChangeEmail(SelectedOrderId, newStatus);

                MessageBox.Show("✅ Status zmieniony i mail wysłany");
                ZaladujZamowienia(); // odśwież listę
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SendStatusChangeEmail(int orderId, string newStatus)
        {
            var mail = new MailMessage();
            mail.From = new MailAddress("esmeralda48@ethereal.email");

            // testowo – Ethereal przyjmie każdy adres
            mail.To.Add("ehereal@email.pl");

            mail.Subject = $"Zmiana statusu zamówienia #{orderId}";
            mail.Body =
                $"Status zamówienia #{orderId} został zmieniony.\n\n" +
                $"Nowy status: {newStatus}\n\n" +
                $"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            var smtp = new SmtpClient("smtp.ethereal.email", 587)
            {
                Credentials = new NetworkCredential(
                    "esmeralda48@ethereal.email",
                    "aqa7jcJRjVNdwDBuUg"
                ),
                EnableSsl = true
            };

            smtp.Send(mail);
        }


        private async void EksportujRaportPDF_Click(object sender, RoutedEventArgs e)
        {
            if (DataOd.SelectedDate == null || DataDo.SelectedDate == null)
            {
                MessageBox.Show("Wybierz zakres dat.");
                return;
            }

            DateTime od = DataOd.SelectedDate.Value;
            DateTime doo = DataDo.SelectedDate.Value.AddDays(1);

            decimal suma = 0;
            int liczba = 0;

            // 1️⃣ Pobieranie danych z SQLite
            using (var con = new SQLiteConnection(_connectionString))
            {
                await con.OpenAsync();

                string sql = @"
            SELECT SUM(suma_brutto) AS SumaKwot,
                   COUNT(*) AS LiczbaZamowien
            FROM zamowienia
            WHERE datetime(data_zamowienia) >= datetime(@od)
              AND datetime(data_zamowienia) < datetime(@do)
        ";

                using var cmd = new SQLiteCommand(sql, con);
                cmd.Parameters.AddWithValue("@od", od.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@do", doo.ToString("yyyy-MM-dd HH:mm:ss"));

                using var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    suma = reader["SumaKwot"] != DBNull.Value ? Convert.ToDecimal(reader["SumaKwot"]) : 0;
                    liczba = reader["LiczbaZamowien"] != DBNull.Value ? Convert.ToInt32(reader["LiczbaZamowien"]) : 0;
                }
            }

            // 2️⃣ Tworzenie PDF
            string filePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"RaportSprzedazy_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            var doc = new PdfSharp.Pdf.PdfDocument();
            doc.Info.Title = "Raport sprzedaży";

            var page = doc.AddPage();
            var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
            var fontTitle = new PdfSharp.Drawing.XFont("Arial", 18);
            var fontText = new PdfSharp.Drawing.XFont("Arial", 12);

            int y = 40;

            gfx.DrawString("Raport sprzedaży", fontTitle, PdfSharp.Drawing.XBrushes.Black, 40, y);
            y += 40;

            gfx.DrawString($"Zakres dat: {od:yyyy-MM-dd} → {doo.AddDays(-1):yyyy-MM-dd}", fontText, PdfSharp.Drawing.XBrushes.Black, 40, y);
            y += 25;

            gfx.DrawString($"Suma sprzedaży brutto:  {suma:C}", fontText, PdfSharp.Drawing.XBrushes.Black, 40, y);
            y += 20;

            gfx.DrawString($"Liczba zamówień:  {liczba}", fontText, PdfSharp.Drawing.XBrushes.Black, 40, y);
            y += 20;

            gfx.DrawString($"Data generowania: {DateTime.Now:yyyy-MM-dd HH:mm}", fontText, PdfSharp.Drawing.XBrushes.Gray, 40, y);

            doc.Save(filePath);

            // 3️⃣ Automatyczne otwarcie PDF w przeglądarce
            System.Diagnostics.Process.Start(new ProcessStartInfo()
            {
                FileName = filePath,
                UseShellExecute = true
            });

            MessageBox.Show("PDF został wygenerowany i otwarty.");
        }

    }
}
