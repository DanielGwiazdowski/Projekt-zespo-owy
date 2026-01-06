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
        public class Opinion
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string ProductKey { get; set; } = "";
            public string UserDisplayName { get; set; } = "Anonim";
            public int Rating { get; set; }
            public string Comment { get; set; } = "";
            public DateTime CreatedAt { get; set; } = DateTime.Now;
            public bool IsApproved { get; set; } = false;
            public bool IsHidden { get; set; } = false;
        }
        public static class OpinionsStore
        {
            private static readonly string FilePath =
                System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AutoPartHub", "opinions.json");

            public static System.Collections.ObjectModel.ObservableCollection<Opinion> Opinions { get; private set; }
                = new System.Collections.ObjectModel.ObservableCollection<Opinion>();

            public static void Load()
            {
                try
                {
                    var dir = System.IO.Path.GetDirectoryName(FilePath);
                    if (!System.IO.Directory.Exists(dir))
                        System.IO.Directory.CreateDirectory(dir!);

                    if (!System.IO.File.Exists(FilePath))
                        return;

                    var json = System.IO.File.ReadAllText(FilePath);
                    Opinions =
                        System.Text.Json.JsonSerializer.Deserialize
                        <System.Collections.ObjectModel.ObservableCollection<Opinion>>(json)
                        ?? new System.Collections.ObjectModel.ObservableCollection<Opinion>();
                }
                catch { }
            }

            public static void Save()
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(
                        Opinions,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                    System.IO.File.WriteAllText(FilePath, json);
                }
                catch { }
            }

            public static void Add(Opinion opinion)
            {
                Opinions.Add(opinion);
                Save();
            }

            public static void Remove(Guid id)
            {
                var item = Opinions.FirstOrDefault(o => o.Id == id);
                if (item != null)
                {
                    Opinions.Remove(item);
                    Save();
                }
            }
        }


        // ====== NOWE: przechowywanie produktów i kategorii ======
        private List<Produkt> _allProducts = new List<Produkt>();
        private List<Produkt> _currentFilteredProducts = new List<Produkt>();

        // ⭐️ DODANE POLE DO PRZECHOWYWANIA AKTYWNEJ KATEGORII
        private string _currentCategory = null;

        // ====== OPINIE (Task 13-16) ======
        private int _selectedRating = 0;                 // <-- DODANE
        private Produkt _selectedProductForOpinion = null; // <-- DODANE (wybrany produkt do opinii)

        public MainWindow()
        {
            InitializeComponent();

            // ===== OPINIE: wczytanie z JSON (bez bazy) =====
            OpinionsStore.Load(); // <-- DODANE

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
                BtnClaims.Visibility = Visibility.Visible;
                BtnAdmin.Visibility = UserSession.Role == "admin" ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                BtnLogin.Visibility = Visibility.Visible;
                BtnRegister.Visibility = Visibility.Visible;
                BtnLogout.Visibility = Visibility.Collapsed;
                BtnClaims.Visibility = Visibility.Collapsed;
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

        private void BtnClaims_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var claimsWindow = new Projekt_zespołowy.Views.ClaimsWindow();
                claimsWindow.Owner = this;
                claimsWindow.ShowDialog();
            }
            catch (Exception)
            {
                MessageBox.Show("Okno reklamacji jest w trakcie przygotowania lub nie zostało odnalezione.");
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

                // ===== OPINIE: wybór produktu do opinii kliknięciem kafelka =====
                border.MouseLeftButtonUp += (s, e) =>
                {
                    _selectedProductForOpinion = p;
                    ShowOpinionsForSelectedProduct();
                };

                var img = new Image
                {
                    Height = 100,
                    Stretch = Stretch.Uniform
                };

                try
                {
                    string imageRelativePath = p.Zdjecie
                        .TrimStart('/')
                        .Replace('\\', '/');

                    if (!string.IsNullOrEmpty(imageRelativePath))
                    {
                        string fullPath = System.IO.Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            imageRelativePath
                        );

                        if (System.IO.File.Exists(fullPath))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            img.Source = bitmap;

                            panel.Children.Add(img);
                        }
                        else
                        {
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

        private void PriceRange_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LabelMin.Text = $"{PriceRange.LowerValue:F0} PLN";
            LabelMax.Text = $"{PriceRange.UpperValue:F0} PLN";
        }

        public void FilterByCategory(string? category)
        {
            _currentCategory = string.IsNullOrWhiteSpace(category) ? null : Normalize(category);
            ClearFilterControls();
            ApplyFilters_Click(null, null);
        }

        private void ClearFilterControls()
        {
            PriceRange.LowerValue = PriceRange.Minimum;
            PriceRange.UpperValue = PriceRange.Maximum;

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

        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<Produkt> filtered;

            if (_currentCategory != null)
            {
                filtered = _allProducts
                    .Where(p => Normalize(p.Kategoria) == _currentCategory);
            }
            else
            {
                filtered = _allProducts;
            }

            decimal minPrice = (decimal)PriceRange.LowerValue;
            decimal maxPrice = (decimal)PriceRange.UpperValue;

            var selectedBrands = new List<string>();

            if (CheckLUK.IsChecked == true) selectedBrands.Add(Normalize("LUK"));
            if (CheckBosch.IsChecked == true) selectedBrands.Add(Normalize("Bosch"));
            if (CheckATE.IsChecked == true) selectedBrands.Add(Normalize("ATE"));
            if (CheckCastrol.IsChecked == true) selectedBrands.Add(Normalize("Castrol"));
            if (CheckSachs.IsChecked == true) selectedBrands.Add(Normalize("Sachs"));
            if (CheckValeo.IsChecked == true) selectedBrands.Add(Normalize("Valeo"));

            filtered = filtered.Where(p =>
                p.Cena >= minPrice && p.Cena <= maxPrice
            );

            if (selectedBrands.Any())
            {
                filtered = filtered.Where(p =>
                    selectedBrands.Contains(Normalize(p.Producent))
                );
            }

            _currentFilteredProducts = filtered.ToList();
            WyswietlProdukty(_currentFilteredProducts);

            if (sender != null)
            {
                MessageBox.Show($"Zastosowano filtry. Wyświetlono {_currentFilteredProducts.Count} produktów.");
            }
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            ClearFilterControls();
            _currentCategory = null;

            _currentFilteredProducts = new List<Produkt>(_allProducts);
            WyswietlProdukty(_currentFilteredProducts);
            MessageBox.Show("Filtry zostały zresetowane. Wyświetlono wszystkie produkty.");
        }

        private void Oleje_Click(object sender, RoutedEventArgs e) => FilterByCategory("Oleje");
        private void Filtry_Click(object sender, RoutedEventArgs e) => FilterByCategory("Filtry");
        private void Sprzegla_Click(object sender, RoutedEventArgs e) => FilterByCategory("Sprzęgła");
        private void KolaDwumasowe_Click(object sender, RoutedEventArgs e) => FilterByCategory("Koła Dwumasowe");
        private void Elektryczny_Click(object sender, RoutedEventArgs e) => FilterByCategory("Układ Elektryczny");
        private void Hamulcowy_Click(object sender, RoutedEventArgs e) => FilterByCategory("Układ Hamulcowy");
        private void Napedowy_Click(object sender, RoutedEventArgs e) => FilterByCategory("Układ Napędowy");

        // =========================
        // OPINIE: Task 13-14 (formularz + zapis do JSON)
        // WYMAGA w MainWindow.xaml kontrolek:
        // - TextBox x:Name="OpinionCommentBox"
        // - TextBlock x:Name="RatingLabel"
        // - 5 przycisków gwiazdek z Click="Star_Click" i Tag="1..5"
        // - Button "Wyślij opinię" z Click="SubmitOpinion_Click"
        // =========================

        private void Star_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && int.TryParse(b.Tag?.ToString(), out int rating))
            {
                _selectedRating = rating;

                if (RatingLabel != null)
                    RatingLabel.Text = $"{_selectedRating}/5";

                if (b.Parent is Panel panel)
                {
                    foreach (var child in panel.Children.OfType<Button>())
                    {
                        if (int.TryParse(child.Tag?.ToString(), out int r))
                            child.Foreground = r <= _selectedRating ? Brushes.Goldenrod : Brushes.Gray;
                    }
                }
            }
        }

        private void SubmitOpinion_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.IsLogged)
            {
                MessageBox.Show("Musisz być zalogowany, aby dodać opinię.");
                return;
            }

            if (_selectedProductForOpinion == null)
            {
                MessageBox.Show("Najpierw kliknij kafelek produktu, który chcesz ocenić.");
                return;
            }

            var comment = OpinionCommentBox?.Text?.Trim() ?? "";

            if (_selectedRating < 1 || _selectedRating > 5)
            {
                MessageBox.Show("Wybierz ocenę 1–5.");
                return;
            }

            if (comment.Length < 3)
            {
                MessageBox.Show("Komentarz jest zbyt krótki.");
                return;
            }

            var opinion = new Opinion
            {
                ProductKey = _selectedProductForOpinion.Id.ToString(),
                Rating = _selectedRating,
                Comment = comment,
                UserDisplayName = UserSession.Username,
                IsApproved = false,
                IsHidden = false
            };

            OpinionsStore.Add(opinion);
            ShowOpinionsForSelectedProduct();

            MessageBox.Show("Opinia wysłana do moderacji.");

            _selectedRating = 0;
            if (RatingLabel != null) RatingLabel.Text = "0/5";
            if (OpinionCommentBox != null) OpinionCommentBox.Text = "";
        }
        private void ShowOpinionsForSelectedProduct()
        {
            if (_selectedProductForOpinion == null)
            {
                if (OpinionsList != null) OpinionsList.ItemsSource = null;
                if (NoOpinionsText != null) NoOpinionsText.Visibility = Visibility.Visible;
                if (OpinionsTitle != null) OpinionsTitle.Text = "Opinie";
                return;
            }

            // Wczytaj najnowszy stan z pliku (bo admin mógł coś zmienić)
            OpinionsStore.Load();

            var list = OpinionsStore.Opinions
                .Where(o =>
                    o.ProductKey == _selectedProductForOpinion.Id.ToString() &&
                    o.IsApproved &&
                    !o.IsHidden)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            if (OpinionsTitle != null)
                OpinionsTitle.Text = $"Opinie: {_selectedProductForOpinion.Nazwa}";

            if (OpinionsList != null)
                OpinionsList.ItemsSource = list;

            if (NoOpinionsText != null)
                NoOpinionsText.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

    }
}
