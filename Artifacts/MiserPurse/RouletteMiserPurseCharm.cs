namespace SpinForFortune
{
    public class RouletteMiserPurseCharm : RouletteModifierCharm
    {
        protected override void Modify(RouletteParams parameters)
        {
            parameters.moneyCostMultiplier *= 1f - 0.4f * EffectLevelProgress;
            parameters.cooldownMultiplier *= 1f + 0.2f * EffectLevelProgress;
        }
    }
}
