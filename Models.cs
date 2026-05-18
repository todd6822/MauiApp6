
//using AndroidX.ConstraintLayout.Core.Motion.Utils;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DBService
{

    public class Sale


    {
        [PrimaryKey, AutoIncrement]
        [SQLite.Column("SaleId")]
        public int SaleId { get; set; }

        [ForeignKey(nameof(EventId)), SQLite.Column("EventId")]
        public int EventId { get; set; }

        /*[ForeignKey(nameof(BookId)),SQLite.Column("BookId")]
        public int BookId { get; set; }*/

        [SQLite.Column("SalePrice")]
        public double SalePrice { get; set; }

        [SQLite.Column("Tax")]
        public double Tax { get; set; }

        [SQLite.Column("Total")]

        public double Total
        {
            get; set;
        }

    }

    public class Event
    {
        [PrimaryKey, AutoIncrement, SQLite.Column("EventID")]
        public int EventId { get; set; }


        [SQLite.Column("EventName")]
        public string EventName { get; set; }

        [SQLite.Column("EventDate")]
        public DateTime EventDate { get; set; }

        [SQLite.Column("Event Tax")]
        public double EventTax { get; set; }





    }

    public class Inventory
    {
        [PrimaryKey, AutoIncrement, SQLite.Column("InventoryId")]
        public int InventoryId { get; set; }

        [SQLite.Column("BookTitle")]
        public string BookTitle { get; set; }

        [SQLite.Column("Cost")]
        public double Cost { get; set; }

        [SQLite.Column("InStock")]
        public int InStock { get; set; }



    }

    public class SaleItem

    {
        [PrimaryKey, AutoIncrement, SQLite.Column("SaleItemId")]
        public int SaleItemId { get; set; }

        [ForeignKey(nameof(SaleId)), SQLite.Column("SaleId")]
        public int SaleId { get; set; }

        [ForeignKey(nameof(BookId)), SQLite.Column("BookId")]

        public int BookId { get; set; }

        [SQLite.Column("Quantity")]
        public int Quantity { get; set; }

        [SQLite.Column("SalePrice")]

        public float SalePrice { get; set; }





    }





}

