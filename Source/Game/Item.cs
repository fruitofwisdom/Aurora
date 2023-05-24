namespace Aurora
{
	// An Item is a type of GameObject that can be picked up, consumed, equipped, read, or some other
	// property that makes it distinct from, for example, NPCs and enemies.
	// TODO: Should these be different types of items or some kind of component system?
	internal class Item : GameObject
	{
		// Consumable properties.
		public bool CanDrink { get; set; } = false;
		public bool CanEat { get; set; } = false;
		public int HPRestore { get; set; }

		// Equipment properties.
		public bool CanEquip { get; set; } = false;
		public int StrengthMod { get; set; }
		public int DefenseMod { get; set; }
		public int AgilityMod { get; set; }

		// Other properties.
		public bool Heavy { get; set; } = false;
		public string Read { get; set; }
	}
}
