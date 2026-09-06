namespace Daybreak.Sim
{
    /// <summary>Pure purchase validation, shared by client (to grey out buttons) and server (authority).</summary>
    public static class CosmeticRules
    {
        public static bool Owns(WalletDto wallet, string cosmeticId)
        {
            if (wallet?.owned == null || string.IsNullOrEmpty(cosmeticId)) return false;
            foreach (var o in wallet.owned) if (o == cosmeticId) return true;
            return false;
        }

        /// <summary>Can this wallet buy this cosmetic? Returns false with a reason if not.</summary>
        public static bool CanBuy(WalletDto wallet, CosmeticDto cosmetic, out string reason)
        {
            if (cosmetic == null) { reason = "No such cosmetic."; return false; }
            if (wallet == null) { reason = "No wallet."; return false; }
            if (Owns(wallet, cosmetic.id)) { reason = "Already owned."; return false; }
            if (wallet.sparks < cosmetic.cost) { reason = "Not enough Sparks."; return false; }
            reason = null;
            return true;
        }
    }
}
