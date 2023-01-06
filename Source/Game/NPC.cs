namespace Aurora
{
	internal class NPC : Mobile
	{
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
