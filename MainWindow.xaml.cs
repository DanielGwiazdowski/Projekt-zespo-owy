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

            // --- KOSZYK: zainicjalizuj badge po starcie ---
            UpdateCartBadge();
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

        // ================================
        //           LOGIKA KOSZYKA
        // ================================
        private int _cartCount = 0;

        /// <summary>
        /// Aktualizuje widoczność i tekst badge'a na ikonie koszyka.
        /// Bezpieczne, gdy kontrolki jeszcze nie są zmaterializowane.
        /// </summary>
        private void UpdateCartBadge()
        {
            // Jeżeli XAML nie ma tych elementów (np. inna strona), po prostu wyjdź.
            if (CartBadge == null || CartCountText == null)
                return;

            CartCountText.Text = _cartCount.ToString();
            CartBadge.Visibility = _cartCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Wywołuj, kiedy użytkownik doda przedmiot do koszyka.
        /// </summary>
        public void AddToCart(object? item = null)
        {
            _cartCount++;
            UpdateCartBadge();
        }

        /// <summary>
        /// Opcjonalnie: zmniejsz licznik (nie schodzi poniżej zera).
        /// </summary>
        public void RemoveFromCart(int qty = 1)
        {
            _cartCount -= qty;
            if (_cartCount < 0) _cartCount = 0;
            UpdateCartBadge();
        }

        /// <summary>
        /// Kliknięcie w ikonę koszyka.
        /// Jeżeli masz stronę koszyka (CartPage), odkomentuj nawigację.
        /// </summary>
        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            // Jeśli masz stronę koszyka:
            // MainFrame.Navigate(new CartPage());

            // Na razie prosta informacja:
            MessageBox.Show($"Koszyk: {_cartCount} element(ów).", "Koszyk",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// (opcjonalnie) Wystaw licznik na zewnątrz, gdyby inne okna chciały go odczytać.
        /// </summary>
        public int CartCount => _cartCount;
    }
}
