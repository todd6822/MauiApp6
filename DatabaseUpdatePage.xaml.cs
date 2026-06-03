using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DBService;

namespace MauiApp6;

public class SaleItemDisplay
{
    public int SaleItemId { get; set; }
    public string? BookTitle { get; set; }
    public int Quantity { get; set; }
    public float SalePrice { get; set; }
    public bool IsTaxed { get; set; }
    public SaleItem? RawItem { get; set; }
}


public partial class DatabaseUpdatePage : ContentPage
{
    readonly LocalDBService _dbService;
    private List<Sale> _allSales = new();

    private enum InventoryMode { None, EditPrompt, EditForm, DeletePrompt, DeleteAffectedSales, DeleteConfirm }
    private InventoryMode _inventoryMode = InventoryMode.None;
    private Inventory? _pendingEditItem;
    private Inventory? _pendingDeleteItem;

    private enum EventMode { None, EditPrompt, EditForm, DeletePrompt, DeleteConfirm }
    private EventMode _eventMode = EventMode.None;
    private Event? _pendingEditEvent;
    private Event? _pendingDeleteEvent;

    private enum SaleMode
    {
        None,
        EditPrompt, EditChoose, EditSaleForm, EditItemPrompt, EditItemForm, AddItem,
        EditDeleteItemSelect, EditDeleteItemConfirm,
        DeletePrompt, DeleteChoose, DeleteSaleConfirm, DeleteItemPrompt, DeleteItemConfirm
    }
    private SaleMode _saleMode = SaleMode.None;
    private Sale? _pendingEditSale;
    private Sale? _pendingDeleteSale;
    private SaleItemDisplay? _pendingEditSaleItem;
    private SaleItemDisplay? _pendingDeleteSaleItem;
    private SaleItemDisplay? _pendingEditDeleteSaleItem;

    private bool _comingFromInventoryDelete;
    private HashSet<int> _affectedSaleIds = new();

    public ObservableCollection<Inventory> ItemsInInventory { get; set; } = new();
    public ObservableCollection<Sale> Sales { get; set; } = new();
    public ObservableCollection<Event> Events { get; set; } = new();
    public ObservableCollection<Event> FilterEvents { get; set; } = new();
    public ObservableCollection<SaleItemDisplay> SelectedSaleItems { get; set; } = new();

    private bool _openEventOnLoad;

    public DatabaseUpdatePage(LocalDBService dbservice, bool openEventOnLoad = false)
    {
        InitializeComponent();
        _dbService = dbservice;
        BindingContext = this;
        _openEventOnLoad = openEventOnLoad;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_openEventOnLoad)
        {
            _openEventOnLoad = false;
            OpenEventOnClick(null, EventArgs.Empty);
        }
    }

    // ── Navigation ──────────────────────────────────────────────────────────

    private async void ReturnToMainPage(object? sender, EventArgs e) =>
        await Navigation.PopAsync();

    private async void OpenInventoryOnClick(object? sender, EventArgs e)
    {
        try
        {
            await RefreshInventory();
            InventorySection.IsVisible = true;
            EventSection.IsVisible = false;
            SalesSection.IsVisible = false;
            ResetEventPanels();
            ResetSalePanels();
            ResetInventoryPanels();
            InventoryActions.IsVisible = true;
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not load inventory: {ex.Message}", "OK"); }
    }

    private async void OpenEventOnClick(object? sender, EventArgs e)
    {
        try
        {
            await RefreshEvents();
            InventorySection.IsVisible = false;
            EventSection.IsVisible = true;
            SalesSection.IsVisible = false;
            ResetInventoryPanels();
            ResetSalePanels();
            ResetEventPanels();
            EventActions.IsVisible = true;
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not load events: {ex.Message}", "OK"); }
    }

    private async void OpenSalesOnClick(object? sender, EventArgs e)
    {
        try
        {
            var eventList = await _dbService.GetEvent();
            FilterEvents.Clear();
            FilterEvents.Add(new Event { EventId = 0, EventName = "All Events" });
            foreach (var ev in eventList) FilterEvents.Add(ev);

            _allSales = await _dbService.GetSales();
            Sales.Clear();
            foreach (var sale in _allSales) Sales.Add(sale);

            SelectedSaleItems.Clear();
            InventorySection.IsVisible = false;
            EventSection.IsVisible = false;
            SalesSection.IsVisible = true;
            ResetInventoryPanels();
            ResetEventPanels();
            ResetSalePanels();
            SalesCollectionView.SelectionMode = SelectionMode.Single;
            SaleActions.IsVisible = true;
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not load sales: {ex.Message}", "OK"); }
    }

    // ── Sales filter / item display ──────────────────────────────────────────

    private void OnEventFilterChanged(object? sender, EventArgs e)
    {
        if (EventFilterPicker.SelectedItem is Event selectedEvent)
        {
            var filtered = selectedEvent.EventId == 0
                ? _allSales
                : _allSales.Where(s => s.EventId == selectedEvent.EventId).ToList();
            Sales.Clear();
            SelectedSaleItems.Clear();
            foreach (var sale in filtered) Sales.Add(sale);
        }
    }

    private async void OnSaleSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Sale selected) return;

        await LoadSaleItemsForDisplay(selected);

        if (_saleMode == SaleMode.EditPrompt)
        {
            _pendingEditSale = selected;
            EditSalePromptPanel.IsVisible = false;
            DeleteSaleFromEditButton.IsVisible = _comingFromInventoryDelete;
            EditSaleChoosePanel.IsVisible = true;
            _saleMode = SaleMode.EditChoose;
        }
        else if (_saleMode == SaleMode.DeletePrompt)
        {
            _pendingDeleteSale = selected;
            DeleteSalePromptPanel.IsVisible = false;
            DeleteSaleChoosePanel.IsVisible = true;
            _saleMode = SaleMode.DeleteChoose;
        }
    }

    private async Task LoadSaleItemsForDisplay(Sale sale)
    {
        try
        {
            var items = await _dbService.GetSaleItemsBySaleId(sale.SaleId);
            SelectedSaleItems.Clear();
            foreach (var item in items)
            {
                var inventory = await _dbService.GetInventoryById(item.BookId);
                SelectedSaleItems.Add(new SaleItemDisplay
                {
                    SaleItemId = item.SaleItemId,
                    BookTitle = inventory?.BookTitle ?? $"Book #{item.BookId}",
                    Quantity = item.Quantity,
                    SalePrice = item.SalePrice,
                    IsTaxed = item.IsTaxed,
                    RawItem = item
                });
            }
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not load sale items: {ex.Message}", "OK"); }
    }

    private void OnSaleItemSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SaleItemDisplay selected) return;

        if (_saleMode == SaleMode.EditItemPrompt)
        {
            _pendingEditSaleItem = selected;
            EditSaleItemBookLabel.Text = selected.BookTitle;
            EditSaleItemQuantityEntry.Text = selected.Quantity.ToString();
            EditSaleItemTaxPicker.SelectedIndex = selected.IsTaxed ? 0 : 1;
            EditSaleItemErrorLabel.IsVisible = false;
            EditSaleItemPromptPanel.IsVisible = false;
            EditSaleItemFormPanel.IsVisible = true;
            SaleItemsCollectionView.SelectionMode = SelectionMode.None;
            _saleMode = SaleMode.EditItemForm;
        }
        else if (_saleMode == SaleMode.DeleteItemPrompt)
        {
            _pendingDeleteSaleItem = selected;
            DeleteSaleItemPromptPanel.IsVisible = false;
            DeleteSaleItemConfirmPanel.IsVisible = true;
            SaleItemsCollectionView.SelectionMode = SelectionMode.None;
            _saleMode = SaleMode.DeleteItemConfirm;
        }
        else if (_saleMode == SaleMode.EditDeleteItemSelect)
        {
            _pendingEditDeleteSaleItem = selected;
            EditDeleteSaleItemLabel.Text = $"Delete \"{selected.BookTitle}\"?";
            EditDeleteSaleItemSelectPanel.IsVisible = false;
            EditDeleteSaleItemConfirmPanel.IsVisible = true;
            SaleItemsCollectionView.SelectionMode = SelectionMode.None;
            _saleMode = SaleMode.EditDeleteItemConfirm;
        }
    }

    // ── Sale helpers ─────────────────────────────────────────────────────────

    private async Task RecalculateSaleTotals(Sale sale)
    {
        var evt = await _dbService.GetEventById(sale.EventId);
        double taxRate = evt?.EventTax ?? 0;
        var items = await _dbService.GetSaleItemsBySaleId(sale.SaleId);

        double netCost = 0, tax = 0;
        foreach (var item in items)
        {
            double basePrice = item.IsTaxed && taxRate > 0
                ? (double)item.SalePrice / (1.0 + taxRate)
                : (double)item.SalePrice;
            netCost += basePrice;
            tax += (double)item.SalePrice - basePrice;
        }

        sale.SalePrice = Math.Round(netCost, 2);
        sale.Tax = Math.Round(tax, 2);
        sale.Total = Math.Round(netCost + tax, 2);
        await _dbService.UpdateSale(sale);
    }

    private async Task RefreshSales()
    {
        _allSales = await _dbService.GetSales();
        var visible = _comingFromInventoryDelete
            ? _allSales.Where(s => _affectedSaleIds.Contains(s.SaleId)).ToList()
            : _allSales;
        Sales.Clear();
        foreach (var sale in visible) Sales.Add(sale);
    }

    private void ResetSalePanels()
    {
        SaleActions.IsVisible = false;
        EditSalePromptPanel.IsVisible = false;
        EditSaleChoosePanel.IsVisible = false;
        EditSaleFormPanel.IsVisible = false;
        EditSaleItemPromptPanel.IsVisible = false;
        EditSaleItemFormPanel.IsVisible = false;
        AddSaleItemPanel.IsVisible = false;
        EditDeleteSaleItemSelectPanel.IsVisible = false;
        EditDeleteSaleItemConfirmPanel.IsVisible = false;
        DeleteSalePromptPanel.IsVisible = false;
        DeleteSaleChoosePanel.IsVisible = false;
        DeleteSaleConfirmPanel.IsVisible = false;
        DeleteSaleItemPromptPanel.IsVisible = false;
        DeleteSaleItemConfirmPanel.IsVisible = false;
        _saleMode = SaleMode.None;
        SaleItemsCollectionView.SelectionMode = SelectionMode.None;
        SaleItemsCollectionView.SelectedItem = null;
        _pendingEditSale = null;
        _pendingDeleteSale = null;
        _pendingEditSaleItem = null;
        _pendingDeleteSaleItem = null;
        _pendingEditDeleteSaleItem = null;
    }

    private void ReturnToSaleIdleState()
    {
        EditSalePromptPanel.IsVisible = false;
        EditSaleChoosePanel.IsVisible = false;
        EditSaleFormPanel.IsVisible = false;
        EditSaleItemPromptPanel.IsVisible = false;
        EditSaleItemFormPanel.IsVisible = false;
        AddSaleItemPanel.IsVisible = false;
        EditDeleteSaleItemSelectPanel.IsVisible = false;
        EditDeleteSaleItemConfirmPanel.IsVisible = false;
        DeleteSalePromptPanel.IsVisible = false;
        DeleteSaleChoosePanel.IsVisible = false;
        DeleteSaleConfirmPanel.IsVisible = false;
        DeleteSaleItemPromptPanel.IsVisible = false;
        DeleteSaleItemConfirmPanel.IsVisible = false;
        SaleItemsCollectionView.SelectionMode = SelectionMode.None;
        SaleItemsCollectionView.SelectedItem = null;
        SalesCollectionView.SelectionMode = SelectionMode.Single;
        SalesCollectionView.SelectedItem = null;
        SelectedSaleItems.Clear();
        _pendingEditSale = null;
        _pendingDeleteSale = null;
        _pendingEditSaleItem = null;
        _pendingDeleteSaleItem = null;
        _pendingEditDeleteSaleItem = null;

        if (_comingFromInventoryDelete)
        {
            EditSalePromptPanel.IsVisible = true;
            BackToDeleteInventoryPanel.IsVisible = true;
            _saleMode = SaleMode.EditPrompt;
        }
        else
        {
            SaleActions.IsVisible = true;
            _saleMode = SaleMode.None;
        }
    }

    private void ReturnToInventoryIdleState()
    {
        EditPromptPanel.IsVisible = false;
        EditFormPanel.IsVisible = false;
        DeletePromptPanel.IsVisible = false;
        DeleteConfirmPanel.IsVisible = false;
        AffectedSalesConfirmPanel.IsVisible = false;
        NewItemPanel.IsVisible = false;
        _inventoryMode = InventoryMode.None;
        _affectedSaleIds.Clear();
        InventoryCollectionView.SelectionMode = SelectionMode.None;
        InventoryCollectionView.SelectedItem = null;
        _pendingEditItem = null;
        _pendingDeleteItem = null;
        InventoryActions.IsVisible = true;
    }

    // ── Sale action buttons ──────────────────────────────────────────────────

    private void OnEditSaleClicked(object? sender, EventArgs e)
    {
        _saleMode = SaleMode.EditPrompt;
        SalesCollectionView.SelectedItem = null;
        SaleItemsCollectionView.SelectedItem = null;
        SelectedSaleItems.Clear();
        EditSalePromptPanel.IsVisible = true;
        EditSaleChoosePanel.IsVisible = false;
        EditSaleFormPanel.IsVisible = false;
        EditSaleItemPromptPanel.IsVisible = false;
        EditSaleItemFormPanel.IsVisible = false;
        AddSaleItemPanel.IsVisible = false;
        DeleteSalePromptPanel.IsVisible = false;
        DeleteSaleChoosePanel.IsVisible = false;
        DeleteSaleConfirmPanel.IsVisible = false;
        DeleteSaleItemPromptPanel.IsVisible = false;
        DeleteSaleItemConfirmPanel.IsVisible = false;
    }

    private void OnDeleteSaleClicked(object? sender, EventArgs e)
    {
        _saleMode = SaleMode.DeletePrompt;
        SalesCollectionView.SelectedItem = null;
        SaleItemsCollectionView.SelectedItem = null;
        SelectedSaleItems.Clear();
        DeleteSalePromptPanel.IsVisible = true;
        EditSalePromptPanel.IsVisible = false;
        EditSaleChoosePanel.IsVisible = false;
        EditSaleFormPanel.IsVisible = false;
        EditSaleItemPromptPanel.IsVisible = false;
        EditSaleItemFormPanel.IsVisible = false;
        AddSaleItemPanel.IsVisible = false;
        DeleteSaleChoosePanel.IsVisible = false;
        DeleteSaleConfirmPanel.IsVisible = false;
        DeleteSaleItemPromptPanel.IsVisible = false;
        DeleteSaleItemConfirmPanel.IsVisible = false;
    }

    // ── Edit sale: choose step ───────────────────────────────────────────────

    private void OnEditWholeSaleClicked(object? sender, EventArgs e)
    {
        if (_pendingEditSale is null) return;
        EditSaleIdLabel.Text = $"Sale #{_pendingEditSale.SaleId}  |  Event #{_pendingEditSale.EventId}";
        EditSalePriceEntry.Text = _pendingEditSale.SalePrice.ToString("F2");
        EditSaleTaxEntry.Text = _pendingEditSale.Tax.ToString("F2");
        EditSaleTotalEntry.Text = _pendingEditSale.Total.ToString("F2");
        EditSaleErrorLabel.IsVisible = false;
        EditSaleChoosePanel.IsVisible = false;
        EditSaleFormPanel.IsVisible = true;
        _saleMode = SaleMode.EditSaleForm;
    }

    private void OnEditSaleItemPromptClicked(object? sender, EventArgs e)
    {
        EditSaleChoosePanel.IsVisible = false;
        EditSaleItemPromptPanel.IsVisible = true;
        SaleItemsCollectionView.SelectionMode = SelectionMode.Single;
        _saleMode = SaleMode.EditItemPrompt;
    }

    private void OnEditSaleChooseCancelClicked(object? sender, EventArgs e) =>
        ReturnToSaleIdleState();

    private async void OnAddSaleItemClicked(object? sender, EventArgs e)
    {
        await RefreshInventory();
        EditSaleChoosePanel.IsVisible = false;
        AddSaleItemBookPicker.SelectedIndex = -1;
        AddSaleItemQuantityEntry.Text = "1";
        AddSaleItemTaxPicker.SelectedIndex = -1;
        AddSaleItemErrorLabel.IsVisible = false;
        AddSaleItemPanel.IsVisible = true;
        _saleMode = SaleMode.AddItem;
    }

    private async void OnAddSaleItemSaveClicked(object? sender, EventArgs e)
    {
        if (_pendingEditSale is null) return;

        var errors = new List<string>();
        Inventory? selectedBook = AddSaleItemBookPicker.SelectedItem as Inventory;
        if (selectedBook == null)
            errors.Add("Please select a book.");
        if (!int.TryParse(AddSaleItemQuantityEntry.Text, out int qty) || qty < 1)
            errors.Add("Quantity must be a whole number greater than 0.");
        if (AddSaleItemTaxPicker.SelectedIndex < 0)
            errors.Add("Please select whether tax applies.");

        if (errors.Count > 0)
        {
            AddSaleItemErrorLabel.Text = string.Join("\n", errors);
            AddSaleItemErrorLabel.IsVisible = true;
            return;
        }

        if (selectedBook!.InStock - qty < 0)
        {
            AddSaleItemErrorLabel.Text = $"Insufficient stock. Only {selectedBook.InStock} in stock.";
            AddSaleItemErrorLabel.IsVisible = true;
            return;
        }

        bool isTaxed = AddSaleItemTaxPicker.SelectedItem?.ToString() == "Yes";
        var evt = await _dbService.GetEventById(_pendingEditSale.EventId);
        double taxRate = evt?.EventTax ?? 0;
        double baseCost = selectedBook.Cost * qty;
        float newSalePrice = (float)Math.Round(isTaxed ? baseCost * (1 + taxRate) : baseCost, 2);

        selectedBook.InStock -= qty;
        await _dbService.UpdateInventoryItem(selectedBook);

        await _dbService.CreateSaleItem(new SaleItem
        {
            SaleId = _pendingEditSale.SaleId,
            BookId = selectedBook.InventoryId,
            Quantity = qty,
            SalePrice = newSalePrice,
            IsTaxed = isTaxed
        });

        try
        {
            await RecalculateSaleTotals(_pendingEditSale);
            await RefreshSales();
            await LoadSaleItemsForDisplay(_pendingEditSale);
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not add sale item: {ex.Message}", "OK"); return; }

        AddSaleItemPanel.IsVisible = false;
        _saleMode = SaleMode.None;
    }

    private void OnAddSaleItemCancelClicked(object? sender, EventArgs e) =>
        ReturnToSaleIdleState();

    // ── Edit sale: whole sale form ───────────────────────────────────────────

    private async void OnEditSaleSaveClicked(object? sender, EventArgs e)
    {
        if (_pendingEditSale is null) return;

        var errors = new List<string>();
        if (!double.TryParse(EditSalePriceEntry.Text, out double price))
            errors.Add("Sale Price must be a number.");
        if (!double.TryParse(EditSaleTaxEntry.Text, out double tax))
            errors.Add("Tax must be a number.");
        if (!double.TryParse(EditSaleTotalEntry.Text, out double total))
            errors.Add("Total must be a number.");

        if (errors.Count > 0)
        {
            EditSaleErrorLabel.Text = string.Join("\n", errors);
            EditSaleErrorLabel.IsVisible = true;
            return;
        }

        _pendingEditSale.SalePrice = price;
        _pendingEditSale.Tax = tax;
        _pendingEditSale.Total = total;

        try
        {
            await _dbService.UpdateSale(_pendingEditSale);
            await RefreshSales();
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not update sale: {ex.Message}", "OK"); return; }

        EditSaleFormPanel.IsVisible = false;
        _pendingEditSale = null;
        _saleMode = SaleMode.None;
    }

    private void OnEditSaleFormCancelClicked(object? sender, EventArgs e) =>
        ReturnToSaleIdleState();

    // ── Edit sale: item form ─────────────────────────────────────────────────

    private async void OnEditSaleItemSaveClicked(object? sender, EventArgs e)
    {
        if (_pendingEditSaleItem?.RawItem is null) return;

        var errors = new List<string>();
        if (!int.TryParse(EditSaleItemQuantityEntry.Text, out int qty) || qty < 1)
            errors.Add("Quantity must be a whole number greater than 0.");
        if (EditSaleItemTaxPicker.SelectedIndex < 0)
            errors.Add("Please select whether tax applies.");

        if (errors.Count > 0)
        {
            EditSaleItemErrorLabel.Text = string.Join("\n", errors);
            EditSaleItemErrorLabel.IsVisible = true;
            return;
        }

        bool isTaxed = EditSaleItemTaxPicker.SelectedItem?.ToString() == "Yes";

        // Fetch inventory for unit cost and stock check
        var inv = await _dbService.GetInventoryById(_pendingEditSaleItem.RawItem.BookId);
        if (inv == null)
        {
            EditSaleItemErrorLabel.Text = "Could not find the inventory item.";
            EditSaleItemErrorLabel.IsVisible = true;
            return;
        }

        // Check stock for any quantity increase
        int oldQty = _pendingEditSaleItem.RawItem.Quantity;
        int qtyDiff = qty - oldQty;
        if (qtyDiff > 0 && inv.InStock - qtyDiff < 0)
        {
            EditSaleItemErrorLabel.Text =
                $"Insufficient stock. Only {inv.InStock} additional units available.";
            EditSaleItemErrorLabel.IsVisible = true;
            return;
        }

        // Get event tax rate for price calculation
        double taxRate = 0;
        if (_pendingEditSale != null)
        {
            var evt = await _dbService.GetEventById(_pendingEditSale.EventId);
            taxRate = evt?.EventTax ?? 0;
        }

        double baseCost = inv.Cost * qty;
        float newSalePrice = (float)Math.Round(isTaxed ? baseCost * (1 + taxRate) : baseCost, 2);

        // Adjust inventory stock for quantity change
        if (qtyDiff != 0)
        {
            inv.InStock -= qtyDiff;
            await _dbService.UpdateInventoryItem(inv);
        }

        _pendingEditSaleItem.RawItem.Quantity = qty;
        _pendingEditSaleItem.RawItem.SalePrice = newSalePrice;
        _pendingEditSaleItem.RawItem.IsTaxed = isTaxed;

        try
        {
            await _dbService.UpdaSaleItem(_pendingEditSaleItem.RawItem);
            if (_pendingEditSale is not null)
            {
                await RecalculateSaleTotals(_pendingEditSale);
                await RefreshSales();
                await LoadSaleItemsForDisplay(_pendingEditSale);
            }
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not update sale item: {ex.Message}", "OK"); return; }

        EditSaleItemFormPanel.IsVisible = false;
        _pendingEditSaleItem = null;
        _saleMode = SaleMode.None;
    }

    private void OnEditSaleItemFormCancelClicked(object? sender, EventArgs e) =>
        ReturnToSaleIdleState();

    // ── Edit sale: delete sale item from edit flow ───────────────────────────

    private void OnEditDeleteSaleItemClicked(object? sender, EventArgs e)
    {
        EditSaleChoosePanel.IsVisible = false;
        EditDeleteSaleItemSelectPanel.IsVisible = true;
        SaleItemsCollectionView.SelectionMode = SelectionMode.Single;
        _saleMode = SaleMode.EditDeleteItemSelect;
    }

    private async void OnEditDeleteSaleItemConfirmClicked(object? sender, EventArgs e)
    {
        if (_pendingEditDeleteSaleItem?.RawItem is null) return;

        try
        {
            var inv = await _dbService.GetInventoryById(_pendingEditDeleteSaleItem.RawItem.BookId);
            if (inv != null)
            {
                inv.InStock += _pendingEditDeleteSaleItem.RawItem.Quantity;
                await _dbService.UpdateInventoryItem(inv);
            }
            await _dbService.DeleteSaleItem(_pendingEditDeleteSaleItem.RawItem);
            if (_pendingEditSale is not null)
            {
                await RecalculateSaleTotals(_pendingEditSale);
                await RefreshSales();
                await LoadSaleItemsForDisplay(_pendingEditSale);
            }
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not delete sale item: {ex.Message}", "OK"); return; }

        EditDeleteSaleItemConfirmPanel.IsVisible = false;
        SaleItemsCollectionView.SelectedItem = null;
        _pendingEditDeleteSaleItem = null;
        _saleMode = SaleMode.None;
    }

    private void OnEditDeleteSaleItemCancelClicked(object? sender, EventArgs e) =>
        ReturnToSaleIdleState();

    private async void OnDeleteSaleFromEditClicked(object? sender, EventArgs e)
    {
        if (_pendingEditSale is null) return;

        bool confirmed = await DisplayAlert(
            "Delete Sale",
            $"Delete Sale #{_pendingEditSale.SaleId} and all its items? Inventory stock will be restored.",
            "Delete Sale", "Cancel");
        if (!confirmed) return;

        try
        {
            var items = await _dbService.GetSaleItemsBySaleId(_pendingEditSale.SaleId);
            foreach (var item in items)
            {
                var inv = await _dbService.GetInventoryById(item.BookId);
                if (inv != null)
                {
                    inv.InStock += item.Quantity;
                    await _dbService.UpdateInventoryItem(inv);
                }
                await _dbService.DeleteSaleItem(item);
            }
            await _dbService.DeleteSale(_pendingEditSale);
            await RefreshSales();
            SelectedSaleItems.Clear();
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not delete sale: {ex.Message}", "OK"); return; }

        ReturnToSaleIdleState();
    }

    // ── Edit sale: cancel/prompt handlers ───────────────────────────────────

    private void OnEditSaleCancelClicked(object? sender, EventArgs e) =>
        ReturnToSaleIdleState();

    private void OnEditSaleItemPromptCancelClicked(object? sender, EventArgs e) =>
        ReturnToSaleIdleState();

    // ── Delete sale: choose step ─────────────────────────────────────────────

    private void OnDeleteWholeSaleClicked(object? sender, EventArgs e)
    {
        DeleteSaleChoosePanel.IsVisible = false;
        DeleteSaleConfirmPanel.IsVisible = true;
        _saleMode = SaleMode.DeleteSaleConfirm;
    }

    private void OnDeleteSaleItemPromptClicked(object? sender, EventArgs e)
    {
        DeleteSaleChoosePanel.IsVisible = false;
        DeleteSaleItemPromptPanel.IsVisible = true;
        SaleItemsCollectionView.SelectionMode = SelectionMode.Single;
        _saleMode = SaleMode.DeleteItemPrompt;
    }

    private void OnDeleteSaleChooseCancelClicked(object? sender, EventArgs e) =>
        ReturnToSaleIdleState();

    // ── Delete sale: whole sale confirm ─────────────────────────────────────

    private async void OnDeleteSaleYesClicked(object? sender, EventArgs e)
    {
        if (_pendingDeleteSale is null) return;

        try
        {
            var items = await _dbService.GetSaleItemsBySaleId(_pendingDeleteSale.SaleId);
            foreach (var item in items)
            {
                var inv = await _dbService.GetInventoryById(item.BookId);
                if (inv != null)
                {
                    inv.InStock += item.Quantity;
                    await _dbService.UpdateInventoryItem(inv);
                }
                await _dbService.DeleteSaleItem(item);
            }
            await _dbService.DeleteSale(_pendingDeleteSale);
            await RefreshSales();
            SelectedSaleItems.Clear();
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not delete sale: {ex.Message}", "OK"); return; }

        DeleteSaleConfirmPanel.IsVisible = false;
        _pendingDeleteSale = null;
        _saleMode = SaleMode.None;
    }

    private void OnDeleteSaleNoClicked(object? sender, EventArgs e) =>
        ReturnToSaleIdleState();

    // ── Delete sale: item confirm ────────────────────────────────────────────

    private async void OnDeleteSaleItemYesClicked(object? sender, EventArgs e)
    {
        if (_pendingDeleteSaleItem?.RawItem is null) return;

        try
        {
            var inv = await _dbService.GetInventoryById(_pendingDeleteSaleItem.RawItem.BookId);
            if (inv != null)
            {
                inv.InStock += _pendingDeleteSaleItem.RawItem.Quantity;
                await _dbService.UpdateInventoryItem(inv);
            }
            await _dbService.DeleteSaleItem(_pendingDeleteSaleItem.RawItem);
            if (_pendingDeleteSale is not null)
            {
                await RecalculateSaleTotals(_pendingDeleteSale);
                await RefreshSales();
                await LoadSaleItemsForDisplay(_pendingDeleteSale);
            }
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not delete sale item: {ex.Message}", "OK"); return; }

        DeleteSaleItemConfirmPanel.IsVisible = false;
        SaleItemsCollectionView.SelectedItem = null;
        _pendingDeleteSaleItem = null;
        _saleMode = SaleMode.None;
    }

    private void OnDeleteSaleItemNoClicked(object? sender, EventArgs e) =>
        ReturnToSaleIdleState();

    // ── Delete sale: cancel/prompt handlers ─────────────────────────────────

    private void OnDeleteSaleCancelClicked(object? sender, EventArgs e) =>
        ReturnToSaleIdleState();

    private void OnDeleteSaleItemPromptCancelClicked(object? sender, EventArgs e) =>
        ReturnToSaleIdleState();

    // ── Inventory helpers ────────────────────────────────────────────────────

    private async Task RefreshInventory()
    {
        var list = await _dbService.GetInventory();
        ItemsInInventory.Clear();
        foreach (var item in list) ItemsInInventory.Add(item);
    }

    private void ResetInventoryPanels()
    {
        InventoryActions.IsVisible = false;
        EditPromptPanel.IsVisible = false;
        EditFormPanel.IsVisible = false;
        DeletePromptPanel.IsVisible = false;
        DeleteConfirmPanel.IsVisible = false;
        AffectedSalesConfirmPanel.IsVisible = false;
        NewItemPanel.IsVisible = false;
        BackToDeleteInventoryPanel.IsVisible = false;
        _inventoryMode = InventoryMode.None;
        _comingFromInventoryDelete = false;
        _affectedSaleIds.Clear();
        InventoryCollectionView.SelectionMode = SelectionMode.None;
        InventoryCollectionView.SelectedItem = null;
        _pendingEditItem = null;
        _pendingDeleteItem = null;
    }

    // ── Inventory action buttons ─────────────────────────────────────────────

    private void OnEditItemClicked(object? sender, EventArgs e)
    {
        _inventoryMode = InventoryMode.EditPrompt;
        EditPromptPanel.IsVisible = true;
        EditFormPanel.IsVisible = false;
        DeletePromptPanel.IsVisible = false;
        DeleteConfirmPanel.IsVisible = false;
        NewItemPanel.IsVisible = false;
        InventoryCollectionView.SelectionMode = SelectionMode.Single;
        InventoryCollectionView.SelectedItem = null;
    }

    private void OnDeleteItemClicked(object? sender, EventArgs e)
    {
        _inventoryMode = InventoryMode.DeletePrompt;
        DeletePromptPanel.IsVisible = true;
        EditPromptPanel.IsVisible = false;
        EditFormPanel.IsVisible = false;
        DeleteConfirmPanel.IsVisible = false;
        NewItemPanel.IsVisible = false;
        InventoryCollectionView.SelectionMode = SelectionMode.Single;
        InventoryCollectionView.SelectedItem = null;
    }

    private void OnNewItemClicked(object? sender, EventArgs e)
    {
        _inventoryMode = InventoryMode.None;
        NewItemPanel.IsVisible = true;
        EditPromptPanel.IsVisible = false;
        EditFormPanel.IsVisible = false;
        DeletePromptPanel.IsVisible = false;
        DeleteConfirmPanel.IsVisible = false;
        NewBookTitleEntry.Text = string.Empty;
        NewCostEntry.Text = string.Empty;
        NewInStockEntry.Text = string.Empty;
        NewItemErrorLabel.IsVisible = false;
        InventoryCollectionView.SelectionMode = SelectionMode.None;
        InventoryCollectionView.SelectedItem = null;
    }

    private async void OnInventoryItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Inventory selected) return;

        if (_inventoryMode == InventoryMode.EditPrompt)
        {
            _pendingEditItem = selected;
            EditBookTitleEntry.Text = selected.BookTitle;
            EditCostEntry.Text = selected.Cost.ToString("F2");
            EditInStockEntry.Text = selected.InStock.ToString();
            EditErrorLabel.IsVisible = false;
            EditPromptPanel.IsVisible = false;
            EditFormPanel.IsVisible = true;
            _inventoryMode = InventoryMode.EditForm;
        }
        else if (_inventoryMode == InventoryMode.DeletePrompt)
        {
            _pendingDeleteItem = selected;
            DeletePromptPanel.IsVisible = false;
            InventoryCollectionView.SelectionMode = SelectionMode.None;
            InventoryCollectionView.SelectedItem = null;
            int count = await LoadAffectedSaleIds(selected.InventoryId);
            if (count > 0)
            {
                AffectedSalesCountLabel.Text = $"{count} sale{(count == 1 ? "" : "s")} will be affected.";
                AffectedSalesConfirmPanel.IsVisible = true;
                _inventoryMode = InventoryMode.DeleteAffectedSales;
            }
            else
            {
                DeleteConfirmPanel.IsVisible = true;
                _inventoryMode = InventoryMode.DeleteConfirm;
            }
        }
    }

    private async Task<int> LoadAffectedSaleIds(int inventoryId)
    {
        _affectedSaleIds.Clear();
        var saleItems = await _dbService.GetSaleItemsByBookId(inventoryId);
        foreach (var si in saleItems)
            _affectedSaleIds.Add(si.SaleId);
        return _affectedSaleIds.Count;
    }

    private async void OnAffectedSalesYesClicked(object? sender, EventArgs e)
    {
        AffectedSalesConfirmPanel.IsVisible = false;
        _comingFromInventoryDelete = true;

        var eventList = await _dbService.GetEvent();
        FilterEvents.Clear();
        FilterEvents.Add(new Event { EventId = 0, EventName = "All Events" });
        foreach (var ev in eventList) FilterEvents.Add(ev);

        _allSales = await _dbService.GetSales();
        var filteredSales = _allSales.Where(s => _affectedSaleIds.Contains(s.SaleId)).ToList();
        Sales.Clear();
        foreach (var s in filteredSales) Sales.Add(s);
        SelectedSaleItems.Clear();

        InventorySection.IsVisible = false;
        InventoryActions.IsVisible = false;
        SalesSection.IsVisible = true;

        ResetSalePanels();
        SalesCollectionView.SelectionMode = SelectionMode.Single;
        SalesCollectionView.SelectedItem = null;
        EditSalePromptPanel.IsVisible = true;
        _saleMode = SaleMode.EditPrompt;
        BackToDeleteInventoryPanel.IsVisible = true;
    }

    private void OnAffectedSalesCancelClicked(object? sender, EventArgs e) =>
        ReturnToInventoryIdleState();

    private async void OnBackToDeleteInventoryClicked(object? sender, EventArgs e)
    {
        BackToDeleteInventoryPanel.IsVisible = false;
        _comingFromInventoryDelete = false;
        ResetSalePanels();
        SalesCollectionView.SelectedItem = null;

        SalesSection.IsVisible = false;
        InventorySection.IsVisible = true;
        InventoryActions.IsVisible = true;

        await RefreshInventory();

        if (_pendingDeleteItem != null)
        {
            int count = await LoadAffectedSaleIds(_pendingDeleteItem.InventoryId);
            if (count > 0)
            {
                AffectedSalesCountLabel.Text = $"{count} sale{(count == 1 ? "" : "s")} will be affected.";
                AffectedSalesConfirmPanel.IsVisible = true;
                _inventoryMode = InventoryMode.DeleteAffectedSales;
            }
            else
            {
                DeleteConfirmPanel.IsVisible = true;
                _inventoryMode = InventoryMode.DeleteConfirm;
            }
        }
    }

    private void OnEditCancelClicked(object? sender, EventArgs e) =>
        ReturnToInventoryIdleState();

    private async void OnEditSaveClicked(object? sender, EventArgs e)
    {
        if (_pendingEditItem is null) return;

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(EditBookTitleEntry.Text))
            errors.Add("Item Name is required.");
        if (!double.TryParse(EditCostEntry.Text, out double cost))
            errors.Add("Cost must be a number (e.g. 9.99).");
        if (!int.TryParse(EditInStockEntry.Text, out int inStock))
            errors.Add("Number in Stock must be a whole number.");

        if (errors.Count > 0)
        {
            EditErrorLabel.Text = string.Join("\n", errors);
            EditErrorLabel.IsVisible = true;
            return;
        }

        _pendingEditItem.BookTitle = EditBookTitleEntry.Text.Trim();
        _pendingEditItem.Cost = cost;
        _pendingEditItem.InStock = inStock;

        try
        {
            await _dbService.UpdateInventoryItem(_pendingEditItem);
            await RefreshInventory();
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not update item: {ex.Message}", "OK"); return; }

        EditFormPanel.IsVisible = false;
        _inventoryMode = InventoryMode.None;
        InventoryCollectionView.SelectionMode = SelectionMode.None;
        InventoryCollectionView.SelectedItem = null;
        _pendingEditItem = null;
    }

    private void OnEditFormCancelClicked(object? sender, EventArgs e) =>
        ReturnToInventoryIdleState();

    private async void OnDeleteYesClicked(object? sender, EventArgs e)
    {
        if (_pendingDeleteItem is null) return;

        try
        {
            await _dbService.DeleteInventoryItem(_pendingDeleteItem);
            await RefreshInventory();
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not delete item: {ex.Message}", "OK"); return; }

        DeleteConfirmPanel.IsVisible = false;
        _inventoryMode = InventoryMode.None;
        InventoryCollectionView.SelectionMode = SelectionMode.None;
        InventoryCollectionView.SelectedItem = null;
        _pendingDeleteItem = null;
    }

    private void OnDeleteNoClicked(object? sender, EventArgs e) =>
        ReturnToInventoryIdleState();

    private async void OnNewItemEnterClicked(object? sender, EventArgs e)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(NewBookTitleEntry.Text))
            errors.Add("Item Name is required.");
        if (!double.TryParse(NewCostEntry.Text, out double cost))
            errors.Add("Cost must be a number (e.g. 9.99).");
        if (!int.TryParse(NewInStockEntry.Text, out int inStock))
            errors.Add("Number in Stock must be a whole number.");

        if (errors.Count > 0)
        {
            NewItemErrorLabel.Text = string.Join("\n", errors);
            NewItemErrorLabel.IsVisible = true;
            return;
        }

        try
        {
            await _dbService.CreateInventoryItem(new Inventory
            {
                BookTitle = NewBookTitleEntry.Text.Trim(),
                Cost = cost,
                InStock = inStock
            });
            await RefreshInventory();
            NewItemPanel.IsVisible = false;
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not create item: {ex.Message}", "OK"); }
    }

    private void OnNewItemCancelClicked(object? sender, EventArgs e)
    {
        NewItemPanel.IsVisible = false;
        NewItemErrorLabel.IsVisible = false;
    }

    // ── Event helpers ────────────────────────────────────────────────────────

    private async Task RefreshEvents()
    {
        var list = await _dbService.GetEvent();
        Events.Clear();
        foreach (var ev in list) Events.Add(ev);
    }

    private void ResetEventPanels()
    {
        EventActions.IsVisible = false;
        EditEventPromptPanel.IsVisible = false;
        EditEventFormPanel.IsVisible = false;
        DeleteEventPromptPanel.IsVisible = false;
        DeleteEventConfirmPanel.IsVisible = false;
        NewEventPanel.IsVisible = false;
        _eventMode = EventMode.None;
        EventCollectionView.SelectionMode = SelectionMode.None;
        EventCollectionView.SelectedItem = null;
        _pendingEditEvent = null;
        _pendingDeleteEvent = null;
    }

    // ── Event action buttons ─────────────────────────────────────────────────

    private void OnEditEventClicked(object? sender, EventArgs e)
    {
        _eventMode = EventMode.EditPrompt;
        EditEventPromptPanel.IsVisible = true;
        EditEventFormPanel.IsVisible = false;
        DeleteEventPromptPanel.IsVisible = false;
        DeleteEventConfirmPanel.IsVisible = false;
        NewEventPanel.IsVisible = false;
        EventCollectionView.SelectionMode = SelectionMode.Single;
        EventCollectionView.SelectedItem = null;
    }

    private void OnDeleteEventClicked(object? sender, EventArgs e)
    {
        _eventMode = EventMode.DeletePrompt;
        DeleteEventPromptPanel.IsVisible = true;
        EditEventPromptPanel.IsVisible = false;
        EditEventFormPanel.IsVisible = false;
        DeleteEventConfirmPanel.IsVisible = false;
        NewEventPanel.IsVisible = false;
        EventCollectionView.SelectionMode = SelectionMode.Single;
        EventCollectionView.SelectedItem = null;
    }

    private void OnNewEventClicked(object? sender, EventArgs e)
    {
        _eventMode = EventMode.None;
        NewEventPanel.IsVisible = true;
        EditEventPromptPanel.IsVisible = false;
        EditEventFormPanel.IsVisible = false;
        DeleteEventPromptPanel.IsVisible = false;
        DeleteEventConfirmPanel.IsVisible = false;
        NewEventNameEntry.Text = string.Empty;
        NewEventDatePicker.Date = DateTime.Today;
        NewEventTaxEntry.Text = string.Empty;
        NewEventErrorLabel.IsVisible = false;
        EventCollectionView.SelectionMode = SelectionMode.None;
        EventCollectionView.SelectedItem = null;
    }

    private void OnEventItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Event selected) return;

        if (_eventMode == EventMode.EditPrompt)
        {
            _pendingEditEvent = selected;
            EditEventNameEntry.Text = selected.EventName;
            EditEventDatePicker.Date = selected.EventDate;
            EditEventTaxEntry.Text = selected.EventTax.ToString("F4");
            EditEventErrorLabel.IsVisible = false;
            EditEventPromptPanel.IsVisible = false;
            EditEventFormPanel.IsVisible = true;
            _eventMode = EventMode.EditForm;
        }
        else if (_eventMode == EventMode.DeletePrompt)
        {
            _pendingDeleteEvent = selected;
            DeleteEventPromptPanel.IsVisible = false;
            DeleteEventConfirmPanel.IsVisible = true;
            _eventMode = EventMode.DeleteConfirm;
        }
    }

    private void OnEditEventCancelClicked(object? sender, EventArgs e)
    {
        EditEventPromptPanel.IsVisible = false;
        _eventMode = EventMode.None;
        EventCollectionView.SelectionMode = SelectionMode.None;
        EventCollectionView.SelectedItem = null;
    }

    private async void OnEditEventSaveClicked(object? sender, EventArgs e)
    {
        if (_pendingEditEvent is null) return;

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(EditEventNameEntry.Text))
            errors.Add("Event Name is required.");
        if (!double.TryParse(EditEventTaxEntry.Text, out double tax))
            errors.Add("Tax Rate must be a number (e.g. 0.08 for 8%).");

        if (errors.Count > 0)
        {
            EditEventErrorLabel.Text = string.Join("\n", errors);
            EditEventErrorLabel.IsVisible = true;
            return;
        }

        _pendingEditEvent.EventName = EditEventNameEntry.Text.Trim();
        _pendingEditEvent.EventDate = EditEventDatePicker.Date ?? DateTime.Today;
        _pendingEditEvent.EventTax = tax;

        try
        {
            await _dbService.UpdateEvent(_pendingEditEvent);
            await RefreshEvents();
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not update event: {ex.Message}", "OK"); return; }

        EditEventFormPanel.IsVisible = false;
        _eventMode = EventMode.None;
        EventCollectionView.SelectionMode = SelectionMode.None;
        EventCollectionView.SelectedItem = null;
        _pendingEditEvent = null;
    }

    private void OnEditEventFormCancelClicked(object? sender, EventArgs e)
    {
        EditEventFormPanel.IsVisible = false;
        _eventMode = EventMode.None;
        EventCollectionView.SelectionMode = SelectionMode.None;
        EventCollectionView.SelectedItem = null;
        _pendingEditEvent = null;
    }

    private async void OnDeleteEventYesClicked(object? sender, EventArgs e)
    {
        if (_pendingDeleteEvent is null) return;

        try
        {
            await _dbService.DeleteEvent(_pendingDeleteEvent);
            await RefreshEvents();
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not delete event: {ex.Message}", "OK"); return; }

        DeleteEventConfirmPanel.IsVisible = false;
        _eventMode = EventMode.None;
        EventCollectionView.SelectionMode = SelectionMode.None;
        EventCollectionView.SelectedItem = null;
        _pendingDeleteEvent = null;
    }

    private void OnDeleteEventNoClicked(object? sender, EventArgs e)
    {
        DeleteEventConfirmPanel.IsVisible = false;
        _eventMode = EventMode.None;
        EventCollectionView.SelectionMode = SelectionMode.None;
        EventCollectionView.SelectedItem = null;
        _pendingDeleteEvent = null;
    }

    private async void OnNewEventEnterClicked(object? sender, EventArgs e)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(NewEventNameEntry.Text))
            errors.Add("Event Name is required.");
        if (!double.TryParse(NewEventTaxEntry.Text, out double tax))
            errors.Add("Tax Rate must be a number (e.g. 0.08 for 8%).");

        if (errors.Count > 0)
        {
            NewEventErrorLabel.Text = string.Join("\n", errors);
            NewEventErrorLabel.IsVisible = true;
            return;
        }

        try
        {
            await _dbService.CreateEvent(new Event
            {
                EventName = NewEventNameEntry.Text.Trim(),
                EventDate = NewEventDatePicker.Date ?? DateTime.Today,
                EventTax = tax
            });
            await RefreshEvents();
            NewEventPanel.IsVisible = false;
        }
        catch (Exception ex) { await DisplayAlertAsync("Error", $"Could not create event: {ex.Message}", "OK"); }
    }

    private void OnNewEventCancelClicked(object? sender, EventArgs e)
    {
        NewEventPanel.IsVisible = false;
        NewEventErrorLabel.IsVisible = false;
    }
}
