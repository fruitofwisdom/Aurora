using System;
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

		public bool ClientDisconnected { get; private set; }
		private readonly TcpClient Client;
		public int ClientID { get; private set; }
		public Thread ClientThread { get; private set; }

		private DateTime TimeSinceInput;
		private const int kTimeoutTime = 5;     // in minutes
		private InputState LocalInputState = InputState.LoginName;
		private int PasswordTries = 3;
		
		private Player LocalPlayer = null;
		private string LocalPlayerName = null;

		public Connection(TcpClient client, int clientID)
		{
			ClientDisconnected = false;
			Client = client;
			ClientID = clientID;
			ClientThread = new Thread(new ThreadStart(Connect));
			ClientThread.Name = "Aurora Client (" + ClientID + ") Thread";
			ClientThread.Start();
			TimeSinceInput = DateTime.Now;

			ServerInfo.Instance.Report("[Connection] Client (" + ClientID + ") connected.\n");

			// on a single-core machine, give our new thread some time
			Thread.Sleep(0);
		}

		public void Connect()
		{
			try
			{
				NetworkStream stream = Client.GetStream();

				// eat incoming data and print a welcome message
				if (stream.DataAvailable)
				{
					byte[] bytes = new byte[256];
					stream.Read(bytes, 0, bytes.Length);
				}
				FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
				string version = versionInfo.ProductVersion;
				foreach (string line in Game.Instance.Title)
				{
					SendMessage(line + "\r\n");
				}
				if (Game.Instance.Subtitle != null)
				{
					SendMessage(Game.Instance.Subtitle + "\r\n");
				}
				SendMessage("\r\n");
				SendMessage("This MUD is running on Aurora version " + version + ".\r\n\r\n");
				SendMessage("What is your name?\r\n> ");

				// loop until the client has left and the thread is terminated
				while (!ClientDisconnected)
				{
					if (stream.DataAvailable)
					{
						byte[] bytes = new byte[256];
						int count = stream.Read(bytes, 0, bytes.Length);
						ParseInput(System.Text.Encoding.ASCII.GetString(bytes, 0, count));
					}

					// time out eventually
					if ((DateTime.Now - TimeSinceInput).Minutes >= kTimeoutTime)
					{
						ServerInfo.Instance.Report("[Connection] Client (" + ClientID + ") timed out after " +
							(DateTime.Now - TimeSinceInput).Minutes + " minutes.\n");
						Disconnect(true);
					}

					// this amount of sleep gives us fairly OK CPU usage
					Thread.Sleep(30);
				}
			}
			catch (ThreadAbortException)
			{
				// this is OK
			}
			catch (Exception exception)
			{
				ServerInfo.Instance.Report(
					ColorCodes.Color.Red,
					"[Connection] Exception caught by client (" + ClientID + "): " + exception.Message + "\n");
				Disconnect(false);
			}
			finally
			{
				// close the connection
				Client.Close();
			}
		}

		public void Disconnect(bool properly)
		{
			// if we were playing, tell the game we quit
			if (LocalInputState == InputState.Play)
			{
				LocalPlayer.SetConnection(null);
				Game.Instance.PlayerQuit(LocalPlayer);
			}

			if (properly)
			{
				ServerInfo.Instance.Report("[Connection] Client (" + ClientID + ") disconnected.\n");
			}
			else
			{
				ServerInfo.Instance.Report("[Connection] Client (" + ClientID + ") disconnected unexpectedly.\n");
			}

			ClientDisconnected = true;
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
					LocalPlayer.HandleInput(input);
					break;
			}

			TimeSinceInput = DateTime.Now;
		}

		private void HandleLoginName(string name)
		{
			if (!Game.Instance.PlayerCanJoin(name))
			{
				SendMessage("\"" + name + "\" is already playing!\r\n");
				ServerInfo.Instance.Report("[Connection] Player \"" + name + "\" was already playing.\n");
				Disconnect(true);
				return;
			}

			if (!NameIsValid(name))
			{
				SendMessage("\"" + name + "\" is not a valid name!\r\n");
				SendMessage("> ");
				ServerInfo.Instance.Report("[Connection] Name \"" + name + "\" was not a valid name.\n");
				return;
			}

			if (Game.Instance.PlayerExists(name))
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

			LocalPlayerName = name;
			ServerInfo.Instance.Report("[Connection] Player \"" + LocalPlayerName + "\" is attempting to login.\n");
		}

		private void HandleLoginExistingPassword(string password)
		{
			Player player = Game.Instance.GetPlayer(LocalPlayerName, false);
			if (PasswordMatches(password, player))
			{
				LoginPlayer(player);
			}
			else
			{
				SendMessage("I'm sorry, that's not correct.\r\n");
				if (--PasswordTries > 0)
				{
					SendMessage("You have " + PasswordTries +  " " +
						(PasswordTries > 1 ? "tries" : "try") + " remaining.\r\n");
					SendMessage("> ");
				}
				else
				{
					ServerInfo.Instance.Report("[Connection] Player \"" + LocalPlayerName + "\" failed to login.\n");
					Disconnect(true);
				}
			}
		}

		private void HandleLoginNewPassword(string password)
		{
			byte[] salt = GenerateSalt();
			string hashedPassword = HashPassword(password, salt);
			string saltAsString = Convert.ToBase64String(salt);
			Player player = Game.Instance.CreatePlayer(LocalPlayerName, hashedPassword, saltAsString);
			LoginPlayer(player);
		}

		private static bool NameIsValid(string name)
		{
			// Names must be 4 characters or longer (prevents garbage from port scanning).
			if (name.Length < 4)
			{
				return false;
			}

			// Names must only contain letters or numbers (prevents spaces or special characters).
			foreach (char letter in name)
			{
				if (!Char.IsLetterOrDigit(letter))
				{
					return false;
				}
			}

			// TODO: Handle disallowing inappropriate names.

			return true;
		}

		private static byte[] GenerateSalt()
		{
			byte[] salt = new byte[8];
			RandomNumberGenerator.Create().GetBytes(salt);
			return salt;
		}

		private static string HashPassword(string password, byte[] salt)
		{
			Rfc2898DeriveBytes pbkdf2 = new(password, salt, 10000, HashAlgorithmName.SHA256);
			byte[] hash = pbkdf2.GetBytes(20);
			return Convert.ToBase64String(hash);
		}

		private static bool PasswordMatches(string password, Player otherPlayer)
		{
			// retrieve the salt from the database, hash the provided password, and see if it
			// matches the actual password
			string actualPassword = otherPlayer.Password;
			string saltAsString = otherPlayer.Salt;
			byte[] salt = Convert.FromBase64String(saltAsString);
			string hashedPassword = Connection.HashPassword(password, salt);
			return actualPassword == hashedPassword;
		}

		private void LoginPlayer(Player player)
		{
			LocalPlayer = player;
			LocalPlayer.SetConnection(this);

			SendMessage(Game.Instance.WelcomeMessage + "\r\n\r\n");
			SendMessage("Type \"help\" for more information.\r\n");

			Game.Instance.PlayerJoined(LocalPlayer);
			LocalPlayer.PrintRoom();
			LocalInputState = InputState.Play;
		}

		// This function will automatically split a message at a terminal width of 80
		// characters, inserting newlines as appropriate. Any ending newlines that are
		// passed in will be preserved.
		public void SendMessage(string message)
		{
			if (!ClientDisconnected)
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

		// Sends the provided message using a specified color. Afterwards, resets the color back.
		public void SendMessage(ColorCodes.Color color, string message)
		{
			SendMessage(ColorCodes.GetAnsiColorCode(color));
			SendMessage(message);
			SendMessage(ColorCodes.GetAnsiColorCode(ColorCodes.Color.Reset));
		}
	}
}
