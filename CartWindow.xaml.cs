using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;

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

        private void Checkout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                $"Przechodzisz do zamówienia.\nKwota: {Store.Total.ToString("C", CultureInfo.GetCultureInfo("pl-PL"))}",
                "Zamówienie", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ====== MODEL + "STORE" ======
        public class CartItem : INotifyPropertyChanged
        {
            private int _quantity;

            public string Name { get; set; }
            public string Sku { get; set; }
            public decimal UnitPrice { get; set; }
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

            public void AddOrIncrease(string sku, string name, decimal unitPrice, int quantity = 1, string imagePath = null)
            {
                var existing = Items.FirstOrDefault(i => i.Sku == sku);
                if (existing != null)
                {
                    existing.Quantity += quantity;
                }
                else
                {
                    Items.Add(new CartItem
                    {
                        Sku = sku,
                        Name = name,
                        UnitPrice = unitPrice,
                        Quantity = Math.Max(1, quantity),
                        ImagePath = imagePath
                    });
                }
                RaiseTotals();
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
    }
}
