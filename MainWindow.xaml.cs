using Projekt_zespołowy.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Data.SQLite;

// DODANE
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Projekt_zespołowy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsLoggedIn = false; // tymczasowo, żeby kontrolować stan logowania
        private string LoggedUserRole = "";
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

                // pokaż admina tylko jeśli rola = admin
                if (LoggedUserRole == "admin")
                    BtnAdmin.Visibility = Visibility.Visible;
                else
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
                IsLoggedIn = true;

                // pobranie roli z LoginWindow
                LoggedUserRole = loginWindow.UserRole;

                UpdateAuthButtons();

                // jeśli admin → pokaż przycisk panelu admina
                if (LoggedUserRole == "admin")
                    BtnAdmin.Visibility = Visibility.Visible;

                MessageBox.Show($"Zalogowano użytkownika: {loginWindow.Username}",
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

        // =====================================================================
        //                          🔽 DODANE: FILTRY
        // =====================================================================

        // Pola stanu filtrów
        private Slider _priceMinSlider, _priceMaxSlider;
        private TextBlock _priceMinLabel, _priceMaxLabel;
        private CheckBox _brandLuk, _brandSachs, _brandValeo;
        private const int PRICE_MIN = 1500;
        private const int PRICE_MAX = 2000;

        /// <summary>
        /// Bez zmiany XAML-a dobudowujemy panel filtrów po lewej
        /// w momencie, gdy zawartość okna jest już załadowana.
        /// </summary>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            try
            {
                InjectFiltersPanel();
            }
            catch
            {
                // Jeśli layout się jeszcze nie zmaterializował — spróbujemy raz.
                Dispatcher.InvokeAsync(() =>
                {
                    try { InjectFiltersPanel(); } catch { /* ignorujemy */ }
                });
            }
        }

        private void InjectFiltersPanel()
        {
            // Znajdź jedynego ScrollViewera w oknie (ten z MainFrame)
            var mainScroll = FindDescendant<ScrollViewer>(this);
            if (mainScroll == null) return;

            // Jeżeli już wstrzyknięte — nie rób nic
            if (mainScroll.Content is Grid g && g.Tag as string == "InjectedWithFilters")
                return;

            // Zapamiętaj oryginalną zawartość (StackPanel z Frame)
            var originalContent = mainScroll.Content as FrameworkElement;
            if (originalContent == null) return;

            // Nowy grid 2-kolumnowy: lewo — filtry, prawo — Twoja treść
            var host = new Grid { Margin = new Thickness(0) };
            host.Tag = "InjectedWithFilters";
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 1) Panel filtrów
            var filtersBorder = BuildFiltersUi();
            Grid.SetColumn(filtersBorder, 0);
            host.Children.Add(filtersBorder);

            // 2) Oryginalna treść po prawej
            originalContent.Margin = new Thickness(0, 0, 0, 0);
            Grid.SetColumn(originalContent, 1);
            host.Children.Add(originalContent);

            // Podmień zawartość ScrollViewera
            mainScroll.Content = host;
        }

        private Border BuildFiltersUi()
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#e6e6e6"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Margin = new Thickness(12, 0, 12, 0)
            };

            var stack = new StackPanel();
            border.Child = stack;

            // Nagłówek
            stack.Children.Add(new TextBlock
            {
                Text = "Filtry",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // Cena
            stack.Children.Add(new TextBlock
            {
                Text = "Cena",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold
            });

            var slidersGrid = new Grid { Margin = new Thickness(0, 12, 0, 4) };
            slidersGrid.ColumnDefinitions.Add(new ColumnDefinition());
            slidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            slidersGrid.ColumnDefinitions.Add(new ColumnDefinition());

            _priceMinSlider = new Slider
            {
                Minimum = PRICE_MIN,
                Maximum = PRICE_MAX,
                Value = PRICE_MIN,
                TickFrequency = 50,
                IsSnapToTickEnabled = true
            };
            _priceMinSlider.ValueChanged += PriceSlider_ValueChanged;

            _priceMaxSlider = new Slider
            {
                Minimum = PRICE_MIN,
                Maximum = PRICE_MAX,
                Value = PRICE_MAX,
                TickFrequency = 50,
                IsSnapToTickEnabled = true
            };
            _priceMaxSlider.ValueChanged += PriceSlider_ValueChanged;

            Grid.SetColumn(_priceMinSlider, 0);
            Grid.SetColumn(_priceMaxSlider, 2);
            slidersGrid.Children.Add(_priceMinSlider);
            slidersGrid.Children.Add(_priceMaxSlider);

            stack.Children.Add(slidersGrid);

            // Etykiety zakresu
            var labelsGrid = new Grid();
            labelsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            labelsGrid.ColumnDefinitions.Add(new ColumnDefinition());

            _priceMinLabel = new TextBlock
            {
                Text = $"{PRICE_MIN} PLN",
                Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            _priceMaxLabel = new TextBlock
            {
                Text = $"{PRICE_MAX} PLN",
                Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(_priceMinLabel, 0);
            Grid.SetColumn(_priceMaxLabel, 1);
            labelsGrid.Children.Add(_priceMinLabel);
            labelsGrid.Children.Add(_priceMaxLabel);

            stack.Children.Add(labelsGrid);

            stack.Children.Add(new Separator { Margin = new Thickness(0, 16, 0, 12) });

            // Marka
            stack.Children.Add(new TextBlock
            {
                Text = "Marka",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            });

            _brandLuk = new CheckBox { Content = "LUK", FontSize = 14, Margin = new Thickness(0, 6, 0, 0) };
            _brandSachs = new CheckBox { Content = "Sachs", FontSize = 14, Margin = new Thickness(0, 6, 0, 0) };
            _brandValeo = new CheckBox { Content = "Valeo", FontSize = 14, Margin = new Thickness(0, 6, 0, 0) };

            stack.Children.Add(_brandLuk);
            stack.Children.Add(_brandSachs);
            stack.Children.Add(_brandValeo);

            var apply = new Button
            {
                Content = "Zastosuj",
                Margin = new Thickness(0, 20, 0, 0),
                Padding = new Thickness(12, 8, 12, 8),
                Background = (Brush)new BrushConverter().ConvertFromString("#1f6feb"),
                Foreground = Brushes.White,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#1f6feb"),
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            apply.Click += Apply_Click;

            stack.Children.Add(apply);

            return border;
        }

        private void PriceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Pilnujemy relacji MIN <= MAX
            if (_priceMinSlider == null || _priceMaxSlider == null) return;

            if (_priceMinSlider.Value > _priceMaxSlider.Value)
            {
                if (sender == _priceMinSlider)
                    _priceMaxSlider.Value = _priceMinSlider.Value;
                else
                    _priceMinSlider.Value = _priceMaxSlider.Value;
            }

            if (_priceMinLabel != null) _priceMinLabel.Text = $"{(int)_priceMinSlider.Value} PLN";
            if (_priceMaxLabel != null) _priceMaxLabel.Text = $"{(int)_priceMaxSlider.Value} PLN";
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            int min = (int)_priceMinSlider.Value;
            int max = (int)_priceMaxSlider.Value;

            var brands = new List<string>();
            if (_brandLuk.IsChecked == true) brands.Add("LUK");
            if (_brandSachs.IsChecked == true) brands.Add("Sachs");
            if (_brandValeo.IsChecked == true) brands.Add("Valeo");

            // << TU PODPINASZ WŁASNĄ LOGIKĘ FILTROWANIA >>
            // Np. przefiltruj kolekcję produktów i odśwież widok listy.
            // FilterProducts(min, max, brands);

            MessageBox.Show(
                $"Zakres ceny: {min}–{max} PLN\nMarki: {(brands.Count == 0 ? "wszystkie" : string.Join(", ", brands))}",
                "Zastosowano filtry",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Pomocnicza funkcja do wyszukania elementu w drzewie
        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t) return t;
                var result = FindDescendant<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
