using LiteDB;

namespace Aurora
{
	internal class GameObject
	{
		[BsonId]
		public ObjectId _id { get; set; }
		public string Name { get; set; } = "nothing";
		[BsonField("current_room_id")]
		public int CurrentRoomId { get; set; } = 0;
		public string Description { get; set; } = "nothing";

		public GameObject()
		{
			_id = ObjectId.NewObjectId();
		}

		public string CapitalizeName()
		{
			return char.ToUpper(Name[0]) + Name.Substring(1);
		}
	}
}
