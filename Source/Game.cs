using System.Collections.Generic;

namespace Aurora
{
    internal class Game
    {
        public string Name = "Unknown Game";
        public long StartingRoomId = 0;

        public List<Player> Players = new List<Player>();

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

        public static string GetRoomName(Player player)
        {
            string roomName = "Unknown Room";

            List<List<object>> roomsTableValues = Database.Instance.ReadTable("rooms", "room_id", player.CurrentRoomId);
            if (roomsTableValues.Count > 0)
            {
                roomName = (string)roomsTableValues[0][1];
            }

            return roomName;
        }

        public static string GetRoomDescription(Player player)
        {
            string roomDescription = "Unknown Description";

            List<List<object>> roomsTableValues  = Database.Instance.ReadTable("rooms", "room_id", player.CurrentRoomId);
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

        public static long? RoomContainsExit(long roomId, string direction)
        {
            long? roomContainsExit = null;

            List<List<object>> roomsTableValues = Database.Instance.ReadTable("rooms", "room_id", roomId);
            long exitId = (long)roomsTableValues[0][3];
            List<List<object>> exitsTableValues = Database.Instance.ReadTable("exits", "exit_id", exitId);
            foreach (List<object> exit in exitsTableValues)
            {
                if ((string)exit[1] == direction)
                {
                    roomContainsExit = (long)exit[2];
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
    }
}
