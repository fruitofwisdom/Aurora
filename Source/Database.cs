using Microsoft.Data.Sqlite;

namespace MUDcat6006
{
	public class Database
	{
		public bool Configured { get; private set; }
		public bool Connected { get; private set; }
		public int Port = 3306;		// default MySQL port
		private SqliteConnection Connection;
		private string ConnectionString = string.Empty;
		public string DatabaseName = "mudcat6006";
		public string ServerName = "localhost";

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
			Connected = false;
			Connection = new SqliteConnection();
		}

		public void Configure()
		{
			// if we're already connected, disconnect before reconfiguring
			if (Configured && Connected)
			{
				Disconnect();
			}

			Connection.ConnectionString = new SqliteConnectionStringBuilder()
			{
				DataSource = "NorthernLights.db",
				Mode = SqliteOpenMode.ReadWrite
			}.ToString();
			Configured = true;
		}

		public void Connect()
		{
			if (Configured && !Connected)
			{
				try
				{
					// go ahead and connect with the prepared connection string
					ServerInfo.Instance.Report("Connecting to the database...\n");
					Connection.Open();
					Connected = true;

					// hang out until the thread is aborted
					while (true)
					{
						;
					}
				}
				catch (System.Threading.ThreadAbortException)
				{
					// this is OK
				}
				catch (SqliteException exception)
				{
					ServerInfo.Instance.Report("Exception caught by the database, \"" + exception.Message + "\"!\n");
				}
				finally
				{
					Disconnect();
				}
			}
		}

		private void Disconnect()
		{
			if (Connected)
			{
				ServerInfo.Instance.Report("Disconnecting from the database...\n");

				Connection.Close();
				Connected = false;
			}
		}
		// TODO: An actual interface for working with the database. -Ward
		/*
		public ArrayList FindTasks(string userName)
		{
			ArrayList tasks = new ArrayList();

			if (Connected)
			{
				SqlCommand command = new SqlCommand(
					"SELECT * FROM dbo.Tasks WHERE Resource LIKE '" + userName + "'", mSQLConnection
					);
				SqlDataReader reader = command.ExecuteReader();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						//Task task = new Task(reader.GetInt32(2), reader.GetString(0), reader.GetString(1));
						//tasks.Add(task);
					}
				}
				reader.Close();
			}

			tasks.Sort();

			return tasks;
		}
		*/
	}
}
