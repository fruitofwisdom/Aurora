using System.Collections.Generic;

namespace Aurora
{
	internal class Ability
	{
		// These fields are deserialized from JSON.
		#region JSON-serialized public fields.
		public string Name { get; set; }
		public string Description { get; set; }
		public int APCost { get; set; }
		#endregion
	}

	internal class Class
	{
		// These fields are deserialized from JSON.
		#region JSON-serialized public fields.
		public string Name { get; set; }
		public List<Ability> Abilities { get; set; }
		#endregion
	}
}
