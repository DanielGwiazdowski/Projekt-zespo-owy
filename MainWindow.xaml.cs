using Projekt_zespołowy.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;

namespace Projekt_zespołowy
{
    public partial class MainWindow : Window
    {
        // ====== STAN APLIKACJI ======
        private int _cartCount = 0;
        public static class UserSession
        {
            public static bool IsLogged { get; set; } = false;
            public static string Username { get; set; } = "";
            public static string Role { get; set; } = "";
            public static int UserId { get; set; } = 0;
        }

        // ====== NOWE: przechowywanie produktów i kategorii ======
        private List<Produkt> _allProducts = new List<Produkt>();
        private List<Produkt> _currentFilteredProducts = new List<Produkt>();

        // ⭐️ DODANE POLE DO PRZECHOWYWANIA AKTYWNEJ KATEGORII
        private string _currentCategory = null;

        public MainWindow()
        {
            InitializeComponent();

            CartPage.SharedStore.PropertyChanged += SharedStore_PropertyChanged;

            UpdateCartBadge();
            UpdateAuthButtons();

            try
            {
                _allProducts = PobierzProdukty() ?? new List<Produkt>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas pobierania produktów: {ex.Message}");
                _allProducts = new List<Produkt>();
            }

            _currentFilteredProducts = new List<Produkt>(_allProducts);
            WyswietlProdukty(_currentFilteredProducts);

            Console.WriteLine($"[DEBUG] Załadowano {_allProducts.Count} produktów z bazy.");
        }

        private void SharedStore_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartPage.CartStore.ItemsCount))
            {
                _cartCount = CartPage.SharedStore.ItemsCount;
                UpdateCartBadge();
            }
        }

        private void UpdateAuthButtons()
        {
            if (UserSession.IsLogged)
            {
                BtnLogin.Visibility = Visibility.Collapsed;
                BtnRegister.Visibility = Visibility.Collapsed;
                BtnLogout.Visibility = Visibility.Visible;
                BtnAdmin.Visibility = UserSession.Role == "admin" ? Visibility.Visible : Visibility.Collapsed;
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
            LoginWindow loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog();

            if (result == true)
            {
                UserSession.IsLogged = true;
                UserSession.Role = loginWindow.UserRole;
                UserSession.Username = loginWindow.Username;
                UserSession.UserId = loginWindow.UserId;

                UpdateAuthButtons();

                if (UserSession.Role == "admin")
                    BtnAdmin.Visibility = Visibility.Visible;

                MessageBox.Show($"Zalogowano użytkownika: {loginWindow.Username}");
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            bool? result = registerWindow.ShowDialog();

            if (result == true)
            {
                MessageBox.Show($"Zarejestrowano użytkownika!");
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.IsLogged)
            {
                MessageBox.Show("Nie jesteś zalogowany!");
                return;
            }

            UserSession.IsLogged = false;
            UserSession.Username = "";
            UserSession.Role = "";
            UserSession.UserId = 0;

            UpdateAuthButtons();
            BtnAdmin.Visibility = Visibility.Collapsed;
            MainFrame.Navigate(null);

            MessageBox.Show("Wylogowano pomyślnie!");
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Window window = (Window)((FrameworkElement)sender).TemplatedParent ??
                             (Window)((FrameworkElement)sender).Parent;
            window?.Close();
        }

        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            AdminWindow adminWindow = new AdminWindow { Owner = this };
            adminWindow.ShowDialog();
        }

        public class Produkt
        {
            public int Id { get; set; }
            public string Nazwa { get; set; } = "";
            public string Opis { get; set; } = "";
            public string Kategoria { get; set; } = "";
            public string Producent { get; set; } = "";
            public decimal Cena { get; set; }
            public string Zdjecie { get; set; } = "";

            public int ilość { get; set; } = 10;
        }

        private List<Produkt> PobierzProdukty()
        {
            var produkty = new List<Produkt>();
            string connectionString = "Data Source=bazaAPH.db;Version=3;";

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM produkty";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
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
            }
            return produkty;
        }

        private void WyswietlProdukty(List<Produkt> produkty)
        {
            Console.WriteLine($"[DEBUG] WyswietlProdukty: {produkty?.Count ?? 0} elementów");

            if (ProductsWrapPanel == null) return;
            ProductsWrapPanel.Children.Clear();

            foreach (var p in produkty)
            {
                var border = new Border
                {
                    Width = 150,
                    Margin = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(5)
                };

                var panel = new StackPanel { Orientation = Orientation.Vertical };
                border.Child = panel;

                // Logika wyświetlania zdjęć... (pominięta dla zwięzłości)
                // ...

                var img = new Image
                {
                    Height = 100,
                    Stretch = Stretch.Uniform
                };

                try
                {
                    // 1. CZYSZCZENIE ŚCIEŻKI:
                    string imageRelativePath = p.Zdjecie
                        .TrimStart('/')
                        .Replace('\\', '/');

                    if (!string.IsNullOrEmpty(imageRelativePath))
                    {
                        // 2. BUDOWANIE PEŁNEJ ŚCIEŻKI:
                        string fullPath = System.IO.Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            imageRelativePath
                        );

                        // 3. WERYFIKACJA I ŁADOWANIE:
                        if (System.IO.File.Exists(fullPath))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            img.Source = bitmap;

                            // ⭐️ DODANIE TYLKO RAZ PO PRAWIDŁOWYM ZAŁADOWANIU
                            panel.Children.Add(img);
                        }
                        else
                        {
                            // Komunikat, gdy plik nie istnieje (dodajemy TextBlock, nie img)
                            panel.Children.Add(new TextBlock
                            {
                                Text = $"[PLIK NIEZALEZIONY W: {fullPath}]",
                                FontSize = 8,
                                TextWrapping = TextWrapping.Wrap,
                                TextAlignment = TextAlignment.Center,
                                Height = 100
                            });
                        }
                    }
                    else
                    {
                        // Fallback, gdy ścieżka w bazie jest pusta
                        panel.Children.Add(new TextBlock
                        {
                            Text = "[brak ścieżki zdjęcia]",
                            TextAlignment = TextAlignment.Center,
                            Height = 100
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Obsługa innych błędów ładowania
                    Console.WriteLine($"[BŁĄD KRYTYCZNY ŁADOWANIA ZDJĘCIA] Produkt: {p.Nazwa}. Błąd: {ex.Message}");
                    panel.Children.Add(new TextBlock
                    {
                        Text = "[błąd ładowania (catch)]",
                        TextAlignment = TextAlignment.Center,
                        Height = 100
                    });
                }
                

            SkipImage:;


                var nazwa = new TextBlock
                {
                    Text = p.Nazwa,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(5, 2, 5, 2),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                };
                panel.Children.Add(nazwa);

                var cena = new TextBlock
                {
                    Text = $"{p.Cena} PLN",
                    Margin = new Thickness(5, 2, 5, 2),
                    TextAlignment = TextAlignment.Center
                };
                panel.Children.Add(cena);

                var quantity = new TextBlock
                {
                    Text = $"Dostępne: {p.ilość} szt.",
                    Margin = new Thickness(5, 0, 5, 5),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.DarkBlue,
                    TextAlignment = TextAlignment.Center
                };
                panel.Children.Add(quantity);

                var btnAdd = new Button
                {
                    Content = "Dodaj do koszyka",
                    Margin = new Thickness(5, 10, 5, 0),
                    Padding = new Thickness(6, 4, 6, 4),
                    Background = Brushes.LightGreen,
                    BorderBrush = Brushes.DarkGreen,
                    Cursor = Cursors.Hand
                };

                btnAdd.Click += (s, e) =>
                {
                    if (p.ilość <= 0)
                    {
                        MessageBox.Show("Brak towaru na stanie!");
                        return;
                    }

                    AddToCart(p);
                    WyswietlProdukty(_currentFilteredProducts);
                    MessageBox.Show($"Dodano do koszyka: {p.Nazwa}");
                };

                panel.Children.Add(btnAdd);

                ProductsWrapPanel.Children.Add(border);
            }
        }

        private void UpdateCartBadge()
        {
            if (CartBadge == null || CartCountText == null) return;
            CartCountText.Text = _cartCount.ToString();
            CartBadge.Visibility = _cartCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public void AddToCart(Produkt produkt)
        {
            CartPage.SharedStore.AddOrIncrease(
                sku: produkt.Id.ToString(),
                name: produkt.Nazwa,
                unitPrice: produkt.Cena,
                quantity: 1,
                imagePath: produkt.Zdjecie,
                productId: produkt.Id
            );

            _cartCount = CartPage.SharedStore.ItemsCount;
            UpdateCartBadge();
        }

        public void RemoveFromCart(int qty = 1)
        {
            _cartCount -= qty;
            if (_cartCount < 0) _cartCount = 0;
            UpdateCartBadge();
        }

        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            var cartWindow = new Projekt_zespołowy.Views.CartPage { Owner = this };

            cartWindow.IsUserLoggedIn = UserSession.IsLogged;

            cartWindow.CurrentUser = new CartPage.User
            {
                Id = UserSession.UserId,
                Email = UserSession.Username
            };

            cartWindow.ShowDialog();
        }

        private void PriceRange_Changed(object sender, RoutedEventArgs e)
        {
            if (LabelMin == null || LabelMax == null)
                return;

            LabelMin.Text = $"{PriceRange.LowerValue:F0} PLN";
            LabelMax.Text = $"{PriceRange.UpperValue:F0} PLN";
        }

        public int CartCount => _cartCount;

        // ⭐️ NOWA OBSŁUGA ZMIANY WARTOŚCI RANGE SLIDERA Z XAML
        private void PriceRange_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // RangeSlider z MahApps.Metro ma właściwości LowerValue i UpperValue, 
            // które zawierają aktualnie wybrane wartości.

            // 1. Aktualizacja etykiet wyświetlających zakres cen
            LabelMin.Text = $"{PriceRange.LowerValue:F0} PLN";
            LabelMax.Text = $"{PriceRange.UpperValue:F0} PLN";

            // 2. Opcjonalnie: automatyczne zastosowanie filtrów po zmianie suwaka. 
            // Zalecam, aby to WYKOMENTOWAĆ, jeśli chcesz, aby filtry były stosowane
            // tylko po kliknięciu przycisku "Zastosuj".

            // ApplyFilters_Click(null, null); 
        }

        // ⭐️ ZAKTUALIZOWANA GŁÓWNA METODA FILTRUJĄCA PO KATEGORII
        public void FilterByCategory(string? category)
        {
            // 1. Zapisz nową aktywną kategorię (null, jeśli pusta)
            _currentCategory = string.IsNullOrWhiteSpace(category) ? null : Normalize(category);

            // 2. Wyczyść filtry cen i marek
            ClearFilterControls();

            // 3. Zastosuj nową kategorię (lub wszystkie, jeśli null)
            ApplyFilters_Click(null, null);
        }

        // ⭐️ NOWA METODA DO RESETOWANIA KONTROLEK FILTRÓW
        private void ClearFilterControls()
        {
            // Reset RangeSlidera
            PriceRange.LowerValue = PriceRange.Minimum;
            PriceRange.UpperValue = PriceRange.Maximum;

            // Reset Checkboxów
            CheckLUK.IsChecked = false;
            CheckBosch.IsChecked = false;
            CheckATE.IsChecked = false;
            CheckCastrol.IsChecked = false;
            CheckSachs.IsChecked = false;
            CheckValeo.IsChecked = false;
        }

        private string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            return text
                .Trim()
                .ToLower()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace("ł", "l")
                .Replace("ą", "a")
                .Replace("ę", "e")
                .Replace("ś", "s")
                .Replace("ć", "c")
                .Replace("ń", "n")
                .Replace("ó", "o")
                .Replace("ż", "z")
                .Replace("ź", "z");
        }

        // ⭐️ ZAKTUALIZOWANA METODA: Obejmuje filtrowanie po KATEGORII
        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<Produkt> filtered;

            // 1. FILTRUJ PO AKTYWNEJ KATEGORII (jeśli jest ustawiona)
            if (_currentCategory != null)
            {
                filtered = _allProducts
                    .Where(p => Normalize(p.Kategoria) == _currentCategory);
            }
            else
            {
                filtered = _allProducts;
            }

            // 2. Zbieranie filtrów cenowych
            decimal minPrice = (decimal)PriceRange.LowerValue;
            decimal maxPrice = (decimal)PriceRange.UpperValue;

            // 3. Zbieranie filtrów marek (Producentów)
            var selectedBrands = new List<string>();

            if (CheckLUK.IsChecked == true) selectedBrands.Add("LUK");
            if (CheckBosch.IsChecked == true) selectedBrands.Add("Bosch");
            if (CheckATE.IsChecked == true) selectedBrands.Add("ATE");
            if (CheckCastrol.IsChecked == true) selectedBrands.Add("Castrol");
            if (CheckSachs.IsChecked == true) selectedBrands.Add("Sachs");
            if (CheckValeo.IsChecked == true) selectedBrands.Add("Valeo");

            // 4. FILTROWANIE PO CENIE I MARCE NA AKTUALNEJ LIŚCIE

            // A. Filtrowanie po cenie (zawsze)
            filtered = filtered.Where(p =>
                p.Cena >= minPrice && p.Cena <= maxPrice
            );

            // B. Filtrowanie po marce (tylko jeśli wybrano)
            if (selectedBrands.Any())
            {
                filtered = filtered.Where(p => selectedBrands.Contains(p.Producent));
            }

            // 5. Aktualizacja listy i wyświetlania
            _currentFilteredProducts = filtered.ToList();
            WyswietlProdukty(_currentFilteredProducts);

            if (sender != null)
            {
                MessageBox.Show($"Zastosowano filtry. Wyświetlono {_currentFilteredProducts.Count} produktów.");
            }
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            // Reset kontroli UI i usunięcie aktywnej kategorii
            ClearFilterControls();
            _currentCategory = null;

            // Wyświetlenie wszystkich produktów
            _currentFilteredProducts = new List<Produkt>(_allProducts);
            WyswietlProdukty(_currentFilteredProducts);
            MessageBox.Show("Filtry zostały zresetowane. Wyświetlono wszystkie produkty.");
        }


        // Metody do kliknięć kategorii
        private void Oleje_Click(object sender, RoutedEventArgs e)
        {
            FilterByCategory("Oleje");
        }

        private void Filtry_Click(object sender, RoutedEventArgs e)
        {
            FilterByCategory("Filtry");
        }

        private void Sprzegla_Click(object sender, RoutedEventArgs e)
        {
            FilterByCategory("Sprzęgła");
        }

        private void KolaDwumasowe_Click(object sender, RoutedEventArgs e)
        {
            FilterByCategory("Koła Dwumasowe");
        }

        private void Elektryczny_Click(object sender, RoutedEventArgs e)
        {
            FilterByCategory("Układ Elektryczny");
        }

        private void Hamulcowy_Click(object sender, RoutedEventArgs e)
        {
            FilterByCategory("Układ Hamulcowy");
        }

        private void Napedowy_Click(object sender, RoutedEventArgs e)
        {
            FilterByCategory("Układ Napędowy");
        }
    }
}