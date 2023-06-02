using System.Collections.Generic;

namespace Aurora
{
	internal class Exit
	{
		public string Direction { get; set; }
		public int RoomId { get; set; }
	}

	internal class Room
	{
		public int RoomId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public List<Exit> Exits { get; set; }
	}
}
