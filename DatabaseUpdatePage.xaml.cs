using MauiApp6;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using SQLite;
using System.Threading.Tasks;
using DBService;


namespace MauiApp6;

public partial class DatabaseUpdatePage : ContentPage
{
    readonly DBService.LocalDBService _dbService;

    public ObservableCollection<Inventory> ItemsInInventory { get; set; } = new();
    public ObservableCollection<Sale> Sales { get; set; } = new();
    public ObservableCollection<Event> Events { get; set; } = new();


    public DatabaseUpdatePage(DBService.LocalDBService dbservice)
    { 


		InitializeComponent();

        _dbService = dbservice;
        BindingContext = this;

        
        
	}

   

    private async void ReturnToMainPage(object sender, EventArgs e)
        {
            
                await Navigation.PopAsync();
                return;
           
        }

    private async void OpenInventoryOnClick(object sender, EventArgs e)  
    {
        try
        {

            

            
            {
                var inventoryList = await _dbService.GetInventory();
                ItemsInInventory.Clear();

                if (ItemsInInventory.Count == 0)
                {
                    ItemsInInventory.Add(new Inventory { InventoryId = 0, BookTitle = "Sample Book", Cost = 9.99, InStock = 10 });


                }

                foreach (var item in inventoryList)

                    ItemsInInventory.Add(item);
                // Show/hide sections so only Inventory results are visible
                InventorySection.IsVisible = true;
                EventSection.IsVisible = false;
                SalesSection.IsVisible = false;
            }
        }

        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not load inventory: {ex.Message}", "OK");
        }
    }

    private async void OpenEventOnClick(object sender, EventArgs e)
    {
    
    

      try
           {
               var eventList = await _dbService.GetEvent();
               Events.Clear();
               foreach (var ev in eventList)
                   Events.Add(ev);
               // Show/hide sections so only Event results are visible
               InventorySection.IsVisible = false;
               EventSection.IsVisible = true;
               SalesSection.IsVisible = false;
           }
           catch (Exception ex)
           {
               await DisplayAlertAsync("Error", $"Could not load events: {ex.Message}", "OK");
           }

    }

    private async void OpenSalesOnClick(object sender, EventArgs e)
    { 
     
        try
        {
            var salesList = await _dbService.GetSales();

            if (salesList.Count == 0)
            {
                salesList.Add(new Sale { SaleId = 0, EventId = 0, SalePrice = 100, Tax = .10, Total = 110 });
                return;
            }
            Sales.Clear();
            foreach (var sale in salesList)
                Sales.Add(sale);
            // Show/hide sections so only Sales results are visible
            InventorySection.IsVisible = false;
            EventSection.IsVisible = false;
            SalesSection.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not load sales: {ex.Message}", "OK");
        }


    }




}
    




