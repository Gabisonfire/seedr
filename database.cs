using System.ComponentModel;
using System.IO;
using System.Reflection.Metadata;
using Microsoft.Data.Sqlite;

namespace Seedr
{
    public static class Database {

        public const string DATABASE = "seedr.db";

        public static SqliteConnection connection = new SqliteConnection($"Data Source={DATABASE}");

        public static void InitDB()
        {
            if(!File.Exists(DATABASE))
            {
                Core.logger.Info($"{DATABASE} does not exist, creating.");
                connection.Open();
                CreateMainTables();
            }
            else 
            {
                Core.logger.Info($"Opening connection to database: {DATABASE}");
                connection.Open();
            }
        }

        static void CreateMainTables()
        {
            Core.logger.Info("Creating required schema");
            string query = @"
            CREATE TABLE download_files 
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                path TEXT UNIQUE NOT NULL,
                hash TEXT UNIQUE
            );
            CREATE TABLE library_files 
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                path TEXT UNIQUE NOT NULL,
                hash TEXT UNIQUE
            );
            ";
            var command = connection.CreateCommand();
            command.CommandText = query;
            command.ExecuteNonQuery();
        }

        public static void Write(string query)
        {
            var command = connection.CreateCommand();
            command.CommandText = query;
            command.ExecuteNonQuery();
        }
    }
}