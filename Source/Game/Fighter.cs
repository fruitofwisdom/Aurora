using System;
using System.Collections.Generic;

namespace Aurora
{
	internal class Fighter : Mobile
	{
		public int Level { get; set; }
		public int CurrentHP { get; set; }
		public int MaxHP { get; set; }
		public int XP { get; set; }
		public int Strength { get; set; }
		public int Defense { get; set; }
		public int Agility { get; set; }
		// TODO: Have a Speed stat too?

		private static readonly Random Rng = new();
		private readonly List<Fighter> Attackers = new();

		public Fighter()
		{
			Level = 1;
			CurrentHP = 10;
			MaxHP = 10;
			XP = 0;
			Strength = 1;
			Defense = 1;
			Agility = 1;
		}

		protected void Attack(Fighter defender)
		{
			ServerInfo.Instance.Report(
				ColorCodes.Color.Yellow,
				"[Fighter] " + Name + "(" + ObjectId + ") is attacking " +
				defender.Name + "(" + defender.ObjectId + ").\n");

			defender.Tag(this);

			int didHit = (Agility + Rng.Next(1, 20)) - (defender.Agility + Rng.Next(1, 20));
			if (didHit > 0)
			{
				int damage = Math.Max((Strength + Rng.Next(1, 20)) - (defender.Defense + Rng.Next(1, 20)), 0);
				this.DealDamage(defender, true, damage);
				defender.TakeDamage(this, true, damage);
				Game.Instance.ReportAttack(this, defender, true);
			}
			else
			{
				this.DealDamage(defender, false);
				defender.TakeDamage(this, false);
				Game.Instance.ReportAttack(this, defender, false);
			}
		}

		// Everyone who attempts to attack us we remember, so we can notify them on death
		private void Tag(Fighter attacker)
		{
			if (!Attackers.Contains(attacker))
			{
				ServerInfo.Instance.Report(
					ColorCodes.Color.Yellow,
					"[Fighter] " + Name + "(" + ObjectId + ") has been tagged by " +
					attacker.Name + "(" + attacker.ObjectId + ").\n");
				Attackers.Add(attacker);
			}
		}

		// This is called when we deal damage to a defender.
		protected virtual void DealDamage(Fighter defender, bool didHit, int damage = 0) { }

		// This is called when we take damage from an attacker.
		protected virtual void TakeDamage(Fighter attacker, bool didHit, int damage = 0)
		{
			if (didHit)
			{
				CurrentHP -= damage;
				if (CurrentHP <= 0)
				{
					Die(attacker);
				}
			}
		}

		// This is called when we die.
		protected virtual void Die(Fighter attacker)
		{
			// Let each of our attackers know that we died.
			foreach (Fighter otherAttacker in Attackers)
			{
				otherAttacker.NotifyDeath(this);
			}

			Game.Instance.ReportDeath(attacker, this);
		}

		// This is called on each attacker when a defender dies.
		protected virtual void NotifyDeath(Fighter defender) { }
	}
}
