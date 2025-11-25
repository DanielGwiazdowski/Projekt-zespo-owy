using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Security.Cryptography;
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
using Projekt_zespołowy;

namespace Projekt_zespołowy
{
    public partial class LoginWindow : Window
    {
        private string connectionString = "Data Source=bazaAPH.db;Version=3;";

        // Publiczne właściwości do przekazania danych do MainWindow
        public string UserRole { get; set; } = "";
        public string Username { get; set; } = "";
        public int UserId { get; set; } = 0;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Proszę wprowadzić nazwę użytkownika i hasło.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    // ZAPYTANIE POPRAWIONE: Używamy 'id_uzytkownik' zgodnie z Twoją strukturą tabeli
                    string query = "SELECT id_uzytkownik, haslo_hash, rola FROM uzytkownicy WHERE login = @login";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", username);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // ODCZYT POPRAWIONY: Używamy 'id_uzytkownik'
                                int id = Convert.ToInt32(reader["id_uzytkownik"]);
                                string storedHash = reader["haslo_hash"].ToString();
                                string role = reader["rola"].ToString();

                                if (VerifyPassword(password, storedHash))
                                {
                                    // Ustawiamy właściwości LoginWindow do przekazania do MainWindow
                                    this.UserRole = role;
                                    this.Username = username;
                                    this.UserId = id;

                                    MessageBox.Show($"Zalogowano jako {username} ({role})",
                                        "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

                                    this.DialogResult = true;
                                    return;
                                }
                                else
                                {
                                    MessageBox.Show("Nieprawidłowe hasło.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Nie znaleziono użytkownika.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd połączenia z bazą danych: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        // Funkcja do weryfikacji hasła SHA-256
        private bool VerifyPassword(string password, string storedHash)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = sha.ComputeHash(passwordBytes);
                string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                return hashString == storedHash.ToLower();
            }
        }
    }
}