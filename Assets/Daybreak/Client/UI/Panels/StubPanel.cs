using UnityEngine;

namespace Daybreak.Client.UI
{
    /// <summary>Placeholder panel used while porting real screens. Shows its name.</summary>
    public sealed class StubPanel : AppPanel
    {
        private readonly string _label;
        private readonly string _note;

        public StubPanel(string label, string note) { _label = label; _note = note; }

        public override string NavLabel => _label;

        protected override void Build(Transform content)
        {
            UIBuilder.VLayout(content.gameObject, 16f, 32, TextAnchor.MiddleCenter);
            UIBuilder.Label(content, _label, UITheme.TitleSize, UITheme.Text, TMPro.TextAlignmentOptions.Center);
            UIBuilder.Label(content, _note, UITheme.BodySize, UITheme.TextDim, TMPro.TextAlignmentOptions.Center);
        }
    }
}
