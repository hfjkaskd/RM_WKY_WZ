using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Bizza
{
    public static class DictionaryUtil
    {
        public static string ToPairString<T, K>(this ICollection<KeyValuePair<T, K>> dictionary)
        {
            StringBuilder sb = new();
            sb.Append('{');
            foreach (var kvp in dictionary)
            {
                sb.Append("\n{");
                sb.Append(kvp.Key.ToString());
                sb.Append(": ");
                sb.Append(kvp.Value.ToString());
                sb.Append("},");
            }
            if (sb.Length > 1)
            {
                sb.Append('\n');
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}
