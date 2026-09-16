using TMPro;
using UnityEngine;

namespace GameFramework.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedTMPText : LocalizedTextBase
    {
        private TMP_Text _text;
        private TMP_Text Text => _text != null ? _text : (_text = GetComponent<TMP_Text>());

        protected override void ApplyText(string value) => Text.text = value;

        protected override void ApplyFont(TMP_FontAsset font)
        {
            if (font != null)
            {
                Text.font = font;
            }
        }
    }
}
