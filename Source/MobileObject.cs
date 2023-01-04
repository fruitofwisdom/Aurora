using System;
using System.Timers;

namespace Aurora
{
	internal class MobileObject : GameObject
	{
		public double ThinkTime { get; set; } = 1000;      // 1 second

		protected Timer ThinkTimer = null;

		public MobileObject()
		{
			// Take time to think every ThinkTime seconds.
			ThinkTimer = new Timer(ThinkTime);
			ThinkTimer.Elapsed += (source, e) => OnTimedThinkEvent(source, e, this);
			ThinkTimer.Start();
		}

		private static void OnTimedThinkEvent(object source, ElapsedEventArgs e, MobileObject mobileObject)
		{
			mobileObject.Think(e.SignalTime);
		}

		protected virtual void Think(DateTime eventTime)
		{
			// Let derived classes decide what to do.
		}
	}
}
