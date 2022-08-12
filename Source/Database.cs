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

        public void Configure(string databaseFilename)
        {
            ServerInfo.Instance.Report($"[Database] File is: {databaseFilename}\n");
            ConnectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = databaseFilename,
                Mode = SqliteOpenMode.ReadWrite
            }.ToString();

            Game.Instance.Load();
        }

        public List<List<object>> ReadTableInternal(string commandText)
        {
            List<List<object>> tableValues = new List<List<object>>();

            SqliteConnection connection = new SqliteConnection(ConnectionString);
            SqliteDataReader reader = null;
            try
            {
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                connection.Open();
                reader = command.ExecuteReader();

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
                ServerInfo.Instance.Report(
                    ColorCodes.Color.Red,
                    "[Database] Exception caught by the database: " + exception.Message + "\n");
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                connection.Close();
            }

            return tableValues;
        }

        public int WriteTableInternal(string commandText)
        {
            int rowsAffected = 0;

            SqliteConnection connection = new SqliteConnection(ConnectionString);
            try
            {
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                connection.Open();
                rowsAffected = command.ExecuteNonQuery();
            }
            catch (SqliteException exception)
            {
                ServerInfo.Instance.Report(
                    ColorCodes.Color.Red,
                    "[Database] Exception caught by the database: " + exception.Message + "\n");
            }
            finally
            {
                connection.Close();
            }

            return rowsAffected;
        }

        public bool DoesValueExistInColumn(string table, string column, object value)
        {
            bool doesValueExistInColumn = false;

            if (value.GetType().Equals(typeof(string)))
            {
                doesValueExistInColumn = ReadTable(table, column, (string)value).Count > 0;
            }
            else if (value.GetType().Equals(typeof(bool)))
            {
                doesValueExistInColumn = ReadTable(table, column, (bool)value ? 1 : 0).Count > 0;
            }
            else
            {
                doesValueExistInColumn = ReadTable(table, column, (long)value).Count > 0;
            }

            return doesValueExistInColumn;
        }

        public List<List<object>> ReadTable(string table)
        {
            string commandText = "SELECT * FROM " + table;
            return ReadTableInternal(commandText);
        }

        public List<List<object>> ReadTable(string table, string column, object value)
        {
            string commandText = "";
            if (value.GetType().Equals(typeof(string)))
            {
                commandText = "SELECT * FROM " + table + " WHERE " + column + " = '" + value + "'";
            }
            else if (value.GetType().Equals(typeof(bool)))
            {
                commandText = "SELECT * FROM " + table + " WHERE " + column + " = " + ((bool)value ? 1 : 0);
            }
            else
            {
                commandText = "SELECT * FROM " + table + " WHERE " + column + " = " + value;
            }
            return ReadTableInternal(commandText);
        }

        // NOTE: The first set of column and value are used to locate where to write in the table.
        public int WriteTable(string table, List<string> columns, List<object> values)
        {
            if (DoesValueExistInColumn(table, columns[0], values[0]))
            {
                return WriteTableWithUpdate(table, columns, values);
            }
            else
            {
                return WriteTableWithInsert(table, columns, values);
            }
        }

        private int WriteTableWithUpdate(string table, List<string> columns, List<object> values)
        {
            string commandText = "UPDATE " + table + " SET ";
            for (int i = 0; i < columns.Count; ++i)
            {
                commandText += columns[i] + " = ";
                if (values[i].GetType().Equals(typeof(string)))
                {
                    commandText += "'" + values[i] + "'";
                }
                else if (values[i].GetType().Equals(typeof(bool)))
                {
                    commandText += (bool)values[i] ? 1 : 0;
                }
                else
                {
                    commandText += values[i];
                }
                if (i != values.Count - 1)
                {
                    commandText += ", ";
                }
            }
            commandText += " WHERE " + columns[0] + " = '" + (string)values[0] + "'";
            return WriteTableInternal(commandText);
        }

        private int WriteTableWithInsert(string table, List<string> columns, List<object> values)
        {
            string commandText = "INSERT INTO " + table + " (";
            for (int i = 0; i < columns.Count; ++i)
            {
                commandText += columns[i];
                if (i != columns.Count - 1)
                {
                    commandText += ", ";
                }
            }
            commandText += ") VALUES (";
            for (int i = 0; i < values.Count; ++i)
            {
                if (values[i].GetType().Equals(typeof(string)))
                {
                    commandText += "'" + values[i] + "'";
                }
                else if (values[i].GetType().Equals(typeof(bool)))
                {
                    commandText += (bool)values[i] ? 1 : 0;
                }
                else
                {
                    commandText += values[i];
                }
                if (i != values.Count - 1)
                {
                    commandText += ", ";
                }
            }
            commandText += ")";
            return WriteTableInternal(commandText);
        }
    }
}
