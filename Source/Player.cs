using System;
using System.Collections.Generic;

namespace Aurora
{
    internal class Player
    {
        private readonly Connection LocalConnection;

        // these fields are stores in the database in this order
        public string Name = "Unknown Player";
        private string Password;
        private string Salt;
        public bool IsAdmin = false;
        public long CurrentRoomId = 0;

        private bool DescriptionNeeded = true;
        private string LastInput = "";

        public Player(Connection connection)
        {
            LocalConnection = connection;
        }

        public bool PasswordMatches(string password)
        {
            bool passwordMatches = false;

            List<List<object>> playersTableValues = Database.Instance.ReadTable("players", "name", Name);
            if (playersTableValues.Count > 0)
            {
                // retrieve the salt from the database, hash the provided password, and see if it
                // matches the actual password
                string actualPassword = (string)playersTableValues[0][2];
                string saltAsString = (string)playersTableValues[0][3];
                byte[] salt = Convert.FromBase64String(saltAsString);
                string hashedPassword = Connection.HashPassword(password, salt);
                passwordMatches = actualPassword == hashedPassword;
            }

            return passwordMatches;
        }

        public void Initialize(string password, string salt, long startingRoomId)
        {
            Password = password;
            Salt = salt;
            CurrentRoomId = startingRoomId;
            Save();
        }

        public void Load()
        {
            List<List<object>> playersTableValues = Database.Instance.ReadTable("players", "name", Name);
            if (playersTableValues.Count > 0)
            {
                Password = (string)playersTableValues[0][2];
                Salt = (string)playersTableValues[0][3];
                IsAdmin = (long)playersTableValues[0][4] != 0;
                CurrentRoomId = (long)playersTableValues[0][5];
            }
        }

        public void Save()
        {
            List<string> columns = new List<string>() { "name", "password", "salt", "is_admin", "current_room_id" };
            List<object> values = new List<object>() { Name, Password, Salt, IsAdmin, CurrentRoomId };
            Database.Instance.WriteTable("players", columns, values);
        }

        public void Message(string message)
        {
            LocalConnection.SendMessage(message);
        }

        private static string GetCommand(string input)
        {
            string command = input.Split()[0].ToLower();
            command = command.Trim();
            return command;
        }

        private static string GetArgument(string input)
        {
            string argument = "";
            string[] words = input.Split(' ');
            for (int i = 1; i < words.Length; ++i)
            {
                argument += words[i];
                if (i != words.Length - 1)
                {
                    argument += " ";
                }
            }
            argument = argument.Trim();
            return argument;
        }

        private string LookupShorthand(string input)
        {
            string toReturn = input;

            Dictionary<string, string> shorthand = new Dictionary<string, string>()
            {
                { "l", "look" },
                { "n", "north" },
                { "ne", "northeast" },
                { "e", "east" },
                { "se", "southeast" },
                { "s", "south" },
                { "sw", "southwest" },
                { "w", "west" },
                { "nw", "northwest" },
                { "u", "up" },
                { "d", "down" }
            };
            if (shorthand.ContainsKey(input))
            {
                toReturn = shorthand[input];
            }

            return toReturn;
        }

        public void HandleInput(string input)
        {
            // the ! command will repeat any previous input
            if (input == "!")
            {
                if (LastInput == "")
                {
                    LocalConnection.SendMessage("You haven't done anything yet.\r\n");
                    return;
                }
                input = LastInput;
            }

            string command = GetCommand(input);
            command = LookupShorthand(command);
            string argument = GetArgument(input);

            switch (command)
            {
                case "exit":
                case "quit":
                    LocalConnection.SendMessage("Good-bye!\r\n");
                    LocalConnection.Disconnect(true);
                    break;
                case "help":
                case "?":
                    PrintHelp();
                    break;
                case "look":
                    DescriptionNeeded = true;
                    break;
                case "exits":
                    PrintExits();
                    break;
                case "who":
                    PrintWho();
                    break;
                case "say":
                    Say(argument);
                    break;
                case "emote":
                    Emote(argument);
                    break;
                default:
                    TryExit(command);
                    break;
            }

            LastInput = input;

            // after each command, we need to tell the player about the state of the room
            PrintRoom();
        }

        public void PrintRoom()
        {
            LocalConnection.SendMessage("\r\n");
            LocalConnection.SendMessage(ColorCodes.Color.Yellow, Game.GetRoomName(CurrentRoomId) + "\r\n");
            if (DescriptionNeeded)
            {
                LocalConnection.SendMessage(Game.GetRoomDescription(CurrentRoomId) + "\r\n");
                DescriptionNeeded = false;
            }
            string roomContents = Game.GetRoomContents(this);
            if (roomContents != "")
            {
                LocalConnection.SendMessage(roomContents);
            }
            LocalConnection.SendMessage("> ");
        }

        private void PrintHelp()
        {
            LocalConnection.SendMessage("Type \"exit\" or \"quit\" to finish playing.\r\n");
            LocalConnection.SendMessage("     \"help\" or \"?\" to see these instructions.\r\n");
            LocalConnection.SendMessage("     \"look\" to look around at your surroundings.\r\n");
            LocalConnection.SendMessage("     \"north\", \"n\", \"south\", etc to move around the environment.\r\n");
            LocalConnection.SendMessage("     \"exits\" to see obvious exits.\r\n");
            LocalConnection.SendMessage("     \"who\" to list who else is playing.\r\n");
            LocalConnection.SendMessage("     \"say\" to say something to everyone nearby.\r\n");
            LocalConnection.SendMessage("     \"emote\" to express yourself.\r\n");
            LocalConnection.SendMessage("     \"!\" to repeat your last command.\r\n");
        }

        private void PrintExits()
        {
            List<(string, long, string)> exits = Game.GetRoomExits(CurrentRoomId);

            if (exits.Count == 0)
            {
                LocalConnection.SendMessage("You see no obvious exits.\r\n");
            }
            else
            {
                LocalConnection.SendMessage("Obvious exits are:\r\n");
                foreach ((string, long, string) exit in exits)
                {
                    string direction = char.ToUpper(exit.Item1[0]) + exit.Item1.Substring(1);
                    LocalConnection.SendMessage("     " + direction + " leads to " + exit.Item3 + ".\r\n");
                }
            }
        }

        private void PrintWho()
        {
            if (Game.Instance.Players.Count == 1)
            {
                LocalConnection.SendMessage("There is 1 player currently:\r\n");
            }
            else
            {
                LocalConnection.SendMessage("There are " + Game.Instance.Players.Count + " players currently:\r\n");
            }
            foreach (Player player in Game.Instance.Players)
            {
                LocalConnection.SendMessage("     " + player.Name + "\r\n");
            }
        }

        private void Say(string argument)
        {
            LocalConnection.SendMessage("You say, \"" + argument + "\"\r\n");
            Game.Instance.ReportPlayerSaid(this, argument);
        }
        private void Emote(string argument)
        {
            LocalConnection.SendMessage("You " + argument + ".\r\n");
            Game.Instance.ReportPlayerEmoted(this, argument);
        }

        private void TryExit(string command)
        {
            bool didExit = false;

            long? newRoomId = Game.RoomContainsExit(CurrentRoomId, command);
            if (newRoomId != null)
            {
                if (Game.RoomExists((long)newRoomId))
                {
                    Game.Instance.ReportPlayerMoved(this, CurrentRoomId, (long)newRoomId);
                    CurrentRoomId = (long)newRoomId;
                    Save();
                    didExit = true;
                }
                else
                {
                    ServerInfo.Instance.Report(
                        ColorCodes.Color.Red,
                        "[Player \"" + Name + "\"] Room with room_id " + newRoomId + " wasn't found!\n");
                }
            }

            if (!didExit)
            {
                LocalConnection.SendMessage("You can't \"" + command + "\" here!\r\n");
            }

            DescriptionNeeded = didExit;
        }
    }
}
