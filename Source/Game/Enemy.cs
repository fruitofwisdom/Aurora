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

		public int XPReward { get; set; } = 0;
		public int GoldReward { get; set; } = 0;

		private readonly List<Target> Targets = new();
		private const double kAttackTime = 5;      // in seconds
		private DateTime LastAttackTime = DateTime.MinValue;

		private const int kAggroAmount = 3;

		public Enemy() { }

		protected override void Think(DateTime eventTime)
		{
			// Attack more often or not if we are faster than our opponent.
			double attackTime = kAttackTime;
			if (Targets.Count > 0)
			{
				attackTime = GetAdjustedAttackTime(kAttackTime, Targets[0].Attacker);
			}

			double elapsedTime = (eventTime - LastAttackTime).Seconds +
				(eventTime - LastAttackTime).Milliseconds / 1000;
			if (elapsedTime > attackTime)
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
						"[Enemy] " + DebugName() + " is removing " +
						targetToRemove.Attacker.DebugName() + " as a target.\n");
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
			// Whenever we take damage, add a fixed amount of aggro to that target.
			bool newAttacker = true;
			foreach (Target target in Targets)
			{
				if (target.Attacker == attacker)
				{
					newAttacker = false;
					target.Aggro += kAggroAmount;
				}
			}
			if (newAttacker)
			{
				Targets.Add(new Target(attacker as Player, kAggroAmount));
				ServerInfo.Instance.Report(
					ColorCodes.Color.Yellow,
					"[Enemy] " + DebugName() + " is adding " +
					attacker.DebugName() + " as a target.\n");
			}

			base.TakeDamage(attacker, didHit, damage);
		}

		protected override void Die(Fighter attacker)
		{
			ServerInfo.Instance.Report(
				ColorCodes.Color.Yellow,
				"[Enemy] " + DebugName() + " was killed by " + attacker.DebugName() + ".\n");

			base.Die(attacker);

			// Stop any timers and remove this enemy from the world.
			Game.Instance.EnemyDied(this);

			// Distribute XP and gold in proportion to aggro generated per target.
			int totalAggro = 0;
			foreach (Target target in Targets)
			{
				totalAggro += target.Aggro;
			}
			foreach (Target target in Targets)
			{
				double aggroPercent = (double)target.Aggro / totalAggro;
				int sharedXP = (int)Math.Ceiling(XPReward * aggroPercent);
				int sharedGold = (int)Math.Ceiling(GoldReward * aggroPercent);
				target.Attacker.Reward(sharedXP, sharedGold);
			}
			Targets.Clear();
		}

		protected override void NotifyDeath(Fighter attacker, Fighter defender)
		{
			base.NotifyDeath(attacker, defender);

			// If one of our targets died, remove them from the list.
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
