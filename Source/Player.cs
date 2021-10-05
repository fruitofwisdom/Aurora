using System.Collections.Generic;

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

        private string LookupShorthand(string input)
        {
            string toReturn = input;

            Dictionary<string, string> shorthand = new Dictionary<string, string>()
            {
                { "n", "north" },
                { "ne", "northeast" },
                { "e", "east" },
                { "se", "southeast" },
                { "s", "south" },
                { "sw", "southwest" },
                { "w", "west" },
                { "nw", "northwest" }
            };
            if (shorthand.ContainsKey(input))
            {
                toReturn = shorthand[input];
            }

            return toReturn;
        }

        public void HandleInput(string input, out bool descriptionNeeded)
        {
            descriptionNeeded = false;

            input = input.ToLower();
            input = LookupShorthand(input);

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
                    descriptionNeeded = true;
                    break;
                default:
                    descriptionNeeded = TryExit(input);
                    break;
            }
        }

        private void PrintHelp()
        {
            LocalConnection.SendMessage("Type \"exit\" or \"quit\" to finish playing.\r\n");
            LocalConnection.SendMessage("     \"help\" or \"?\" to see these instructions.\r\n");
            LocalConnection.SendMessage("     \"look\" to look around at your surroundings.\r\n");
            LocalConnection.SendMessage("     \"north\", \"n\", \"south\", etc to move around the environment.\r\n");
        }

        private bool TryExit(string input)
        {
            bool didExit = false;

            long? newRoomId = Game.RoomContainsExit(CurrentRoomId, input);
            if (newRoomId != null)
            {
                if (Game.RoomExists((long)newRoomId))
                {
                    CurrentRoomId = (long)newRoomId;
                    didExit = true;
                }
                else
                {
                    ServerInfo.Instance.Report("[Player \"" + Name + "\"] Room with room_id " + newRoomId + " wasn't found!\n");
                }
            }

            if (!didExit)
            {
                LocalConnection.SendMessage("You can't \"" + input + "\" here!\r\n");
            }

            return didExit;
        }
    }
}
