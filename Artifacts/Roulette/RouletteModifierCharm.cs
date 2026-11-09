using System;
using System.Collections.Generic;

namespace SpinForFortune
{
    // 「ルーレット」を強化するアーティファクトの共通基底。
    // 新しい強化アーティファクトは、これを継承して Modify で倍率を書き換えるだけでよい。
    public abstract class RouletteModifierCharm : Charm_Basic
    {
        private static readonly List<RouletteModifierCharm> Active = new List<RouletteModifierCharm>();

        protected int EffectLevel => Math.Max(1, Math.Min(maxLevel, limitedEffectEnabledLevel));
        protected float EffectLevelProgress => EffectLevel / (float)maxLevel;

        // owner が装備中の強化アーティファクトをすべて反映した調整値を返す。
        public static RouletteParams Collect(UnitAvatar owner)
        {
            var result = new RouletteParams();
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var charm = Active[i];
                if (charm == null)
                {
                    Active.RemoveAt(i);
                    continue;
                }
                if (charm.NetworkAvatar == owner && charm.IsEffectEnabled)
                    charm.Modify(result);
            }
            return result;
        }

        protected abstract void Modify(RouletteParams parameters);

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            if (!Active.Contains(this))
                Active.Add(this);
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            Active.Remove(this);
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}
