using System;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace czesci1
{
    public partial class LoginWindow : Window
    {
        public string Username { get; private set; }
        public int UserId { get; private set; }
        public bool IsAdmin { get; private set; }

        private const string ConnectionString = "Data Source=database.db;Version=3;Timeout=30;";
        private const string SelectUserQuery = @"
            SELECT UserId, Username, IsAdmin 
            FROM Users 
            WHERE (Username = @username OR Email = @username) 
            AND Password = @password";

        public LoginWindow()
        {
            InitializeComponent();
            txtUsername.Focus();
            txtUsername.KeyDown += (s, e) => { if (e.Key == Key.Enter) txtPassword.Focus(); };
            txtPassword.KeyDown += (s, e) => { if (e.Key == Key.Enter) BtnLogin_Click(s, e); };
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Proszę wprowadzić nazwę użytkownika i hasło.", "Błąd logowania",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AuthenticateUser(username, password))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Nieprawidłowa nazwa użytkownika lub hasło.", "Błąd logowania",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool AuthenticateUser(string username, string password)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        dynamic user = null;
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = SelectUserQuery;
                            command.Parameters.AddWithValue("@username", username);
                            command.Parameters.AddWithValue("@password", HashPassword(password));

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    user = new
                                    {
                                        UserId = Convert.ToInt32(reader["UserId"]),
                                        Username = reader["Username"].ToString(),
                                        IsAdmin = Convert.ToBoolean(reader["IsAdmin"])
                                    };
                                }
                            }
                        }

                        if (user == null) return false;

                        UserId = user.UserId;
                        Username = user.Username;
                        IsAdmin = user.IsAdmin;

                        using (var updateCommand = connection.CreateCommand())
                        {
                            updateCommand.CommandText =
                                "UPDATE Users SET LastLogin = datetime('now', 'localtime') WHERE UserId = @userId";
                            updateCommand.Parameters.AddWithValue("@userId", UserId);
                            updateCommand.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
