using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
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
using static Projekt_zespołowy.MainWindow;

#nullable enable
using IOPath = System.IO.Path;

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
        }

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
    }
}
