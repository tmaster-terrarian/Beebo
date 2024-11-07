using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

using Semver;

namespace Beebo;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ModInfoAttribute([DisallowNull] string guid, string displayName, [DisallowNull] string versionString) : Attribute
{
    public string Guid => guid;
    public string DisplayName => displayName;
    public string VersionString => versionString;

    public SemVersion Version { get; } = SemVersion.Parse(versionString, SemVersionStyles.OptionalPatch);
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ModDependency(
    [DisallowNull] string guid,
    ModDependency.DependencyKind dependencyKind,
    [DisallowNull] string versionRange
) : Attribute, IEquatable<ModDependency>
{
    public string Guid => guid;
    public string VersionRange => versionRange;

    [JsonPropertyName("type")]
    public DependencyKind Kind => dependencyKind;

    public enum DependencyKind
    {
        /// <summary>
        /// States that the specified mod guid and version range is required by this mod.
        /// This mod will also fail to load if the dependency is present but the installed version of the dependency doesn't match the range.
        /// </summary>
        Required,

        /// <summary>
        /// States that the specified mod guid and version range is compatible with this mod, but not essential for it to work properly.
        /// This mod will load normally regardless of the installed version of the dependency.
        /// </summary>
        Optional,

        /// <summary>
        /// States that the specified mod guid and version range is mutually exclusive with this mod.
        /// This mod will still load normally if the installed version of the dependency doesn't match the range.
        /// This mod will fail to load otherwise.
        /// </summary>
        Incompatible
    }

    public bool Equals(ModDependency other)
    {
        return other.Guid == Guid
            && other.Kind == Kind
            && other.VersionRange == VersionRange;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as ModDependency);
    }

    public static IEqualityComparer<ModDependency> GetGuidEqualityComparer() => new GuidEqualityComparer();

    public class GuidEqualityComparer : IEqualityComparer<ModDependency>
    {
        public bool Equals(ModDependency x, ModDependency y)
        {
            return x.Guid == y.Guid;
        }

        public int GetHashCode([DisallowNull] ModDependency obj)
        {
            return obj.Guid.GetHashCode();
        }
    }
}
