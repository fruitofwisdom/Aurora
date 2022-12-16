using System.Timers;

namespace Aurora
{
	// A WorldObject is a meta object that exists in the world and will respawn instances of its
	// GameObject, such as an enemy or placed item in the environment. These are placed in a world
	// to create its initial state before a game is launched.
	internal class WorldObject : GameObject
	{
		public double SpawnTime { get; set; } = 20000;      // 20 seconds
		public GameObject SpawnObject { get; set; }

		private Timer SpawnTimer = null;

		public WorldObject()
		{
			// All WorldObjects are invisible; only their SpawnObject is intended for interaction.
			Invisible = true;

			// Try to respawn every SpawnTime seconds.
			SpawnTimer = new Timer(SpawnTime);
			SpawnTimer.Elapsed += (source, e) => OnTimedSpawnEvent(source, e, this);
			SpawnTimer.Start();
		}

		private static void OnTimedSpawnEvent(object source, ElapsedEventArgs e, WorldObject worldObject)
		{
			Game.Instance.TrySpawn(worldObject.SpawnObject);
		}
	}
}
