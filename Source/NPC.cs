using System;

namespace Aurora
{
	internal class NPC : MobileObject
	{
		private DateTime LastIdleTime;

		protected override void Do(string action)
		{
			if (action == "Say Hello")
			{
				Game.Instance.ReportNPCSaid(this, "Hello, there!");
			}
			else if (action == "Stretch")
			{
				Game.Instance.ReportNPCEmoted(this, "stretches, with her arms raised towards the ceiling");
			}
		}
	}
}
