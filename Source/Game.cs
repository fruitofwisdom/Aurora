using System.Collections.Generic;

namespace Aurora
{
    class Game
    {
		public string Name = "Unknown Aurora Game";
		public string StartingRoom = "Limbo";

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
			Dictionary <string, object> infoTableValues = Database.Instance.ReadTable("Info");
			Name = (string)infoTableValues["Name"];
			StartingRoom = (string)infoTableValues["StartingRoom"];
			ServerInfo.Instance.Report("[Game] Game \"" + Name + "\" loaded.\n");
			ServerInfo.Instance.RaiseEvent(new ServerInfoGameArgs(true));
		}
	}
}
