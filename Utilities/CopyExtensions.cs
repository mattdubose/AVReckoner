using System;
using System.Reflection;
using System.Text.Json;

namespace PurpleValley.Extensions 
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Performs a shallow copy by copying all public writable properties from the source to a new object.
        /// Reference-type properties are copied by reference.
        /// </summary>
        public static T ShallowCopy<T>(this T source) where T : class, new()
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var target = new T();
            target.CopyFrom(source);
            return target;
        }

        /// <summary>
        /// Performs a deep copy by serializing and deserializing the entire object graph.
        /// </summary>
        public static T DeepCopy<T>(this T source) where T : class
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var json = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<T>(json)!;
        }

        /// <summary>
        /// Copies public readable/writable properties from one object to another (shallow).
        /// </summary>
        public static void CopyFrom<T>(this T target, T source)
        {
            throw new NotImplementedException();
            //if (source == null || target == null) return;
            //
            //var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            //                     .Where(p => p.CanRead && p.CanWrite);
            //
            //foreach (var prop in props)
            //{
            //    var value = prop.GetValue(source);
            //    prop.SetValue(target, value);
            //}
        }
    }
}
