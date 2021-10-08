using System.Collections.Generic;

namespace Aurora
{
    internal class Game
    {
        public string Name { get; private set; }
        public long StartingRoomId { get; private set; }

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
            List<List<object>> infoTableValues = Database.Instance.ReadTable("info");
            if (infoTableValues.Count > 0)
            {
                Name = (string)infoTableValues[0][0];
                StartingRoomId = (long)infoTableValues[0][1];
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

        public static string GetRoomName(long roomId)
        {
            string roomName = "Unknown Room";

            List<List<object>> roomsTableValues = Database.Instance.ReadTable("rooms", "room_id", roomId);
            if (roomsTableValues.Count > 0)
            {
                roomName = (string)roomsTableValues[0][1];
            }

            return roomName;
        }

        public static string GetRoomDescription(long roomId)
        {
            string roomDescription = "Unknown Description";

            List<List<object>> roomsTableValues = Database.Instance.ReadTable("rooms", "room_id", roomId);
            if (roomsTableValues.Count > 0)
            {
                roomDescription = (string)roomsTableValues[0][2];
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
        public static List<(string, long, string)> GetRoomExits(long roomId)
        {
            List<(string, long, string)> exits = new List<(string, long, string)>();

            List<List<object>> roomsTableValues = Database.Instance.ReadTable("rooms", "room_id", roomId);
            long exitId = (long)roomsTableValues[0][3];
            List<List<object>> exitsTableValues = Database.Instance.ReadTable("exits", "exit_id", exitId);
            foreach (List<object> exit in exitsTableValues)
            {
                exits.Add(((string)exit[1], (long)exit[2], GetRoomName((long)exit[2])));
            }

            return exits;
        }

        // Returns the room_id of the room in a direction or null if there is no such exit.
        public static long? RoomContainsExit(long roomId, string direction)
        {
            long? roomContainsExit = null;

            List<(string, long, string)> exits = GetRoomExits(roomId);
            foreach ((string, long, string) exit in exits)
            {
                if (exit.Item1 == direction)
                {
                    roomContainsExit = exit.Item2;
                }
            }

            return roomContainsExit;
        }

        public static bool RoomExists(long roomId)
        {
            List<List<object>> roomsTableValues = Database.Instance.ReadTable("rooms", "room_id", roomId);
            return roomsTableValues.Count > 0;
        }

        public void ReportPlayerMoved(Player player, long fromRoomId, long toRoomId)
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
