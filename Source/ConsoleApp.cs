using System;

namespace Aurora
{
    internal class ConsoleApp
    {
        // this callback lets us subscribe to events sent from other parts of the program
        private readonly ServerInfoHandler EventHandler = null;
        private bool Running = false;

        public ConsoleApp(string databaseFilename)
        {
            EventHandler = new ServerInfoHandler(ServerInfoEventHandler);
            ServerInfo.Instance.EventReceived += ServerInfoEventHandler;

            Database.Instance.Configure(databaseFilename);

            Running = true;
        }

        private void ServerInfoEventHandler(object sender, ServerInfoEventArgs args)
        {
            if (typeof(ServerInfoGameArgs).IsInstanceOfType(args))
            {
                if (((ServerInfoGameArgs)args).Loaded)
                {
                    Server.Instance.ListenAsync();
                }
            }
            else if (typeof(ServerInfoReportArgs).IsInstanceOfType(args))
            {
                Console.Write(((ServerInfoReportArgs)args).Report);
            }
            else if (typeof(ServerInfoServerArgs).IsInstanceOfType(args))
            {
                if (!((ServerInfoServerArgs)args).Running)
                {
                    Running = false;
                }
            }
        }

        private static void Shutdown(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;     // We'll handle it from here.
            Server.Instance.ShutdownAsync();
        }

        public void Run()
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Shutdown);
            while (Running)
            {
                ;
            }
        }
    }
}
