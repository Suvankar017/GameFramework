using GameFramework.Runtime.Diagnostics;
using UnityEngine;

namespace GameFramework.Gameplay.Entities
{
    /// <summary>
    /// The one gap Unity's own <c>[RequireComponent]</c> doesn't cover: a required component that
    /// lives on a parent or child, not the same GameObject. Prefer <c>[RequireComponent]</c> for
    /// same-GameObject requirements — it is Editor-enforced and needs no runtime check at all.
    /// These two methods are named (not extension methods) so a hierarchy walk is always visible at
    /// the call site rather than hidden behind an innocent-looking dot call; they are not intended
    /// for per-frame use — cache the result once (e.g. in <c>Awake</c>).
    /// </summary>
    public static class ComponentLookup
    {
        private const string LogCategory = "Entities";

        /// <summary>Returns the first <typeparamref name="T"/> found on <paramref name="from"/> or
        /// one of its parents, or logs an error and returns null if none exists.</summary>
        public static T RequireInParent<T>(Component from, bool includeInactive = true) where T : class
        {
            T found = from != null ? from.GetComponentInParent<T>(includeInactive) : null;
            if (found == null)
            {
                Log.Error(LogCategory, $"Required component '{typeof(T).Name}' not found in parents of " +
                    $"'{(from != null ? from.name : "<null>")}'.", from);
            }

            return found;
        }

        /// <summary>Returns the first <typeparamref name="T"/> found on <paramref name="from"/> or
        /// one of its children, or logs an error and returns null if none exists.</summary>
        public static T RequireInChildren<T>(Component from, bool includeInactive = true) where T : class
        {
            T found = from != null ? from.GetComponentInChildren<T>(includeInactive) : null;
            if (found == null)
            {
                Log.Error(LogCategory, $"Required component '{typeof(T).Name}' not found in children of " +
                    $"'{(from != null ? from.name : "<null>")}'.", from);
            }

            return found;
        }
    }
}
