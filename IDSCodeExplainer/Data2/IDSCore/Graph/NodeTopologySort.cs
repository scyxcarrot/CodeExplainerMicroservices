using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace IDS.Core.Graph
{
    //Credit from https://www.codeproject.com/Articles/869059/Topological-sorting-in-Csharp
    public class NodeTopologySort
    {
        public string CyclingDependencyErrorMessage => "ERROR!Cyclic Dependency!";

        private Func<T, IEnumerable<T>> RemapDependencies<T, TKey>(IEnumerable<T> source, Func<T, IEnumerable<TKey>> getDependencies, Func<T, TKey> getKey) where T : class
        {
            var map = source.ToDictionary(getKey);
            return item =>
            {
                var dependencies = getDependencies(item);
                return dependencies != null
                    ? dependencies.Select(key => map[key])
                    : null;
            };
        }


        public IList<T> Sort<T, TKey>(IEnumerable<T> source, Func<T, IEnumerable<TKey>> getDependencies, Func<T, TKey> getKey, bool ignoreCycles) where T : class
        {
            ICollection<T> source2 = (source as ICollection<T>) ?? source.ToArray();
            return Sort<T>(source2, RemapDependencies(source2, getDependencies, getKey), null, ignoreCycles);
        }

        public IList<T> Sort<T, TKey>(IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies, Func<T, TKey> getKey, bool ignoreCycles) where T : class
        {
            return Sort<T>(source, getDependencies, new GenericEqualityComparer<T, TKey>(getKey), ignoreCycles);
        }

        public IList<T> Sort<T>(IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies) where T : class
        {
            return Sort<T>(source, getDependencies, null, false);
        }

        public IList<T> Sort<T>(IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies, IEqualityComparer<T> comparer, bool ignoreCycles) where T : class
        {
            var sorted = new List<T>();
            var visited = new Dictionary<T, bool>(comparer);

            foreach (var item in source)
            {
                Visit(item, getDependencies, sorted, visited, ignoreCycles);
            }

            return sorted;
        }

        public void Visit<T>(T item, Func<T, IEnumerable<T>> getDependencies, List<T> sorted, Dictionary<T, bool> visited, bool ignoreCycles) where T : class
        {
            bool inProcess;
            var alreadyVisited = visited.TryGetValue(item, out inProcess);

            if (alreadyVisited)
            {
                if (inProcess && !ignoreCycles)
                {
                    throw new ArgumentException(CyclingDependencyErrorMessage);
                }
            }
            else
            {
                visited[item] = true;

                var dependencies = getDependencies(item);
                if (dependencies != null)
                {
                    foreach (var dependency in dependencies)
                    {
                        Visit(dependency, getDependencies, sorted, visited, ignoreCycles);
                    }
                }

                visited[item] = false;
                sorted.Add(item);
            }
        }
        
        public int Visit<T>(T item, Func<T, IEnumerable<T>> getDependencies, List<ICollection<T>> sorted, Dictionary<T, int> visited, bool ignoreCycles) where T : class
        {
            const int inProcess = -1;
            int level;
            var alreadyVisited = visited.TryGetValue(item, out level);

            if (alreadyVisited)
            {
                if (level == inProcess && ignoreCycles)
                {
                    throw new ArgumentException(CyclingDependencyErrorMessage);
                }
            }
            else
            {
                visited[item] = (level = inProcess);

                var dependencies = getDependencies(item);
                if (dependencies != null)
                {
                    foreach (var dependency in dependencies)
                    {
                        var depLevel = Visit(dependency, getDependencies, sorted, visited, ignoreCycles);
                        level = Math.Max(level, depLevel);
                    }
                }

                visited[item] = ++level;
                while (sorted.Count <= level)
                {
                    sorted.Add(new Collection<T>());
                }
                sorted[level].Add(item);
            }

            return level;
        }

        public IList<ICollection<T>> Group<T, TKey>(IEnumerable<T> source, Func<T, IEnumerable<TKey>> getDependencies, Func<T, TKey> getKey, bool ignoreCycles) where T : class
        {
            ICollection<T> source2 = (source as ICollection<T>) ?? source.ToArray();
            return Group<T>(source2, RemapDependencies(source2, getDependencies, getKey), null, ignoreCycles);
        }

        public IList<ICollection<T>> Group<T, TKey>(IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies, Func<T, TKey> getKey, bool ignoreCycles) where T : class
        {
            return Group<T>(source, getDependencies, new GenericEqualityComparer<T, TKey>(getKey), ignoreCycles);
        }

        public IList<ICollection<T>> Group<T>(IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies) where T : class
        {
            return Group<T>(source, getDependencies, null, true);
        }

        public IList<ICollection<T>> Group<T>(IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies, IEqualityComparer<T> comparer, bool ignoreCycles) where T : class
        {
            var sorted = new List<ICollection<T>>();
            var visited = new Dictionary<T, int>(comparer);

            foreach (var item in source)
            {
                Visit(item, getDependencies, sorted, visited, ignoreCycles);
            }

            return sorted;
        }
    }
}
