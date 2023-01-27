using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Aurora
{
    internal class Server
    {
        private bool Running;
        private TcpListener TcpListener;

        private int TotalClients;
        // we have to remember all connections so we can close them properly if the server is
        // shutdown while clients are connected
        private readonly List<Connection> Connections;

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
            Running = false;
            TcpListener = null;
            TotalClients = 0;
            Connections = new List<Connection>();

            Thread.CurrentThread.Name = "Aurora Server Thread";
        }

        private void Listen()
        {
            try
            {
				Thread.CurrentThread.Name = "Aurora Server Thread";

				// start listening on port 6006
				ServerInfo.Instance.Report("[Server] Starting the server...\n");
                TcpListener = new TcpListener(System.Net.IPAddress.Any, 6006);
                TcpListener.Start();

                Running = true;
                ServerInfo.Instance.RaiseEvent(new ServerInfoServerArgs(Running));

                while (Running)
                {
                    // don't block waiting for connections or we'll never catch our exceptions
                    if (TcpListener.Pending())
                    {
                        TcpClient tcpClient = TcpListener.AcceptTcpClient();
                        Connection connection = new Connection(tcpClient, TotalClients++);
                        Connections.Add(connection);
                        ServerInfo.Instance.RaiseEvent(new ServerInfoConnectionsArgs(Connections.Count));
                    }

                    // if any client has quit, close its thread
                    // NOTE: Build a copy of Connections so we can safely remove from the original. -Ward
                    List<Connection> originalConnections = new List<Connection>(Connections);
                    foreach (Connection currentConnection in originalConnections)
                    {
                        if (currentConnection.ClientDisconnected)
                        {
                            Connections.Remove(currentConnection);
                            ServerInfo.Instance.Report("[Server] Pruned client (" + currentConnection.ClientID + ").\n");
                            ServerInfo.Instance.RaiseEvent(new ServerInfoConnectionsArgs(Connections.Count));
                        }
                    }

                    // this amount of sleep gives us fairly OK CPU usage
                    Thread.Sleep(30);
                }
            }
            catch (ThreadAbortException)
            {
                // this is OK
            }
            catch (System.Exception exception)
            {
                ServerInfo.Instance.Report(
                    ColorCodes.Color.Red,
                    "[Server] Exception caught by the server: " + exception.Message + "\n");
            }
            finally
            {
                Game.Instance.Save();
				Shutdown();
            }
        }

        public Task ListenAsync()
        {
            return Task.Run(() => { Listen(); });
        }

        public void Remove(Connection connection)
        {
            Connections.Remove(connection);
        }

        private void Shutdown()
        {
            ServerInfo.Instance.Report("[Server] Stopping the server...\n");

            // close and remove all open connections
            foreach (Connection currentConnection in Connections)
            {
                // TODO: Handle notifying clients about shutdown.
                currentConnection.Disconnect(true);
            }
            Connections.Clear();
            ServerInfo.Instance.RaiseEvent(new ServerInfoConnectionsArgs(Connections.Count));

            if (TcpListener != null)
            {
                TcpListener.Stop();
            }

            ServerInfo.Instance.RaiseEvent(new ServerInfoServerArgs(Running));
        }

        public void ShutdownAsync()
        {
            Running = false;
        }
    }
}
