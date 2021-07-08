using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace Aurora
{
	public class Database
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

		public Dictionary<string, object> ReadTable(string table)
		{
			Dictionary<string, object> tableValues = new Dictionary<string, object>();

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
					while (reader.Read())
					{
						tableValues[reader.GetString(0)] = reader.GetValue(1);
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
