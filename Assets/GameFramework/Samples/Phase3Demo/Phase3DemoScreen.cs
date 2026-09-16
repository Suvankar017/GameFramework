using GameFramework.UI;
using TMPro;
using UnityEngine;

namespace GameFramework.Samples.Phase3Demo
{
    /// <summary>
    /// Minimal <see cref="UIScreen"/> for the Phase 3 demo scene — a single localized greeting.
    /// Built entirely in code via <see cref="Create"/> so the demo needs no authored prefab/asset.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class Phase3DemoScreen : UIScreen
    {
        public static Phase3DemoScreen Create()
        {
            var root = new GameObject("Phase3DemoScreen", typeof(RectTransform));
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Phase3DemoScreen screen = root.AddComponent<Phase3DemoScreen>();

            var textGo = new GameObject("Greeting", typeof(RectTransform));
            textGo.transform.SetParent(root.transform, worldPositionStays: false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(800f, 200f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 48f;

            var localizedText = textGo.AddComponent<LocalizedTMPText>();
            localizedText.Key = "Demo.Hello";

            return screen;
        }
    }
}
