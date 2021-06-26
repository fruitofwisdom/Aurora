using System;
using System.Threading;

namespace MUDcat6006
{
	public partial class MainForm : System.Windows.Forms.Form
	{
		// this callback lets us interface with Form components from threaded events
		private ServerInfoHandler EventHandler = null;
		private Thread databaseThread = null;
		private Thread serverThread = null;

		public MainForm()
		{
			InitializeComponent();

			EventHandler = new ServerInfoHandler(ServerInfoEventHandler);
			ServerInfo.Instance.EventReceived += ServerInfoEventHandler;

			ConnectToDatabase();
			StartServer();
		}

		private void ConnectToDatabase()
		{
			if (databaseThread == null)
			{
				Database.Instance.Configure();

				databaseThread = new Thread(new ThreadStart(Database.Instance.Connect));
				databaseThread.Start();
				// on a single-core machine, give our new thread some time
				Thread.Sleep(0);
			}
		}

		private void DisconnectFromDatabase()
		{
			if (databaseThread != null)
			{
				if (databaseThread.ThreadState != ThreadState.Aborted)
				{
					databaseThread.Abort();
				}
				databaseThread = null;
			}
		}

		private void HandleEvent(ServerInfoConnectionsArgs args)
		{
			connectionsStatusLabel.Text = args.Connections + " active connections";
		}

		private void HandleEvent(ServerInfoReportArgs args)
		{
			reportTextBox.Text += args.Report;
		}

		protected override void OnFormClosing(System.Windows.Forms.FormClosingEventArgs e)
		{
			ServerInfo.Instance.EventReceived -= EventHandler;

			StopServer();
			DisconnectFromDatabase();

			base.OnFormClosing(e);
		}

		private void ServerInfoEventHandler(object sender, ServerInfoEventArgs args)
		{
			if (reportTextBox.InvokeRequired)
			{
				// we came from a different thread, invoke our thread-safe callback
				Invoke(EventHandler, new object[] { sender, args });
			}
			else
			{
				// TODO: Find a scalable way to do this. Automatically? -Ward
				if (typeof(ServerInfoConnectionsArgs).IsInstanceOfType(args))
				{
					HandleEvent((ServerInfoConnectionsArgs)args);
				}
				else if (typeof(ServerInfoReportArgs).IsInstanceOfType(args))
				{
					HandleEvent((ServerInfoReportArgs)args);
				}
			}
		}

		private void StartServer()
		{
			if (serverThread == null)
			{
				serverThread = new Thread(new ThreadStart(Server.Instance.Listen));
				serverThread.Start();
				// on a single-core machine, give our new thread some time
				Thread.Sleep(0);

				// notify all UI
				serverStatusLabel.Text = "Started";
				startToolStripMenuItem.Enabled = false;
				stopToolStripMenuItem.Enabled = true;
			}
		}

		private void StopServer()
		{
			if (serverThread != null)
			{
				// the listener thread never ends itself (at the moment), so we must abort it manually
				if (serverThread.ThreadState != ThreadState.Aborted)
				{
					serverThread.Abort();
				}
				serverThread = null;

				// notify all UI
				serverStatusLabel.Text = "Stopped";
				startToolStripMenuItem.Enabled = true;
				stopToolStripMenuItem.Enabled = false;
			}
		}

		private void ExitMenuItemClick(object sender, EventArgs e)
		{
			Close();
		}

		private void StartMenuItemClick(object sender, EventArgs e)
		{
			StartServer();
		}

		private void StopMenuItemClick(object sender, EventArgs e)
		{
			StopServer();
		}

		private void ClearMenuItemClick(object sender, EventArgs e)
		{
			reportTextBox.Clear();
		}
	}
}
