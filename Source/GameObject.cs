namespace Aurora
{
	internal class GameObject
	{
		public string Name { get; set; } = "nothing";
		public int CurrentRoomId { get; set; } = 0;
		public string Description { get; set; } = "nothing";

		public string CapitalizeName()
		{
			return char.ToUpper(Name[0]) + Name.Substring(1);
		}
	}
}
