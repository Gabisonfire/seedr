using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
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
                mapped_path TEXT UNIQUE NOT NULL,
                real_path TEXT UNIQUE NOT NULL,
                hash TEXT,
                source TEXT NOT NULL,
                for_deletion INT
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

        public static void Write(string query, List<KVP> values)
        {
            var command = connection.CreateCommand();
            command.CommandText = query;
            foreach(var v in values)
            {
                command.Parameters.AddWithValue(v.Key, v.Value);
            }
            command.ExecuteNonQuery();
        }

        public static void Write(string query, KVP values)
        {
            Write(query, new List<KVP>(){values});
        }

        public static void MarkForDeletion(string hash)
        {
            string query = @"
            UPDATE media_files SET for_deletion = 1 WHERE hash=$hash
            ";
            Write(query, new KVP("$hash", hash));
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
                    reader.GetString(0), reader.GetString(1), reader.GetString(2)
                ));
            }
            return hashes.ToArray();
        }

        public static HashValue[] GetDuplicateHashes(string source = "both")
        {
            List<HashValue> hashes = new();
            if(source == "both") {source = "%";}
            var query = @"
            SELECT 
                t.mapped_path,
                t.real_path
                t.hash,
                ( SELECT COUNT(hash) 
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
                    reader.GetString(0), reader.GetString(1), reader.GetString(2)
                ));
            }
            return hashes.ToArray();
        }
    }
}