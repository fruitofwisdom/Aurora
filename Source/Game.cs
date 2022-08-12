using LiteDB;
using System.Collections.Generic;
using System.Windows.Documents;

namespace Aurora
{
    internal class Game
    {
        public string Name { get; private set; }
        public int StartingRoomId { get; private set; }

        public List<Player> Players { get; private set; }

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

        private Game()
        {
            Name = "Unknown Game";
            StartingRoomId = 0;
            Players = new List<Player>();
        }

        public void Load()
        {
            List<Dictionary<string, object>> infoTable = Database.Instance.ReadTable("info");
            if (infoTable.Count > 0)
            {
                Name = (string)infoTable[0]["name"];
                StartingRoomId = (int)infoTable[0]["starting_room_id"];
                ServerInfo.Instance.Report("[Game] Game \"" + Name + "\" loaded.\n");
                ServerInfo.Instance.RaiseEvent(new ServerInfoGameArgs(true));
            }
        }

        public bool PlayerCanJoin(string name)
        {
            bool playerCanJoin = true;

            // If a player has already joined, they can't join again.
            foreach (Player player in Players)
            {
                if (player.Name == name)
                {
                    playerCanJoin = false;
                }
            }

            return playerCanJoin;
        }

        public void PlayerJoined(Player player)
        {
            Players.Add(player);
            Game.Instance.ReportPlayerMoved(player, -1, player.CurrentRoomId);
            ServerInfo.Instance.Report("[Game] Player \"" + player.Name + "\" has joined.\n");
        }

        public void PlayerQuit(Player player)
        {
            if (Players.Contains(player))
            {
                player.Save();
                Players.Remove(player);
                ReportPlayerMoved(player, player.CurrentRoomId, -1);
                ServerInfo.Instance.Report("[Game] Player \"" + player.Name + "\" has quit.\n");
            }
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

        public static string GetRoomContents(Player player)
        {
            string roomContents = "";

            foreach (Player otherPlayer in Instance.Players)
            {
                if (otherPlayer != player && otherPlayer.CurrentRoomId == player.CurrentRoomId)
                {
                    roomContents += otherPlayer.Name + " is here.\r\n";
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
                List<BsonValue> exitsList = roomsTable[0]["exits"] as List<BsonValue>;
                foreach (BsonDocument exit in exitsList)
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
            foreach (Player otherPlayer in Players)
            {
                if (otherPlayer == player)
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
                if (otherPlayer == player)
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
