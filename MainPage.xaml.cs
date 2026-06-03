using MauiApp6;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using SQLite;
using System.Threading.Tasks;



namespace MauiApp6
{

    
    public partial class MainPage : ContentPage
    {
        DBService.LocalDBService _dbService;

        
        


        public MainPage(DBService.LocalDBService dbService)
        {


            InitializeComponent();

            _dbService = dbService;
            
        }

        public async void OnNextPageClicked(object? sender, EventArgs e)
        {
           await Navigation.PushAsync(new DatabaseUpdatePage(_dbService));
        }

        public async void OnSalePageClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new SalePage(_dbService));
        }

        /* private void OnCounterClicked(object? sender, EventArgs e)
         {
             count++;

             if (count == 1)
                 CounterBtn.Text = $"Clicked {count} time";
             else
                 CounterBtn.Text = $"Clicked {count} times";

             SemanticScreenReader.Announce(CounterBtn.Text);
         }*/
    }
}
