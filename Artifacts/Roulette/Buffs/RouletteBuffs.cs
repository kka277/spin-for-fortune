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
        public int attackSpeedBonus;

        private int appliedBonus;
        private int appliedAttackSpeedBonus;

        public override string ID => "PHYSICALDAMAGEPLUS_ROULETTE_DAMAGE_" + customID;

        protected override void ApplyStatusInner()
        {
            base.ApplyStatusInner();
            appliedBonus = Mathf.RoundToInt(damageBonus * Amplified);
            appliedAttackSpeedBonus = Mathf.RoundToInt(attackSpeedBonus * Amplified);
            NetworkTarget.AddCustomStat(ECustomStat.AllDamageBonus, appliedBonus);
            NetworkTarget.AddCustomStat(ECustomStat.AttackSpeed, appliedAttackSpeedBonus);
        }

        protected override void RemoveStatusInner()
        {
            base.RemoveStatusInner();
            NetworkTarget.AddCustomStat(ECustomStat.AllDamageBonus, -appliedBonus);
            NetworkTarget.AddCustomStat(ECustomStat.AttackSpeed, -appliedAttackSpeedBonus);
        }

        public override bool Weaved()
        {
            return true;
        }
    }

    public class RouletteInvulnerabilityBuff : CharacterBuff_Invulnerable
    {
        private static readonly System.Collections.Generic.List<RouletteInvulnerabilityBuff> ActiveBuffs =
            new System.Collections.Generic.List<RouletteInvulnerabilityBuff>();
        private bool showedLocalOverlay;

        public override string ID => "PHYSICALDAMAGEPLUS_ROULETTE_INVULNERABLE";

        internal static bool IsActiveFor(UnitAvatar target)
        {
            if (target == null)
                return false;

            for (int i = ActiveBuffs.Count - 1; i >= 0; i--)
            {
                var buff = ActiveBuffs[i];
                if (buff == null)
                {
                    ActiveBuffs.RemoveAt(i);
                    continue;
                }
                if (buff.NetworkTarget == target)
                    return true;
            }
            return false;
        }

        protected override void ApplyStatusInner()
        {
            base.ApplyStatusInner();
            if (!ActiveBuffs.Contains(this))
                ActiveBuffs.Add(this);
        }

        protected override void RemoveStatusInner()
        {
            base.RemoveStatusInner();
            ActiveBuffs.Remove(this);
        }

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
            ActiveBuffs.Remove(this);
            if (showedLocalOverlay)
                RouletteInvulnerabilityUI.SetVisible(false);
            base.OnDestroy();
        }
    }
}
