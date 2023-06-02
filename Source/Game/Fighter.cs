using System;
using System.Collections.Generic;

namespace Aurora
{
	internal class Fighter : Mobile
	{
		// These fields are deserialized from JSON.
		#region JSON-serialized public fields.
		public int Level { get; set; } = 1;
		public int CurrentHP { get; set; } = 10;
		public int MaxHP { get; set; } = 10;
		public int BaseStrength { get; set; } = 1;
		public int BaseDefense { get; set; } = 1;
		public int BaseAgility { get; set; } = 1;
		// TODO: Have a Speed stat too?
		#endregion

		// Derived classes can apply their own modifiers for these stats.
		public virtual int Strength { get => BaseStrength; }
		public virtual int Defense { get => BaseDefense; }
		public virtual int Agility { get => BaseAgility; }

		private static readonly Random Rng = new();
		private readonly List<Fighter> Attackers = new();

		public Fighter() { }

		public override void Spawn()
		{
			base.Spawn();

			CurrentHP = MaxHP;
		}

		// Attempt to attack (hit and damage) a defender.
		protected virtual void Attack(Fighter defender)
		{
			defender.Tag(this);

			float chanceToHit = (0.8f - (defender.Agility - Agility) / 10.0f) * 100.0f;
			bool didHit = Rng.Next(0, 100) <= (int)Math.Round(chanceToHit, 0);
			if (didHit)
			{
				float minDamage = 1 + (Strength - defender.Defense) / 5.0f;
				float maxDamage = 5 + (Strength - defender.Defense) / 5.0f;
				int damage = Rng.Next((int)Math.Round(minDamage, 0), (int)Math.Round(maxDamage, 0));
				if (damage < 0)
				{
					damage = 0;
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
				otherAttacker.NotifyDeath(attacker, this);
			}

			Game.Instance.ReportDeath(attacker, this);
		}

		// This is called on each attacker when a defender dies. attacker is who dealt the kill.
		protected virtual void NotifyDeath(Fighter attacker, Fighter defender) { }

		// Returns an adjusted attack time based on the Agility of the attacker and defender.
		protected double GetAdjustedAttackTime(double attackTime, Fighter defender)
		{
			double adjustedAttackTime = attackTime;
			if (Agility > defender.Agility)
			{
				adjustedAttackTime -= 0.5;
			}
			else if (Agility < defender.Agility)
			{
				adjustedAttackTime += 0.5;
			}
			else
			{
				// Pick a delay between -0.25 and 0.25.
				adjustedAttackTime += Random.Shared.NextDouble() / 2 - 0.25;
			}
			return adjustedAttackTime;
		}
	}
}
