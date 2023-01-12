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
        public int StartingRoomId { get; set; }
        public string PlayersFilename { get; set; }
		public string RoomsFilename { get; set; }
		public string WorldObjectsFilename { get; set; }

        // All the players of the game.
        private List<Player> Players;
        // All the rooms.
        private List<Room> Rooms;
        // All the current game objects in the world.
        private List<GameObject> WorldObjects;

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
            StartingRoomId = 0;
            Players = new List<Player>();
            Rooms = new List<Room>();
			WorldObjects = new List<GameObject>();

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
            string jsonString = JsonSerializer.Serialize<List<Player>>(Players);
            File.WriteAllText(PlayersFilename, jsonString);

            // TODO: Save rooms too?

            jsonString = JsonSerializer.Serialize<List<GameObject>>(WorldObjects);
            File.WriteAllText(WorldObjectsFilename, jsonString);
		}

        private static void OnTimedSaveEvent(object source, ElapsedEventArgs e)
        {
            Game.Instance.Save();
        }

        public Player CreatePlayer(string name, string password, string salt)
        {
            Player newPlayer = new Player(name, StartingRoomId, password, salt);
            Players.Add(newPlayer);
            return newPlayer;
        }

		public bool PlayerCanJoin(string name)
        {
            // If a player already has a connection, they can't join again.
            Player player = GetPlayer(name);
            return player == null || !player.HasConnection();
        }

        public bool PlayerExists(string name)
        {
            return GetPlayer(name) != null;
        }

        public void PlayerJoined(Player player)
        {
            ReportPlayerMoved(player, -1, player.CurrentRoomId);
            ServerInfo.Instance.Report("[Game] Player \"" + player.Name + "\" has joined.\n");
        }

        public void PlayerQuit(Player player)
        {
            ReportPlayerMoved(player, player.CurrentRoomId, -1);
            ServerInfo.Instance.Report("[Game] Player \"" + player.Name + "\" has quit.\n");
        }

        public bool PlayerIsAdmin(string name)
        {
            Player player = GetPlayer(name);
            return player != null && player.IsAdmin;
        }

        public GameObject GetGameObject(string gameObjectName, int roomId)
        {
            GameObject gameObject = null;

            List<GameObject> gameObjectsInRoom = new List<GameObject>();
            gameObjectsInRoom.AddRange(GetPlayersInRoom(roomId));
            gameObjectsInRoom.AddRange(GetWorldObjectsInRoom(roomId));
            gameObject = GameObject.GetBestMatch(gameObjectName, gameObjectsInRoom);

            return gameObject;
		}

        public void TrySpawn(GameObject gameObject)
        {
            if (GetGameObject(gameObject.Name, gameObject.CurrentRoomId) == null)
            {
                WorldObjects.Add(gameObject);

                // Report the object was spawned.
				foreach (Player player in Players)
                {
                    if (player.CurrentRoomId == gameObject.CurrentRoomId)
                    {
						player.Message(gameObject.CapitalizeName() + " has appeared.\r\n");
					}
				}
			}
		}

        public Player GetPlayer(string playerName)
        {
            Player toReturn = null;

			foreach (Player player in Players)
			{
				if (player.Name.ToLower() == playerName.ToLower())
				{
					toReturn = player;
				}
			}

            return toReturn;
		}

        public int GetPlayerCount()
        {
            int toReturn = 0;

            foreach (Player player in Players)
            {
                if (player.HasConnection())
                {
                    toReturn++;
                }
            }

            return toReturn;
        }

        public List<string> GetPlayerNames()
        {
            List<string> toReturn = new();

            foreach (Player player in Players)
            {
                if (player.HasConnection())
                {
                    toReturn.Add(player.Name);
                }
            }

            return toReturn;
		}

        public List<Player> GetPlayersInRoom(int roomId)
        {
            List<Player> toReturn = new();

			foreach (Player player in Players)
            {
                if (player.HasConnection() && player.CurrentRoomId == roomId && !player.Invisible)
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

        public List<GameObject> GetWorldObjectsInRoom(int roomId)
        {
            List<GameObject> worldObjects = new List<GameObject>();

			foreach (GameObject worldObject in WorldObjects)
            {
				if (worldObject.CurrentRoomId == roomId && !worldObject.Invisible)
				{
                    worldObjects.Add(worldObject);
				}
			}

            return worldObjects;
        }

        public void ReportPlayerMoved(Player player, int fromRoomId, int toRoomId)
        {
            foreach (Player otherPlayer in Players)
            {
                if (otherPlayer == player || !otherPlayer.HasConnection())
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
            foreach (Player otherPlayer in Players)
            {
                if (otherPlayer == player || !otherPlayer.HasConnection())
                {
                    continue;
                }

                if (otherPlayer.CurrentRoomId == player.CurrentRoomId)
                {
                    otherPlayer.Message(player.Name + " says, \"" + speech + "\"\r\n");
                }
            }
        }

        public void ReportPlayerEmoted(Player player, string action)
        {
            foreach (Player otherPlayer in Players)
            {
                if (otherPlayer == player || !otherPlayer.HasConnection())
                {
                    continue;
                }

                if (otherPlayer.CurrentRoomId == player.CurrentRoomId)
                {
                    otherPlayer.Message(player.Name + " " + action + ".\r\n");
                }
            }
        }

        public void ReportMobileMoved(Mobile mobile, int fromRoomId, int toRoomId)
        {
			foreach (Player otherPlayer in Players)
            {
				if (!otherPlayer.HasConnection())
				{
					continue;
				}

				if (otherPlayer.CurrentRoomId == fromRoomId)
                {
					otherPlayer.Message(mobile.CapitalizeName() + " has left.\r\n");
				}
				else if (otherPlayer.CurrentRoomId == toRoomId)
                {
					otherPlayer.Message(mobile.CapitalizeName() + " has arrived.\r\n");
				}
			}
		}

		public void ReportNPCEmoted(NPC npc, string action)
        {
            foreach (Player otherPlayer in Players)
            {
                if (!otherPlayer.HasConnection())
                {
                    continue;
                }

                if (otherPlayer.CurrentRoomId == npc.CurrentRoomId)
                {
                    otherPlayer.Message(npc.CapitalizeName() + " " + action + ".\r\n");
                }
            }
        }

        public void ReportNPCSaid(NPC npc, string speech)
        {
			foreach (Player otherPlayer in Players)
            {
				if (!otherPlayer.HasConnection())
                {
                    continue;
                }
                if (otherPlayer.CurrentRoomId == npc.CurrentRoomId)
                {
					otherPlayer.Message(npc.CapitalizeName() + " says, \"" + speech + "\"\r\n");
				}
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
				foreach (Player otherPlayer in Players)
				{
					if (otherPlayer == player || !otherPlayer.HasConnection())
					{
						continue;
					}

					if (otherPlayer.CurrentRoomId == player.CurrentRoomId)
                    {
						otherPlayer.Message(player.Name + " took " + toReturn.Description + ".\r\n");
					}
				}
			}

			return toReturn;
        }

        public void TryDrop(Player player, GameObject gameObject)
        {
            gameObject.CurrentRoomId = player.CurrentRoomId;
            WorldObjects.Add(gameObject);

            // Report the object was dropped.
			foreach (Player otherPlayer in Players)
			{
				if (otherPlayer == player || !otherPlayer.HasConnection())
				{
					continue;
				}

				if (otherPlayer.CurrentRoomId == player.CurrentRoomId)
				{
					otherPlayer.Message(player.Name + " dropped " + gameObject.Description + ".\r\n");
				}
			}
		}
	}
}
