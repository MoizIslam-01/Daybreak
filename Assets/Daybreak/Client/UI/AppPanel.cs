using UnityEngine;

namespace Daybreak.Client.UI
{
    /// <summary>
    /// Base for a full-screen panel hosted by AppShell. Built once, then shown/hidden as the user
    /// navigates. OnShow is where a panel (re)loads its data.
    /// </summary>
    public abstract class AppPanel
    {
        public GameObject Root { get; private set; }
        protected AppShell Shell { get; private set; }

        public abstract string NavLabel { get; }

        public void Init(Transform host, AppShell shell)
        {
            Shell = shell;
            Root = UIBuilder.Panel(host, UITheme.Bg, GetType().Name);
            UIBuilder.Stretch(UIBuilder.Rect(Root));
            Build(Root.transform);
            Root.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if (Root != null) Root.SetActive(visible);
            if (visible) OnShow();
        }

        /// <summary>Build the panel's static structure once.</summary>
        protected abstract void Build(Transform content);

        /// <summary>Called each time the panel becomes visible — refresh data here.</summary>
        public virtual void OnShow() { }
    }
}
