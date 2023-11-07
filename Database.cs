using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using NLog;
using Seedr.Utils;

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

        public class KVP
        {
            public string Key = string.Empty;
            public string Value = string.Empty;
            public KVP(string k, string v)
            {
                Key = k;
                Value = v;
            }
        }

        static void CreateMainTables()
        {
            Core.logger.Info("Creating required schema");
            string query = @"
            CREATE TABLE media_files 
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                mapped_path TEXT UNIQUE,
                real_path TEXT UNIQUE NOT NULL,
                hash TEXT,
                source TEXT NOT NULL,
                for_deletion INT
            );
            ";
            var command = connection.CreateCommand();
            command.CommandText = query;
            try {
                command.ExecuteNonQuery();
            }
            catch(Exception e)
            {
                Core.logger.Error($"Error creating initial schema: {e.Message}");
                Environment.Exit(1);
            }
        }

        // For parameter less queries
        public static void Write(string query)
        {
            var command = connection.CreateCommand();
            command.CommandText = query;
            try
            {
                command.ExecuteNonQuery();
            }
            catch(Exception e)
            {
                Core.logger.Error($"Error executing query({query}): {e.Message}");
            }
        }

        // For queries with parameters
        public static void Write(string query, List<KVP> values)
        {
            var command = connection.CreateCommand();
            command.CommandText = query;
            foreach(var v in values)
            {
                command.Parameters.AddWithValue(v.Key, v.Value);
            }
            try
            {
                command.ExecuteNonQuery();
            }
            catch(Exception e)
            {
                Core.logger.Error($"Error executing query({query}): {e.Message}");
            }
        }

        // public static void Write(string query, KVP values)
        // {
        //     Write(query, new List<KVP>(){values});
        // }

        // public static void MarkForDeletion(string hash, string mappedPath)
        // {
        //     string query = @"
        //     UPDATE media_files SET for_deletion = 1 WHERE hash=$hash AND mapped_path=$mapped_path
        //     ";
        //     Write(query, new List<KVP>(){new("$hash", hash), new("$mapped_path", mappedPath)});
        // }

        // public static void DeleteAllMarked()
        // {
        //     string query = @"
        //     DELETE FROM media_files WHERE for_deletion = 1
        //     ";
        //     Write(query);
        // }

        // Delete based on hash+mappedPath
        public static void Delete(string hash, string mappedPath)
        {
            string query = @"
            DELETE FROM media_files WHERE hash=$hash AND mapped_path=$mapped_path
            ";
            Write(query, new List<KVP>(){new("$hash", hash), new("$mapped_path", mappedPath)});
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

        public static void WriteCommands(SqliteCommand[] commands)
        {
            var transaction = connection.BeginTransaction();   
            foreach(var command in commands)
            {
                command.Transaction = transaction;
                command.Connection = connection;
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

        public static HashValue[] ReadAllHashesFromDB(string source)
        {
            List<HashValue> hashes = new();
            var query = @"
            SELECT mapped_path, real_path, hash FROM media_files WHERE source=$source
            ";
            var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("$source", source);
            var reader = command.ExecuteReader();
            while(reader.Read())
            {
                hashes.Add(new HashValue(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)
                ));
            }
            return hashes.ToArray();
        }

        public static HashValue[] GetDuplicateHashes(string source = FileSource.All)
        {
            List<HashValue> hashes = new();
            if(source == FileSource.All) {source = "%";}
            var query = @"
            SELECT 
                t.mapped_path,
                t.real_path,
                t.hash,
                t.source,
                (SELECT COUNT(hash) 
                FROM media_files ct 
                WHERE ct.hash = t.hash
                ) as counter
            FROM
            media_files t
            WHERE counter > 1 AND source LIKE $source
            ";
            var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("$source", source);
            var reader = command.ExecuteReader();
            while(reader.Read())
            {
                hashes.Add(new HashValue(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)
                ));
            }
            return hashes.ToArray();
        }

        public static List<IHashable> GetUnhashedFiles()
        {
            List<IHashable> allFiles = new();
            var query = @"
            SELECT real_path,source FROM media_files WHERE hash IS NULL;
            ";
            var command = new SqliteCommand(query, connection);
            var reader = command.ExecuteReader();
            while(reader.Read())
            {
                string path = reader.GetString(0);
                string source = reader.GetString(1);
                if(source == FileSource.Library){
                    allFiles.Add(new LibraryFile(path));
                }
                if(source == FileSource.Torrent){
                    allFiles.Add(new Torrent("", path));
                }
            }
            return allFiles;
        }
    }
}