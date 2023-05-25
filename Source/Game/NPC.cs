using System.Collections.Generic;

namespace Aurora
{
	// NPCs are more complex Mobiles who can talk and have custom, built-in responses hard-coded
	// here.
	internal class NPC : Mobile
	{
		public string Talk { get; set; }
		public List<Item> VendorList { get; set; }

		protected override void Do(string action)
		{
			if (action == "Say Hello")
			{
				Game.Instance.ReportNPCSaid(this, "Hello, there!");
			}
			else if (action == "Say News")
			{
				Game.Instance.ReportNPCSaid(this, "Have you visited The Fat Baker yet?");
			}
			else if (action == "Stretch")
			{
				Game.Instance.ReportNPCEmoted(this, "stretches, with her arms raised towards the ceiling");
			}
		}
	}
}
