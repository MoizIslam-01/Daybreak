using System;
using Daybreak.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Daybreak.Client.UI
{
    /// <summary>Cosmetic shop: buy title flair with Sparks and equip it.</summary>
    public sealed class ShopPanel : AppPanel
    {
        public override string NavLabel => "Shop";

        private TextMeshProUGUI _header;
        private RectTransform _list;
        private WalletDto _wallet;
        private ProfileDto _profile;

        protected override void Build(Transform content)
        {
            var top = new GameObject("Top", typeof(RectTransform));
            top.transform.SetParent(content, false);
            var trt = UIBuilder.Rect(top);
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0.5f, 1);
            trt.sizeDelta = new Vector2(0, 170);
            UIBuilder.VLayout(top, 10f, 20);

            _header = UIBuilder.Label(top.transform, "", UITheme.SmallSize, UITheme.Text);
            UIBuilder.Sizing(_header.gameObject, minHeight: 60, preferredHeight: 60);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var grant = UIBuilder.Button(top.transform, "Grant 100 Sparks (dev)", Grant, UITheme.SurfaceAlt, UITheme.SmallSize);
            UIBuilder.Sizing(grant.gameObject, minHeight: 56, preferredHeight: 56);
#endif

            var listHost = new GameObject("ListHost", typeof(RectTransform));
            listHost.transform.SetParent(content, false);
            var lrt = UIBuilder.Rect(listHost);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = new Vector2(0, -170);
            _list = UIBuilder.ScrollView(listHost.transform, out _);
        }

        public override async void OnShow()
        {
            _header.text = "Loading...";
            try
            {
                _wallet = await DataService.LoadWalletAsync() ?? new WalletDto();
                _profile = await DataService.LoadProfileAsync() ?? new ProfileDto();
                await Shell.RefreshSparks();
                Refresh();
            }
            catch (Exception e) { _header.text = "Load failed: " + e.Message; }
        }

        private void Refresh()
        {
            string equipped = _profile != null ? CosmeticCatalog.DisplayName(_profile.title) : "";
            _header.text = "Sparks: " + (_wallet?.sparks ?? 0)
                + "     Equipped: " + (string.IsNullOrEmpty(equipped) ? "(none)" : equipped);

            UIBuilder.Clear(_list);
            foreach (var c in CosmeticCatalog.All())
            {
                bool owned = CosmeticRules.Owns(_wallet, c.id);
                bool isEquipped = _profile != null && c.id == _profile.title;

                var row = UIBuilder.Row(_list);
                var name = UIBuilder.Label(row.transform, c.name, UITheme.SmallSize, UITheme.Text);
                UIBuilder.Sizing(name.gameObject, flexibleWidth: 1);
                var cost = UIBuilder.Label(row.transform, owned ? "owned" : c.cost + " sp", UITheme.SmallSize, UITheme.Gold);
                UIBuilder.Sizing(cost.gameObject, preferredWidth: 120, minWidth: 120);

                Button btn;
                if (!owned)
                {
                    btn = UIBuilder.Button(row.transform, "Buy", () => Buy(c.id), UITheme.Accent, UITheme.SmallSize);
                    btn.interactable = CosmeticRules.CanBuy(_wallet, c, out _);
                }
                else
                {
                    btn = UIBuilder.Button(row.transform, isEquipped ? "Equipped" : "Equip",
                        () => Equip(isEquipped ? "" : c.id), isEquipped ? UITheme.SurfaceAlt : UITheme.Accent, UITheme.SmallSize);
                }
                UIBuilder.Sizing(btn.gameObject, preferredWidth: 170, minWidth: 170, preferredHeight: 58, minHeight: 58);
            }
            UIBuilder.Rebuild(_list);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private async void Grant()
        {
            try { await CloudCodeService.GrantSparksAsync(100); await OnShowAsync(); }
            catch (Exception e) { _header.text = "Grant failed: " + e.Message; }
        }
#endif

        private async void Buy(string id)
        {
            try { var r = await CloudCodeService.BuyCosmeticAsync(id); if (!r.ok) _header.text = "Can't buy: " + r.error; await OnShowAsync(); }
            catch (Exception e) { _header.text = "Buy failed: " + e.Message; }
        }

        private async void Equip(string id)
        {
            try { await CloudCodeService.EquipTitleAsync(id); await OnShowAsync(); }
            catch (Exception e) { _header.text = "Equip failed: " + e.Message; }
        }

        private async System.Threading.Tasks.Task OnShowAsync()
        {
            _wallet = await DataService.LoadWalletAsync() ?? new WalletDto();
            _profile = await DataService.LoadProfileAsync() ?? new ProfileDto();
            await Shell.RefreshSparks();
            Refresh();
        }
    }
}
