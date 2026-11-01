using UnityEngine;

namespace SpinForFortune
{
    // ルーレットの報酬として付与するバフ。
    public class RouletteDamageBuff : CharacterBuff
    {
        public string customID;
        public int damageBonus;

        private int appliedBonus;

        public override string ID => "PHYSICALDAMAGEPLUS_ROULETTE_DAMAGE_" + customID;

        protected override void ApplyStatusInner()
        {
            base.ApplyStatusInner();
            appliedBonus = Mathf.RoundToInt(damageBonus * Amplified);
            NetworkTarget.AddCustomStat(ECustomStat.AllDamageBonus, appliedBonus);
        }

        protected override void RemoveStatusInner()
        {
            base.RemoveStatusInner();
            NetworkTarget.AddCustomStat(ECustomStat.AllDamageBonus, -appliedBonus);
        }

        public override bool Weaved()
        {
            return true;
        }
    }

    public class RouletteInvulnerabilityBuff : CharacterBuff_Invulnerable
    {
        private bool showedLocalOverlay;

        public override string ID => "PHYSICALDAMAGEPLUS_ROULETTE_INVULNERABLE";

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (NetworkTarget != null && NetworkTarget.isLocalPlayer)
            {
                showedLocalOverlay = true;
                RouletteInvulnerabilityUI.SetVisible(true);
            }
        }

        protected override void OnDestroy()
        {
            if (showedLocalOverlay)
                RouletteInvulnerabilityUI.SetVisible(false);
            base.OnDestroy();
        }
    }
}
