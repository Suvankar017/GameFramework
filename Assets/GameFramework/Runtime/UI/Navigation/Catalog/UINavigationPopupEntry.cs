using System;
using GameFramework.UI;
using UnityEngine;

namespace GameFramework.UI.Navigation
{
    /// <summary>One authored popup registration - see <see cref="UINavigationScreenEntry"/>'s remarks.</summary>
    [Serializable]
    public sealed class UINavigationPopupEntry
    {
        [SerializeField] private string _id;
        [SerializeField] private UIPopup _prefab;

        public UIPopupId Id => new UIPopupId(_id);
        public UIPopup Prefab => _prefab;
    }
}
