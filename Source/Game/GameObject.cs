using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aurora
{
	// A game object is any object that can exist in the world and be saved and loaded.
	[JsonDerivedType(typeof(Enemy), typeDiscriminator: "Enemy")]
	[JsonDerivedType(typeof(EnemySpawner), typeDiscriminator: "EnemySpawner")]
	// TODO: No need for raw GameObjects?
	[JsonDerivedType(typeof(GameObject), typeDiscriminator: "GameObject")]
	[JsonDerivedType(typeof(Item), typeDiscriminator: "Item")]
	[JsonDerivedType(typeof(NPC), typeDiscriminator: "NPC")]
	internal class GameObject
	{
		// Each GameObject gets a sequentially numbered ID.
		public int ObjectId { get; set; } = 0;
		public string Name { get; set; } = "nothing";
		public int CurrentRoomId { get; set; } = 0;
		public string Description { get; set; } = "nothing";
		// Invisible objects can't be seen.
		public bool Invisible { get; set; } = false;

		private static int NumObjects = 0;

		public GameObject() { }

		public virtual void Spawn()
		{
			++NumObjects;
			if (ObjectId == 0)
			{
				ObjectId = NumObjects;
			}
		}

		public virtual void Despawn() { }

		public string DebugName()
		{
			return "\"" + Name + "\"(" + ObjectId + ")";
		}

		public string IndefiniteName()
		{
			string article = "a ";
			if (Name[0] == 'A' || Name[0] == 'a' ||
				Name[0] == 'E' || Name[0] == 'e' ||
				Name[0] == 'I' || Name[0] == 'i' ||
				Name[0] == 'O' || Name[0] == 'o' ||
				Name[0] == 'U' || Name[0] == 'u' ||
				Name[0] == 'H' || Name[0] == 'h')
			{
				article = "an ";
			}
			return article + Name;
		}

		// Create a new GameObject that is a clone (via-serialization) of the provided GameObject.
		public static T Clone<T>(T gameObject) where T : GameObject
		{
			string gameObjectString = JsonSerializer.Serialize<T>(gameObject);
			T newGameObject = JsonSerializer.Deserialize<T>(gameObjectString);
			return newGameObject;
		}

		// Given an object name, find the GameObject whose name best matches.
		public static T GetBestMatch<T>(string searchName, List<T> gameObjects) where T : GameObject
		{
			T bestMatch = null;

			if (searchName != null)
			{
				// Split all the names into a HashSet and then compare how many words are in common.
				HashSet<string> searchNameHashSet = searchName.ToLower().Split(' ').ToHashSet<string>();
				int bestIntersectCount = 0;
				foreach (T gameObject in gameObjects)
				{
					HashSet<string> objectNameHashSet = gameObject.Name.ToLower().Split(' ').ToHashSet<string>();
					int intersectCount = objectNameHashSet.Intersect(searchNameHashSet).Count();
					if (intersectCount > bestIntersectCount)
					{
						bestMatch = gameObject;
						bestIntersectCount = intersectCount;
					}
				}
			}

			return bestMatch;
		}
	}
}
