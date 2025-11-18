using System;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Projekt_zespołowy
{
    public partial class UserListWindow : Window
    {
        private readonly ObservableCollection<Uzytkownik> uzytkownicy = new();
        private readonly string _connectionString = "Data Source=bazaAPH.db;Version=3;";

        public UserListWindow()
        {
            InitializeComponent();
            ListaUzytkownikow.ItemsSource = uzytkownicy;
            ZaladujUzytkownikow();
        }

        private void ZaladujUzytkownikow()
        {
            uzytkownicy.Clear();

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id_uzytkownik, login, email, rola, data_rejestracji FROM uzytkownicy ORDER BY id_uzytkownik DESC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                uzytkownicy.Add(new Uzytkownik
                {
                    Id = Convert.ToInt32(reader["id_uzytkownik"]),
                    Login = reader["login"]?.ToString() ?? "",
                    Email = reader["email"]?.ToString() ?? "",
                    Rola = reader["rola"]?.ToString() ?? "",
                    DataRejestracji = reader["data_rejestracji"]?.ToString() ?? ""
                });
            }
        }

        private void UsunUzytkownika_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is null) return;

            int id = Convert.ToInt32(fe.Tag);

            var uzytkownik = uzytkownicy.FirstOrDefault(x => x.Id == id);
            if (uzytkownik == null) return;

            if (MessageBox.Show($"Usunąć użytkownika {uzytkownik.Login}?", "Potwierdź", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM uzytkownicy WHERE id_uzytkownik=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            uzytkownicy.Remove(uzytkownik);

            MessageBox.Show("Użytkownik został usunięty!", "Sukces");
        }
    }
}
