using System.Collections.Generic;
using System.Text;

namespace HostileTakeover2.Thraxus.Common.BaseClasses
{
    /// <summary>
    /// Base class for the Enumeration Class pattern: a type-safe, extensible replacement
    /// for enums that can be subclassed by individual mods to define their own log categories.
    ///
    /// Subclasses declare their members as public static readonly fields of the subclass
    /// type.  Each instance self-registers into a shared ordered registry on construction,
    /// making all categories discoverable at runtime via <see cref="AllRegistered"/>.
    ///
    /// The registry is populated when the subclass's static fields are first accessed.
    /// Call the subclass's <c>Initialize()</c> method from <c>EarlyInit()</c> in the
    /// session component to guarantee the registry is fully populated before any
    /// settings parsing or log-gating calls occur.
    ///
    /// Use <see cref="Id"/> for sandbox serialization (int bitmask), <see cref="Name"/>
    /// for human-readable config values, and <see cref="TryGetByName"/> for safe
    /// runtime lookup during config load.
    /// </summary>
    public abstract class LogCategory
    {
        private static readonly Dictionary<string, LogCategory> _registry =
            new Dictionary<string, LogCategory>(System.StringComparer.OrdinalIgnoreCase);
        private static readonly List<LogCategory> _registrationOrder = new List<LogCategory>();

        /// <summary>Bitmask-compatible integer id for sandbox variable serialization.</summary>
        public readonly int Id;

        /// <summary>Human-readable name used in config files and log output.</summary>
        public readonly string Name;

        /// <summary>
        /// Registers this instance into the shared ordered registry.
        /// Called automatically by every subclass instance on construction.
        /// </summary>
        protected LogCategory(int id, string name)
        {
            Id = id;
            Name = name;
            _registry[name] = this;
            _registrationOrder.Add(this);
        }

        /// <summary>
        /// Looks up a registered <see cref="LogCategory"/> by name (case-insensitive).
        /// Returns <c>false</c> if the name is not found.
        /// </summary>
        public static bool TryGetByName(string name, out LogCategory category)
        {
            return _registry.TryGetValue(name, out category);
        }

        /// <summary>
        /// Returns all registered categories in the order they were constructed.
        /// Only complete after the subclass type has been initialized via its
        /// <c>Initialize()</c> method.
        /// </summary>
        public static IEnumerable<LogCategory> AllRegistered => _registrationOrder;

        /// <summary>
        /// Returns a comma-separated string of all registered category names.
        /// Useful for generating human-readable setting descriptions.
        /// </summary>
        public static string GetRegisteredNames()
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (var cat in _registrationOrder)
            {
                if (!first) sb.Append(", ");
                sb.Append(cat.Name);
                first = false;
            }
            return sb.ToString();
        }

        public override string ToString() => Name;

        public override int GetHashCode() => Id;

        public override bool Equals(object obj)
        {
            var other = obj as LogCategory;
            return other != null && other.Id == Id;
        }
    }
}
