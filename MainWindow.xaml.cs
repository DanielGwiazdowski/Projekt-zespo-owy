using Projekt_zespołowy.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Projekt_zespołowy
{
    public partial class MainWindow : Window
    {
        // ====== STAN APLIKACJI ======
        private bool IsLoggedIn = false;
        private string LoggedUserRole = "";
        private int _cartCount = 0;

        // ====== NOWE: przechowywanie produktów ======
        private List<Produkt> _allProducts = new List<Produkt>();
        private List<Produkt> _currentFilteredProducts = new List<Produkt>();

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
            if (IsLoggedIn)
            {
                BtnLogin.Visibility = Visibility.Collapsed;
                BtnRegister.Visibility = Visibility.Collapsed;
                BtnLogout.Visibility = Visibility.Visible;
                BtnAdmin.Visibility = LoggedUserRole == "admin" ? Visibility.Visible : Visibility.Collapsed;
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
                IsLoggedIn = true;
                LoggedUserRole = loginWindow.UserRole;
                UpdateAuthButtons();

                if (LoggedUserRole == "admin")
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
                MessageBox.Show($"Zarejestrowano użytkownika: {registerWindow.txtUsername.Text}");
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoggedIn)
            {
                MessageBox.Show("Nie jesteś zalogowany!");
                return;
            }

            IsLoggedIn = false;
            LoggedUserRole = "";
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

                if (!string.IsNullOrEmpty(p.Zdjecie))
                {
                    try
                    {
                        string fullPath;
                        if (p.Zdjecie.StartsWith("/"))
                        {
                            fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p.Zdjecie.TrimStart('/', '\\'));
                        }
                        else if (System.IO.Path.IsPathRooted(p.Zdjecie))
                        {
                            fullPath = p.Zdjecie;
                        }
                        else
                        {
                            fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", p.Zdjecie);
                        }

                        if (System.IO.File.Exists(fullPath))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();

                            var img = new Image
                            {
                                Source = bitmap,
                                Height = 100,
                                Stretch = Stretch.Uniform
                            };
                            panel.Children.Add(img);
                        }
                        else
                        {
                            panel.Children.Add(new TextBlock
                            {
                                Text = "[brak zdjęcia]",
                                TextAlignment = TextAlignment.Center
                            });
                        }
                    }
                    catch
                    {
                        panel.Children.Add(new TextBlock
                        {
                            Text = "[błąd zdjęcia]",
                            TextAlignment = TextAlignment.Center
                        });
                    }
                }

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

                // ==========================================================
                // =============== DODANY PRZYCISK „DODAJ DO KOSZYKA” =======
                // ==========================================================

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
                    AddToCart(p);
                    MessageBox.Show($"Dodano do koszyka: {p.Nazwa}");
                };

                panel.Children.Add(btnAdd);

                // ==========================================================

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
                imagePath: produkt.Zdjecie
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
            cartWindow.ShowDialog();
        }

        public int CartCount => _cartCount;

        // ==============================
        //   FILTRY: panel lewy (slidery i checkboxy) - pola UI
        // ==============================
        private Slider _priceMinSlider, _priceMaxSlider;
        private TextBlock _priceMinLabel, _priceMaxLabel;
        private CheckBox _brandLuk, _brandSachs, _brandValeo;
        private const int PRICE_MIN = 1500;
        private const int PRICE_MAX = 2000;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            try
            {
                InjectFiltersPanel();
            }
            catch
            {
                Dispatcher.InvokeAsync(() =>
                {
                    try { InjectFiltersPanel(); } catch { }
                });
            }
        }

        private void InjectFiltersPanel()
        {
            var mainScroll = FindDescendant<ScrollViewer>(this);
            if (mainScroll == null) return;

            if (mainScroll.Content is Grid g && g.Tag as string == "InjectedWithFilters")
                return;

            var originalContent = mainScroll.Content as FrameworkElement;
            if (originalContent == null) return;

            var host = new Grid { Margin = new Thickness(0) };
            host.Tag = "InjectedWithFilters";
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var filtersBorder = BuildFiltersUi();
            Grid.SetColumn(filtersBorder, 0);
            host.Children.Add(filtersBorder);

            originalContent.Margin = new Thickness(0, 0, 0, 0);
            Grid.SetColumn(originalContent, 1);
            host.Children.Add(originalContent);

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

            stack.Children.Add(new TextBlock
            {
                Text = "Filtry",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            });

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
            apply.Click += ApplyFilters_Click;

            stack.Children.Add(apply);

            return border;
        }

        private void PriceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_priceMinSlider == null || _priceMaxSlider == null) return;

            if (_priceMinSlider.Value > _priceMaxSlider.Value)
            {
                if (sender == _priceMinSlider)
                    _priceMaxSlider.Value = _priceMinSlider.Value;
                else
                    _priceMinSlider.Value = _priceMaxSlider.Value;
            }

            if (_priceMinLabel != null)
                _priceMinLabel.Text = $"{(int)_priceMinSlider.Value} PLN";

            if (_priceMaxLabel != null)
                _priceMaxLabel.Text = $"{(int)_priceMaxSlider.Value} PLN";
        }

        public void FilterByCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                _currentFilteredProducts = new List<Produkt>(_allProducts);
            }
            else
            {
                _currentFilteredProducts = _allProducts
                    .Where(p => !string.IsNullOrEmpty(p.Kategoria) &&
                                p.Kategoria.Equals(category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            Console.WriteLine($"[DEBUG] FilterByCategory('{category}') => {_currentFilteredProducts.Count} produktów");
            WyswietlProdukty(_currentFilteredProducts);
        }

        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFilteredProducts == null)
                _currentFilteredProducts = new List<Produkt>(_allProducts);

            int min = (int)(_priceMinSlider?.Value ?? PRICE_MIN);
            int max = (int)(_priceMaxSlider?.Value ?? PRICE_MAX);

            var brands = new List<string>();
            if (_brandLuk.IsChecked == true) brands.Add("LUK");
            if (_brandSachs.IsChecked == true) brands.Add("Sachs");
            if (_brandValeo.IsChecked == true) brands.Add("Valeo");

            var filtered = _currentFilteredProducts.Where(p => p.Cena >= min && p.Cena <= max);

            if (brands.Count > 0)
            {
                filtered = filtered.Where(p => !string.IsNullOrEmpty(p.Producent) &&
                                               brands.Any(b =>
                                                   string.Equals(b, p.Producent, StringComparison.OrdinalIgnoreCase)));
            }

            _currentFilteredProducts = filtered.ToList();

            Console.WriteLine($"[DEBUG] ApplyFilters => {_currentFilteredProducts.Count}");
            WyswietlProdukty(_currentFilteredProducts);

            MessageBox.Show($"Zastosowano filtry. Wynik: {_currentFilteredProducts.Count} produktów.");
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            SliderMin.Value = 1500;
            SliderMax.Value = 2000;

            LabelMin.Text = "1500 PLN";
            LabelMax.Text = "2000 PLN";

            CheckLUK.IsChecked = false;
            CheckSachs.IsChecked = false;
            CheckValeo.IsChecked = false;

            _currentFilteredProducts = new List<Produkt>(_allProducts);
            WyswietlProdukty(_currentFilteredProducts);
        }

        private void BtnOleje_Click(object sender, RoutedEventArgs e)
        {
            FilterByCategory("oleje");
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(root);

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);

                if (child is T t)
                    return t;

                var result = FindDescendant<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
