using System;
using Daybreak.Sim;
using UnityEngine;

namespace Daybreak.Client
{
    /// <summary>
    /// The cosmetic shop: spend Sparks on titles (bragging rights, no power), then equip one to show
    /// next to your name. One component, IMGUI.
    /// </summary>
    public sealed class ShopScreen : MonoBehaviour
    {
        private WalletDto _wallet;
        private ProfileDto _profile;
        private string _status = "Loading...";
        private Vector2 _scroll;

        private async void Start()
        {
            try
            {
                await AuthService.SignInAnonymouslyAsync();
                await Refresh();
            }
            catch (Exception e) { _status = "Sign-in failed: " + e.Message; }
        }

        private async System.Threading.Tasks.Task Refresh()
        {
            try
            {
                _wallet = await DataService.LoadWalletAsync() ?? new WalletDto();
                _profile = await DataService.LoadProfileAsync() ?? new ProfileDto();
                _status = "Sparks: " + _wallet.sparks;
            }
            catch (Exception e) { _status = "Load failed: " + e.Message; }
        }

        private void OnGUI()
        {
            var area = new Rect(12, 12, Mathf.Min(520, Screen.width - 24), Screen.height - 24);
            GUILayout.BeginArea(area, GUI.skin.box);

            GUILayout.Label("DAYBREAK — Shop (titles)");
            int sparks = _wallet != null ? _wallet.sparks : 0;
            string equipped = _profile != null ? _profile.title : "";
            GUILayout.Label("Sparks: " + sparks + "     Equipped: "
                + (string.IsNullOrEmpty(equipped) ? "(none)" : TitleName(equipped)));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(90))) _ = Refresh();
            if (GUILayout.Button("Grant 100 (dev)", GUILayout.Width(130))) Grant();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var c in CosmeticCatalog.All())
            {
                bool owned = CosmeticRules.Owns(_wallet, c.id);
                bool isEquipped = c.id == equipped;

                GUILayout.BeginHorizontal();
                GUILayout.Label(c.name, GUILayout.Width(160));
                GUILayout.Label(owned ? "owned" : (c.cost + " sp"), GUILayout.Width(90));

                if (!owned)
                {
                    GUI.enabled = CosmeticRules.CanBuy(_wallet, c, out _);
                    if (GUILayout.Button("Buy", GUILayout.Width(80))) Buy(c.id);
                    GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = !isEquipped;
                    if (GUILayout.Button(isEquipped ? "Equipped" : "Equip", GUILayout.Width(90))) Equip(c.id);
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(equipped))
                if (GUILayout.Button("Unequip title", GUILayout.Width(120))) Equip("");

            GUILayout.Space(6);
            GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        private async void Grant()
        {
            _status = "Granting...";
            try { await CloudCodeService.GrantSparksAsync(100); await Refresh(); }
            catch (Exception e) { _status = "Grant error: " + e.Message; }
        }

        private async void Buy(string id)
        {
            _status = "Buying...";
            try
            {
                var r = await CloudCodeService.BuyCosmeticAsync(id);
                _status = r.ok ? ("Bought! Sparks: " + r.sparks) : ("Can't buy: " + r.error);
                if (r.ok) await Refresh();
            }
            catch (Exception e) { _status = "Buy error: " + e.Message; }
        }

        private async void Equip(string id)
        {
            _status = "Equipping...";
            try
            {
                var r = await CloudCodeService.EquipTitleAsync(id);
                _status = r.ok ? "Updated." : ("Failed: " + r.error);
                if (r.ok) await Refresh();
            }
            catch (Exception e) { _status = "Equip error: " + e.Message; }
        }

        private static string TitleName(string id)
        {
            var c = CosmeticCatalog.Get(id);
            return c != null ? c.name : id;
        }
    }
}
