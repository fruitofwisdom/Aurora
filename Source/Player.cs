namespace Aurora
{
    internal class Player
    {
        private readonly Connection LocalConnection;
        public string Name = "player";
        public long CurrentRoomId = 0;

        public Player(Connection connection)
        {
            LocalConnection = connection;
        }

        // TODO: Properly save and load players. -Ward
        public void Load(long currentRoomId)
        {
            CurrentRoomId = currentRoomId;
        }

        public void HandleInput(string input, out bool needLook)
        {
            input = input.Trim().ToLower();
            needLook = false;

            switch (input)
            {
                case "exit":
                case "quit":
                    LocalConnection.SendMessage("Good-bye!\r\n");
                    LocalConnection.Quit(true);
                    break;
                case "help":
                case "?":
                    PrintHelp();
                    break;
                case "look":
                    needLook = true;
                    break;
                default:
                    LocalConnection.SendMessage("I don't understand, \"" + input + "\".\r\n");
                    break;
            }
        }

        private void PrintHelp()
        {
            LocalConnection.SendMessage("Type \"exit\" or \"quit\" to finish playing.\r\n");
            LocalConnection.SendMessage("     \"help\" or \"?\" to see these instructions.\r\n");
            LocalConnection.SendMessage("     \"look\" to look around at your surroundings.\r\n");
            //LocalConnection.SendMessage("     \"north\", \"n\", \"south\", etc to move around the environment.\r\n");
        }
    }
}
