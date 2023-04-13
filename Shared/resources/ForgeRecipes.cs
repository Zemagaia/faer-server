using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace common.resources
{
    public class ForgeRecipes
    {
        Dictionary<string, string> recipes;
        public IDictionary<string, string> Recipes { get; private set; }

        public ForgeRecipes()
        {
            // Object Id, not Display Id
            Recipes = new ReadOnlyDictionary<string, string>(recipes = new Dictionary<string, string>());
        }

        public void AddRecipes()
        {
            recipes.Add(GetSorted(new[] { "Cultist Potion" }), "Health Potion");
        }

        public static string GetSorted(string[] array)
        {
            Array.Sort(array, StringComparer.InvariantCulture);
            var sb = new StringBuilder();
            for (var i = 0; i < array.Length; i++)
            {
                sb.Append(array[i]);
            }
            return sb.ToString();
        }
    }
}