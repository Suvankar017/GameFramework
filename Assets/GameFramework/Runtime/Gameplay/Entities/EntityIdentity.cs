using UnityEngine;

namespace GameFramework.Gameplay.Entities
{
    /// <summary>
    /// Optional component that gives a GameObject a runtime <see cref="EntityId"/> — add it only to
    /// entities that actually need identity tracking (e.g. for interaction/targeting/save systems),
    /// not to every GameObject in a scene. <see cref="Id"/> is assigned once, on first
    /// <c>Awake</c>, and survives re-enabling; a pooled instance keeps the same <see cref="Id"/>
    /// across Get/Release cycles, since pooling reuses the GameObject rather than recreating it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EntityIdentity : MonoBehaviour
    {
        [Tooltip("Optional, author-assigned key for designer/debug lookup. Not validated for " +
                 "uniqueness by the framework.")]
        [SerializeField] private string _designerKey;

        public EntityId Id { get; private set; } = EntityId.None;

        public string DesignerKey => _designerKey;

        private void Awake()
        {
            if (!Id.IsValid)
            {
                Id = EntityId.New();
            }
        }
    }
}
