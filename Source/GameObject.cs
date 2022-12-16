using System.Text.Json.Serialization;

namespace Aurora
{
	// A game object is any object that can exist in the world and be saved and loaded.
	[JsonDerivedType(typeof(GameObject), typeDiscriminator: "GameObject")]
	[JsonDerivedType(typeof(WorldObject), typeDiscriminator: "WorldObject")]
	internal class GameObject
	{
		public string Name { get; set; } = "nothing";
		public int CurrentRoomId { get; set; } = 0;
		public string Description { get; set; } = "nothing";
		public bool Invisible { get; set; } = false;

		public string CapitalizeName()
		{
			return char.ToUpper(Name[0]) + Name.Substring(1);
		}
	}
}
