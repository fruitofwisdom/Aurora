using System;
using System.Collections.Generic;

namespace Aurora
{
	internal class Enemy : Fighter
	{
		// A Target is a particular attacker and how much aggro they've generated.
		internal class Target
		{
			public Player Attacker = null;
			public int Aggro = 0;

			public Target(Player attacker, int aggro)
			{
				Attacker = attacker;
				Aggro = aggro;
			}
		};

		public int XPReward { get; set; }
		public int GoldReward { get; set; }

		private List<Target> Targets = new();
		private double AttackTime = 5;      // in seconds
		private DateTime LastAttackTime = DateTime.MinValue;

		public Enemy()
		{
			XPReward = 10;
			GoldReward = 10;
		}

		protected override void Think(DateTime eventTime)
		{
			if ((eventTime - LastAttackTime).Seconds > AttackTime)
			{
				Target bestTarget = null;
				int highestAggro = 0;
				List<Target> targetsToRemove = new();
				foreach (Target target in Targets)
				{
					// Only try to attack players who are active and in our room.
					if (Game.Instance.PlayerIsActive(target.Attacker) &&
						target.Attacker.CurrentRoomId == CurrentRoomId &&
						target.Aggro > highestAggro)
					{
						bestTarget = target;
						highestAggro = target.Aggro;
					}

					// Aggro diminishes over time until they're no longer a target.
					target.Aggro--;
					if (target.Aggro == 0)
					{
						targetsToRemove.Add(target);
					}
				}
				foreach (Target targetToRemove in targetsToRemove)
				{
					ServerInfo.Instance.Report(
						ColorCodes.Color.Yellow,
						"[Enemy] " + CapitalizeName() + "(" + ObjectId + ") will lose " +
						targetToRemove.Attacker.Name + "(" + targetToRemove.Attacker.ObjectId + ") as a target.\r\n");
					Targets.Remove(targetToRemove);
				}

				if (bestTarget != null)
				{
					Attack(bestTarget.Attacker);
				}

				LastAttackTime = eventTime;
			}
		}

		protected override void TakeDamage(Fighter attacker, bool didHit, int damage)
		{
			bool newAttacker = true;
			foreach (Target target in Targets)
			{
				if (target.Attacker == attacker)
				{
					newAttacker = false;
					target.Aggro += 3;		// TODO: Is this a good number?
				}
				ServerInfo.Instance.Report(
					ColorCodes.Color.Yellow,
					"[Enemy] " + CapitalizeName() + "(" + ObjectId + ") is increasing aggro towards " +
					attacker.Name + "(" + attacker.ObjectId + ").\r\n");
			}
			if (newAttacker)
			{
				Targets.Add(new Target(attacker as Player, 3));
				ServerInfo.Instance.Report(
					ColorCodes.Color.Yellow,
					"[Enemy] " + CapitalizeName() + "(" + ObjectId + ") is adding " +
					attacker.Name + "(" + attacker.ObjectId + ") as a target.\r\n");
			}

			base.TakeDamage(attacker, didHit, damage);
		}

		protected override void Die(Fighter attacker)
		{
			base.Die(attacker);

			ServerInfo.Instance.Report(
				ColorCodes.Color.Yellow,
				"[Enemy] " + CapitalizeName() + "(" + ObjectId + ") was killed by " +
				attacker.Name + "(" + attacker.ObjectId + ").\n");
			// TODO: Divide up XP and gold reward.
			Targets.Clear();
			ThinkTimer.Stop();
			ThinkTimer = null;

			Game.Instance.EnemyDied(this);
		}

		protected override void NotifyDeath(Fighter defender)
		{
			base.NotifyDeath(defender);

			// One of our potential targets died.
			Target toRemove = null;
			foreach (Target target in Targets)
			{
				if (target.Attacker == defender)
				{
					toRemove = target;
				}
			}
			if (toRemove != null)
			{
				Targets.Remove(toRemove);
			}
		}
	}
}
