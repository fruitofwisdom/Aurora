using System.Collections.Generic;

namespace Aurora
{
    internal partial class Player : GameObject
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

		// Is a word one of the ten most commonly used prepositions?
		private static bool IsPreposition(string word)
        {
            List<string> prepositions = new()
                { "of", "with", "at", "from", "into", "to", "in", "for", "on", "by" };
            return prepositions.Contains(word);
        }

        // Combines the input into an object, removing any article at the front. For example,
        // ["the", "fat", "baker"] becomes "fat baker".
        private static string GetObjectFromInput(string[] input, int index)
        {
            string inputObject = "";
            for (int i = index; i < input.Length; ++i)
            {
                // ignore these articles
                if (input[i] != "a" && input[i] != "an" && input[i] != "the")
                {
                    inputObject += input[i];
                    if (i != input.Length - 1)
                    {
                        inputObject += ' ';
                    }
                }
            }
            return inputObject;
        }

        // Split an input into three possible parts: the verb, a preposition, and an object. For
        // example: ("look", "at", "fat baker") or ("drop", null, "manual").
        private static (string, string, string) SplitInput(string input)
        {
            string inputVerb = null;
            string inputPreposition = null;
            string inputObject = null;

            string[] words = input.ToLower().Split(' ');

            if (words.Length > 0)
            {
				// The first word is always treated as the verb.
				inputVerb = LookupShorthand(words[0]);
            }
            if (words.Length > 1)
            {
                // The next word may be a preposition.
                if (IsPreposition(words[1]))
                {
                    inputPreposition = words[1];

                    if (words.Length > 2)
                    {
                        // The rest of the words are the object.
                        inputObject = GetObjectFromInput(words, 2);
                    }
				}
                else if (words.Length > 1)
				{
					// The rest of the words are the object.
					inputObject = GetObjectFromInput(words, 1);
				}
			}

            return (inputVerb, inputPreposition, inputObject);
        }

        private static string LookupShorthand(string input)
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

            (string, string, string) splitInput = SplitInput(input);
            string inputVerb = splitInput.Item1;
            string inputPreposition = splitInput.Item2;
            string inputObject = splitInput.Item3;

            switch (inputVerb)
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
                    if (inputObject != null)
                    {
                        LookAt(inputObject);
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
                    Say(inputObject);
                    break;
                case "emote":
                    Emote(inputObject);
                    break;
                case "inventory":
                case "inv":
                    PrintInventory();
                    break;
                case "take":
                    Take(inputObject);
                    break;
                case "drop":
                    Drop(inputObject);
                    break;
                case "shutdown":
                    Shutdown();
                    break;
                default:
                    TryExit(inputVerb);
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
	}
}
