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
            List<List<object>> infoTableValues = Database.Instance.ReadTable("Info");
            Name = (string)infoTableValues[0][0];
            StartingRoomId = (long)infoTableValues[0][1];
            ServerInfo.Instance.Report("[Game] Game \"" + Name + "\" loaded.\n");
            ServerInfo.Instance.RaiseEvent(new ServerInfoGameArgs(true));
        }

        public List<string> GetRoomDescription(Player player)
        {
            List<string> roomDescription = new List<string>();

            // TODO: Describe the player's current room. -Ward
            //Database.Instance.ReadTable("Rooms", player.CurrentRoomId);
            roomDescription.Add("Unknown Room");
            roomDescription.Add("You are in an unknown room, a swirling miasma of scintillating thoughts and turgid ideas.");

            // TODO: Describe other players. -Ward

            return roomDescription;
        }
    }
}
