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
        public string DatabaseFilename { get; set; }
        public string PlayersFilename { get; set; }
        public string WorldObjectsFilename { get; set; }

        // All the players of the game.
        private List<Player> Players;
        // All the current game objects in the world.
        private List<GameObject> WorldObjects;

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
            DatabaseFilename = "unknown.db";
            Players = new List<Player>();
			WorldObjects = new List<GameObject>();

			// Automatically save every 15 seconds.
			SaveTimer = new Timer(15000);
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

            jsonString = File.ReadAllText(WorldObjectsFilename);
            WorldObjects = JsonSerializer.Deserialize<List<GameObject>>(jsonString);

            Database.Instance.Open(DatabaseFilename);

            SaveTimer.Start();
		}

		public void Save()
        {
            string jsonString = JsonSerializer.Serialize<List<Player>>(Players);
            File.WriteAllText(PlayersFilename, jsonString);

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

        public static GameObject GetGameObject(string gameObjectName, int currentRoomId)
        {
            GameObject gameObject = null;

			// Search through all the players in a room first.
			foreach (Player player in Instance.Players)
            {
                if (player.CurrentRoomId == currentRoomId && player.Name.ToLower() == gameObjectName.ToLower())
                {
                    gameObject = player;
				}
			}
			// Then through the other objects in a room.
			foreach (GameObject worldObject in Instance.WorldObjects)
            {
				if (worldObject.CurrentRoomId == currentRoomId && worldObject.Name.ToLower() == gameObjectName.ToLower())
				{
                    gameObject = worldObject;
				}
			}

            return gameObject;
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
                if (player.HasConnection() && player.CurrentRoomId == roomId)
                {
                    toReturn.Add(player);
                }
            }

			return toReturn;
        }

		public static string GetRoomName(int roomId)
        {
            string roomName = "Unknown Room";

            List<Dictionary<string, object>> roomsTable = Database.Instance.ReadTable("rooms", "room_id", roomId);
            if (roomsTable.Count > 0)
            {
                roomName = (string)roomsTable[0]["name"];
            }

            return roomName;
        }

        public static string GetRoomDescription(int roomId)
        {
            string roomDescription = "Unknown Description";

            List<Dictionary<string, object>> roomsTable = Database.Instance.ReadTable("rooms", "room_id", roomId);
            if (roomsTable.Count > 0)
            {
                roomDescription = (string)roomsTable[0]["description"];
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
            // Then all the other objects in a room.
            foreach (GameObject worldObject in WorldObjects)
            {
                if (worldObject.CurrentRoomId == player.CurrentRoomId)
                {
                    roomContents += worldObject.CapitalizeName() + " is here.\r\n";
                }
            }

            return roomContents;
        }

        // Returns a list of the exits in a room, including the direction, room_id, and the
        // adjoining room's name.
        public static List<(string, int, string)> GetRoomExits(int roomId)
        {
            List<(string, int, string)> exits = new();

            List<Dictionary<string, object>> roomsTable = Database.Instance.ReadTable("rooms", "room_id", roomId);
            if (roomsTable.Count > 0)
            {
                List<LiteDB.BsonValue> exitsList = roomsTable[0]["exits"] as List<LiteDB.BsonValue>;
                foreach (LiteDB.BsonDocument exit in exitsList)
                {
                    exits.Add(((string)exit["direction"], (int)exit["room_id"], GetRoomName((int)exit["room_id"])));
                }
            }

            return exits;
        }

        // Returns the room_id of the room in a direction or null if there is no such exit.
        public static int? RoomContainsExit(int roomId, string direction)
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

        public static bool RoomExists(int roomId)
        {
            List<Dictionary<string, object>> roomsTable = Database.Instance.ReadTable("rooms", "room_id", roomId);
            return roomsTable.Count > 0;
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
    }
}
