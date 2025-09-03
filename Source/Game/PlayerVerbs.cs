using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Aurora
{
	internal partial class Player : Fighter
	{
		private void LookAt(string inputObject)
		{
			// Try looking in your equipment first.
			GameObject gameObject = GetBestMatch(inputObject, Equipment);
			bool wasEquipped = gameObject != null;
			bool wasInInventory = false;
			if (gameObject == null)
			{
				// Then try looking in your inventory.
				gameObject = GetBestMatch(inputObject, Inventory);
				wasInInventory = gameObject != null;
				if (gameObject == null)
				{
					// Then try looking in the world.
					gameObject = Game.Instance.GetGameObject(inputObject, CurrentRoomId);
				}
			}

			if (gameObject != null)
			{
				// If it's a player, describe the player.
				Player player = gameObject as Player;
				Item item = gameObject as Item;
				if (player != null)
				{
					LocalConnection.SendMessage("You see " + player.Describe() + ".\r\n");
				}
				// If it's an item, describe its properties.
				else if (item != null)
				{
					string message = "";
					if (wasEquipped)
					{
						message += "(equipped) ";
					}
					if (wasInInventory)
					{
						message += "(in your inventory) ";
					}
					message += "You see " + item.Description + ".\r\n";
					LocalConnection.SendMessage(message);
					LocalConnection.SendMessage(item.Describe());
				}
				else
				{
					// If a description has a period at the end, treat it as its own sentence.
					if (gameObject.Description[^1] == '.')
					{
						LocalConnection.SendMessage(gameObject.Description + "\r\n");
					}
					// Otherwise, describe the object succinctly.
					else
					{
						LocalConnection.SendMessage("You see " + gameObject.Description + ".\r\n");
					}
				}
			}
			else
			{
				LocalConnection.SendMessage("You don't see a " + inputObject + " here.\r\n");
			}
		}

		private void PrintExits(bool verbose = true)
		{
			List<(string, int, string)> exits = Game.Instance.GetRoomExits(CurrentRoomId);

			if (exits.Count == 0)
			{
				LocalConnection.SendMessage("You see no obvious exits.\r\n");
			}
			else if (verbose)
			{
				LocalConnection.SendMessage("Obvious exits are:\r\n");
				foreach ((string, int, string) exit in exits)
				{
					string direction = char.ToUpper(exit.Item1[0]) + exit.Item1[1..];
					LocalConnection.SendMessage("     " + direction + " leads to " + exit.Item3 + ".\r\n");
				}
			}
			else
			{
				List<string> exitDirections = new();
				for (int i = 0; i < exits.Count; ++i)
				{
					exitDirections.Add(exits[i].Item1);
				}
				if (exitDirections.Count == 0)
				{
					LocalConnection.SendMessage("There are no obvious exits.\r\n");
				}
				else if (exitDirections.Count == 1)
				{
					LocalConnection.SendMessage("The obvious exit is " + exitDirections[0] + ".\r\n");
				}
				else
				{
					LocalConnection.SendMessage("Obvious exits are " + Utilities.GetPrettyList(exitDirections) + ".\r\n");
				}
			}
		}

		private void PrintMap()
		{
			LocalConnection.SendMessage("Here is what you can see of your surroundings:\r\n");
			Map map = new(CurrentRoomId);
			List<string> mapString = map.Print(CurrentRoomId);
			foreach (string mapRow in mapString)
			{
				LocalConnection.SendMessage(mapRow);
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

		private void Say(string inputArgument)
		{
			LocalConnection.SendMessage("You say, \"" + inputArgument + "\"\r\n");
			Game.Instance.ReportPlayerSaid(this, inputArgument);
		}

		private void Emote(string inputArgument)
		{
			LocalConnection.SendMessage("You " + inputArgument + ".\r\n");
			Game.Instance.ReportPlayerEmoted(this, inputArgument);
		}

		private void Read(string inputObject)
		{
			// Try looking in your inventory first.
			bool wasInInventory = true;
			GameObject gameObject = GetBestMatch(inputObject, Inventory);
			if (gameObject == null)
			{
				// Then try looking in the world.
				wasInInventory = false;
				gameObject = Game.Instance.GetGameObject(inputObject, CurrentRoomId);
			}

			if (gameObject != null)
			{
				Item item = gameObject as Item;
				if (item != null && item.Read != null)
				{
					string message = "";
					if (wasInInventory)
					{
						message += "(in your inventory) ";
					}
					message += "The " + item.Name + " reads:" + "\r\n";
					LocalConnection.SendMessage(message);
					LocalConnection.SendMessage("\"" + item.Read + "\"\r\n");
				}
				else
				{
					LocalConnection.SendMessage("You can't read that.\r\n");
				}
			}
			else
			{
				LocalConnection.SendMessage("You don't see a " + inputObject + " here.\r\n");
			}
		}

		private void Talk(string inputObject)
		{
			NPC npc = Game.Instance.GetGameObject(inputObject, CurrentRoomId) as NPC;
			if (npc != null && npc.Talk != null)
			{
				LocalConnection.SendMessage(Utilities.Capitalize(npc.Name) + " says: \"" + npc.Talk + "\"\r\n");
			}
			else
			{
				LocalConnection.SendMessage("You can't talk to that.\r\n");
			}
		}

		private void List()
		{
			NPC vendor = Game.Instance.GetVendorInRoom(CurrentRoomId);
			if (vendor != null && vendor.VendorList != null)
			{
				if (vendor.VendorList.Count > 0)
				{
					LocalConnection.SendMessage("They have for sale:\r\n");
					foreach (Item item in vendor.VendorList)
					{
						LocalConnection.SendMessage("     " + Utilities.Capitalize(item.Name) + " for " +
							item.Cost + " " + Game.Instance.Currency + "\r\n");
					}
				}
				else
				{
					LocalConnection.SendMessage("They have nothing for sale.\r\n");
				}
			}
			else
			{
				LocalConnection.SendMessage("There's no shopkeeper here.\r\n");
			}
		}

		private void Browse(string inputObject)
		{
			NPC vendor = Game.Instance.GetVendorInRoom(CurrentRoomId);
			if (vendor != null && vendor.VendorList != null)
			{
				Item itemToBuy = GameObject.GetBestMatch(inputObject, vendor.VendorList);
				if (itemToBuy != null)
				{
					if (itemToBuy.Cost > 0)
					{
						LocalConnection.SendMessage("The " + itemToBuy.Name + " costs " +
							itemToBuy.Cost + " " + Game.Instance.Currency + ".\r\n");
						LocalConnection.SendMessage(itemToBuy.Describe());
					}
					else
					{
						LocalConnection.SendMessage("That's not for sale.\r\n");
					}
				}
				else
				{
					LocalConnection.SendMessage("You can't buy that.\r\n");
				}
			}
			else
			{
				LocalConnection.SendMessage("There's no shopkeeper here.\r\n");
			}
		}

		private void Buy(string inputObject)
		{
			NPC vendor = Game.Instance.GetVendorInRoom(CurrentRoomId);
			if (vendor != null && vendor.VendorList != null)
			{
				Item itemToBuy = GameObject.GetBestMatch(inputObject, vendor.VendorList);
				if (itemToBuy != null)
				{
					if (itemToBuy.Cost > 0)
					{
						if (Gold >= itemToBuy.Cost)
						{
							Item newItem = GameObject.Clone<Item>(itemToBuy);
							Inventory.Add(newItem);
							Gold -= itemToBuy.Cost;
							LocalConnection.SendMessage("You buy " + itemToBuy.Name + " for " +
								itemToBuy.Cost + " " + Game.Instance.Currency + "\r\n");

						}
						else
						{
							LocalConnection.SendMessage("You can't afford that.\r\n");
						}
					}
					else
					{
						LocalConnection.SendMessage("That's not for sale.\r\n");
					}
				}
				else
				{
					LocalConnection.SendMessage("You can't buy that.\r\n");
				}
			}
			else
			{
				LocalConnection.SendMessage("There's no shopkeeper here.\r\n");
			}
		}

		private void Sell(string inputObject)
		{
			NPC vendor = Game.Instance.GetVendorInRoom(CurrentRoomId);
			if (vendor != null && vendor.VendorList != null)
			{
				Item itemToSell = GetBestMatch(inputObject, Inventory);
				if (itemToSell != null)
				{
					if (itemToSell.Cost > 0)
					{
						Gold += itemToSell.Cost / 2;
						Inventory.Remove(itemToSell);
						LocalConnection.SendMessage("You sell " + itemToSell.Name + " for " +
							itemToSell.Cost / 2 + " " + Game.Instance.Currency + "\r\n");
					}
					else
					{
						LocalConnection.SendMessage("You can't sell that.\r\n");
					}
				}
				else
				{
					LocalConnection.SendMessage("You don't have that.\r\n");
				}
			}
			else
			{
				LocalConnection.SendMessage("There's no shopkeeper here.\r\n");
			}
		}

		private void PrintStats()
		{
			LocalConnection.SendMessage("You are level " + Level + " with " + XP + " experience points.\r\n");
			if (Game.Instance.GetXPForLevel(Level + 1) > 0)
			{
				LocalConnection.SendMessage("You need " + (Game.Instance.GetXPForLevel(Level + 1) - XP) +
					" experience points for the next level.\r\n");
			}
			LocalConnection.SendMessage("You have " + CurrentHP + " out of " + MaxHP + " hit points.\r\n");
			LocalConnection.SendMessage("You are " + Utilities.IndefiniteName(Utilities.Lowercase(Class)) + ".\r\n");
			LocalConnection.SendMessage("Your strength is " + Strength + ".\r\n");
			LocalConnection.SendMessage("Your defense is " + Defense + ".\r\n");
			LocalConnection.SendMessage("Your agility is " + Agility + ".\r\n");
		}

		private void PrintInventory()
		{
			if (Equipment.Count == 0)
			{
				LocalConnection.SendMessage("You have nothing equipped.\r\n");
			}
			else
			{
				LocalConnection.SendMessage("You have equipped:\r\n");
				foreach (Item item in Equipment)
				{
					LocalConnection.SendMessage("     " + Utilities.Capitalize(item.Name) + "\r\n");
				}
			}

			if (Inventory.Count == 0 && Gold == 0)
			{
				LocalConnection.SendMessage("You're not carrying anything.\r\n");
			}
			else
			{
				LocalConnection.SendMessage("You are carrying:\r\n");
				foreach (Item item in Inventory)
				{
					LocalConnection.SendMessage("     " + Utilities.Capitalize(item.Name) + "\r\n");
				}
				if (Gold > 0)
				{
					LocalConnection.SendMessage("     " + Gold + " " + Game.Instance.Currency + "\r\n");
				}
			}
		}

		private void Take(string inputObject)
		{
			Item item = Game.Instance.TryTake(this, inputObject);
			if (item != null)
			{
				Inventory.Add(item);
				LocalConnection.SendMessage("You take " + item.Description + ".\r\n");
			}
			else
			{
				LocalConnection.SendMessage("You can't take that.\r\n");
			}
		}

		private void Drop(string inputObject)
		{
			Item item = GetBestMatch(inputObject, Inventory);
			if (item != null)
			{
				Game.Instance.TryDrop(this, item);
				Inventory.Remove(item);
				LocalConnection.SendMessage("You drop " + item.Description + ".\r\n");
			}
			else
			{
				LocalConnection.SendMessage("You don't have that.\r\n");
			}
		}

		private void Eat(string inputObject)
		{
			Item item = GetBestMatch(inputObject, Inventory);
			if (item != null && item.CanEat)
			{
				CurrentHP += item.HPRestore;
				if (CurrentHP > MaxHP)
				{
					CurrentHP = MaxHP;
				}
				Inventory.Remove(item);
				LocalConnection.SendMessage("You eat the " + item.Name + ".\r\n");
			}
			else if (item != null)
			{
				LocalConnection.SendMessage("You can't eat that.\r\n");
			}
			else
			{
				LocalConnection.SendMessage("You don't have that.\r\n");
			}
		}

		private void Drink(string inputObject)
		{
			Item item = GetBestMatch(inputObject, Inventory);
			if (item != null && item.CanDrink)
			{
				CurrentHP += item.HPRestore;
				if (CurrentHP > MaxHP)
				{
					CurrentHP = MaxHP;
				}
				Inventory.Remove(item);
				LocalConnection.SendMessage("You drink the " + item.Name + ".\r\n");
			}
			else if (item != null)
			{
				LocalConnection.SendMessage("You can't drink that.\r\n");
			}
			else
			{
				LocalConnection.SendMessage("You don't have that.\r\n");
			}
		}

		// Returns an equipped item of the same category (e.g., armor or weapons).
		private Item FindEquippedItemLike(Item newItem)
		{
			Item previousItem = null;

			foreach (Item item in Equipment)
			{
				if (newItem.IsArmor && item.IsArmor)
				{
					previousItem = item;
					break;
				}
				else if (newItem.IsWeapon && item.IsWeapon)
				{
					previousItem = item;
					break;
				}
			}

			return previousItem;
		}

		private void Equip(string inputObject)
		{
			Item item = GetBestMatch(inputObject, Inventory);
			if (item != null && item.CanEquip)
			{
				Item previousItem = FindEquippedItemLike(item);
				if (previousItem != null)
				{
					Inventory.Add(previousItem);
					Equipment.Remove(previousItem);
					LocalConnection.SendMessage("You unequip the " + previousItem.Name + ".\r\n");
				}
				Equipment.Add(item);
				Inventory.Remove(item);
				LocalConnection.SendMessage("You equip the " + item.Name + ".\r\n");
			}
			else if (item != null)
			{
				LocalConnection.SendMessage("You can't equip that.\r\n");
			}
			else if (GetBestMatch(inputObject, Equipment) != null)
			{
				LocalConnection.SendMessage("That's already equipped.\r\n");
			}
			else
			{
				LocalConnection.SendMessage("You don't have that.\r\n");
			}
		}

		private void Unequip(string inputObject)
		{
			Item item = GetBestMatch(inputObject, Equipment);
			if (item != null)
			{
				Inventory.Add(item);
				Equipment.Remove(item);
				LocalConnection.SendMessage("You unequip the " + item.Name + ".\r\n");
			}
			else if (GetBestMatch(inputObject, Inventory) != null)
			{
				LocalConnection.SendMessage("That's not equipped yet.\r\n");
			}
			else
			{
				LocalConnection.SendMessage("You don't have that.\r\n");
			}
		}

		// Start attacking a specified target.
		private bool Attack(string inputObject)
		{
			bool needToPrintRoom = true;

			if (inputObject != null)
			{
				GameObject target = Game.Instance.GetGameObject(inputObject, CurrentRoomId);
				if (target != null && target is Fighter)
				{
					Target = target as Fighter;
					LastAttackTime = DateTime.MinValue;

					LocalConnection.SendMessage("You start attacking the " + target.Name + "!\r\n");
					ServerInfo.Instance.Report(
						ColorCodes.Color.Yellow,
						"[Player] Player " + DebugName() + " is attacking " + target.DebugName() + ".\n");

					// Now that combat has started, no need to print the room right away.
					needToPrintRoom = false;
				}
				else
				{
					LocalConnection.SendMessage("You can't attack that.\r\n");
				}
			}
			else
			{
				LocalConnection.SendMessage("What do you want to attack?\r\n");
			}

			return needToPrintRoom;
		}

		// Stop attacking.
		private bool Yield()
		{
			bool needToPrintRoom = true;

			if (Target != null)
			{
				LocalConnection.SendMessage("You stop attacking the " + Target.Name + "!\r\n");
				Target = null;
				ServerInfo.Instance.Report(
					ColorCodes.Color.Yellow,
					"[Player] Player " + DebugName() + " is yielding.\n");
				// Now that combat has ended, no need to print the room right away.
				needToPrintRoom = false;
			}
			else
			{
				LocalConnection.SendMessage("You aren't attacking anything.\r\n");
			}

			return needToPrintRoom;
		}

		private void Consider(string inputObject)
		{
			Fighter target = Game.Instance.GetGameObject(inputObject, CurrentRoomId) as Fighter;
			if (target != null)
			{
				// TODO: Fix this?
				int statDifference =
					target.Strength - Strength +
					target.Defense - Defense +
					target.Agility - Agility;
				if (statDifference < -10)
				{
					LocalConnection.SendMessage("This should be easy.\r\n");
				}
				else if (statDifference > 10)
				{
					LocalConnection.SendMessage("This could be challenging.\r\n");
				}
				else
				{
					LocalConnection.SendMessage("This looks like a fair fight.\r\n");
				}
			}
			else
			{
				LocalConnection.SendMessage("You can't consider that.\r\n");
			}
		}

		private void Config(string inputObject)
		{
			if (inputObject == "prompt")
			{
				ConfigLongPrompt = !ConfigLongPrompt;
				LocalConnection.SendMessage("Your prompt is now configured to be " +
					(ConfigLongPrompt ? "longer" : "shorter") + ".\r\n");
			}
			else
			{
				LocalConnection.SendMessage("Just type \"config\" and a setting below.\r\n");
				LocalConnection.SendMessage("     \"prompt\" to switch between a long and short prompt.\r\n");
			}
		}

		private void Debug(string inputObject)
		{
			if (!IsAdmin)
			{
				LocalConnection.SendMessage("Only admins may debug.\r\n");
				return;
			}

			if (inputObject != null)
			{
				string[] words = inputObject.ToLower().Split(' ');
				if (words[0] == "room")
				{
					if (words.Length > 1)
					{
						DebugRoom(words[1]);
					}
					else
					{
						DebugRoom(null);
					}
				}
				else
				{
					DebugGameObject(inputObject);
				}
			}
			else
			{
				LocalConnection.SendMessage("What do you want to debug?\r\n");
			}
		}

		private void DebugRoom(string inputObject)
		{
			Room room = null;
			if (inputObject != null)
			{
				if (int.TryParse(inputObject, out int roomId))
				{
					room = Game.Instance.GetRoom(roomId);
				}
			}
			else
			{
				room = Game.Instance.GetRoom(CurrentRoomId);
			}

			if (room != null)
			{
				foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(room))
				{
					LocalConnection.SendMessage(descriptor.Name + ": " + descriptor.GetValue(room) + "\r\n");
				}
			}
			else
			{
				LocalConnection.SendMessage("That's not a valid Room ID.\r\n");
			}
		}

		private void DebugGameObject(string inputObject)
		{
			// Try looking in your equipment first.
			GameObject gameObject = GetBestMatch(inputObject, Equipment);
			bool wasEquipped = gameObject != null;
			bool wasInInventory = false;
			if (gameObject == null)
			{
				// Then try looking in your inventory.
				gameObject = GetBestMatch(inputObject, Inventory);
				wasInInventory = gameObject != null;
				if (gameObject == null)
				{
					// Then try looking in the world.
					gameObject = Game.Instance.GetGameObject(inputObject, CurrentRoomId);
				}
			}

			if (gameObject != null)
			{
				if (wasEquipped)
				{
					LocalConnection.SendMessage("(equipped) \r\n");
				}
				if (wasInInventory)
				{
					LocalConnection.SendMessage("(in your inventory) \r\n");
				}
				foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(gameObject))
				{
					LocalConnection.SendMessage(descriptor.Name + ": " + descriptor.GetValue(gameObject) + "\r\n");
				}
			}
			else
			{
				LocalConnection.SendMessage("You don't see a " + inputObject + " here.\r\n");
			}
		}

		private void Teleport(string inputObject)
		{
			if (!IsAdmin)
			{
				LocalConnection.SendMessage("Only admins may teleport.\r\n");
				return;
			}

			if (int.TryParse(inputObject, out int newRoomId))
			{
				if (Game.Instance.RoomExists(newRoomId))
				{
					Game.Instance.ReportPlayerTeleported(this, CurrentRoomId, newRoomId);
					CurrentRoomId = newRoomId;
					DescriptionNeeded = true;
				}
				else
				{
					LocalConnection.SendMessage("That's not a valid Room ID.\r\n");
				}
			}
			else
			{
				LocalConnection.SendMessage("That's not a valid Room ID.\r\n");
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
				LocalConnection.SendMessage("Only admins may shutdown the server.\r\n");
			}
		}

		private void TryExit(string inputVerb)
		{
			bool didExit = false;

			int? newRoomId = Game.Instance.RoomContainsExit(CurrentRoomId, inputVerb);
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
				LocalConnection.SendMessage("You can't " + inputVerb + " here.\r\n");
			}

			DescriptionNeeded = didExit;
		}
	}
}
