using System;

namespace GameFramework.RemoteConfig.LiveOps
{
    /// <summary>Resolved, effective data for one live event at a point in time - the local authored
    /// <see cref="LiveEventDefinition"/> with any remote overrides already applied (see
    /// <see cref="LiveOpsService"/>'s remarks). Immutable; a new instance is produced whenever the
    /// effective schedule is recomputed.</summary>
    public readonly struct LiveEvent
    {
        public readonly LiveEventId Id;
        public readonly bool Enabled;
        public readonly DateTime StartUtc;
        public readonly DateTime EndUtc;
        public readonly string TitleLocalizationKey;
        public readonly string DescriptionLocalizationKey;
        public readonly string IconId;

        /// <summary>Optional prefix (e.g. <c>"liveops.summer_event."</c>) a game can prepend to its
        /// own sub-keys to read this event's associated remote configuration - see CLAUDE.md's Phase
        /// 17 brief, section 46. This framework never reads or interprets those sub-keys itself.</summary>
        public readonly string ConfigKeyPrefix;

        public readonly bool IsScheduleValid;

        public LiveEvent(LiveEventId id, bool enabled, DateTime startUtc, DateTime endUtc, string titleLocalizationKey, string descriptionLocalizationKey, string iconId, string configKeyPrefix, bool isScheduleValid)
        {
            Id = id;
            Enabled = enabled;
            StartUtc = startUtc;
            EndUtc = endUtc;
            TitleLocalizationKey = titleLocalizationKey;
            DescriptionLocalizationKey = descriptionLocalizationKey;
            IconId = iconId;
            ConfigKeyPrefix = configKeyPrefix;
            IsScheduleValid = isScheduleValid;
        }

        public LiveEventState ResolveState(DateTime nowUtc)
        {
            if (!IsScheduleValid)
            {
                return LiveEventState.Invalid;
            }

            if (!Enabled)
            {
                return LiveEventState.Disabled;
            }

            if (nowUtc < StartUtc)
            {
                return LiveEventState.Upcoming;
            }

            return nowUtc < EndUtc ? LiveEventState.Active : LiveEventState.Ended;
        }
    }
}
