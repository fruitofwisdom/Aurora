﻿using System;
using System.Collections.Generic;

namespace Aurora
{
	internal partial class Player : Fighter
	{
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
				if (gameObject.Description[^1] == '.')
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
					string direction = char.ToUpper(exit.Item1[0]) + exit.Item1[1..];
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
				// NPCs are types of GameObjects with properties that allow them to be read.
				NPC npcObject = gameObject as NPC;
				if (npcObject != null && npcObject.Read != null)
				{
					string message = "";
					if (wasInInventory)
					{
						message += "(in your inventory) ";
					}
					message += npcObject.CapitalizeName() + " reads:" + "\r\n";
					LocalConnection.SendMessage(message);
					LocalConnection.SendMessage("\"" + npcObject.Read + "\"\r\n");
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
				LocalConnection.SendMessage(npc.CapitalizeName() + " says:\r\n");
				LocalConnection.SendMessage("\"" + npc.Talk + "\"\r\n");
			}
			else
			{
				LocalConnection.SendMessage("You can't talk to that.\r\n");
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
			LocalConnection.SendMessage("Your strength is " + Strength + ".\r\n");
			LocalConnection.SendMessage("Your defense is " + Defense + ".\r\n");
			LocalConnection.SendMessage("Your agility is " + Agility + ".\r\n");
		}

		private void PrintInventory()
		{
			if (Inventory.Count == 0 && Gold == 0)
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
				if (Gold > 0)
				{
					LocalConnection.SendMessage("     " + Gold + " " + Game.Instance.Currency + "\r\n");
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
				LocalConnection.SendMessage("You can't take that.\r\n");
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
				LocalConnection.SendMessage("You don't have that.\r\n");
			}
		}

		// Start attacking a specified target.
		private bool Attack(string inputObject)
		{
			bool needToPrintRoom = true;

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
				int statDifference =
					target.MaxHP - MaxHP +
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
				LocalConnection.SendMessage("Class: " + gameObject.GetType().ToString() + "\r\n");
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
