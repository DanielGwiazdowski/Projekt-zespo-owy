using System;
using System.Collections.Generic;
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
using System.Data.SQLite;
using System.Security.Cryptography;

namespace Projekt_zespołowy
{
    /// <summary>
    /// Logika interakcji dla klasy RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();

            // Podpięcie zdarzeń przycisków (dla przycisków w XAML)
            foreach (var child in LogicalTreeHelper.GetChildren(this))
            {
                if (child is Button btn)
                {
                    if (btn.Content.ToString() == "Zarejestruj")
                        btn.Click += BtnRegister_Click;
                    else if (btn.Content.ToString() == "Anuluj")
                        btn.Click += BtnCancel_Click;
                }
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string login = txtUsername.Text.Trim();
            string haslo = txtPassword.Password.Trim();
            string confirmHaslo = txtConfirmPassword.Password.Trim();
            string email = txtEmail.Text.Trim();

            // Walidacja pól
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(haslo) ||
                string.IsNullOrEmpty(confirmHaslo) || string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Uzupełnij wszystkie pola!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (haslo != confirmHaslo)
            {
                MessageBox.Show("Hasła nie są zgodne!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string hasloHash = HashPassword(haslo);

            try
            {
                string connectionString = "Data Source=bazaAPH.db;Version=3;";
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    // Sprawdź czy login już istnieje
                    string checkUser = "SELECT COUNT(*) FROM uzytkownicy WHERE login = @login";
                    using (SQLiteCommand checkCmd = new SQLiteCommand(checkUser, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@login", login);
                        long count = (long)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show("Taki login już istnieje. Wybierz inny.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    // Wstaw użytkownika do bazy
                    string insertUser = @"INSERT INTO uzytkownicy 
                        (login, haslo_hash, rola, email, data_rejestracji)
                        VALUES (@login, @haslo, 'klient', @email, @data)";
                    using (SQLiteCommand cmd = new SQLiteCommand(insertUser, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@haslo", hasloHash);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }

                    conn.Close();
                }

                MessageBox.Show("Rejestracja zakończona sukcesem!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas rejestracji: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        // Funkcja hashująca hasło przy użyciu SHA256
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
