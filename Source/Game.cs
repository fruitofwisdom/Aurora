using System.Collections.Generic;

namespace Aurora
{
    internal class Game
    {
        public string Name = "Unknown Aurora Game";
        public long StartingRoomId = 0;

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

        public static List<string> GetRoomDescription(Player player)
        {
            List<string> roomDescription = new List<string>();

            List<List<object>> roomsTableValues  = Database.Instance.ReadTable("rooms", "room_id", player.CurrentRoomId);
            string name = (string)roomsTableValues[0][1];
            string description = (string)roomsTableValues[0][2];
            roomDescription.Add(name);
            roomDescription.Add(description);

            // TODO: Describe other players. -Ward

            return roomDescription;
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
    }
}
