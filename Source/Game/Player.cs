using System.Collections.Generic;
using System.Windows;

namespace Aurora
{
    internal class Player : GameObject
    {
        private Connection LocalConnection;

        // These public fields all serialize.
        public string Password { get; set; }
        public string Salt { get; set; }
        public bool IsAdmin { get; set; } = false;
        public List<GameObject> Inventory { get; set; }

        private bool DescriptionNeeded = true;
        private string LastInput = "";

        public Player(string name, int currentRoomId, string password, string salt)
        {
            Name = name;
			CurrentRoomId = currentRoomId;
			Description = "the player " + Name;
			Password = password;
            Salt = salt;
            Inventory = new();
        }

        public bool HasConnection()
        {
            return LocalConnection != null;
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
                { "d", "down" },
                { "i", "inventory" }
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
                    // Return early. Our connection has gone away.
                    return;
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
                case "inventory":
                case "inv":
                    PrintInventory();
                    break;
                case "take":
                    Take(argument);
                    break;
                case "drop":
                    Drop(argument);
                    break;
                case "shutdown":
                    Shutdown();
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
            LocalConnection.SendMessage(ColorCodes.Color.Yellow, Game.Instance.GetRoomName(CurrentRoomId) + "\r\n");
            if (DescriptionNeeded)
            {
                LocalConnection.SendMessage(Game.Instance.GetRoomDescription(CurrentRoomId) + "\r\n");
                DescriptionNeeded = false;
            }
            string roomContents = Game.Instance.GetRoomContents(this);
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
			LocalConnection.SendMessage("     \"inventory\" or \"inv\" to list what you're carrying.\r\n");
			LocalConnection.SendMessage("     \"take\" to pick something up.\r\n");
			LocalConnection.SendMessage("     \"drop\" to drop something.\r\n");
			LocalConnection.SendMessage("     \"!\" to repeat your last command.\r\n");
            if (IsAdmin)
            {
				LocalConnection.SendMessage("     \"shutdown\" to shutdown the server. (admin)\r\n");
			}
		}

        private void LookAt(string gameObjectName)
        {
			// Try looking in your inventory first.
			bool wasInInventory = true;
			GameObject gameObject = GetObjectFromInventory(gameObjectName);
            if (gameObject == null)
            {
                // Then try looking in the world.
				wasInInventory = false;
				gameObject = Game.GetGameObject(gameObjectName, CurrentRoomId);
            }

            if (gameObject != null)
            {
                string message = "";
                if (wasInInventory)
                {
                    message += "(in your inventory) ";
                }
                // If a description has a period at the end, treat it as its own sentence.
                if (gameObject.Description[gameObject.Description.Length - 1] == '.')
                {
                    message += gameObject.Description + "\r\n";
                }
                // Otherwise, describe the object succinctly.
                else
                {
                    message += "You see " + gameObject.Description + ".\r\n";
                }
				LocalConnection.SendMessage(message);
			}
            else
            {
                LocalConnection.SendMessage("You don't see a " + gameObjectName + " here.\r\n");
            }
		}

        private void PrintExits()
        {
            List<(string, int, string)> exits = Game.Instance.GetRoomExits(CurrentRoomId);

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
            if (Game.Instance.GetPlayerCount() == 1)
            {
                LocalConnection.SendMessage("There is 1 player currently:\r\n");
            }
            else
            {
                LocalConnection.SendMessage("There are " + Game.Instance.GetPlayerCount() + " players currently:\r\n");
            }
            foreach (string playerName in Game.Instance.GetPlayerNames())
            {
                if (Game.Instance.PlayerIsAdmin(playerName))
                {
					LocalConnection.SendMessage("     " + playerName + " (admin)\r\n");
				}
				else
                {
                    LocalConnection.SendMessage("     " + playerName + "\r\n");
                }
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

        private GameObject GetObjectFromInventory(string gameObjectName)
        {
            GameObject toReturn = null;

            foreach (GameObject gameObject in Inventory)
            {
                if (gameObject.Name.ToLower() == gameObjectName.ToLower())
                {
                    toReturn = gameObject;
                }
            }

			return toReturn;
        }

        private void PrintInventory()
        {
            if (Inventory.Count == 0)
            {
				LocalConnection.SendMessage("You're not carrying anything.\r\n");
			}
            else
            {
				LocalConnection.SendMessage("You are carrying:\r\n");
                foreach (GameObject gameObject in Inventory)
                {
                    LocalConnection.SendMessage("     " + gameObject.CapitalizeName() + "\r\n");
				}
			}
		}

        private void Take(string argument)
        {
            GameObject gameObject = Game.Instance.TryTake(this, argument);
            if (gameObject != null)
            {
                LocalConnection.SendMessage("You take " + gameObject.Description + ".\r\n");
                Inventory.Add(gameObject);
            }
            else
            {
                LocalConnection.SendMessage("You can't take that!\r\n");
            }
        }

        private void Drop(string argument)
        {
            GameObject gameObject = GetObjectFromInventory(argument);
			if (gameObject != null)
			{
                Game.Instance.TryDrop(this, gameObject);
				LocalConnection.SendMessage("You drop " + gameObject.Description + ".\r\n");
				Inventory.Remove(gameObject);
			}
			else
			{
				LocalConnection.SendMessage("You don't have that!\r\n");
			}
		}

        private void Shutdown()
        {
            if (IsAdmin)
            {
                Server.Instance.ShutdownAsync();
            }
            else
            {
				LocalConnection.SendMessage("Only admins may shutdown the server!\r\n");
			}
		}

		private void TryExit(string command)
        {
            bool didExit = false;

            int? newRoomId = Game.Instance.RoomContainsExit(CurrentRoomId, command);
            if (newRoomId != null)
            {
                if (Game.Instance.RoomExists((int)newRoomId))
                {
                    Game.Instance.ReportPlayerMoved(this, CurrentRoomId, (int)newRoomId);
                    CurrentRoomId = (int)newRoomId;
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
