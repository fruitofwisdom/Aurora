using LiteDB;
using System;
using System.Collections.Generic;

namespace Aurora
{
    internal class Player : GameObject
    {
        private Connection LocalConnection;

        // These public fields all serialize to the database.
        public string Password { get; set; }
        public string Salt { get; set; }
        [BsonField("is_admin")]
        public bool IsAdmin { get; set; } = false;

        private bool DescriptionNeeded = true;
        private string LastInput = "";

        public Player(string name, int currentRoomId, string password, string salt)
        {
            Name = name;
			CurrentRoomId = currentRoomId;
			Description = "the player " + Name;
			Password = password;
            Salt = salt;
        }

        public bool PasswordMatches(string password)
        {
            bool passwordMatches = false;

            Player otherPlayer = Load(Name);
            if (otherPlayer != null)
            {
                // retrieve the salt from the database, hash the provided password, and see if it
                // matches the actual password
                string actualPassword = otherPlayer.Password;
                string saltAsString = otherPlayer.Salt;
                byte[] salt = Convert.FromBase64String(saltAsString);
                string hashedPassword = Connection.HashPassword(password, salt);
                passwordMatches = actualPassword == hashedPassword;
            }

            return passwordMatches;
        }

        // Create a new player and insert them into the database.
        public static Player Create(string name, string password, string salt, int startingRoomId)
        {
            Player newPlayer = new(name, startingRoomId, password, salt);
			ILiteCollection<Player> players = Database.Instance.GetCollection<Player>("players");
            players.Insert(newPlayer);
			return newPlayer;
        }

        // Load a player from the database.
        public static Player Load(string name)
        {
            ILiteCollection<Player> players = Database.Instance.GetCollection<Player>("players");
            return players.FindOne(x => x.Name == name);
        }

        // Save ("update") a player to the database.
        public bool Save()
        {
            ILiteCollection<Player> players = Database.Instance.GetCollection<Player>("players");
            return players.Update(_id, this);
        }

        public void SetConnection(Connection localConnection)
        {
            LocalConnection = localConnection;
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
                    if (argument.Split(' ')[0] == "at")
                    {
                        LookAt(GetArgument(argument));
                    }
                    else
                    {
                        DescriptionNeeded = true;
                    }
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
            LocalConnection.SendMessage("     \"look at\" to look at something near you.\r\n");
            LocalConnection.SendMessage("     \"north\", \"n\", \"south\", etc to move around the environment.\r\n");
            LocalConnection.SendMessage("     \"exits\" to see obvious exits.\r\n");
            LocalConnection.SendMessage("     \"who\" to list who else is playing.\r\n");
            LocalConnection.SendMessage("     \"say\" to say something to everyone nearby.\r\n");
            LocalConnection.SendMessage("     \"emote\" to express yourself.\r\n");
            LocalConnection.SendMessage("     \"!\" to repeat your last command.\r\n");
        }

        private void LookAt(string gameObjectName)
        {
            GameObject gameObject = Game.GetGameObject(gameObjectName, CurrentRoomId);
            if (gameObject != null)
            {
                LocalConnection.SendMessage("You see " + gameObject.Description + ".\r\n");
			}
            else
            {
                LocalConnection.SendMessage("You don't see a " + gameObjectName + " here.\r\n");
            }
		}

        private void PrintExits()
        {
            List<(string, int, string)> exits = Game.GetRoomExits(CurrentRoomId);

            if (exits.Count == 0)
            {
                LocalConnection.SendMessage("You see no obvious exits.\r\n");
            }
            else
            {
                LocalConnection.SendMessage("Obvious exits are:\r\n");
                foreach ((string, int, string) exit in exits)
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

            int? newRoomId = Game.RoomContainsExit(CurrentRoomId, command);
            if (newRoomId != null)
            {
                if (Game.RoomExists((int)newRoomId))
                {
                    Game.Instance.ReportPlayerMoved(this, CurrentRoomId, (int)newRoomId);
                    CurrentRoomId = (int)newRoomId;
                    bool didSave = Save();
                    if (!didSave)
                    {
						ServerInfo.Instance.Report("[Game] Player \"" + Name + "\" failed to save.\n");
					}
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
