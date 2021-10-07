using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;

namespace Aurora
{
    internal class Connection
    {
        private enum InputState
        {
            LoginName,
            LoginExistingPassword,
            LoginNewPassword,
            Play,
        };

        private bool Running = false;
        private bool DescriptionNeeded = true;
        public bool ClientQuit { get; private set; }
        private DateTime TimeSinceInput;
        private InputState LocalInputState = InputState.LoginName;
        public int ClientID { get; private set; }
        private readonly Player LocalPlayer;
        private readonly TcpClient Client;
        public Thread ClientThread { get; private set; }

        public Connection(TcpClient client, int clientID)
        {
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
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
                string version = versionInfo.ProductVersion;
                SendMessage("Welcome to \"" + Game.Instance.Name + "\", running on Aurora version " + version + "!\r\n\r\n");
                SendMessage("What is your name?\r\n> ");

                // loop until the client has left and the thread is terminated
                while (Running)
                {
                    // NOTE: Don't react if the client has already quit - we're waiting to be pruned. -Ward
                    if (!ClientQuit)
                    {
                        if (stream.DataAvailable)
                        {
                            byte[] bytes = new byte[256];
                            int count = stream.Read(bytes, 0, bytes.Length);
                            ParseInput(System.Text.Encoding.ASCII.GetString(bytes, 0, count));
                            TimeSinceInput = DateTime.Now;
                        }

                        // time out after five minutes
                        if (DateTime.Now - TimeSinceInput > TimeSpan.FromMinutes(5))
                        {
                            ServerInfo.Instance.Report("[Connection] Client (" + ClientID + ") timed out after " + (DateTime.Now - TimeSinceInput).Minutes + " minutes.\n");
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
                Quit(false);
            }
            finally
            {
                // close the connection
                Client.Close();
            }
        }

        private void HandleInput(string input)
        {
            if (input == string.Empty)
            {
                return;
            }

            switch (LocalInputState)
            {
                case InputState.LoginName:
                    HandleLoginName(input);
                    break;

                case InputState.LoginExistingPassword:
                    HandleLoginExistingPassword(input);
                    break;

                case InputState.LoginNewPassword:
                    HandleLoginNewPassword(input);
                    break;

                case InputState.Play:
                    LocalPlayer.HandleInput(input, out DescriptionNeeded);
                    break;
            }

            if (LocalInputState == InputState.Play)
            {
                SendMessage("\r\n");
                SendMessage(Game.GetRoomName(LocalPlayer) + "\r\n");
                if (DescriptionNeeded)
                {
                    SendMessage(Game.GetRoomDescription(LocalPlayer) + "\r\n");
                    DescriptionNeeded = false;
                }
                SendMessage("> ");
            }
        }

        private void HandleLoginName(string name)
        {
            if (Database.Instance.DoesValueExistInColumn("players", "name", name))
            {
                SendMessage("Welcome back, " + name + "!\r\n");
                SendMessage("What is your password?\r\n> ");
                LocalInputState = InputState.LoginExistingPassword;
            }
            else
            {
                SendMessage("Pleased to meet you, " + name + "!\r\n");
                SendMessage("What would you like your password to be?\r\n> ");
                LocalInputState = InputState.LoginNewPassword;
            }

            LocalPlayer.Name = name;
            ServerInfo.Instance.Report("[Connection] Player \"" + LocalPlayer.Name + "\" joined.\n");
        }

        private void HandleLoginExistingPassword(string password)
        {
            if (LocalPlayer.PasswordMatches(password))
            {
                SendMessage("Enjoy your stay!\r\n");
                SendMessage("Type \"help\" for more information.\r\n");

                LocalPlayer.Load();
                ServerInfo.Instance.Report("[Connection] Player \"" + LocalPlayer.Name + "\" entered the game.\n");
                LocalInputState = InputState.Play;
            }
            else
            {
                SendMessage("I'm sorry, that's not correct.\r\n");

                ServerInfo.Instance.Report("[Connection] Player \"" + LocalPlayer.Name + "\" failed to login.\n");
                Quit(true);
            }
        }

        private void HandleLoginNewPassword(string password)
        {
            SendMessage("Enjoy your stay!\r\n");
            SendMessage("Type \"help\" for more information.\r\n");

            byte[] salt = GenerateSalt();
            string hashedPassword = HashPassword(password, salt);
            string saltAsString = Convert.ToBase64String(salt);
            LocalPlayer.Initialize(hashedPassword, saltAsString, Game.Instance.StartingRoomId);
            ServerInfo.Instance.Report("[Connection] Player \"" + LocalPlayer.Name + "\" entered the game.\n");
            LocalInputState = InputState.Play;
        }

        private static byte[] GenerateSalt()
        {
            byte[] salt = new byte[8];
            new RNGCryptoServiceProvider().GetBytes(salt);
            return salt;
        }

        public static string HashPassword(string password, byte[] salt)
        {
            Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);
            return Convert.ToBase64String(hash);
        }

        private void ParseInput(string input)
        {
            string bufferedInput = string.Empty;
            foreach (char letter in input)
            {
                // an end of line is the end of our valid input
                if (letter == '\n' || letter == '\r' || letter == 0)
                {
                    HandleInput(bufferedInput.Trim());
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
                ServerInfo.Instance.Report("[Connection] Client (" + ClientID + ") quit unexpectedly.\n");
            }
            ClientQuit = true;
        }

        public void Close()
        {
            Running = false;
        }

        // This function will automatically split a message at a terminal width of 80
        // characters, inserting newlines as appropriate. Any ending newlines that are
        // passed in will be preserved.
        public void SendMessage(string message)
        {
            if (!ClientQuit)
            {
                string line = "";
                int width = 0;
                string[] words = message.Split(' ');

                for (int i = 0; i < words.Length; ++i)
                {
                    line += words[i];
                    width += words[i].Length;

                    if (i == words.Length - 1)
                    {
                        byte[] messageData = System.Text.Encoding.ASCII.GetBytes(line);
                        Client.GetStream().Write(messageData, 0, line.Length);
                    }
                    else
                    {
                        if (width + words[i + 1].Length >= 80 - 1)
                        {
                            line += "\r\n";
                            byte[] messageData = System.Text.Encoding.ASCII.GetBytes(line);
                            Client.GetStream().Write(messageData, 0, line.Length);
                            line = "";
                            width = 0;
                        }
                        else
                        {
                            line += ' ';
                            width += 1;
                        }
                     }
                }
            }
        }
    }
}
