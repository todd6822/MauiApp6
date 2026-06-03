using SQLite;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.ServerSentEvents;
//using Xamarin.KotlinX.Coroutines;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MauiApp6;
//using Android.Provider;
//using Android.Media.Metrics;

namespace DBService
{
    public class LocalDBService
    {

        //private const string DB_name = "LocalDB for sales";
        SQLiteAsyncConnection _connection;


        public LocalDBService()
        {

            _connection = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, Constants.DB_NAME));

            _connection.CreateTableAsync<Sale>();
            _connection.CreateTableAsync<Event>();
            _connection.CreateTableAsync<Inventory>();
            _connection.CreateTableAsync<SaleItem>();

        }



        public async Task<SaleItem> GetSale(int saleId, int inventoryId)


        {


            await _connection.Table<Sale>().ToListAsync();
            await _connection.Table<SaleItem>().Where(y => y.SaleId == saleId).FirstOrDefaultAsync();
            //  await _connection.Table<Inventory>().Where(x=>x.InventoryId==inventoryId).FirstOrDefaultAsync();



            var bookLocation = await _connection.Table<Inventory>().Where(x => x.InventoryId == inventoryId).FirstOrDefaultAsync();
            if (bookLocation != null)



            {
                string? bookName = bookLocation.BookTitle;
                Console.WriteLine(bookName);
            }

            var specificSale = await _connection.Table<Sale>().Where(y => y.SaleId == saleId).FirstOrDefaultAsync();
            if (specificSale != null)



            {
                int thisSale = specificSale.SaleId;
                Console.WriteLine(thisSale);
            }




            return await _connection.Table<SaleItem>().Where(x => x.SaleId == saleId).Where(y => y.BookId == inventoryId).FirstOrDefaultAsync();

            //return await _connection.Table<Inventory>().Where(x => x.InventoryId == Id).FirstOrDefaultAsync();





        }

        public async Task<List<Sale>> GetSales()
        {
            return await _connection.Table<Sale>().ToListAsync();
        }

        public async Task<List<SaleItem>> GetSaleItemsBySaleId(int saleId)
        {
            return await _connection.Table<SaleItem>().Where(x => x.SaleId == saleId).ToListAsync();
        }

        public async Task<List<SaleItem>> GetSaleItemsByBookId(int bookId)
        {
            return await _connection.Table<SaleItem>().Where(x => x.BookId == bookId).ToListAsync();
        }

        //public async Task<Create>

        /*  public async Task<int[]> retunIds(int bookNumber, int eventNumber)

          {

              await 

              int[]myArray = new int[2];
              return myArray ;


          }*/
        public async Task<List<Event>> GetEvent()
        {
            return await _connection.Table<Event>().ToListAsync();

        }


        public async Task<List<Inventory>> GetInventory()
        {
            return await _connection.Table<Inventory>().ToListAsync();

        }

        public async Task<Sale> GetSaleById(int Id)

        {
            return await _connection.Table<Sale>().Where(x => x.SaleId == Id).FirstOrDefaultAsync();

        }

        public async Task<SaleItem> GetItemById(int itemId)

        {


            return await _connection.Table<SaleItem>().Where(x => x.SaleItemId == itemId).FirstOrDefaultAsync();
        }

        /* public async Task<List<Sale>> GetSale(int Id)

         {

            List<Sale> SaleList;
            List<SaleItem> ItemsSold;


             //List <Sale> RerturnList=SaleList + ItemsSold;



             SaleList= await _connection.Table<Sale>().Where(x=>x.SaleId==Id).ToListAsync();
             ItemsSold=await _connection.Table<SaleItem>().Where(y=>y.SaleItemId==Id).ToListAsync();
             SaleList = SaleList.Concat(ItemsSold).ToList;

             return SaleList;




         }*/


        public async Task<Event> GetEventById(int Id)

        {
            return await _connection.Table<Event>().Where(x => x.EventId == Id).FirstOrDefaultAsync();

        }

        public async Task<Inventory> GetInventoryById(int Id)

        {
            return await _connection.Table<Inventory>().Where(x => x.InventoryId == Id).FirstOrDefaultAsync();

        }



        public async Task CreateSale(Sale sale)
        {
            await _connection.InsertAsync(sale);


        }

        public async Task CreateInventoryItem(Inventory item)
        {

            await _connection.InsertAsync(item);
        }

        public async Task CreateEvent(Event salesEvent)

        {

            await _connection.InsertAsync(salesEvent);

        }

        public async Task CreateSaleItem(SaleItem itemSold)
        {
            await _connection.InsertAsync(itemSold);
        }



        public async Task DeleteSale(Sale sale)
        {
            await _connection.DeleteAsync(sale);


        }

        public async Task DeleteInventoryItem(Inventory item)
        {

            await _connection.DeleteAsync(item);
        }

        public async Task DeleteEvent(Event salesEvent)

        {

            await _connection.DeleteAsync(salesEvent);

        }

        public async Task DeleteSaleItem(SaleItem itemSold)
        {
            await _connection.DeleteAsync(itemSold);
        }


        public async Task UpdateSale(Sale sale)
        {
            await _connection.UpdateAsync(sale);


        }

        public async Task UpdateInventoryItem(Inventory item)
        {

            await _connection.UpdateAsync(item);
        }

        public async Task UpdateEvent(Event salesEvent)

        {

            await _connection.UpdateAsync(salesEvent);

        }

        public async Task UpdaSaleItem(SaleItem itemSold)
        {
            await _connection.UpdateAsync(itemSold);
        }


        /* public async Task CreateSaleItem(Sale sale, int saleID, int numberOfitemsSold )
               e



            // await _connection.Table<Sale>.Where(x=>x.SaleId==saleID)

             //await _connection.InsertAsync(sale);


             when calling this call use an int do show hoe may books were sold, a for loop to cycle for each item sold and to creat an entry for the saleItem table



             int numberOfItems=0;


             numberOf Items=
              for (int x;x< saleItemNumber; x++)
             {
                call the create sale item here for each item

             }


         }*/

        public async Task SeedAsync()
        {
            var testData = await _connection.Table<Sale>().CountAsync();
            var testDataInventory = await _connection.Table<Inventory>().CountAsync();
            var testDataEvents = await _connection.Table<Event>().CountAsync();
            var testDataSaleItems = await _connection.Table<SaleItem>().CountAsync();

            if (testData == 0)
            {
                await _connection.InsertAsync(new Sale { SaleId = 1, Total = 19.98 });
                await _connection.InsertAsync(new Inventory { InventoryId = 1, BookTitle = "Book A", Cost = 9.99, InStock = 10 });
                await _connection.InsertAsync(new Inventory { InventoryId = 2, BookTitle = "Book B", Cost = 9.99, InStock = 5 });
                await _connection.InsertAsync(new Event { EventId = 1, EventName = "Book Signing", EventDate = DateTime.Now.AddDays(7) });
                await _connection.InsertAsync(new SaleItem { SaleItemId = 1, SaleId = 1, BookId = 1, Quantity = 2 });
            }







        }




    }
}



