using Projekt_zespołowy.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Projekt_zespołowy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsLoggedIn = false; // tymczasowo, żeby kontrolować stan logowania
        private int _cartCount = 0; // licznik koszyka

        public MainWindow()
        {
            InitializeComponent();

            // Inicjalizacja przy starcie
            UpdateCartBadge();
            UpdateAuthButtons(); // ustawia widoczność przycisków logowania
        }

        // ================================
        //        LOGIKA AUTORYZACJI
        // ================================

        /// <summary>
        /// Uaktualnia widoczność przycisków logowania / rejestracji / wylogowania.
        /// </summary>
        private void UpdateAuthButtons()
        {
            if (IsLoggedIn)
            {
                BtnLogin.Visibility = Visibility.Collapsed;
                BtnRegister.Visibility = Visibility.Collapsed;
                BtnLogout.Visibility = Visibility.Visible;

                // Tymczasowo ukrywamy panel admina
                BtnAdmin.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnLogin.Visibility = Visibility.Visible;
                BtnRegister.Visibility = Visibility.Visible;
                BtnLogout.Visibility = Visibility.Collapsed;

                BtnAdmin.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Otwieramy okno logowania
            LoginWindow loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog(); // Okno jako dialog

            if (result == true)
            {
                // Po udanym logowaniu
                IsLoggedIn = true;
                UpdateAuthButtons();

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
                // Po udanej rejestracji
                MessageBox.Show($"Zarejestrowano użytkownika: {registerWindow.txtUsername.Text}",
                                "Rejestracja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoggedIn)
            {
                MessageBox.Show("Nie jesteś zalogowany!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Wylogowanie użytkownika
            IsLoggedIn = false;
            UpdateAuthButtons();

            // Wyczyść zawartość głównego frame
            MainFrame.Navigate(null);

            MessageBox.Show("Wylogowano pomyślnie!", "Wylogowanie", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Zamknięcie okna logowania/rejestracji po kliknięciu "Anuluj"
            Window window = (Window)((FrameworkElement)sender).TemplatedParent ??
                            (Window)((FrameworkElement)sender).Parent;
            window?.Close();
        }

        // ================================
        //           LOGIKA KOSZYKA
        // ================================

        /// <summary>
        /// Aktualizuje widoczność i tekst badge'a na ikonie koszyka.
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
        /// Dodaje element do koszyka.
        /// </summary>
        public void AddToCart(object? item = null)
        {
            _cartCount++;
            UpdateCartBadge();
        }

        /// <summary>
        /// Usuwa element(y) z koszyka.
        /// </summary>
        public void RemoveFromCart(int qty = 1)
        {
            _cartCount -= qty;
            if (_cartCount < 0) _cartCount = 0;
            UpdateCartBadge();
        }

        /// <summary>
        /// Kliknięcie w ikonę koszyka.
        /// </summary>
        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            var cartWindow = new Projekt_zespołowy.Views.CartPage
            {
                Owner = this
            };
            cartWindow.ShowDialog();
        }

        /// <summary>
        /// Odczyt aktualnej liczby elementów w koszyku.
        /// </summary>
        public int CartCount => _cartCount;
    }
}
