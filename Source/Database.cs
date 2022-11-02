using LiteDB;
using System.Collections.Generic;

namespace Aurora
{
    internal class Database
    {
        private LiteDatabase _database;

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
            _database = new LiteDatabase(databaseFilename);

            Game.Instance.Load();
        }

        public ILiteCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }

        // Actually execute the command on the database and return the results.
        public List<Dictionary<string, object>> ReadTableInternal(string command)
        {
            List<Dictionary<string, object>> table = new();

            try
            {
				IBsonDataReader reader = _database.Execute(command);
                while (reader.Read())
                {
                    BsonDocument bsonDocument = reader.Current as BsonDocument;
                    foreach (BsonArray values in bsonDocument.Values)
                    {
                        // For each row found in the database, map its keys and values into a
                        // dictionary that we will return.
                        foreach (BsonDocument row in values)
                        {
                            Dictionary<string, object> dict = new();
                            foreach (string key in row.Keys)
                            {
                                row.TryGetValue(key, out BsonValue value);
                                dict[key] = value.RawValue;
                            }
                            table.Add(dict);
                        }
                    }
                }
            }
            catch (LiteException exception)
            {
				ServerInfo.Instance.Report(
					ColorCodes.Color.Red,
					"[Database] Exception caught by the database: " + exception.Message + "\n");
			}

			return table;
        }

        // Actually execute the command on the database and return how many rows were affected.
        public int WriteTableInternal(string command)
        {
            int rowsAffected = 0;

            try
            {
				IBsonDataReader reader = _database.Execute(command);
                rowsAffected = (int)reader.Current;
			}
			catch (LiteException exception)
            {
				ServerInfo.Instance.Report(
					ColorCodes.Color.Red,
					"[Database] Exception caught by the database: " + exception.Message + "\n");
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
                doesValueExistInColumn = ReadTable(table, column, (int)value).Count > 0;
            }

            return doesValueExistInColumn;
        }

        public List<Dictionary<string, object>> ReadTable(string table)
        {
            string commandText = "SELECT * FROM " + table;
            return ReadTableInternal(commandText);
        }

        public List<Dictionary<string, object>> ReadTable(string table, string column, object value)
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
            string commandText = "UPDATE " + table + " SET {";
            for (int i = 0; i < columns.Count; ++i)
            {
                commandText += "'" + columns[i] + "': ";
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
            commandText += "} WHERE " + columns[0] + " = '" + (string)values[0] + "'";
            return WriteTableInternal(commandText);
        }

        private int WriteTableWithInsert(string table, List<string> columns, List<object> values)
        {
            string commandText = "INSERT INTO " + table + " VALUES {";
            for (int i = 0; i < values.Count; ++i)
            {
				commandText += "'" + columns[i] + "': ";
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
            commandText += "}";
            return WriteTableInternal(commandText);
        }
    }
}
