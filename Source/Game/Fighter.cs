namespace Aurora
{
	internal class Fighter : Mobile
	{
		public int Level { get; set; }
		public int CurrentHP { get; set; }
		public int MaxHP { get; set; }
		public int XP { get; set; }
		public int Strength { get; set; }
		public int Defense { get; set; }

		public Fighter()
		{
			Level = 1;
			CurrentHP = 10;
			MaxHP = 10;
			XP = 0;
			Strength = 1;
			Defense = 1;
		}
	}
}
