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

	internal class Mobile : GameObject
	{
		public List<int> RoomList { get; set; } = null;
		public List<Behavior> Logic { get; set; } = null;

		public double ThinkTime { get; set; } = 1000;      // 1 second

		protected Timer ThinkTimer = null;

		public Mobile()
		{
			// All mobiles, by default, can't be taken by players.
			Heavy = true;
		}

		public override void Spawn()
		{
			base.Spawn();

			// Take time to think every ThinkTime seconds.
			ThinkTimer = new Timer(ThinkTime);
			ThinkTimer.Elapsed += (source, e) => OnTimedThinkEvent(source, e, this);
			ThinkTimer.Start();
		}

		public override void Despawn()
		{
			base.Despawn();

			ThinkTimer.Stop();
			ThinkTimer.Dispose();
		}

		private static void OnTimedThinkEvent(object source, ElapsedEventArgs e, Mobile mobile)
		{
			mobile.Think(e.SignalTime);
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
						// We'll handle any Move actions here.
						if (behavior.Action == "Move" && RoomList.Count > 0)
						{
							List<(string, int, string)> exits = Game.Instance.GetRoomExits(CurrentRoomId);
							List<int> validExits = new();
							foreach ((string, int, string) exit in exits)
							{
								if (RoomList.Contains(exit.Item2))
								{
									validExits.Add(exit.Item2);
								}
							}
							if (validExits.Count > 0)
							{
								int randomExit = rng.Next(0, validExits.Count);
								Game.Instance.ReportMobileMoved(this, CurrentRoomId, validExits[randomExit]);
								CurrentRoomId = validExits[randomExit];
							}
						}

						// Derived classes can implement their own actions.
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
