﻿using System;
using System.Collections.Generic;

namespace Aurora
{
	internal class Fighter : Mobile
	{
		public int Level { get; set; } = 1;
		public int CurrentHP { get; set; } = 10;
		public int MaxHP { get; set; } = 10;
		public int XP { get; set; } = 0;
		public int Strength { get; set; } = 1;
		public int Defense { get; set; } = 1;
		public int Agility { get; set; } = 1;
		// TODO: Have a Speed stat too?

		private static readonly Random Rng = new();
		private readonly List<Fighter> Attackers = new();

		public Fighter() { }

		// Attempt to attack (hit and damage) a defender.
		protected void Attack(Fighter defender)
		{
			ServerInfo.Instance.Report(
				ColorCodes.Color.Yellow,
				"[Fighter] " + DebugName() + " is attacking " + defender.DebugName() + ".\n");

			defender.Tag(this);

			// TODO: Better hit calculation?
			int didHit = (Agility + Rng.Next(1, 20)) - (defender.Agility + Rng.Next(1, 20));
			if (didHit > 0)
			{
				// TODO: Better damage calculation?
				int damage = (Strength + Rng.Next(1, 20)) - (defender.Defense + Rng.Next(1, 20));
				// Let's have a minimum amount of damage per hit.
				if (damage <= 0)
				{
					damage = 1;
				}

				// Notify all interested parties that an attack hit.
				this.DealtDamage(defender, true, damage);
				Game.Instance.ReportAttack(this, defender, true);
				defender.TakeDamage(this, true, damage);
			}
			else
			{
				// Notify all interested parties that an attack missed.
				this.DealtDamage(defender, false);
				Game.Instance.ReportAttack(this, defender, false);
				defender.TakeDamage(this, false);
			}
		}

		// Everyone who attempts to attack us we remember, so we can notify them on death
		private void Tag(Fighter attacker)
		{
			if (!Attackers.Contains(attacker))
			{
				ServerInfo.Instance.Report(
					ColorCodes.Color.Yellow,
					"[Fighter] " + DebugName() + " has been tagged by " + attacker.DebugName() + ".\n");
				Attackers.Add(attacker);
			}
		}

		// This is called when we deal damage to a defender.
		protected virtual void DealtDamage(Fighter defender, bool didHit, int damage = 0) { }

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