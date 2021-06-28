namespace Aurora
{
	class Player
	{
		private Connection LocalConnection;
		public string Name = "player";

		public Player(Connection connection)
		{
			LocalConnection = connection;
		}

		public void HandleInput(string input)
		{
			// our only handled command: to quit
			if (input.ToLower() == "quit")
			{
				LocalConnection.SendMessage("Bye!\r\n");
				LocalConnection.Quit(true);
			}
			else
			{
				// echo back their input
				LocalConnection.SendMessage("I don't understand, \"" + input + "\".\r\n> ");
			}
		}
	}
}
