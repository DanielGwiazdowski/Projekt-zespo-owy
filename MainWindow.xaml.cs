using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SQLite;

namespace Projekt_zespołowy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Otwieramy okno logowania
            LoginWindow loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog(); // Okno jako dialog

            if (result == true)
            {
                // Tutaj możesz pobrać np. loginWindow.txtUsername.Text
                MessageBox.Show($"Zalogowano użytkownika: {loginWindow.txtUsername.Text}",
                                "Logowanie", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            // Otwieramy okno rejestracji
            RegisterWindow registerWindow = new RegisterWindow();
            bool? result = registerWindow.ShowDialog(); // Okno jako dialog

            if (result == true)
            {
                // Tutaj możesz pobrać np. dane rejestracyjne
                MessageBox.Show($"Zarejestrowano użytkownika: {registerWindow.txtUsername.Text}",
                                "Rejestracja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Zamknięcie okna logowania/rejestracji po kliknięciu "Anuluj"
            Window window = (Window)((FrameworkElement)sender).TemplatedParent ??
                            (Window)((FrameworkElement)sender).Parent;
            window?.Close();
        }
        private bool IsLoggedIn = false; // tymczasowo, żeby kontrolować stan logowania

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoggedIn)
            {
                MessageBox.Show("Nie jesteś zalogowany!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Wylogowanie użytkownika
            IsLoggedIn = false;

            // Wyczyść zawartość głównego frame
            MainFrame.Navigate(null);

            MessageBox.Show("Wylogowano pomyślnie!", "Wylogowanie", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}