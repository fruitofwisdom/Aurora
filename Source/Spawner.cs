using System.Timers;

namespace Aurora
{
	// A Spawner can be placed in the world to respawn instances of its GameObject, such as an
	// enemy or placed item, whenever necessary.
	internal class Spawner : GameObject
	{
		public double SpawnTime { get; set; } = 20000;      // 20 seconds
		public GameObject SpawnObject { get; set; }

		private Timer SpawnTimer = null;

		public Spawner()
		{
			// All WorldObjects are invisible; only their SpawnObject is intended for interaction.
			Invisible = true;

			// Try to respawn every SpawnTime seconds.
			SpawnTimer = new Timer(SpawnTime);
			SpawnTimer.Elapsed += (source, e) => OnTimedSpawnEvent(source, e, this);
			SpawnTimer.Start();
		}

		private static void OnTimedSpawnEvent(object source, ElapsedEventArgs e, Spawner worldObject)
		{
			Game.Instance.TrySpawn(worldObject.SpawnObject);
		}
	}
}
