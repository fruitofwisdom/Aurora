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

		// Is a word one of the ten most commonly used prepositions?
		public static bool IsPreposition(string word)
		{
			List<string> prepositions = new()
				{ "of", "with", "at", "from", "into", "to", "in", "for", "on", "by" };
			return prepositions.Contains(word);
		}
	}
}
