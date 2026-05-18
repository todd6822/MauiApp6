using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace MauiApp6
{
    internal class Constants
    {

       public const string DB_NAME = "LocalDB for sales";

       public const SQLiteOpenFlags flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;
        public static string DB_PATH => Path.Combine(FileSystem.AppDataDirectory, DB_NAME);


    }
}
