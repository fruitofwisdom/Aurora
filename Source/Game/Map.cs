using System.Collections.Generic;

namespace Aurora
{
	internal class Map
	{
		private class MapRoom
		{
			public int RoomId = -1;

			// Note that the map doesn't handle up/down/in/out.
			public bool NorthExit = false;
			public bool NortheastExit = false;
			public bool EastExit = false;
			public bool SoutheastExit = false;
			public bool SouthExit = false;
			public bool SouthwestExit = false;
			public bool WestExit = false;
			public bool NorthwestExit = false;

			public MapRoom() { }
		}

		private const int kSize = 5;        // width and height in rooms
		private readonly MapRoom[,] Rooms = null;

		public Map(int middleRoomId)
		{
			Rooms = new MapRoom[kSize, kSize];
			for (int x = 0; x < kSize; ++x)
			{
				for (int y = 0; y < kSize; ++y)
				{
					Rooms[x, y] = new MapRoom();
				}
			}

			// Recursively populate the map from the middle.
			PopulateRoom(kSize / 2, kSize / 2, middleRoomId);
		}

		private void PopulateRoom(int mapX, int mapY, int roomId)
		{
			if (Rooms[mapX, mapY].RoomId != -1)
			{
				return;
			}

			Rooms[mapX, mapY].RoomId = roomId;
			List<(string, int, string)> exits = Game.Instance.GetRoomExits(roomId);
			foreach ((string, int, string) exit in exits)
			{
				switch (exit.Item1)
				{
					case "north":
						Rooms[mapX, mapY].NorthExit = true;
						if (mapY > 0)
						{
							PopulateRoom(mapX, mapY - 1, exit.Item2);
						}
						break;
					case "northeast":
						Rooms[mapX, mapY].NortheastExit = true;
						if (mapX < kSize - 1 && mapY > 0)
						{
							PopulateRoom(mapX + 1, mapY - 1, exit.Item2);
						}
						break;
					case "east":
						Rooms[mapX, mapY].EastExit = true;
						if (mapX < kSize - 1)
						{
							PopulateRoom(mapX + 1, mapY, exit.Item2);
						}
						break;
					case "southeast":
						Rooms[mapX, mapY].SoutheastExit = true;
						if (mapX < kSize - 1 && mapY < kSize - 1)
						{
							PopulateRoom(mapX + 1, mapY + 1, exit.Item2);
						}
						break;
					case "south":
						Rooms[mapX, mapY].SouthExit = true;
						if (mapY < kSize - 1)
						{
							PopulateRoom(mapX, mapY + 1, exit.Item2);
						}
						break;
					case "southwest":
						Rooms[mapX, mapY].SouthwestExit = true;
						if (mapX > 0 && mapY < kSize - 1)
						{
							PopulateRoom(mapX - 1, mapY + 1, exit.Item2);
						}
						break;
					case "west":
						Rooms[mapX, mapY].WestExit = true;
						if (mapX > 0)
						{
							PopulateRoom(mapX - 1, mapY, exit.Item2);
						}
						break;
					case "northwest":
						Rooms[mapX, mapY].NorthwestExit = true;
						if (mapX > 0 && mapY > 0)
						{
							PopulateRoom(mapX - 1, mapY - 1, exit.Item2);
						}
						break;
				}
			}
		}

		public List<string> Print(int playerRoomId)
		{
			// Create an array of chars to draw the map into.
			char[,] mapChars = new char[kSize * 4 + 1, kSize * 2 + 1];
			for (int x = 0; x < kSize * 4 + 1; ++x)
			{
				for (int y = 0; y < kSize * 2 + 1; ++y)
				{
					mapChars[x, y] = ' ';
				}
			}

			// For each room and exit, write into the map array.
			for (int mapX = 0; mapX < kSize; ++mapX)
			{
				for (int mapY = 0; mapY < kSize; ++mapY)
				{
					MapRoom room = Rooms[mapX, mapY];
					int x = mapX * 4;
					int y = mapY * 2;
					mapChars[x, y] = room.NorthwestExit ? '\\' : ' ';
					mapChars[x + 2, y] = room.NorthExit ? '|' : ' ';
					mapChars[x + 4, y] = room.NortheastExit ? '/' : ' ';
					mapChars[x, y + 1] = room.WestExit ? '-' : ' ';
					if (room.RoomId != -1)
					{
						mapChars[x + 1, y + 1] = '[';
						mapChars[x + 2, y + 1] = playerRoomId == room.RoomId ? '@' : ' ';
						mapChars[x + 3, y + 1] = ']';
					}
					mapChars[x + 4, y + 1] = room.EastExit ? '-' : ' ';
					mapChars[x, y + 2] = room.SouthwestExit ? '/' : ' ';
					mapChars[x + 2, y + 2] = room.SouthExit ? '|' : ' ';
					mapChars[x + 4, y + 2] = room.SoutheastExit ? '\\' : ' ';
				}
			}

			// Then convert the array of chars back into a list of strings.
			List<string> mapString = new();
			for (int y = 0; y < kSize * 2 + 1; ++y)
			{
				string mapRow = "";
				for (int x = 0; x < kSize * 4 + 1; ++x)
				{
					mapRow += mapChars[x, y];
				}
				mapRow += "\r\n";
				mapString.Add(mapRow);
			}
			return mapString;
		}
	}
}
