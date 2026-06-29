using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Editor.Export
{
    /// <summary>
    /// Utility class used during the POI export pipeline to discover all Unity asset references
    /// contained inside a level template.
    /// </summary>
    /// <remarks>
    /// This collector uses reflection to recursively inspect the fields of any object passed as root.
    /// Whenever it finds a value that derives from <see cref="UnityEngine.Object"/>, such as
    /// <see cref="Sprite"/> or <see cref="AudioClip"/>, it stores that asset together with the
    /// field from which it was reached.
    ///
    /// The collector does not modify assets, does not create Addressables entries, and does not
    /// serialize JSON. Its only responsibility is to return the list of Unity assets referenced by
    /// the template so that another service, such as an Addressables setup service, can process them.
    ///
    /// This makes the export pipeline extensible: new template classes can add new Sprite, AudioClip,
    /// or other UnityEngine.Object fields without requiring the exporter to know about each concrete
    /// template type.
    /// </remarks>
    public static class UnityAssetReferenceCollector
    {
        /// <summary>
        /// Represents a Unity asset reference that is  found while scanning a template.
        /// </summary>
        /// <remarks>
        /// <see cref="Asset"/> is the actual Unity asset found, for example a Sprite or an AudioClip.
        /// <see cref="SourceField"/> is the field through which the asset was discovered.
        /// The source field can be used later to infer the semantic role of the asset, for example
        /// whether a Sprite came from an images list or from a poiBadge field.
        /// </remarks>
        public readonly struct UnityObjectReference
        {
            /// <summary>
            /// The Unity asset found inside the scanned object graph.
            /// </summary>
            public readonly UnityEngine.Object Asset;
            /// <summary>
            /// The field from which the asset was reached.
            /// </summary>
            /// <remarks>
            /// This may be useful for address resolution. For example, an AudioClip found in a field
            /// named "music" can receive a different Addressables address from an AudioClip found in
            /// a field named "audioDescription".
            /// </remarks>
            public readonly FieldInfo SourceField;

            /// <summary>
            /// Creates a new reference record for a discovered Unity asset.
            /// </summary>
            /// <param name="asset">The Unity asset discovered during the scan.</param>
            /// <param name="sourceField">The field from which the asset was reached.</param>
            public UnityObjectReference(UnityEngine.Object asset, FieldInfo sourceField)
            {
                Asset = asset;
                SourceField = sourceField;
            }
        }

        /// <summary>
        /// Recursively scans an object and returns all Unity asset references contained in it.
        /// </summary>
        /// <param name="root">
        /// The root object to inspect. In the POI export pipeline this is usually a
        /// <see cref="LevelTemplateBase"/> instance.
        /// </param>
        /// <returns>
        /// A list of Unity asset references that is found inside the object graph.
        /// </returns>
        /// <remarks>
        /// The method is template-agnostic. It does not require knowledge of concrete template types
        /// such as MultipleChoiceTemplate or OpenAnswerTemplate. It walks through fields, nested objects,
        /// and enumerable collections until it finds values deriving from <see cref="UnityEngine.Object"/>.
        /// </remarks>
        public static List<UnityObjectReference> CollectUnityObjectReferences(object root)
        {
            var results = new List<UnityObjectReference>();
            var visited = new HashSet<object>();

            CollectRecursive(root, null, results, visited);

            return results;
        }

        /// <summary>
        /// Internal recursive scanner used to walk through the object graph.
        /// </summary>
        /// <param name="obj">The current object being inspected.</param>
        /// <param name="sourceField">
        /// The field through which the current object was reached.
        /// This value is preserved when scanning elements inside collections.
        /// </param>
        /// <param name="results">The output list where discovered Unity assets are stored.</param>
        /// <param name="visited">
        /// A set of already visited reference objects, used to avoid infinite recursion in cyclic graphs.
        /// </param>
        private static void CollectRecursive(
            object obj,
            FieldInfo sourceField,
            List<UnityObjectReference> results,
            HashSet<object> visited)
        {
            if (obj == null)
                return;

            if (obj is UnityEngine.Object unityObject)
            {
                if (unityObject != null)
                    results.Add(new UnityObjectReference(unityObject, sourceField));

                return;
            }

            Type type = obj.GetType();

            if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
                return;

            if (!type.IsValueType)
            {
                if (visited.Contains(obj))
                    return;

                visited.Add(obj);
            }

            if (obj is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                    CollectRecursive(item, sourceField, results, visited);

                return;
            }

            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            foreach (FieldInfo field in type.GetFields(flags))
            {
                if (field.IsNotSerialized)
                    continue;

                object value = field.GetValue(obj);
                CollectRecursive(value, field, results, visited);
            }
        }
    }
}