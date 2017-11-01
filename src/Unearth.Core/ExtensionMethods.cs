using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Unearth
{
    public static class DictionaryExtensions
    {
        public static bool TryGetString(this IDictionary<string, StringValues> dictionary, string key, out string value)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (!dictionary.TryGetValue(key, out StringValues values))
            {
                value = null;
                return false;
            }

            value = values.First();
            return true;
        }
    }
}
