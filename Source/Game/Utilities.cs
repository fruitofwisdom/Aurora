using System.Collections.Generic;

namespace Aurora
{
	internal class Utilities
	{
		// Capitalize the first letter in a string.
		public static string Capitalize(string toCapitalize)
		{
			return char.ToUpper(toCapitalize[0]) + toCapitalize[1..];
		}

		// Lowercase (uncapitalize?) the first letter in a string.
		public static string Lowercase(string toLowercase)
		{
			return char.ToLower(toLowercase[0]) + toLowercase[1..];
		}

		// Returns a nicely formatted string of items. For example, "north, south, and down".
		public static string GetPrettyList(List<string> items)
		{
			string prettyList = "";
			for (int i = 0; i < items.Count; ++i)
			{
				prettyList +=
					(items.Count == 2 && i > 0 ? " and " : "") +
					(items.Count > 2 && i > 0 ? ", " : "") +
					(items.Count > 2 && i == items.Count - 1 ? "and " : "") +
					items[i];
			}
			return prettyList;
		}

		// The name with its indefinite article in front.
		public static string IndefiniteName(string name)
		{
			string article = "a ";
			if (name[0] == 'A' || name[0] == 'a' ||
				name[0] == 'E' || name[0] == 'e' ||
				name[0] == 'I' || name[0] == 'i' ||
				name[0] == 'O' || name[0] == 'o' ||
				name[0] == 'U' || name[0] == 'u' ||
				name[0] == 'H' || name[0] == 'h')
			{
				article = "an ";
			}
			return article + name;
		}

		// Is a word one of the ten most commonly used prepositions?
		public static bool IsPreposition(string word)
		{
			List<string> prepositions = new()
				{ "of", "with", "at", "from", "into", "to", "in", "for", "on", "by" };
			return prepositions.Contains(word);
		}
	}
}
