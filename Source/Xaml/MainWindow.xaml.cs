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
        private readonly ServerInfoHandler EventHandler = null;

        public MainWindow()
        {
            InitializeComponent();

            EventHandler = new ServerInfoHandler(ServerInfoEventHandler);
            ServerInfo.Instance.EventReceived += ServerInfoEventHandler;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string databaseFilename = (string)Properties.Settings.Default["DatabaseFilename"];
            if (databaseFilename != "")
            {
                Database.Instance.Configure(databaseFilename);
            }
            else
            {
                ServerInfo.Instance.Report("[Aurora] Please choose a database file!\n");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ServerInfo.Instance.EventReceived -= EventHandler;

            StopServer();
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
                Database.Instance.Configure((string)Properties.Settings.Default["DatabaseFilename"]);
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

        private void HandleEvent(ServerInfoGameArgs args)
        {
            if (args.Loaded)
            {
                this.Title = "Aurora - " + Game.Instance.Name;
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

        private void HandleEvent(ServerInfoServerArgs args)
        {
            if (args.Running)
            {
                ServerStatusBarItem.Content = "Started";
                StartMenuItem.IsEnabled = false;
                StopMenuItem.IsEnabled = true;
            }
            else
            {
                ServerStatusBarItem.Content = "Stopped";
                StartMenuItem.IsEnabled = true;
                StopMenuItem.IsEnabled = false;
            }
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
                else if (typeof(ServerInfoGameArgs).IsInstanceOfType(args))
                {
                    HandleEvent((ServerInfoGameArgs)args);
                }
                else if (typeof(ServerInfoReportArgs).IsInstanceOfType(args))
                {
                    HandleEvent((ServerInfoReportArgs)args);
                }
                else if (typeof(ServerInfoServerArgs).IsInstanceOfType(args))
                {
                    HandleEvent((ServerInfoServerArgs)args);
                }
            }
        }

        private void StartServer()
        {
            Server.Instance.ListenAsync();
        }

        private void StopServer()
        {
            Server.Instance.ShutdownAsync();
        }
    }
}
