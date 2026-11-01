using UnityEngine;

namespace SpinForFortune
{
    public class RouletteProgressBuff : CharacterBuff
    {
        private const string IdPrefix = "PHYSICALDAMAGEPLUS_ROULETTE_PROGRESS_";

        public string customID;

        public override string ID => IdPrefix + customID;

        public void SetProgressStack(int value)
        {
            CurrentStack = value;
        }

        public override bool Weaved()
        {
            return true;
        }

        // フィーバーは残り時間をバフのタイマーに反映し、円形ゲージで確認できるようにする。
        protected override void OnUpdate_Server()
        {
            if (customID == "FEVER" && NetworkTarget != null)
            {
                RouletteProgression.Tick(NetworkTarget, Time.deltaTime);
                RouletteProgression.SyncFeverBuff(NetworkTarget, this);
            }
            base.OnUpdate_Server();
        }
    }

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
