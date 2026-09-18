using System.Collections.Generic;
using GameFramework.Runtime.Services;
using GameFramework.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Presentation.Tests
{
    /// <summary>Minimal test double for <see cref="IUIService"/> - only <see cref="GetLayerRoot"/>
    /// is exercised by <see cref="PresentationService"/>'s screen channel; every screen/popup member
    /// is an unused stub. Lazily creates one plain GameObject per requested layer, cleaned up via
    /// <see cref="DestroyAll"/> from the owning test's TearDown.</summary>
    internal sealed class FakeUIService : IUIService
    {
        private readonly Dictionary<UILayer, Transform> _roots = new Dictionary<UILayer, Transform>();

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public Transform GetLayerRoot(UILayer layer)
        {
            if (!_roots.TryGetValue(layer, out Transform root))
            {
                root = new GameObject($"FakeUILayer_{layer}").transform;
                _roots[layer] = root;
            }

            return root;
        }

        public void DestroyAll()
        {
            foreach (Transform root in _roots.Values)
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root.gameObject);
                }
            }

            _roots.Clear();
        }

        public T OpenScreen<T>(T prefab) where T : UIScreen => null;
        public void CloseScreen(UIScreen screen)
        {
        }
        public void CloseTopScreen()
        {
        }
        public void PopToRoot()
        {
        }
        public UIScreen CurrentScreen => null;
        public T OpenPopup<T>(T prefab) where T : UIPopup => null;
        public void ClosePopup(UIPopup popup, UIPopupResult result = UIPopupResult.None)
        {
        }
        public void CloseTopPopup(UIPopupResult result = UIPopupResult.None)
        {
        }
        public UIPopup CurrentPopup => null;
    }
}
