using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Aurora
{
	// A game object is any object that can exist in the world and be saved and loaded.
	[JsonDerivedType(typeof(GameObject), typeDiscriminator: "GameObject")]
	[JsonDerivedType(typeof(NPC), typeDiscriminator: "NPC")]
	[JsonDerivedType(typeof(Spawner), typeDiscriminator: "Spawner")]
	internal class GameObject
	{
		public string Name { get; set; } = "nothing";
		public int CurrentRoomId { get; set; } = 0;
		public string Description { get; set; } = "nothing";
		// Heavy objects can't be taken.
		public bool Heavy { get; set; } = false;
		// Invisible objects can't be seen.
		public bool Invisible { get; set; } = false;

		public string CapitalizeName()
		{
			return char.ToUpper(Name[0]) + Name.Substring(1);
		}

		// Given an object name, find the GameObject whose name best matches.
		public static GameObject GetBestMatch(string searchName, List<GameObject> objects)
		{
			GameObject bestMatch = null;

			// Split all the names into a HashSet and then compare how many words are in common.
			HashSet<string> searchNameHashSet = searchName.ToLower().Split(' ').ToHashSet<string>();
			int bestIntersectCount = 0;
			foreach (GameObject gameObject in objects)
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
