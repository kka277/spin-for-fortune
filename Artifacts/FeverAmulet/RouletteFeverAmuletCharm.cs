namespace SpinForFortune
{
    public class RouletteFeverAmuletCharm : RouletteModifierCharm
    {
        protected override void Modify(RouletteParams parameters)
        {
            parameters.buffDurationMultiplier *= 1f + 0.5f * EffectLevelProgress;
            parameters.cooldownMultiplier *= 1f + 0.2f * EffectLevelProgress;
        }
    }
}
