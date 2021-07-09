using System;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace Aurora
{
    internal class Connection
    {
        private enum InputState
        {
            IS_Login,
            IS_Play,
        };

        private bool Running;
        public bool ClientQuit { get; private set; }
        private DateTime TimeSinceInput;
        private InputState LocalInputState = InputState.IS_Login;
        public int ClientID { get; private set; }
        private readonly Player LocalPlayer;
        private readonly TcpClient Client;
        public Thread ClientThread { get; private set; }

        public Connection(TcpClient client, int clientID)
        {
            Running = false;
            ClientQuit = false;
            TimeSinceInput = DateTime.Now;
            ClientID = clientID;
            LocalPlayer = new Player(this);
            Client = client;
            ClientThread = new Thread(new ThreadStart(Connect));
            ClientThread.Start();

            ServerInfo.Instance.Report("[Connection] Client (" + ClientID + ") connected.\n");

            // on a single-core machine, give our new thread some time
            Thread.Sleep(0);
        }

        public void Connect()
        {
            try
            {
                NetworkStream stream = Client.GetStream();

                Running = true;
                // eat incoming data and print a welcome message
                if (stream.DataAvailable)
                {
                    byte[] bytes = new byte[256];
                    stream.Read(bytes, 0, bytes.Length);
                }
                string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
                string welcomeMessage = "Welcome to \"" + Game.Instance.Name + "\", running on Aurora version." + version + "!\r\n\r\nWhat is your name? ";
                stream.Write(System.Text.Encoding.ASCII.GetBytes(welcomeMessage), 0, welcomeMessage.Length);

                // loop until the client has left and the thread is terminated
                while (Running)
                {
                    // NOTE: Don't react if the client has already quit - we're waiting to be pruned. -Ward
                    if (!ClientQuit)
                    {
                        // TODO: Is it OK that we don't block waiting for data? -Ward
                        if (stream.DataAvailable)
                        {
                            byte[] bytes = new byte[256];
                            int count = stream.Read(bytes, 0, bytes.Length);
                            ParseInput(System.Text.Encoding.ASCII.GetString(bytes, 0, count));
                        }
                        // time out after five minutes
                        else if (DateTime.Now - TimeSinceInput > TimeSpan.FromMinutes(5))
                        {
                            Quit(false);
                        }
                    }

                    // this amount of sleep gives us fairly OK CPU usage
                    Thread.Sleep(7);
                }
            }
            catch (ThreadAbortException)
            {
                // this is OK
            }
            catch (Exception exception)
            {
                ServerInfo.Instance.Report("[Connection] Exception caught by client (" + ClientID + "), \"" + exception.Message + "\"!\n");
            }
            finally
            {
                // close the connection
                Client.Close();
            }
        }

        private void HandleInput(string input)
        {
            if (input != string.Empty)
            {
                bool needLook = false;

                switch (LocalInputState)
                {
                    case InputState.IS_Login:
                        LocalPlayer.Name = input;
                        SendMessage("Hello, " + LocalPlayer.Name + ". Nice to meet you.\r\n");
                        SendMessage("Type \"help\" for more information.\r\n\r\n");
                        ServerInfo.Instance.Report("[Connection] Player \"" + LocalPlayer.Name + "\" joined.\n");

                        // TODO: Properly save and load players. -Ward
                        LocalPlayer.Load(Game.Instance.StartingRoom);

                        LocalInputState = InputState.IS_Play;
                        needLook = true;
                        break;

                    case InputState.IS_Play:
                        LocalPlayer.HandleInput(input, out needLook);
                        break;
                }

                TimeSinceInput = DateTime.Now;
                if (needLook)
                {
                    // TODO: Describe the player's current room. -Ward
                    SendMessage("Unknown Room\r\n");
                    SendMessage("You are in an unknown room, a swirling miasma of scintillating thoughts and turgid ideas.\r\n");
                }
                SendMessage("\r\n> ");
            }
        }

        private void ParseInput(string input)
        {
            string bufferedInput = string.Empty;
            foreach (char letter in input)
            {
                // an end of line is the end of our valid input
                if (letter == '\n' || letter == '\r' || letter == 0)
                {
                    HandleInput(bufferedInput);
                    bufferedInput = string.Empty;
                }
                else
                {
                    bufferedInput += letter;
                }
            }
        }

        // TODO: Improve this. -Ward
        public void Quit(bool properly)
        {
            if (properly)
            {
                ServerInfo.Instance.Report("[Connection] Client (" + ClientID + ") quit.\n");
            }
            else
            {
                ServerInfo.Instance.Report("[Connection] Client (" + ClientID + ") timed out after " + (DateTime.Now - TimeSinceInput).Minutes + " minutes.\n");
            }
            ClientQuit = true;
        }

        public void Close()
        {
            Running = false;
        }

        public void SendMessage(string message)
        {
            if (!ClientQuit)
            {
                byte[] messageData = System.Text.Encoding.ASCII.GetBytes(message);
                Client.GetStream().Write(messageData, 0, message.Length);
            }
        }
    }
}
