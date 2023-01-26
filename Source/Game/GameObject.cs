using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aurora
{
	// A game object is any object that can exist in the world and be saved and loaded.
	[JsonDerivedType(typeof(GameObject), typeDiscriminator: "GameObject")]
	[JsonDerivedType(typeof(NPC), typeDiscriminator: "NPC")]
	[JsonDerivedType(typeof(Spawner), typeDiscriminator: "Spawner")]
	internal class GameObject
	{
		// Each GameObject gets a sequentially numbered ID.
		public int ObjectId { get; set; } = 0;
		public string Name { get; set; } = "nothing";
		public int CurrentRoomId { get; set; } = 0;
		public string Description { get; set; } = "nothing";
		// Heavy objects can't be taken.
		public bool Heavy { get; set; } = false;
		// Invisible objects can't be seen.
		public bool Invisible { get; set; } = false;

		private static int NumObjects = 0;

		public GameObject()
		{
			ObjectId = ++NumObjects;
		}

		public string CapitalizeName()
		{
			return char.ToUpper(Name[0]) + Name.Substring(1);
		}

		// Create and return a new GameObject that is a clone (via-serialization) of the provided
		// GameObject. The new GameObject will have a new ObjectId as well.
		public static GameObject Clone(GameObject gameObject)
		{
			string gameObjectString = JsonSerializer.Serialize(gameObject);
			GameObject newGameObject = JsonSerializer.Deserialize<GameObject>(gameObjectString);
			// The correct, incremented ObjectId gets overwritten by deserialization. Restore it.
			newGameObject.ObjectId = NumObjects;
			return newGameObject;
		}

		// Given an object name, find the GameObject whose name best matches.
		public static GameObject GetBestMatch(string searchName, List<GameObject> gameObjects)
		{
			GameObject bestMatch = null;

			// Split all the names into a HashSet and then compare how many words are in common.
			HashSet<string> searchNameHashSet = searchName.ToLower().Split(' ').ToHashSet<string>();
			int bestIntersectCount = 0;
			foreach (GameObject gameObject in gameObjects)
			{
				HashSet<string> objectNameHashSet = gameObject.Name.ToLower().Split(' ').ToHashSet<string>();
				if (objectNameHashSet.Intersect(searchNameHashSet).Count() > bestIntersectCount)
				{
					bestMatch = gameObject;
					bestIntersectCount = objectNameHashSet.Count();
				}
			}

			return bestMatch;
		}
	}
}
