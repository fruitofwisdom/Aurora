using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace Aurora
{
    internal class Database
    {
        private string ConnectionString;

        private static Database _instance = null;
        public static Database Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Database();
                }
                return _instance;
            }
        }

        public void Configure()
        {
            if ((string)Properties.Settings.Default["DatabaseFilename"] != "")
            {
                ServerInfo.Instance.Report("[Database] File is: " + Properties.Settings.Default["DatabaseFilename"] + "\n");
                ConnectionString = new SqliteConnectionStringBuilder()
                {
                    DataSource = (string)Properties.Settings.Default["DatabaseFilename"],
                    Mode = SqliteOpenMode.ReadWrite
                }.ToString();

                Game.Instance.Load();
            }
            else
            {
                ServerInfo.Instance.Report("[Database] No file specified!\n");
            }
        }

        public List<List<object>> ReadTable(string table)
        {
            List<List<object>> tableValues = new List<List<object>>();

            // TODO: Is this the right paradigm? -Ward
            // https://stackoverflow.com/questions/9705637/executereader-requires-an-open-and-available-connection-the-connections-curren
            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM " + table;
                connection.Open();
                SqliteDataReader reader = command.ExecuteReader();
                try
                {
                    int numRows = 0;
                    while (reader.Read())
                    {
                        object[] values = new object[reader.FieldCount];
                        _ = reader.GetValues(values);
                        tableValues.Add(new List<object>(values));
                        numRows++;
                    }
                }
                catch (SqliteException exception)
                {
                    ServerInfo.Instance.Report("[Database] Exception caught by the database, \"" + exception.Message + "\"!\n");
                }
                finally
                {
                    reader.Close();
                }
            }

            return tableValues;
        }

        public List<List<object>> ReadTable(string table, string id_name, long id)
        {
            List<List<object>> tableValues = new List<List<object>>();

            // TODO: Is this the right paradigm? -Ward
            // https://stackoverflow.com/questions/9705637/executereader-requires-an-open-and-available-connection-the-connections-curren
            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM " + table + " WHERE " + id_name + " = " + id;
                connection.Open();
                SqliteDataReader reader = command.ExecuteReader();
                try
                {
                    int numRows = 0;
                    while (reader.Read())
                    {
                        object[] values = new object[reader.FieldCount];
                        _ = reader.GetValues(values);
                        tableValues.Add(new List<object>(values));
                        numRows++;
                    }
                }
                catch (SqliteException exception)
                {
                    ServerInfo.Instance.Report("[Database] Exception caught by the database, \"" + exception.Message + "\"!\n");
                }
                finally
                {
                    reader.Close();
                }
            }

            return tableValues;
        }
    }
}
