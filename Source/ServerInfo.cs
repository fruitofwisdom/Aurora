namespace Aurora
{
	public abstract class ServerInfoEventArgs : System.EventArgs {}

	public class ServerInfoConnectionsArgs : ServerInfoEventArgs
	{
		public readonly int Connections;

		public ServerInfoConnectionsArgs(int connections)
		{
			Connections = connections;
		}
	}

	public class ServerInfoGameArgs : ServerInfoEventArgs
	{
		public readonly bool Loaded;

		public ServerInfoGameArgs(bool loaded)
		{
			Loaded = loaded;
		}
	}

	public class ServerInfoReportArgs : ServerInfoEventArgs
	{
		public readonly string Report;

		public ServerInfoReportArgs(string report)
		{
			Report = report;
		}
	}

	public class ServerInfoServerArgs : ServerInfoEventArgs
    {
		public readonly bool Running;

		public ServerInfoServerArgs(bool running)
        {
			Running = running;
        }
    }

	public delegate void ServerInfoHandler(object sender, ServerInfoEventArgs args);

	class ServerInfo
	{
		public event ServerInfoHandler EventReceived;

		private static ServerInfo _instance = null;
		public static ServerInfo Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new ServerInfo();
				}
				return _instance;
			}
		}

		private ServerInfo()
		{
			;
		}

		public void RaiseEvent(ServerInfoEventArgs eventArgs)
		{
			// don't raise the event if no one is subscribed
			if (EventReceived != null)
			{
				EventReceived(this, eventArgs);
			}
		}

		public void Report(string report)
		{
			RaiseEvent(new ServerInfoReportArgs(report));
		}
	}
}
