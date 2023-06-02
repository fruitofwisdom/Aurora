using System.Timers;

namespace Aurora
{
	// An EnemySpawner will do exactly that, try to respawn an enemy in the world periodically.
	// However, TrySpawn will only actually spawn a GameObject if none currently exist.
	internal class EnemySpawner : GameObject
	{
		public double SpawnTime { get; set; } = 20000;      // 20 seconds
		public Enemy EnemyToSpawn { get; set; }

		private Timer SpawnTimer = null;

		public EnemySpawner()
		{
			// All WorldObjects are invisible; only their SpawnObject is intended for interaction.
			Invisible = true;
		}

		public override void Spawn()
		{
			base.Spawn();

			// Try to respawn every SpawnTime seconds.
			SpawnTimer = new Timer(SpawnTime);
			SpawnTimer.Elapsed += (source, e) => OnTimedSpawnEvent(source, e, this);
			SpawnTimer.Start();
		}

		public override void Despawn()
		{
			base.Despawn();

			SpawnTimer.Stop();
			SpawnTimer.Dispose();
		}

		private static void OnTimedSpawnEvent(object source, ElapsedEventArgs e, EnemySpawner spawner)
		{
			Game.Instance.TrySpawn(spawner.EnemyToSpawn, spawner.CurrentRoomId);
		}
	}
}
