using System;
using System.Collections.Generic;

namespace IDS.Core.Graph
{
    //Credit from https://www.codeproject.com/Articles/869059/Topological-sorting-in-Csharp
    public class GenericEqualityComparer<TItem, TKey> : EqualityComparer<TItem> where TItem : class
    {
        private readonly Func<TItem, TKey> getKey;
        private readonly EqualityComparer<TKey> keyComparer;

        public GenericEqualityComparer(Func<TItem, TKey> getKey)
        {
            this.getKey = getKey;
            keyComparer = EqualityComparer<TKey>.Default;
        }

        public override bool Equals(TItem x, TItem y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }
            return keyComparer.Equals(getKey(x), getKey(y));
        }

        public override int GetHashCode(TItem obj)
        {
            if (obj == null)
            {
                return 0;
            }
            return keyComparer.GetHashCode(getKey(obj));
        }
    }
}
