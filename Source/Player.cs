namespace Aurora
{
    internal class Player
    {
        private readonly Connection LocalConnection;
        public string Name = "player";
        private string CurrentRoom = "Limbo";

        public Player(Connection connection)
        {
            LocalConnection = connection;
        }

        // TODO: Properly save and load players. -Ward
        public void Load(string currentRoom)
        {
            CurrentRoom = currentRoom;
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
