using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace Aurora
{
	class Server
	{
		private int TotalClients = 0;
		// we have to remember all connections so we can close them properly if the server is
		// shutdown while clients are connected
		private List<Connection> Connections;

		private static Server _instance = null;
		public static Server Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new Server();
				}
				return _instance;
			}
		}

		private Server()
		{
			Connections = new List<Connection>();
		}

		public void Listen()
		{
			TcpListener tcpListener = null;
			try
			{
				// start listening on port 6006
				ServerInfo.Instance.Report("Starting the server...\n");
				tcpListener = new TcpListener(System.Net.IPAddress.Any, 6006);
				tcpListener.Start();

				// nothing can stop me now (but you could if you tried hard enough)
				while (true)
				{
					// don't block waiting for connections or we'll never catch our exceptions
					if (tcpListener.Pending())
					{
						TcpClient tcpClient = tcpListener.AcceptTcpClient();
						Connection connection = new Connection(tcpClient, TotalClients++);
						Connections.Add(connection);
						ServerInfo.Instance.RaiseEvent(new ServerInfoConnectionsArgs(Connections.Count));
					}

					// if any client has quit, close its thread
					// NOTE: Build a copy of Connections so we can safely remove from the original. -Ward
					List<Connection> originalConnections = new List<Connection>(Connections);
					foreach (Connection currentConnection in originalConnections)
					{
						if (currentConnection.ClientQuit)
						{
							currentConnection.ClientThread.Abort();
							Connections.Remove(currentConnection);
							ServerInfo.Instance.Report("Pruned client (" + currentConnection.ClientID + ").\n");
							ServerInfo.Instance.RaiseEvent(new ServerInfoConnectionsArgs(Connections.Count));
						}
					}

					// this amount of sleep gives us fairly OK CPU usage
					Thread.Sleep(7);
				}
			}
			catch (System.Threading.ThreadAbortException)
			{
				// this is OK
			}
			catch (System.Exception exception)
			{
				ServerInfo.Instance.Report("Exception caught by the server, \"" + exception.Message + "\"!\n");
			}
			finally
			{
				Shutdown();

				if (tcpListener != null)
				{
					tcpListener.Stop();
				}
			}
		}

		public void Remove(Connection connection)
		{
			Connections.Remove(connection);
		}

		private void Shutdown()
		{
			ServerInfo.Instance.Report("Stopping the server...\n");

			// close and remove all open connections
			foreach (Connection currentConnection in Connections)
			{
				// closing a connection also removes its thread
				currentConnection.ClientThread.Abort();
			}
			Connections.Clear();
			ServerInfo.Instance.RaiseEvent(new ServerInfoConnectionsArgs(Connections.Count));
		}
	}
}
