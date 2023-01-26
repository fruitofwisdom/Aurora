using System.Collections.Generic;

namespace Aurora
{
	internal partial class Player : GameObject
	{
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
				LocalConnection.SendMessage("     \"debugobject\" to print debug information about an object. (admin)\r\n");
				LocalConnection.SendMessage("     \"shutdown\" to shutdown the server. (admin)\r\n");
			}
		}

		private void LookAt(string inputObject)
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
				LocalConnection.SendMessage("You don't see a " + inputObject + " here.\r\n");
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

		private void Say(string inputObject)
		{
			LocalConnection.SendMessage("You say, \"" + inputObject + "\"\r\n");
			Game.Instance.ReportPlayerSaid(this, inputObject);
		}

		private void Emote(string inputObject)
		{
			LocalConnection.SendMessage("You " + inputObject + ".\r\n");
			Game.Instance.ReportPlayerEmoted(this, inputObject);
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

		private void Take(string inputObject)
		{
			GameObject gameObject = Game.Instance.TryTake(this, inputObject);
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

		private void Drop(string inputObject)
		{
			GameObject gameObject = GetBestMatch(inputObject, Inventory);
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

		private void DebugObject(string inputObject)
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
				if (wasInInventory)
				{
					LocalConnection.SendMessage("(in your inventory)\r\n");
				}
				LocalConnection.SendMessage("ObjectId: " + gameObject.ObjectId + "\r\n");
				LocalConnection.SendMessage("Name: \"" + gameObject.Name + "\"\r\n");
				LocalConnection.SendMessage("CurrentRoomId: " + gameObject.CurrentRoomId + "\r\n");
				LocalConnection.SendMessage("Description: \"" + gameObject.Description + "\"\r\n");
				LocalConnection.SendMessage(gameObject.Heavy ? "heavy\r\n" : "not heavy\r\n");
				LocalConnection.SendMessage(gameObject.Invisible ? "invisible\r\n" : "not invisible\r\n");
			}
			else
			{
				LocalConnection.SendMessage("You don't see a " + inputObject + " here.\r\n");
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
				LocalConnection.SendMessage("You can't \"" + inputVerb + "\" here!\r\n");
			}

			DescriptionNeeded = didExit;
		}
	}
}
