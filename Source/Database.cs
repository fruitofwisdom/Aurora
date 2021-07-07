using Microsoft.Data.Sqlite;

namespace Aurora
{
	public class Database
	{
		public bool Configured { get; private set; }
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

		private Database()
		{
			Configured = false;
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
				GetGameInfo();
			}
			else
            {
				ServerInfo.Instance.Report("[Database] No file specified!\n");
            }
		}

		private void GetGameInfo()
		{
			// TODO: Is this the right paradigm? -Ward
			// https://stackoverflow.com/questions/9705637/executereader-requires-an-open-and-available-connection-the-connections-curren
			using (SqliteConnection connection = new SqliteConnection(ConnectionString))
			{
				SqliteCommand command = connection.CreateCommand();
				command.CommandText = "SELECT * FROM Info";
				connection.Open();
				SqliteDataReader reader = command.ExecuteReader();
				try
				{
					// TODO: Save this to a proper Game instance. -Ward
					while (reader.Read())
					{
						ServerInfo.Instance.Report("[Database] " + reader.GetString(0) + ": " + reader.GetString(1) + "\n");
					}

					Configured = true;
					ServerInfo.Instance.RaiseEvent(new ServerInfoDatabaseArgs(Configured));
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
		}
	}
}
