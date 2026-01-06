using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace Projekt_zespo³owy.Views
{
    public partial class ClaimsWindow : Window
    {
        // Korzystamy z tej samej œcie¿ki co w AdminWindow
        private readonly string _connectionString = "Data Source=bazaAPH.db;Version=3;";

        public ClaimsWindow()
        {
            InitializeComponent();
        }

        private void BtnSendClaim_Click(object sender, RoutedEventArgs e)
        {
            string orderIdRaw = InputOrderId.Text;
            string reason = InputClaimReason.Text;

            // 1. Walidacja danych wejœciowych
            if (string.IsNullOrWhiteSpace(orderIdRaw) || string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Proszê wype³niæ wszystkie pola formularza.", "B³¹d", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(orderIdRaw, out int orderId))
            {
                MessageBox.Show("Numer zamówienia musi byæ liczb¹.", "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // 2. Po³¹czenie z baz¹ (œcie¿ka dynamiczna, aby zawsze trafiæ w plik)
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bazaAPH.db");

                using (var con = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    con.Open();

                    // 3. SQL UPDATE - zmiana statusu na "Reklamacja"
                    // Mo¿esz tu te¿ dodaæ kolumnê 'uwagi_reklamacyjne' jeœli masz j¹ w bazie
                    string sql = "UPDATE zamowienia SET status = @status WHERE id_zamowienia = @id";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@status", "Reklamacja");
                        cmd.Parameters.AddWithValue("@id", orderId);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        // 4. Sprawdzenie, czy zamówienie w ogóle istnia³o
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show($"Reklamacja Wys³ana! Wkrótce siê z Tob¹ skontaktujemy.",
                                            "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Opcjonalnie: Tutaj móg³byœ wywo³aæ wysy³kê maila, jak w AdminWindow
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("? Nie znaleziono zamówienia o podanym numerze w bazie danych.",
                                            "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("B³¹d bazy danych: " + ex.Message, "B³¹d krytyczny", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}