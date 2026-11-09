namespace SpinForFortune
{
    public class RouletteGreedyWagerCharm : RouletteModifierCharm
    {
        protected override void Modify(RouletteParams parameters)
        {
            parameters.rewardMultiplier *= 1f + 0.5f * EffectLevelProgress;
            parameters.winChanceMultiplier *= 1f - 0.25f * EffectLevelProgress;
        }
    }
}
