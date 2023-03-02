using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Timers;

namespace Aurora
{
    internal class Game
    {
        // These fields are deserialized from JSON.
        public string Name { get; set; }
        public string PlayersFilename { get; set; }
		public string RoomsFilename { get; set; }
        public string WorldObjectsFilename { get; set; }
        // Where players initially spawn from and respawn to.
        public int StartingRoomId { get; set; }
        // All new players are cloned from this initial player.
        public Player InitialPlayer { get; set; }
        // The amount of experience needed to reach each level.
        public int[] XpPerLevel { get; set; }

		// These three lists are all deserialized from individual JSON files.
		private List<Player> Players;
        private List<Room> Rooms;
        private List<GameObject> WorldObjects;

        // This list is just players who are currently active.
        private readonly List<Player> ActivePlayers;

        private const double kSaveTime = 10000;     // 10 seconds
        private Timer SaveTimer = null;

        private static Game _instance = null;
        public static Game Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Game();
                }
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
            XpPerLevel = new int[ 9999 ];

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

            SaveTimer.Start();
		}

		public void Save()
        {
#if DEBUG
            var options = new JsonSerializerOptions { WriteIndented = true};
#endif

            string jsonString = JsonSerializer.Serialize<List<Player>>(Players, options);
            File.WriteAllText(PlayersFilename, jsonString);

            // TODO: Save rooms too?

            jsonString = JsonSerializer.Serialize<List<GameObject>>(WorldObjects, options);
            File.WriteAllText(WorldObjectsFilename, jsonString);
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
            ReportPlayerMoved(player, -1, player.CurrentRoomId);
            ServerInfo.Instance.Report("[Game] Player \"" + player.Name + "\" has joined.\n");
        }

        public void PlayerQuit(Player player)
        {
            ActivePlayers.Remove(player);
            ReportPlayerMoved(player, player.CurrentRoomId, -1);
            ServerInfo.Instance.Report("[Game] Player \"" + player.Name + "\" has quit.\n");
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
            GameObject gameObject = null;

            List<GameObject> gameObjectsInRoom = new();
            gameObjectsInRoom.AddRange(GetPlayersInRoom(roomId));
            gameObjectsInRoom.AddRange(GetWorldObjectsInRoom(roomId));
            gameObject = GameObject.GetBestMatch(gameObjectName, gameObjectsInRoom);

            return gameObject;
		}

        public void TrySpawn<T>(T gameObject) where T : GameObject
        {
            if (GetGameObject(gameObject.Name, gameObject.CurrentRoomId) == null)
            {
                T newGameObject = GameObject.Clone<T>(gameObject);
                WorldObjects.Add(newGameObject);

                // Report the object was spawned.
				foreach (Player player in ActivePlayers)
                {
                    if (player.CurrentRoomId == newGameObject.CurrentRoomId)
                    {
						player.Message(newGameObject.CapitalizeName() + " has appeared.\r\n");
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

            // Report all the players in a room first.
            List<Player> playersInRoom = GetPlayersInRoom(player.CurrentRoomId);
            foreach (Player otherPlayer in playersInRoom)
            {
                if (otherPlayer != player)
                {
                    roomContents += otherPlayer.Name + " is here.\r\n";
                }
            }
            // Then all the world objects in a room.
            List<GameObject> worldObjectsInRoom = GetWorldObjectsInRoom(player.CurrentRoomId);
            foreach (GameObject worldObject in worldObjectsInRoom)
            {
                roomContents += worldObject.CapitalizeName() + " is here.\r\n";
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

        public void ReportAttack(Fighter attacker, Fighter defender, bool didHit)
        {
            List<Player> players = GetPlayersInRoom(attacker.CurrentRoomId);
            foreach (Player player in players)
            {
                if (player != attacker && player != defender)
                {
                    player.Message(attacker.Name + " attacks " + defender.Name +
                        (didHit ? " and hits!" : " and misses!") + "\r\n");
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
                    player.Message(defender.Name + " was killed by " + attacker.Name + "!\r\n");
                }
            }
		}

		public void ReportMobileMoved(Mobile mobile, int fromRoomId, int toRoomId)
        {
			foreach (Player player in ActivePlayers)
            {
				if (player.CurrentRoomId == fromRoomId)
                {
					player.Message(mobile.CapitalizeName() + " has left.\r\n");
				}
				else if (player.CurrentRoomId == toRoomId)
                {
					player.Message(mobile.CapitalizeName() + " has arrived.\r\n");
				}
			}
		}

		public void ReportNPCEmoted(NPC npc, string action)
        {
            foreach (Player player in ActivePlayers)
            {
                if (player.CurrentRoomId == npc.CurrentRoomId)
                {
					player.Message(npc.CapitalizeName() + " " + action + ".\r\n");
                }
            }
        }

        public void ReportNPCSaid(NPC npc, string speech)
        {
			foreach (Player player in ActivePlayers)
            {
                if (player.CurrentRoomId == npc.CurrentRoomId)
                {
					player.Message(npc.CapitalizeName() + " says, \"" + speech + "\"\r\n");
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
				else if (otherPlayer.CurrentRoomId == toRoomId)
				{
					otherPlayer.Message(player.Name + " has arrived.\r\n");
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

		public GameObject TryTake(Player player, string gameObjectName)
        {
            GameObject toReturn;

			List <GameObject> worldObjects = GetWorldObjectsInRoom(player.CurrentRoomId);
			toReturn = GameObject.GetBestMatch(gameObjectName, worldObjects);
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
            WorldObjects.Remove(enemy);
        }
	}
}
