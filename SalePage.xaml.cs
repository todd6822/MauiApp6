using System.Collections.ObjectModel;
using DBService;

namespace MauiApp6;

public class CartItem
{
    public string? BookTitle { get; set; }
    public int InventoryNumber { get; set; }
    public double Cost { get; set; }
    public double Tax { get; set; }
    public double Total { get; set; }
    public int InventoryId { get; set; }
    public int Quantity { get; set; }
}

public class SaleSummary
{
    public double NetCost { get; set; }
    public double TotalTax { get; set; }
    public double SaleTotal { get; set; }
}

public partial class SalePage : ContentPage
{
    readonly LocalDBService _dbService;

    public int CurrentEventId { get; set; }
    public float SalesTax { get; set; }
    public int ItemQuantity { get; set; } = 1;
    public bool Taxed { get; set; }

    private Inventory? _selectedInventoryItem;

    public ObservableCollection<Event> Events { get; set; } = new();
    public ObservableCollection<Inventory> InventoryItems { get; set; } = new();
    public ObservableCollection<CartItem> CartItems { get; set; } = new();
    public ObservableCollection<SaleSummary> SaleSummaryItems { get; set; } = new();

    public SalePage(LocalDBService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadEvents();
        await LoadInventory();
    }

    private async Task LoadEvents()
    {
        var events = await _dbService.GetEvent();
        Events.Clear();
        foreach (var ev in events)
            Events.Add(ev);
        Events.Add(new Event { EventId = -1, EventName = "+ New Event" });
    }

    private async Task LoadInventory()
    {
        var items = await _dbService.GetInventory();
        InventoryItems.Clear();
        foreach (var item in items)
            InventoryItems.Add(item);
    }

    private async void OnEventSelected(object? sender, EventArgs e)
    {
        if (EventPicker.SelectedItem is Event selectedEvent)
        {
            if (selectedEvent.EventId == -1)
            {
                EventPicker.SelectedIndex = -1;
                await Navigation.PushAsync(new DatabaseUpdatePage(_dbService, openEventOnLoad: true));
                return;
            }
            CurrentEventId = selectedEvent.EventId;
            SalesTax = (float)selectedEvent.EventTax;
        }
    }

    private void OnBookSelected(object? sender, EventArgs e)
    {
        if (BooksPicker.SelectedItem is Inventory selectedBook)
        {
            _selectedInventoryItem = selectedBook;
            PopupBookLabel.Text = selectedBook.BookTitle;
            QuantityEntry.Text = "1";
            TaxPicker.SelectedIndex = -1;
            PopupOverlay.IsVisible = true;
        }
    }

    private void OnEnterClicked(object? sender, EventArgs e)
    {
        if (_selectedInventoryItem == null) return;

        if (!int.TryParse(QuantityEntry.Text, out int parsedQuantity) || parsedQuantity < 1)
            parsedQuantity = 1;
        ItemQuantity = parsedQuantity;

        Taxed = TaxPicker.SelectedItem?.ToString() == "Yes";

        double cost = _selectedInventoryItem.Cost * ItemQuantity;
        double tax = Taxed ? Math.Round(SalesTax * cost, 2) : 0;
        double total = cost + tax;

        CartItems.Add(new CartItem
        {
            BookTitle = _selectedInventoryItem.BookTitle,
            InventoryNumber = _selectedInventoryItem.InventoryId,
            Cost = cost,
            Tax = tax,
            Total = total,
            InventoryId = _selectedInventoryItem.InventoryId,
            Quantity = ItemQuantity
        });

        PopupOverlay.IsVisible = false;
        BooksPicker.SelectedIndex = -1;
        _selectedInventoryItem = null;
    }

    private void OnCartItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is CartItem selected)
        {
            CartItems.Remove(selected);
            CartView.SelectedItem = null;
        }
    }

    private void OnTotalClicked(object? sender, EventArgs e)
    {
        double netCost = CartItems.Sum(x => x.Cost);
        double totalTax = CartItems.Sum(x => x.Tax);
        double saleTotal = netCost + totalTax;

        SaleSummaryItems.Clear();
        SaleSummaryItems.Add(new SaleSummary
        {
            NetCost = netCost,
            TotalTax = totalTax,
            SaleTotal = saleTotal
        });
    }

    private async void OnSaleClicked(object? sender, EventArgs e)
    {
        if (CartItems.Count == 0)
        {
            await DisplayAlertAsync("Empty Cart", "Please add items to the cart before recording a sale.", "OK");
            return;
        }

        if (CurrentEventId == 0)
        {
            await DisplayAlertAsync("No Event Selected", "Please select an event before recording a sale.", "OK");
            return;
        }

        // Validate stock before writing anything
        var inventoryUpdates = new List<(DBService.Inventory Item, int SellQty)>();
        foreach (var cartItem in CartItems)
        {
            var inv = await _dbService.GetInventoryById(cartItem.InventoryId);
            if (inv == null)
            {
                await DisplayAlertAsync("Error", $"'{cartItem.BookTitle}' was not found in inventory.", "OK");
                return;
            }
            if (inv.InStock - cartItem.Quantity < 0)
            {
                await DisplayAlertAsync("Insufficient Stock",
                    $"Cannot sell {cartItem.Quantity} of '{cartItem.BookTitle}'. Only {inv.InStock} in stock.", "OK");
                return;
            }
            inventoryUpdates.Add((inv, cartItem.Quantity));
        }

        double netCost = CartItems.Sum(x => x.Cost);
        double totalTax = CartItems.Sum(x => x.Tax);
        double saleTotal = netCost + totalTax;

        var sale = new Sale
        {
            EventId = CurrentEventId,
            SalePrice = netCost,
            Tax = totalTax,
            Total = saleTotal
        };

        await _dbService.CreateSale(sale);

        foreach (var item in CartItems)
        {
            await _dbService.CreateSaleItem(new SaleItem
            {
                SaleId = sale.SaleId,
                BookId = item.InventoryId,
                Quantity = item.Quantity,
                SalePrice = (float)item.Total,
                IsTaxed = item.Tax > 0
            });
        }

        foreach (var (inv, qty) in inventoryUpdates)
        {
            inv.InStock -= qty;
            await _dbService.UpdateInventoryItem(inv);
        }

        CartItems.Clear();
        SaleSummaryItems.Clear();
        await DisplayAlertAsync("Sale Complete", "Sale has been recorded successfully.", "OK");
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        CartItems.Clear();
        SaleSummaryItems.Clear();
    }

    private async void OnMainMenuClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
