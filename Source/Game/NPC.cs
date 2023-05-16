namespace Aurora
{
	// NPCs are simple mobiles who can have built-in responses to being read (such as a sign or book)
	// or talked to (such as a person). Other responses can be hard-coded here.
	internal class NPC : Mobile
	{
		#region JSON-serialized public fields.
		public string Read { get; set; }
		public string Talk { get; set; }
		#endregion

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
