using System;
using System.Collections.Generic;
using System.Windows.Navigation;

namespace Aurora
{
	internal partial class Player : Fighter
	{
		// These fields are deserialized from JSON.
		#region JSON-serialized public fields.
		public string Password { get; set; }
		public string Salt { get; set; }
		public bool IsAdmin { get; set; } = false;
		public List<Item> Equipment { get; set; }
		public List<Item> Inventory { get; set; }
		public int XP { get; set; } = 0;
		public int Gold { get; set; } = 0;
		public bool ConfigLongPrompt { get; set; } = true;
		#endregion

		private Connection LocalConnection;
		private bool DescriptionNeeded = true;
		private string LastInput = "";

		private Fighter Target = null;
		private const double kAttackTime = 5;      // in seconds
		private DateTime LastAttackTime = DateTime.MinValue;

		// HP regenerates after some time has passed.
		private const double kStartRegenTime = 10;      // in seconds
		private const double kContinueRegenTime = 5;        // in seconds
		private const double kHPToRestore = 0.1;		// in percent
		private DateTime LastDamageTime = DateTime.MinValue;
		private DateTime LastRegenTime = DateTime.MinValue;
		private bool RegenJustStarted = true;

		public Player(string name, int currentRoomId, string password, string salt)
		{
			Name = name;
			CurrentRoomId = currentRoomId;
			Description = "the player " + Name;
			Password = password;
			Salt = salt;
			Inventory = new();
		}

		protected override void Think(DateTime eventTime)
		{
			// Attack more often or not if we are faster than our opponent.
			double attackTime = kAttackTime;
			if (Target != null)
			{
				attackTime = GetAdjustedAttackTime(kAttackTime, Target);
			}

			double elapsedTime = (eventTime - LastAttackTime).Seconds +
				(eventTime - LastAttackTime).Milliseconds / 1000;
			if (elapsedTime > attackTime)
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

			// Restore HP if enough time has passed since taking damage.
			if ((eventTime - LastDamageTime).Seconds > kStartRegenTime &&
				(eventTime - LastRegenTime).Seconds > kContinueRegenTime)
			{
				if (CurrentHP < MaxHP)
				{
					CurrentHP += (int)(MaxHP * kHPToRestore);
					if (CurrentHP >= MaxHP)
					{
						CurrentHP = MaxHP;
						LocalConnection.SendMessage("You feel fully recovered.\r\n");
						PrintPrompt();
						LastDamageTime = eventTime;
					}
					else if (RegenJustStarted)
					{
						RegenJustStarted = false;
						LocalConnection.SendMessage("You begin to feel better.\r\n");
						PrintPrompt();
					}
					LastRegenTime = eventTime;
				}
			}
		}

		protected override void DealtDamage(Fighter defender, bool didHit, int damage)
		{
			if (didHit)
			{
				PrintCombatPrompt();
				LocalConnection.SendMessage("You hit the " + defender.Name + " for " + damage + " damage!\r\n");
			}
			else
			{
				PrintCombatPrompt();
				LocalConnection.SendMessage("Your attack misses the " + defender.Name + "!\r\n");
			}

			base.DealtDamage(defender, didHit, damage);
		}

		protected override void TakeDamage(Fighter attacker, bool didHit, int damage)
		{
			if (didHit)
			{
				// NOTE: Take damage here (and not in base.TakeDamage) so the new HP are reflected
				// in the prompt.
				CurrentHP -= damage;
				PrintCombatPrompt();
				LocalConnection.SendMessage("The " + attacker.Name + " hits you for " + damage + " damage!\r\n");
			}
			else
			{
				PrintCombatPrompt();
				LocalConnection.SendMessage("The " + attacker.Name + "'s attack misses you!\r\n");
			}

			// We've just taken damage, don't regen yet.
			LastDamageTime = DateTime.Now;
			RegenJustStarted = true;

			base.TakeDamage(attacker, didHit, 0);
		}

		protected override void Die(Fighter attacker)
		{
			ServerInfo.Instance.Report(
				ColorCodes.Color.Yellow,
				"[Player] Player " + DebugName() + " was killed by " + attacker.DebugName() + ".\n");

			base.Die(attacker);

			LocalConnection.SendMessage("You are defeated by the " + attacker.Name + "!\r\n");
			Target = null;

			// Respawn in the starting room.
			CurrentRoomId = Game.Instance.StartingRoomId;
			CurrentHP = MaxHP;
			LocalConnection.SendMessage("...NOT YET, FRIEND...\r\nYou wake up dazed. What happened? Where are you?\r\n");
			PrintRoom();
		}

		protected override void NotifyDeath(Fighter attacker, Fighter defender)
		{
			base.NotifyDeath(attacker, defender);

			if (attacker == this)
			{
				// We killed the defender.
				LocalConnection.SendMessage("You defeat the " + defender.Name + "!\r\n");
				Target = null;
			}
			else if (defender == Target)
			{
				// Our target was killed by someone else.
				Target = null;
			}
			else if (CurrentRoomId != defender.CurrentRoomId)
			{
				// We participated in the kill, but it's not our target.
				LocalConnection.SendMessage("You helped defeat the " + defender.Name + "!\r\n");
			}
		}

		// Reward a player with a certain amount of experience and gold, potentially leveling up.
		public void Reward(int xp, int gold)
		{
			if (xp > 0)
			{
				XP += xp;
				LocalConnection.SendMessage("You gain " + xp + " experience!\r\n");

				int newLevel = Game.Instance.GetLevelForXP(XP);
				// NOTE: Players cannot currently go down a level.
				if (newLevel > Level)
				{
					int numLevelsGained = newLevel - Level;
					Level = newLevel;
					MaxHP += Game.Instance.MaxHPPerLevel * numLevelsGained;
					BaseStrength += Game.Instance.StrengthPerLevel * numLevelsGained;
					BaseDefense += Game.Instance.DefensePerLevel * numLevelsGained;
					BaseAgility += Game.Instance.AgilityPerLevel * numLevelsGained;
					LocalConnection.SendMessage("You are now level " + Level + "!\r\n");
				}
			}
			if (gold > 0)
			{
				Gold += gold;
				LocalConnection.SendMessage("You find " + gold + " " + Game.Instance.Currency + "!\r\n");
			}

			PrintRoom();
		}

		public override int Strength
		{
			get
			{
				int strength = BaseStrength;
				foreach (Item item in Equipment)
				{
					strength += item.StrengthMod;
				}
				return strength;
			}
		}

		public override int Defense
		{
			get
			{
				int defense = BaseDefense;
				foreach (Item item in Equipment)
				{
					defense += item.DefenseMod;
				}
				return defense;
			}
		}

		public override int Agility
		{
			get
			{
				int agility = BaseAgility;
				foreach (Item item in Equipment)
				{
					agility += item.AgilityMod;
				}
				return agility;
			}
		}

		public void SetConnection(Connection localConnection)
		{
			LocalConnection = localConnection;
		}

		public void Message(string message)
		{
			LocalConnection.SendMessage(message);
		}

		// Returns a nicely formatted string of items. For example, "north, south, and down".
		private static string GetPrettyList(List<string> items)
		{
			string prettyList = "";
			for (int i = 0; i < items.Count; ++i)
			{
				prettyList +=
					(items.Count == 2 && i > 0 ? " and " : "") +
					(items.Count > 2 && i > 0 ? ", " : "") +
					(items.Count > 2 && i == items.Count - 1 ? "and " : "") +
					items[i];
			}
			return prettyList;
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

			// Do we need to print the room after this input?
			bool needToPrintRoom = true;

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
					PrintHelp(inputObject);
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
				case "read":
					Read(inputObject);
					break;
				case "talk":
					Talk(inputObject);
					break;
				case "list":
					List();
					break;
				case "browse":
					Browse(inputObject);
					break;
				case "buy":
					Buy(inputObject);
					break;
				case "sell":
					Sell(inputObject);
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
				case "eat":
					Eat(inputObject);
					break;
				case "drink":
					Drink(inputObject);
					break;
				case "equip":
					Equip(inputObject);
					break;
				case "unequip":
					Unequip(inputObject);
					break;
				case "attack":
					needToPrintRoom = Attack(inputObject);
					break;
				case "yield":
					needToPrintRoom = Yield();
					break;
				case "consider":
					Consider(inputObject);
					break;
				case "config":
					Config(inputObject);
					break;
				case "debug":
					Debug(inputObject);
					break;
				case "teleport":
					Teleport(inputObject);
					break;
				case "shutdown":
					Shutdown();
					break;
				default:
					TryExit(inputVerb);
					break;
			}

			LastInput = input;

			// After each input, we may need to tell the player about the state of the room again.
			if (needToPrintRoom)
			{
				PrintRoom();
			}
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
			PrintExits(false);
			string roomContents = Game.Instance.GetRoomContents(this);
			if (roomContents != "")
			{
				LocalConnection.SendMessage(roomContents);
			}

			PrintPrompt();
		}

		private void PrintPrompt()
		{
			// print the current level
			LocalConnection.SendMessage(ColorCodes.Color.Green,
				(ConfigLongPrompt ? "Lvl: " : "") + Level + " ");
			// and progress to the next level
			if (Game.Instance.GetXPForLevel(Level + 1) > 0)
			{
				int xpPreviousLevel = Game.Instance.GetXPForLevel(Level);
				int xpNextLevel = Game.Instance.GetXPForLevel(Level + 1);
				double percentToNextLevel = (double)(XP - xpPreviousLevel) /
					(xpNextLevel - xpPreviousLevel) * 100;
				LocalConnection.SendMessage(ColorCodes.Color.Green, "(" + (int)percentToNextLevel + "%) ");
			}
			// print the current HP in different colors
			if ((float)CurrentHP / MaxHP < 0.25)
			{
				LocalConnection.SendMessage(ColorCodes.Color.Red,
					(ConfigLongPrompt ? "HP: " : "") + CurrentHP + "/" + MaxHP);
			}
			else if ((float)CurrentHP / MaxHP < 0.75)
			{
				LocalConnection.SendMessage(ColorCodes.Color.Yellow,
					(ConfigLongPrompt ? "HP: " : "") + CurrentHP + "/" + MaxHP);
			}
			else
			{
				LocalConnection.SendMessage(
					(ConfigLongPrompt ? "HP: " : "") + CurrentHP + "/" + MaxHP);
			}
			LocalConnection.SendMessage("> ");
		}

		// This special "prompt" is used to provide information during combat.
		private void PrintCombatPrompt()
		{
			LocalConnection.SendMessage("(");
			// print the current HP in different colors
			if ((float)CurrentHP / MaxHP < 0.25)
			{
				LocalConnection.SendMessage(ColorCodes.Color.Red,
					(ConfigLongPrompt ? "HP: " : "") + CurrentHP + "/" + MaxHP);
			}
			else if ((float)CurrentHP / MaxHP < 0.75)
			{
				LocalConnection.SendMessage(ColorCodes.Color.Yellow,
					(ConfigLongPrompt ? "HP: " : "") + CurrentHP + "/" + MaxHP);
			}
			else
			{
				LocalConnection.SendMessage(
					(ConfigLongPrompt ? "HP: " : "") + CurrentHP + "/" + MaxHP);
			}
			LocalConnection.SendMessage(") ");
		}

		private void PrintHelp(string inputObject)
		{
			if (inputObject == "basics")
			{
				LocalConnection.SendMessage("A MUD is a Multi-User Dungeon, a game where multiple people can play " +
					"together, explore, and interact simultaneously. You play by typing any number of text " +
					"commands to navigate the world, talk to people or non-player characters, buy equipment, " +
					"attack monsters to gain experience and levels, and generally just have fun. Here are some " +
					"more of the most basic commands, but explore the other help topics to learn more.\r\n");
				LocalConnection.SendMessage("     \"who\" to list who else is playing.\r\n");
				LocalConnection.SendMessage("     \"say\" or \"emote\" to express yourself.\r\n");
				LocalConnection.SendMessage("     \"read\" to read a sign, for example.\r\n");
				LocalConnection.SendMessage("     \"talk to\" to talk to a non-player character.\r\n");
				LocalConnection.SendMessage("     \"stats\" to see your current level, HP, etc.\r\n");
				LocalConnection.SendMessage("     \"inventory\" or \"inv\" to list what you're carrying.\r\n");
				LocalConnection.SendMessage("     \"take\" to pick something up.\r\n");
				LocalConnection.SendMessage("     \"drop\" to drop something.\r\n");
				LocalConnection.SendMessage("     \"config\" to change various settings.\r\n");
			}
			else if (inputObject == "combat")
			{
				LocalConnection.SendMessage("Combat happens automatically, though you have to \"attack\" an enemy " +
					"to begin. Before you attack something, however, make sure you are properly-equipped and " +
					"consider using \"consider\" to judge an enemy's difficulty. If you find yourself in trouble, " +
					"you can always run away or \"yield\" to stop attacking, though monsters may not immediately " +
					"stop trying to kill you. Eventually, they will lose interest and your health will regenerate " +
					"outside of combat. There is no real penalty to death, so have fun and take risks!\r\n");
				LocalConnection.SendMessage("     \"attack\" to start attacking an enemy.\r\n");
				LocalConnection.SendMessage("     \"yield\" to stop attacking.\r\n");
				LocalConnection.SendMessage("     \"consider\" to consider an enemy, gauging its difficulty.\r\n");
			}
			else if (inputObject == "shopping")
			{
				LocalConnection.SendMessage("Before you go out to explore the world and try to kill the things in " +
					"it, you need to be properly equipped and maybe have some food on hand. Visit one of the shops " +
					"and you can see a \"list\" of what they sell or \"browse\" a specific item. Buy the equipment " +
					"you need (or sell what you no longer need, though resale values are lower) and be sure to " +
					"equip armor and weapons. Food and drink can be held in your inventory and used when you need " +
					"them, especially in an emergency.\r\n");
				LocalConnection.SendMessage("     \"list\" to see what a shopkeeper has for sale.\r\n");
				LocalConnection.SendMessage("     \"browse\" to see details on an item for sale.\r\n");
				LocalConnection.SendMessage("     \"buy\" or \"sell\" to trade with a shopkeeper.\r\n");
				LocalConnection.SendMessage("     \"eat\" or \"drink\" to consume an item in your inventory.\r\n");
				LocalConnection.SendMessage("     \"equip\" or \"unequip\" to equip or unequip equipment.\r\n");
			}
			else
			{
				LocalConnection.SendMessage("Type \"exit\" or \"quit\" to finish playing.\r\n");
				LocalConnection.SendMessage("     \"help\" or \"?\" to see these instructions.\r\n");
				LocalConnection.SendMessage("     \"look\" to look around at your surroundings.\r\n");
				LocalConnection.SendMessage("     \"look at\" to look at something near you.\r\n");
				LocalConnection.SendMessage("     \"north\", \"n\", \"south\", \"in\", etc to move around the environment.\r\n");
				LocalConnection.SendMessage("     \"exits\" to see obvious exits.\r\n");
				LocalConnection.SendMessage("     \"!\" to repeat your last command.\r\n");
				if (IsAdmin)
				{
					LocalConnection.SendMessage("     \"debug\" to print debug information about an object. (admin)\r\n");
					LocalConnection.SendMessage("     \"debug room\" and an optional Room ID to debug a room. (admin)\r\n");
					LocalConnection.SendMessage("     \"teleport\" to teleport to a specific Room ID. (admin)\r\n");
					LocalConnection.SendMessage("     \"shutdown\" to shutdown the server. (admin)\r\n");
				}
				LocalConnection.SendMessage("For additional information, see one of the topics below.\r\n");
				LocalConnection.SendMessage("     \"help basics\" to learn more about MUDs in general.\r\n");
				LocalConnection.SendMessage("     \"help combat\" to learn more about combat.\r\n");
				LocalConnection.SendMessage("     \"help shopping\" to learn more about shopping.\r\n");
			}
		}
	}
}
