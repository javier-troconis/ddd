using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace shared
{
    public class ValueEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Lazy<string[]> _memberNameExclusions;

        public ValueEqualityComparer(params Expression<Func<T, object>>[] memberExclussions)
        {
            _memberNameExclusions = new Lazy<string[]>(() => memberExclussions.Select(ExpressionExtensions.GetMemberName).ToArray());
        }

        public bool Equals(T x, T y)
        {
            var x1 = JObject.Parse(JsonConvert.SerializeObject(x));
            var y1 = JObject.Parse(JsonConvert.SerializeObject(y));
            foreach (var memberNameExclusion in _memberNameExclusions.Value)
            {
                x1.Remove(memberNameExclusion);
                y1.Remove(memberNameExclusion);
            }
            return JToken.DeepEquals(x1, y1);
        }

        public int GetHashCode(T obj)
        {
            var x = JObject.Parse(JsonConvert.SerializeObject(obj));
            foreach (var memberNameExclusion in _memberNameExclusions.Value)
            {
                x.Remove(memberNameExclusion);
            }
            return JsonConvert.SerializeObject(x).GetHashCode();
        }
    }
}
