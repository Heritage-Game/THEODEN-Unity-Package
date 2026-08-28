using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Theoden.Editor.POIDefinitionClasses
{
    /// <summary>
    /// Associates the challenge type stored in a POI JSON with
    /// the corresponding concrete editor template.
    /// </summary>
    public static class PoiTemplateTypeRegistry
    {
        private static Dictionary<string, Type> _typesByChallengeId;
        private static string _initializationError;

        public static Type[] GetRegisteredTemplateTypes()
        {
            EnsureInitialized();

            if (!string.IsNullOrWhiteSpace(_initializationError))
                return Array.Empty<Type>();

            return _typesByChallengeId.Values
                .Distinct()
                .OrderBy(type => type.Name)
                .ToArray();
        }

        public static bool TryGetTemplateType(
            string challengeTypeId,
            out Type templateType,
            out string error)
        {
            templateType = null;
            error = null;

            EnsureInitialized();

            if (!string.IsNullOrWhiteSpace(_initializationError))
            {
                error = _initializationError;
                return false;
            }

            if (string.IsNullOrWhiteSpace(challengeTypeId))
            {
                error = "The POI JSON does not contain a valid challenge type.";
                return false;
            }

            if (!_typesByChallengeId.TryGetValue(
                    challengeTypeId,
                    out templateType))
            {
                error =
                    $"No POI template is registered for challenge type " +
                    $"'{challengeTypeId}'.";

                return false;
            }

            return true;
        }

        private static void EnsureInitialized()
        {
            if (_typesByChallengeId != null)
                return;

            _typesByChallengeId =
                new Dictionary<string, Type>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (Type templateType in
                     TypeCache.GetTypesDerivedFrom<POITemplate>())
            {
                if (templateType.IsAbstract || !templateType.IsClass)
                    continue;

                var attribute =
                    (PoiChallengeTypeAttribute)Attribute.GetCustomAttribute(
                        templateType,
                        typeof(PoiChallengeTypeAttribute)
                    );

                if (attribute == null)
                    continue;

                if (_typesByChallengeId.TryGetValue(
                        attribute.TypeId,
                        out Type existingType))
                {
                    _initializationError =
                        $"Challenge type '{attribute.TypeId}' is registered " +
                        $"by both '{existingType.Name}' and " +
                        $"'{templateType.Name}'.";

                    return;
                }

                _typesByChallengeId.Add(
                    attribute.TypeId,
                    templateType
                );
            }

            if (_typesByChallengeId.Count == 0)
            {
                _initializationError =
                    "No POI templates have been registered. " +
                    "Add PoiChallengeTypeAttribute to at least one template.";
            }
        }
    }
}