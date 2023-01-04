using System;

namespace Aurora
{
	internal class NPC : MobileObject
	{
		private DateTime LastIdleTime;

		protected override void Think(DateTime eventTime)
		{
			if ((eventTime - LastIdleTime).Seconds > 15)
			{
				// Do idle behavior.
				Game.Instance.ReportNPCEmoted(this, "fidgeted slightly");

				LastIdleTime = eventTime;
			}
		}
	}
}
