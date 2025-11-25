using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Data.SQLite;

namespace Projekt_zespołowy.Views
{
    public partial class CartPage : Window
    {
        public CartPage()
        {
            InitializeComponent();
            Language = System.Windows.Markup.XmlLanguage.GetLanguage("pl-PL");
            DataContext = Store;
        }

        private void Increase_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CartItem item)
                item.Quantity++;
        }

        private void Decrease_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CartItem item && item.Quantity > 1)
                item.Quantity--;
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CartItem item)
                Store.Remove(item);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Store.Clear();
        }

        // ====== MODEL + "STORE" ======

        public class CartItem : INotifyPropertyChanged
        {
            private int _quantity;

            public string Name { get; set; }
            public string Sku { get; set; }

            public decimal UnitPrice { get; set; }
            public decimal NetPrice => UnitPrice;
            public int ProductId { get; set; }
            public string ImagePath { get; set; }

            public int Quantity
            {
                get => _quantity;
                set
                {
                    if (_quantity == value) return;
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(LineTotal));
                }
            }

            public decimal LineTotal => UnitPrice * Quantity;

            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string name) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class CartStore : INotifyPropertyChanged
        {
            public ObservableCollection<CartItem> Items { get; } = new();

            public int ItemsCount => Items.Sum(i => i.Quantity);
            public decimal Subtotal => Items.Sum(i => i.LineTotal);
            public decimal Vat => Math.Round(Subtotal * 0.23m, 2);
            public decimal Total => Subtotal + Vat;

            public event PropertyChangedEventHandler PropertyChanged;

            public void AddOrIncrease(string sku, string name, decimal unitPrice, int quantity = 1, string imagePath = null, int? productId = null, decimal? netPrice = null)
            {
                var existing = Items.FirstOrDefault(i => i.Sku == sku);
                if (existing == null)
                {
                    var newItem = new CartItem
                    {
                        Sku = sku,
                        Name = name,
                        UnitPrice = netPrice ?? unitPrice,
                        Quantity = Math.Max(1, quantity),
                        ImagePath = imagePath,
                        ProductId = productId ?? 0
                    };

                    newItem.PropertyChanged += Item_PropertyChanged;

                    Items.Add(newItem);
                }
                else
                {
                    existing.Quantity += Math.Max(1, quantity);
                }

                RaiseTotals();
            }

            private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(CartItem.Quantity) ||
                    e.PropertyName == nameof(CartItem.LineTotal))
                {
                    RaiseTotals();
                }
            }

            public void Remove(CartItem item)
            {
                if (Items.Contains(item)) Items.Remove(item);
                RaiseTotals();
            }

            public void Clear()
            {
                Items.Clear();
                RaiseTotals();
            }

            private void RaiseTotals()
            {
                OnPropertyChanged(nameof(ItemsCount));
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(Vat));
                OnPropertyChanged(nameof(Total));
            }

            private void OnPropertyChanged(string name) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private static readonly CartStore Store = new CartStore();

        public static CartStore SharedStore => Store;

        // ====== DODANE: PROSTE REPREZENTACJE UŻYTKOWNIKA ======
        public class User
        {
            public int Id { get; set; }
            public string Email { get; set; }
        }

        public User CurrentUser { get; set; }

        public bool IsUserLoggedIn { get; set; } = true;

        private void Checkout_Click(object sender, RoutedEventArgs e)
        {
            if (!IsUserLoggedIn)
            {
                MessageBox.Show("Musisz być zalogowany, aby przejść do zamówienia.",
                    "Logowanie wymagane", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Sprawdzenie, czy koszyk jest pusty
            if (Store.ItemsCount == 0)
            {
                MessageBox.Show("Koszyk jest pusty. Dodaj produkty przed przejściem do zamówienia.",
                   "Pusty koszyk", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ukryj podsumowanie – pokaż formularz
            SummaryPanel.Visibility = Visibility.Collapsed;
            OrderFormPanel.Visibility = Visibility.Visible;
        }

        private void BackToCart_Click(object sender, RoutedEventArgs e)
        {
            OrderFormPanel.Visibility = Visibility.Collapsed;
            SummaryPanel.Visibility = Visibility.Visible;
        }

        private void PlaceOrder_Click(object sender, RoutedEventArgs e)
        {
            // 1. Walidacja
            if (string.IsNullOrWhiteSpace(InputName.Text) ||
                string.IsNullOrWhiteSpace(InputStreet.Text) ||
                string.IsNullOrWhiteSpace(InputBuildingNumber.Text) ||
                string.IsNullOrWhiteSpace(InputCity.Text) ||
                string.IsNullOrWhiteSpace(InputZip.Text) ||
                string.IsNullOrWhiteSpace(InputPhone.Text))
            {
                MessageBox.Show("Uzupełnij wszystkie pola!");
                return;
            }

            // 2. Musi być zalogowany
            if (CurrentUser == null || CurrentUser.Id == 0)
            {
                MessageBox.Show("Musisz być zalogowany! Brak ID użytkownika.");
                return;
            }

            // 3. Sprawdzenie czy koszyk nie jest pusty (dodatkowe zabezpieczenie)
            if (Store.ItemsCount == 0)
            {
                MessageBox.Show("Nie można złożyć pustego zamówienia.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var con = new SQLiteConnection("Data Source=bazaAPH.db;Version=3;"))
                {
                    con.Open();

                    // 1. Sprawdzenie/utworzenie klienta powiązanego z użytkownikiem
                    int klientId = EnsureClientExists(con, CurrentUser.Id);

                    // 2. Utworzenie zamówienia
                    int orderId = CreateOrder(con, klientId);

                    // 3. Wstawienie pozycji zamówienia
                    InsertOrderItems(con, orderId);

                    MessageBox.Show("Zamówienie złożone pomyślnie!");

                    // 4. Reset
                    Store.Clear();
                    OrderFormPanel.Visibility = Visibility.Collapsed;
                    SummaryPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas składania zamówienia: " + ex.Message,
                                "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private int EnsureClientExists(SQLiteConnection con, int userId)
        {
            // Sprawdzenie czy user ma już klienta
            using (var cmd = new SQLiteCommand("SELECT klienci_id_klienta FROM uzytkownicy WHERE id_uzytkownik=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", userId);
                var result = cmd.ExecuteScalar();

                if (result != DBNull.Value && result != null && Convert.ToInt32(result) > 0)
                {
                    // Klient już istnieje
                    return Convert.ToInt32(result);
                }
            }

            // Tworzenie klienta
            int newClientId;
            // Polecenie to INSERT i od razu SELECT ID nowo utworzonego wiersza
            using (var cmd = new SQLiteCommand(
                @"INSERT INTO klienci (imie, nazwisko, ulica, numer_budynku, miejscowosc, kod_pocztowy, numer_telefonu)
             VALUES (@imie, @naz, @ul, @nb, @mc, @kod, @tel);
             SELECT last_insert_rowid();", con))
            {
                var parts = InputName.Text.Split(new char[] { ' ' }, 2);
                cmd.Parameters.AddWithValue("@imie", parts[0]);
                cmd.Parameters.AddWithValue("@naz", parts.Length > 1 ? parts[1] : "");
                cmd.Parameters.AddWithValue("@ul", InputStreet.Text);
                cmd.Parameters.AddWithValue("@nb", InputBuildingNumber.Text);
                cmd.Parameters.AddWithValue("@mc", InputCity.Text);
                cmd.Parameters.AddWithValue("@kod", InputZip.Text);
                cmd.Parameters.AddWithValue("@tel", InputPhone.Text);

                newClientId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Aktualizacja użytkownika (powiązanie z nowym klientem)
            using (var cmd = new SQLiteCommand(
                "UPDATE uzytkownicy SET klienci_id_klienta=@kid WHERE id_uzytkownik=@uid", con))
            {
                cmd.Parameters.AddWithValue("@kid", newClientId);
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.ExecuteNonQuery();
            }

            return newClientId;
        }
        private int CreateOrder(SQLiteConnection con, int clientId)
        {
            decimal totalBruttoDecimal = Store.Total;

            using (var cmd = new SQLiteCommand(
                @"INSERT INTO zamowienia (data_zamowienia, status, suma_brutto, adres_dostawy, uwagi, klienci_id_klienta)
             VALUES (@data, @status, @suma, @adres, @uwagi, @klient);
             SELECT last_insert_rowid();", con))
            {
                cmd.Parameters.AddWithValue("@data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@status", "Nowe");
                cmd.Parameters.AddWithValue("@suma", (double)totalBruttoDecimal);

                string fullAddress = $"{InputStreet.Text} {InputBuildingNumber.Text}, {InputZip.Text} {InputCity.Text}";
                cmd.Parameters.AddWithValue("@adres", fullAddress);

                // POPRAWKA: Usunięto odwołanie do InputUwagi.Text
                cmd.Parameters.AddWithValue("@uwagi", "");

                cmd.Parameters.AddWithValue("@klient", clientId);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void InsertOrderItems(SQLiteConnection con, int orderId)
        {
            // Transakcje są kluczowe, aby zamówienie było spójne
            using (var transaction = con.BeginTransaction())
            {
                try
                {
                    foreach (var item in Store.Items)
                    {
                        // Wstawiamy pozycję zamówienia z ID produktu
                        using (var cmd = new SQLiteCommand(
                            @"INSERT INTO pozycje_zamowienia (ilosc, cena_brutto, rabat, zamowienia_id_zamowienia, produkty_id_produktu)
                         VALUES (@ilosc, @cena, @rabat, @zamid, @prodid)", con, transaction))
                        {
                            decimal cenaBrutto = item.UnitPrice * 1.23m;

                            cmd.Parameters.AddWithValue("@ilosc", item.Quantity);
                            cmd.Parameters.AddWithValue("@cena", (double)cenaBrutto);
                            cmd.Parameters.AddWithValue("@rabat", 0);
                            cmd.Parameters.AddWithValue("@zamid", orderId);
                            cmd.Parameters.AddWithValue("@prodid", item.ProductId);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("Błąd podczas wstawiania pozycji zamówienia.", ex);
                }
            }
        }
    }
}