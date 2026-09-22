using System;
using System.Collections.Generic;

namespace GameFramework.PlayerData
{
    /// <summary>
    /// One logical player's active, in-memory persistent data - a <see cref="ProfileId"/> plus one
    /// instance of every section <see cref="IPlayerProfileService.RegisterSection{TSection}"/>
    /// registered. Constructed and owned by <see cref="PlayerProfileService"/>; a game only ever
    /// reads <see cref="IPlayerProfileService.ActiveProfile"/>, it never constructs one itself - see
    /// CLAUDE.md's Phase 13 brief, section 5.
    /// </summary>
    public sealed class PlayerProfile
    {
        private readonly Dictionary<Type, IPlayerDataSection> _sections;

        public ProfileId Id { get; }

        internal PlayerProfileMetadata Metadata { get; set; }

        internal PlayerProfile(ProfileId id, PlayerProfileMetadata metadata, Dictionary<Type, IPlayerDataSection> sections)
        {
            Id = id;
            Metadata = metadata;
            _sections = sections;
        }

        /// <summary>Read-only summary of this profile's metadata.</summary>
        public PlayerProfileInfo Info => new PlayerProfileInfo(Metadata);

        /// <summary>True if any registered section is dirty - see <see cref="IPlayerDataSection.IsDirty"/>.</summary>
        public bool IsDirty
        {
            get
            {
                foreach (IPlayerDataSection section in _sections.Values)
                {
                    if (section.IsDirty)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Returns the registered section of type <typeparamref name="TSection"/>. Throws
        /// if no such section was registered via <see cref="IPlayerProfileService.RegisterSection{TSection}"/>
        /// before this profile was loaded - a missing registration is a composition-root bug, not a
        /// recoverable runtime condition, so this deliberately does not silently return null.</summary>
        public TSection GetSection<TSection>() where TSection : class, IPlayerDataSection
        {
            if (TryGetSection(out TSection section))
            {
                return section;
            }

            throw new InvalidOperationException(
                $"No player data section of type '{typeof(TSection).Name}' is registered. " +
                $"Call {nameof(IPlayerProfileService.RegisterSection)} before loading a profile.");
        }

        public bool TryGetSection<TSection>(out TSection section) where TSection : class, IPlayerDataSection
        {
            if (_sections.TryGetValue(typeof(TSection), out IPlayerDataSection raw))
            {
                section = (TSection)raw;
                return true;
            }

            section = null;
            return false;
        }

        internal IReadOnlyCollection<IPlayerDataSection> Sections => _sections.Values;
    }
}
