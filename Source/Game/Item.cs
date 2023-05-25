namespace Aurora
{
	// An Item is a type of GameObject that can be picked up, consumed, equipped, read, or some other
	// property that makes it distinct from, for example, NPCs and enemies.
	// TODO: Should these be different types of items or some kind of component system?
	internal class Item : GameObject
	{
		// These fields are deserialized from JSON.
		#region JSON-serialized public fields.
		// Consumable properties.
		public int Cost { get; set; }
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
		#endregion

		public string Describe()
		{
			string description = "";

			if (HPRestore > 0)
			{
				description = "It will restore " + HPRestore + " hit points";
			}
			else if (HPRestore < 0)
			{
				description = "It looks poisonous";
			}

			if (StrengthMod > 0)
			{
				if (description == "")
				{
					description = "It will ";
				}
				description += "increase strength by " + StrengthMod;
			}
			if (StrengthMod < 0)
			{
				if (description == "")
				{
					description = "It will ";
				}
				description += "hurt your strength";
			}
			else if (DefenseMod > 0)
			{
				if (description == "")
				{
					description = "It will ";
				}
				else
				{
					description += " and ";
				}
				description += "increase defense by " + DefenseMod;
			}
			else if (DefenseMod < 0)
			{
				if (description == "")
				{
					description = "It will ";
				}
				else
				{
					description += " and ";
				}
				description += "hurt your defense";
			}
			if (AgilityMod > 0)
			{
				if (description == "")
				{
					description = "It will ";
				}
				else
				{
					description += " and ";
				}
				description += "increase agility by " + AgilityMod;
			}
			else if (AgilityMod < 0)
			{
				if (description == "")
				{
					description = "It will ";
				}
				else
				{
					description += " and ";
				}
				description += "hurt your agility";
			}

			if (description != "")
			{
				description += ".\r\n";
			}

			return description;
		}
	}
}
