using Microsoft.Win32;
using System.Windows;

namespace Aurora
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // this callback lets us interface with Form components from threaded events
        private ServerInfoHandler EventHandler = null;
        // TODO: New threading model? -Ward
        //private Thread serverThread = null;

        public MainWindow()
        {
            InitializeComponent();

            EventHandler = new ServerInfoHandler(ServerInfoEventHandler);
            ServerInfo.Instance.EventReceived += ServerInfoEventHandler;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if ((string)Properties.Settings.Default["DatabaseFilename"] != "")
            {
                ConnectToDatabase();
            }
            else
            {
                ServerInfo.Instance.Report("Please choose a database!\n");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ServerInfo.Instance.EventReceived -= EventHandler;

            StopServer();
            DisconnectFromDatabase();
        }

        private void ChooseDatabase_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Database files (*.db)|*.db|All files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;

            if ((bool)openFileDialog.ShowDialog())
            {
                Properties.Settings.Default["DatabaseFilename"] = openFileDialog.FileName;
                Properties.Settings.Default.Save();
                DisconnectFromDatabase();
                ConnectToDatabase();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void StartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StartServer();
        }

        private void StopMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StopServer();
        }

        private void ClearMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ConsoleTextBox.Clear();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutBox aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }

        private void HandleEvent(ServerInfoConnectionsArgs args)
        {
            ServerStatusBarItem.Content = args.Connections + " active connections";
        }

        private void HandleEvent(ServerInfoDatabaseArgs args)
        {
            if (args.Connected)
            {
                StartMenuItem.IsEnabled = true;
                StopMenuItem.IsEnabled = false;
            }
            else
            {
                StopServer();
                StartMenuItem.IsEnabled = false;
                StopMenuItem.IsEnabled = false;
            }
        }

        private void HandleEvent(ServerInfoReportArgs args)
        {
            ConsoleTextBox.Text += args.Report;
        }

        private void ServerInfoEventHandler(object sender, ServerInfoEventArgs args)
        {
            if (!CheckAccess())
            {
                // we came from a different thread, invoke our thread-safe callback
                Dispatcher.Invoke(EventHandler, new object[] { sender, args });
            }
            else
            {
                // TODO: Find a scalable way to do this. Automatically? -Ward
                if (typeof(ServerInfoConnectionsArgs).IsInstanceOfType(args))
                {
                    HandleEvent((ServerInfoConnectionsArgs)args);
                }
                else if (typeof(ServerInfoDatabaseArgs).IsInstanceOfType(args))
                {
                    HandleEvent((ServerInfoDatabaseArgs)args);
                }
                else if (typeof(ServerInfoReportArgs).IsInstanceOfType(args))
                {
                    HandleEvent((ServerInfoReportArgs)args);
                }
            }
        }

        private void ConnectToDatabase()
        {
            Database.Instance.Configure();
            Database.Instance.ConnectAsync();
        }

        private void DisconnectFromDatabase()
        {
            Database.Instance.DisconnectAsync();
        }

        private void StartServer()
        {
            // TODO: New threading model? -Ward
            /*
            if (serverThread == null)
            {
                serverThread = new Thread(new ThreadStart(Server.Instance.Listen));
                serverThread.Start();
                // on a single-core machine, give our new thread some time
                Thread.Sleep(0);

                // notify all UI
                ServerStatusBarItem.Content = "Started";
                StartMenuItem.IsEnabled = false;
                StopMenuItem.IsEnabled = true;
            }
            */
        }

        private void StopServer()
        {
            // TODO: New threading model? -Ward
            /*
            if (serverThread != null)
            {
                // the listener thread never ends itself (at the moment), so we must abort it manually
                if (serverThread.ThreadState != ThreadState.Aborted)
                {
                    serverThread.Abort();
                }
                serverThread = null;

                // notify all UI
                ServerStatusBarItem.Content = "Stopped";
                StartMenuItem.IsEnabled = true;
                StopMenuItem.IsEnabled = false;
            }
            */
        }
    }
}
