using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class Graphs
    {
        public static IEnumerable<T> DepthFirstTraversal<T>(T parent, Func<T, IEnumerable<T>> getChildren, IEqualityComparer<T> comparer = null)
        {
            var visited = new HashSet<T>(comparer ?? EqualityComparer<T>.Default);
            var stack = new Stack<T>();
            stack.Push(parent);
            while (stack.Any())
            {
                T current = stack.Pop();
                if (!visited.Add(current))
                {
                    continue;
                }
                yield return current;
                var children = getChildren(current);
                var unvisitedChildren = children.Where(child => !visited.Contains(child));
                foreach (var child in unvisitedChildren)
                {
                    stack.Push(child);
                }
            }
        }

        public static Task<IEnumerable<T>> DepthFirstTraversalAsync<T>(T parent, Func<T, Task<IEnumerable<T>>> getChildren, IEqualityComparer<T> comparer = null)
        {
            var visited = new HashSet<T>(comparer ?? EqualityComparer<T>.Default);
            Func<T, Task<IEnumerable<T>>> traverse = null;
            traverse = async current =>
            {
                if (!visited.Add(current))
                {
                    return Enumerable.Empty<T>();
                }
                var children = await getChildren(current);
                var unvisitedChildren = await Task.WhenAll(children.Where(x => !visited.Contains(x)).Select(traverse));
                return new[] { current }.Concat(unvisitedChildren.SelectMany(x => x));
            };
            return traverse(parent);
        }
    }
}