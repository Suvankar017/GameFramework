using System;
using GameFramework.UI;
using UnityEngine;

namespace GameFramework.UI.Navigation
{
    /// <summary>One authored screen registration - id + prefab, the same
    /// [SerializeField] string id -> typed id property shape <c>GameFlow.LevelDefinition</c> uses.</summary>
    [Serializable]
    public sealed class UINavigationScreenEntry
    {
        [SerializeField] private string _id;
        [SerializeField] private UIScreen _prefab;

        public UIScreenId Id => new UIScreenId(_id);
        public UIScreen Prefab => _prefab;
    }
}
