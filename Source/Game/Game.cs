using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Timers;

namespace Aurora
{
	internal class Game
	{
		// These fields are deserialized from JSON.
		#region JSON-serialized public fields.
		public string Name { get; set; }
		// The title as it should be displayed upon connection (e.g., as ASCII art).
		public string[] Title { get; set; }
		public string Subtitle { get; set; }
		public string PlayersFilename { get; set; }
		public string RoomsFilename { get; set; }
		public string WorldObjectsFilename { get; set; }
		// Where players initially spawn from and respawn to.
		public int StartingRoomId { get; set; }
		// All new players are cloned from this initial player.
		public Player InitialPlayer { get; set; }
		// The amount of experience needed to reach each level.
		public int[] XPNeededPerLevel { get; set; }
		// How much players' stats increase per level.
		public int MaxHPPerLevel { get; set; }
		public int StrengthPerLevel { get; set; }
		public int DefensePerLevel { get; set; }
		public int AgilityPerLevel { get; set; }
		// The name of the currency ("gold", "dollar", etc).
		public string Currency { get; set; }
		public string WelcomeMessage { get; set; }
		#endregion

		// These three lists are all deserialized from individual JSON files.
		private List<Player> Players;
		private List<Room> Rooms;
		private List<GameObject> WorldObjects;

		// This list is just players who are currently active.
		private readonly List<Player> ActivePlayers;

		private const double kSaveTime = 10000;     // 10 seconds
		private readonly Timer SaveTimer = null;

		// Various lock objects.
		private readonly object SaveLock = new();
		private readonly object SpawnLock = new();

		private static Game _instance = null;
		public static Game Instance
		{
			get
			{
				_instance ??= new Game();
				return _instance;
			}
		}

		public Game()
		{
			Name = "Unknown Game";
			Players = new List<Player>();
			Rooms = new List<Room>();
			WorldObjects = new List<GameObject>();
			InitialPlayer = null;

			ActivePlayers = new List<Player>();

			// Automatically save every kSaveTime seconds.
			SaveTimer = new Timer(kSaveTime);
			SaveTimer.Elapsed += OnTimedSaveEvent;
		}

		public static bool Run(string gameFilename)
		{
			bool running = false;

			try
			{
				string jsonString = File.ReadAllText(gameFilename);
				_instance = JsonSerializer.Deserialize<Game>(jsonString);
				_instance.Load();
				running = true;

				ServerInfo.Instance.Report("[Game] Game \"" + _instance.Name + "\" loaded.\n");
				ServerInfo.Instance.RaiseEvent(new ServerInfoGameArgs(true));
			}
			catch (System.Exception exception)
			{
				ServerInfo.Instance.Report("[Game] Game \"" + _instance.Name + "\" failed to load.\n");
				ServerInfo.Instance.Report("[Game] Exception caught by the game: " + exception.Message + "\n");
			}

			return running;
		}

		public void Load()
		{
			string jsonString = File.ReadAllText(PlayersFilename);
			Players = JsonSerializer.Deserialize<List<Player>>(jsonString);

			jsonString = File.ReadAllText(RoomsFilename);
			Rooms = JsonSerializer.Deserialize<List<Room>>(jsonString);

			jsonString = File.ReadAllText(WorldObjectsFilename);
			WorldObjects = JsonSerializer.Deserialize<List<GameObject>>(jsonString);
			foreach (GameObject gameObject in WorldObjects)
			{
				gameObject.Spawn();
			}

			SaveTimer.Start();
		}

		public void Save()
		{
			lock (SaveLock)
			{
				JsonSerializerOptions options = new();
#if DEBUG
				// For the sake of debugging, keep our JSON files readable.
				options.WriteIndented = true;
#endif
				string jsonString = JsonSerializer.Serialize<List<Player>>(Players, options);
				File.WriteAllText(PlayersFilename, jsonString);

				// TODO: Save rooms too?

				jsonString = JsonSerializer.Serialize<List<GameObject>>(WorldObjects, options);
				File.WriteAllText(WorldObjectsFilename, jsonString);
			}
		}

		private static void OnTimedSaveEvent(object source, ElapsedEventArgs e)
		{
			Game.Instance.Save();
		}

		public Player CreatePlayer(string name, string password, string salt)
		{
			Player newPlayer = GameObject.Clone<Player>(InitialPlayer);
			newPlayer.Name = name;
			newPlayer.Password = password;
			newPlayer.Salt = salt;
			newPlayer.Description = "the player " + name;
			newPlayer.CurrentRoomId = StartingRoomId;
			Players.Add(newPlayer);
			return newPlayer;
		}

		public bool PlayerCanJoin(string name)
		{
			// If a player is already active, they can't join again.
			Player player = GetPlayer(name);
			return player == null;
		}

		// Returns whether a player with a matching name exists, active or not.
		public bool PlayerExists(string name)
		{
			foreach (Player player in Players)
			{
				if (player.Name.ToLower() == name.ToLower())
				{
					return true;
				}
			}

			return false;
		}

		public void PlayerJoined(Player player)
		{
			ActivePlayers.Add(player);
			player.Spawn();

			ReportPlayerMoved(player, -1, player.CurrentRoomId);
			ServerInfo.Instance.Report("[Game] Player " + player.DebugName() + " has joined.\n");
		}

		public void PlayerQuit(Player player)
		{
			player.Despawn();
			ActivePlayers.Remove(player);

			ReportPlayerMoved(player, player.CurrentRoomId, -1);
			ServerInfo.Instance.Report("[Game] Player " + player.DebugName() + " has quit.\n");
		}

		public bool PlayerIsActive(Player player)
		{
			return ActivePlayers.Contains(player);
		}

		public bool PlayerIsAdmin(string name)
		{
			Player player = GetPlayer(name);
			return player != null && player.IsAdmin;
		}

		public GameObject GetGameObject(string gameObjectName, int roomId)
		{
			List<GameObject> gameObjectsInRoom = new();
			gameObjectsInRoom.AddRange(GetPlayersInRoom(roomId));
			gameObjectsInRoom.AddRange(GetWorldObjectsInRoom(roomId));
			return GameObject.GetBestMatch(gameObjectName, gameObjectsInRoom);
		}

		// Try to spawn something derived from GameObject as long as nothing with the same name
		// already exists in the provided room.
		public void TrySpawn<T>(T gameObject, int roomId) where T : GameObject
		{
			lock (SpawnLock)
			{
				if (GetGameObject(gameObject.Name, roomId) == null)
				{
					T newGameObject = GameObject.Clone<T>(gameObject);
					WorldObjects.Add(newGameObject);
					newGameObject.CurrentRoomId = roomId;
					newGameObject.Spawn();

					// Report the object was spawned.
					foreach (Player player in ActivePlayers)
					{
						if (player.CurrentRoomId == newGameObject.CurrentRoomId)
						{
							player.Message(Utilities.Capitalize(newGameObject.IndefiniteName()) +
								" has appeared.\r\n");
						}
					}
				}
			}
		}

		// Returns a Player by name, optionally if the player is active or not.
		public Player GetPlayer(string playerName, bool activePlayers = true)
		{
			Player toReturn = null;

			List<Player> players = activePlayers ? ActivePlayers : Players;
			foreach (Player player in players)
			{
				if (player.Name.ToLower() == playerName.ToLower())
				{
					toReturn = player;
				}
			}

			return toReturn;
		}

		// Returns the number of active players.
		public int GetPlayerCount()
		{
			return ActivePlayers.Count;
		}

		// Returns a list of the names of all active players.
		public List<string> GetPlayerNames()
		{
			List<string> toReturn = new();

			foreach (Player player in ActivePlayers)
			{
				toReturn.Add(player.Name);
			}

			return toReturn;
		}

		public List<Player> GetPlayersInRoom(int roomId)
		{
			List<Player> toReturn = new();

			foreach (Player player in ActivePlayers)
			{
				if (player.CurrentRoomId == roomId && !player.Invisible)
				{
					toReturn.Add(player);
				}
			}

			return toReturn;
		}

		public Room GetRoom(int roomId)
		{
			Room toReturn = null;

			foreach (Room room in Rooms)
			{
				if (room.RoomId == roomId)
				{
					toReturn = room;
				}
			}

			return toReturn;
		}

		public string GetRoomName(int roomId)
		{
			string roomName = "Unknown Room";

			Room room = GetRoom(roomId);
			if (room != null)
			{
				roomName = room.Name;
			}

			return roomName;
		}

		public string GetRoomDescription(int roomId)
		{
			string roomDescription = "Unknown Description";

			Room room = GetRoom(roomId);
			if (room != null)
			{
				roomDescription = room.Description;
			}

			return roomDescription;
		}

		public string GetRoomContents(Player player)
		{
			string roomContents = "";

			// List all the players in the room.
			List<Player> playersInRoom = GetPlayersInRoom(player.CurrentRoomId);
			List<string> playerNames = new();
			foreach (Player otherPlayer in playersInRoom)
			{
				if (otherPlayer != player)
				{
					playerNames.Add(otherPlayer.Name);
				}
			}

			// Then all the world objects.
			List<GameObject> worldObjectsInRoom = GetWorldObjectsInRoom(player.CurrentRoomId);
			List<string> worldObjectNames = new();
			foreach (GameObject worldObject in worldObjectsInRoom)
			{
				if (worldObject is Enemy)
				{
					worldObjectNames.Add(worldObject.IndefiniteName());
				}
				else
				{
					worldObjectNames.Add(worldObject.Name);
				}
			}

			if (playerNames.Count > 0)
			{
				roomContents += Utilities.GetPrettyList(playerNames);
				roomContents += playerNames.Count > 1 ? " are here." : " is here.";
				roomContents += worldObjectNames.Count > 0 ? " " : "\r\n";
			}
			if (worldObjectNames.Count > 0)
			{
				roomContents += Utilities.Capitalize(Utilities.GetPrettyList(worldObjectNames));
				roomContents += worldObjectNames.Count > 1 ? " are here.\r\n" : " is here.\r\n";
			}

			return roomContents;
		}

		// Returns a list of the exits in a room, including the direction, room_id, and the
		// adjoining room's name.
		public List<(string, int, string)> GetRoomExits(int roomId)
		{
			List<(string, int, string)> exits = null;

			Room room = GetRoom(roomId);
			if (room != null)
			{
				exits = new();
				foreach (Exit exit in room.Exits)
				{
					exits.Add((exit.Direction, exit.RoomId, GetRoomName(exit.RoomId)));
				}
			}

			return exits;
		}

		// Returns the room_id of the room in a direction or null if there is no such exit.
		public int? RoomContainsExit(int roomId, string direction)
		{
			int? roomContainsExit = null;

			List<(string, int, string)> exits = GetRoomExits(roomId);
			foreach ((string, int, string) exit in exits)
			{
				if (exit.Item1 == direction)
				{
					roomContainsExit = exit.Item2;
				}
			}

			return roomContainsExit;
		}

		public bool RoomExists(int roomId)
		{
			return GetRoom(roomId) != null;
		}

		public List<Player> GetOtherPlayersInRoom(Player player)
		{
			List<Player> otherPlayers = new();

			foreach (Player otherPlayer in ActivePlayers)
			{
				if (otherPlayer != player && otherPlayer.CurrentRoomId == player.CurrentRoomId)
				{
					otherPlayers.Add(otherPlayer);
				}
			}

			return otherPlayers;
		}

		public List<GameObject> GetWorldObjectsInRoom(int roomId)
		{
			List<GameObject> worldObjects = new();

			foreach (GameObject worldObject in WorldObjects)
			{
				if (worldObject.CurrentRoomId == roomId && !worldObject.Invisible)
				{
					worldObjects.Add(worldObject);
				}
			}

			return worldObjects;
		}

		// Return the first NPC with a VendorList.
		public NPC GetVendorInRoom(int roomId)
		{
			NPC vendor = null;

			List <GameObject> worldObjects = GetWorldObjectsInRoom(roomId);
			foreach (GameObject worldObject in worldObjects)
			{
				NPC npc = worldObject as NPC;
				if (npc != null && npc.VendorList != null)
				{
					vendor = npc;
					break;
				}
			}

			return vendor;
		}

		// Return what level an amount of XP would qualify for.
		public int GetLevelForXP(int xp)
		{
			int level = 0;
			foreach (int xpNeeded in XPNeededPerLevel)
			{
				if (xp >= xpNeeded)
				{
					level++;
				}
			}
			return level;
		}

		// Return how much XP is needed for a particular level.
		public int GetXPForLevel(int level)
		{
			int xp = 0;
			if (level <= XPNeededPerLevel.Length)
			{
				xp = XPNeededPerLevel[level - 1];
			}
			return xp;
		}

		public void ReportAttack(Fighter attacker, Fighter defender, bool didHit)
		{
			List<Player> players = GetPlayersInRoom(attacker.CurrentRoomId);
			foreach (Player player in players)
			{
				if (player != attacker && player != defender)
				{
					if (attacker is Player)
					{
						player.Message(attacker.Name + " attacks the " + defender.Name +
							(didHit ? " and hits!" : " and misses!") + "\r\n");
					}
					else
					{
						player.Message("The " + attacker.Name + " attacks " + defender.Name +
							(didHit ? " and hits!" : " and misses!") + "\r\n");
					}
				}
			}
		}

		public void ReportDeath(Fighter attacker, Fighter defender)
		{
			List<Player> players = GetPlayersInRoom(defender.CurrentRoomId);
			foreach (Player player in players)
			{
				if (player != attacker && player != defender)
				{
					if (defender is Player)
					{
						player.Message(defender.Name + " was defeated by the " + attacker.Name + "!\r\n");
					}
					else
					{
						player.Message("The " + defender.Name + " was defeated by " + attacker.Name + "!\r\n");
					}
				}
			}
		}

		public void ReportMobileMoved(Mobile mobile, int fromRoomId, int toRoomId)
		{
			foreach (Player player in ActivePlayers)
			{
				if (player.CurrentRoomId == fromRoomId)
				{
					player.Message(Utilities.Capitalize(mobile.Name) + " has left.\r\n");
				}
				else if (player.CurrentRoomId == toRoomId)
				{
					player.Message(Utilities.Capitalize(mobile.Name) + " has arrived.\r\n");
				}
			}
		}

		public void ReportNPCEmoted(NPC npc, string action)
		{
			foreach (Player player in ActivePlayers)
			{
				if (player.CurrentRoomId == npc.CurrentRoomId)
				{
					player.Message(Utilities.Capitalize(npc.Name) + " " + action + ".\r\n");
				}
			}
		}

		public void ReportNPCSaid(NPC npc, string speech)
		{
			foreach (Player player in ActivePlayers)
			{
				if (player.CurrentRoomId == npc.CurrentRoomId)
				{
					player.Message(Utilities.Capitalize(npc.Name) + " says, \"" + speech + "\"\r\n");
				}
			}
		}

		public void ReportPlayerMoved(Player player, int fromRoomId, int toRoomId)
		{
			foreach (Player otherPlayer in ActivePlayers)
			{
				if (otherPlayer == player)
				{
					continue;
				}

				if (otherPlayer.CurrentRoomId == fromRoomId)
				{
					otherPlayer.Message(player.Name + " has left.\r\n");
				}
				if (otherPlayer.CurrentRoomId == toRoomId)
				{
					otherPlayer.Message(player.Name + " has arrived.\r\n");
				}
			}
		}

		public void ReportPlayerTeleported(Player player, int fromRoomId, int toRoomId)
		{
			foreach (Player otherPlayer in ActivePlayers)
			{
				if (otherPlayer == player)
				{
					continue;
				}

				if (otherPlayer.CurrentRoomId == fromRoomId)
				{
					otherPlayer.Message(player.Name + " disappears in a puff of smoke.\r\n");
				}
				if (otherPlayer.CurrentRoomId == toRoomId)
				{
					otherPlayer.Message(player.Name + " appears in a puff of smoke.\r\n");
				}
			}
		}

		public void ReportPlayerSaid(Player player, string speech)
		{
			List<Player> otherPlayers = GetOtherPlayersInRoom(player);
			foreach (Player otherPlayer in otherPlayers)
			{
				otherPlayer.Message(player.Name + " says, \"" + speech + "\"\r\n");
			}
		}

		public void ReportPlayerEmoted(Player player, string action)
		{
			List<Player> otherPlayers = GetOtherPlayersInRoom(player);
			foreach (Player otherPlayer in otherPlayers)
			{
				otherPlayer.Message(player.Name + " " + action + ".\r\n");
			}
		}

		public Item TryTake(Player player, string gameObjectName)
		{
			Item toReturn;

			List <GameObject> worldObjects = GetWorldObjectsInRoom(player.CurrentRoomId);
			toReturn = GameObject.GetBestMatch(gameObjectName, worldObjects) as Item;

			// Heavy objects can't be taken.
			if (toReturn != null && toReturn.Heavy)
			{
				toReturn = null;
			}

			if (toReturn != null)
			{
				WorldObjects.Remove(toReturn);

				// Report the object was taken.
				List<Player> otherPlayers = GetOtherPlayersInRoom(player);
				foreach (Player otherPlayer in otherPlayers)
				{
					otherPlayer.Message(player.Name + " took " + toReturn.Description + ".\r\n");
				}
			}

			return toReturn;
		}

		public void TryDrop(Player player, GameObject gameObject)
		{
			gameObject.CurrentRoomId = player.CurrentRoomId;
			WorldObjects.Add(gameObject);

			// Report the object was dropped.
			List<Player> otherPlayers = GetOtherPlayersInRoom(player);
			foreach (Player otherPlayer in otherPlayers)
			{
				otherPlayer.Message(player.Name + " dropped " + gameObject.Description + ".\r\n");
			}
		}

		public void EnemyDied(Enemy enemy)
		{
			enemy.Despawn();
			WorldObjects.Remove(enemy);
		}
	}
}
