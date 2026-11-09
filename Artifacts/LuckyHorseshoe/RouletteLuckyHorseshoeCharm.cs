namespace SpinForFortune
{
    public class RouletteLuckyHorseshoeCharm : RouletteModifierCharm
    {
        protected override void Modify(RouletteParams parameters)
        {
            parameters.winChanceMultiplier *= 1f + 0.25f * EffectLevelProgress;
            parameters.rewardMultiplier *= 1f - 0.2f * EffectLevelProgress;
        }
    }
}
