using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.UI
{
    /// <summary>Legacy UGUI <see cref="Text"/> variant — no font-swap support (see
    /// <see cref="LocalizedTMPText"/> for that), since legacy <see cref="Text"/> uses
    /// <see cref="Font"/>, not <see cref="TMPro.TMP_FontAsset"/>.</summary>
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : LocalizedTextBase
    {
        private Text _label;
        private Text Label => _label != null ? _label : (_label = GetComponent<Text>());

        protected override void ApplyText(string value) => Label.text = value;
    }
}
