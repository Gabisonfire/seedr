using System.ComponentModel;
using System.IO;
using System.Reflection.Metadata;
using Microsoft.Data.Sqlite;
using NLog;

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
            CREATE TABLE media_files 
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                path TEXT UNIQUE NOT NULL,
                hash TEXT,
                source TEXT NOT NULL
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

        public static void WriteMany(string[] queries)
        {
            var transaction = connection.BeginTransaction();
            var command = connection.CreateCommand();
            foreach(var query in queries)
            {
                command.CommandText = query;
                command.ExecuteNonQuery();
            }
            try {
                transaction.Commit();
            }
            catch(Exception e)
            {
                Core.logger.Error($"Error writing to database. {e.Message}");
                transaction.Rollback();
            }
        }
    }
}