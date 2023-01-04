using System;
using System.Collections.Generic;
using System.Timers;

namespace Aurora
{
	internal struct Behavior
	{
		public int Chance { get; set; } = 0;
		public string Action { get; set; } = "";

		public Behavior() { }
	}

	internal class MobileObject : GameObject
	{
		public List<Behavior> Logic { get; set; } = null;

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

			// Optionally, derived classes can simply handle the Behaviors found in our Logic.
			if (Logic != null && Logic.Count > 0)
			{
				// Generate a random number between 0 and the total of all chances.
				int totalOdds = 0;
				foreach (var action in Logic)
				{
					totalOdds += action.Chance;
				}
				Random rng = new Random();
				int randomBehavior = rng.Next(0, totalOdds + 1);
				// Choose a Behavior at random.
				foreach (var behavior in Logic)
				{
					if (randomBehavior < behavior.Chance)
					{
						Do(behavior.Action);
						break;
					}
					else
					{
						randomBehavior -= behavior.Chance;
					}
				}
			}

			// Re-apply any changed think time.
			ThinkTimer.Interval = ThinkTime;
		}

		protected virtual void Do(string action)
		{
			// Let derived classes decide what to do.
		}
	}
}
