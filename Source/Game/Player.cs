using System;
using System.Collections.Generic;

namespace Aurora
{
    internal partial class Player : Fighter
    {
        private Connection LocalConnection;

        // These public fields all serialize.
        public string Password { get; set; }
        public string Salt { get; set; }
        public bool IsAdmin { get; set; } = false;
        public List<GameObject> Inventory { get; set; }

        private bool DescriptionNeeded = true;
        private string LastInput = "";

        private Fighter Target = null;
		private const double kAttackTime = 5;      // in seconds
		private DateTime LastAttackTime = DateTime.MinValue;

		public Player(string name, int currentRoomId, string password, string salt)
        {
            Name = name;
			CurrentRoomId = currentRoomId;
			Description = "the player " + Name;
			Password = password;
            Salt = salt;
            Inventory = new();
        }

		public void SetConnection(Connection localConnection)
        {
            LocalConnection = localConnection;
        }

        public void Message(string message)
        {
            LocalConnection.SendMessage(message);
        }

        protected override void Think(DateTime eventTime)
        {
            if ((eventTime - LastAttackTime).Seconds > kAttackTime)
            {
				if (Target != null)
				{
                    if (Target.CurrentRoomId != CurrentRoomId)
                    {
                        // our target died, left the room, or we did, etc
                        Target = null;
                    }
                    else
                    {
                        Attack(Target);
                    }
				}

				LastAttackTime = eventTime;
            }
        }

		protected override void DealtDamage(Fighter defender, bool didHit, int damage)
		{
			ServerInfo.Instance.Report(
				ColorCodes.Color.Yellow,
				"[Player] Player " + DebugName() + " is dealing damage to " + defender.DebugName() + ".\n");
			
			if (didHit)
            {
                PrintPrompt();
				LocalConnection.SendMessage("You hit the " + defender.Name + " for " + damage + " damage!\r\n");
			}
            else
            {
				PrintPrompt();
				LocalConnection.SendMessage("Your attack misses the " + defender.Name + "!\r\n");
			}

			base.DealtDamage(defender, didHit, damage);
		}

		protected override void TakeDamage(Fighter attacker, bool didHit, int damage)
        {
			ServerInfo.Instance.Report(
				ColorCodes.Color.Yellow,
				"[Player] Player " + DebugName() + " is taking damage from " + attacker.DebugName() + ".\n");
			
			if (didHit)
            {
				PrintPrompt();
				LocalConnection.SendMessage("The " + attacker.Name + " hits you for " + damage + " damage!\r\n");
            }
            else
            {
				PrintPrompt();
				LocalConnection.SendMessage("The " + attacker.Name + "'s attack misses you!\r\n");
			}

			base.TakeDamage(attacker, didHit, damage);
		}

		protected override void Die(Fighter attacker)
		{
			ServerInfo.Instance.Report(
				ColorCodes.Color.Yellow,
				"[Player] Player " + DebugName() + " was killed by " + attacker.DebugName() + ".\n");

			base.Die(attacker);

			LocalConnection.SendMessage("You are killed by the " + attacker.Name + "!\r\n");
            Target = null;

			// Respawn in the starting room.
			CurrentRoomId = Game.Instance.StartingRoomId;
			CurrentHP = MaxHP;
            LocalConnection.SendMessage("...NOT YET, FRIEND...\r\nYou wake up dazed. What happened? Where are you?\r\n");
            PrintRoom();
		}

		protected override void NotifyDeath(Fighter defender)
		{
            base.NotifyDeath(defender);

            // Our target died.
			if (defender == Target)
            {
				LocalConnection.SendMessage("You kill the " + defender.Name + "!\r\n");
				Target = null;
            }
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

		private static string LookupShorthand(string input)
        {
            string toReturn = input;

            Dictionary<string, string> shorthand = new()
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

            // parse out the verb, any preposition, and any object
            (string, string, string) splitInput = SplitInput(input);
            string inputVerb = splitInput.Item1;
            string inputPreposition = splitInput.Item2;
            string inputObject = splitInput.Item3;

            // also keep the entire non-verb portion of the input for "say", "emote", etc
            string inputArgument = GetArgument(input);

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
                    Say(inputArgument);
                    break;
                case "emote":
                    Emote(inputArgument);
                    break;
                case "stats":
                    PrintStats();
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
                case "attack":
                    Attack(inputObject);
                    break;
                case "yield":
                    Yield();
                    break;
                case "debugobject":
                    DebugObject(inputObject);
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

            PrintPrompt();
		}

        private void PrintPrompt()
        {
            LocalConnection.SendMessage(ColorCodes.Color.Green, Level + " ");
            // display the current HP in different colors based on how hurt we are
            if ((float)CurrentHP / MaxHP < 0.25)
            {
                LocalConnection.SendMessage(ColorCodes.Color.Red, CurrentHP + "/" + MaxHP);
            }
            else if ((float)CurrentHP / MaxHP < 0.75)
            {
                LocalConnection.SendMessage(ColorCodes.Color.Yellow, CurrentHP + "/" + MaxHP);
            }
            else
            {
                LocalConnection.SendMessage(CurrentHP + "/" + MaxHP);
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
			LocalConnection.SendMessage("     \"say\" or \"emote\" to express yourself.\r\n");
			LocalConnection.SendMessage("     \"stats\" to see your current level, HP, etc.\r\n");
			LocalConnection.SendMessage("     \"inventory\" or \"inv\" to list what you're carrying.\r\n");
			LocalConnection.SendMessage("     \"take\" to pick something up.\r\n");
			LocalConnection.SendMessage("     \"drop\" to drop something.\r\n");
            LocalConnection.SendMessage("     \"attack\" to start attacking an enemy.\r\n");
            LocalConnection.SendMessage("     \"yield\" to stop attacking.\r\n");
			LocalConnection.SendMessage("     \"!\" to repeat your last command.\r\n");
			if (IsAdmin)
			{
				LocalConnection.SendMessage("     \"debugobject\" to print debug information about an object. (admin)\r\n");
				LocalConnection.SendMessage("     \"shutdown\" to shutdown the server. (admin)\r\n");
			}
		}
	}
}
