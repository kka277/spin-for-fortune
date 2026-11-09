namespace SpinForFortune
{
    public class RouletteBloodContractCharm : RouletteModifierCharm
    {
        protected override void Modify(RouletteParams parameters)
        {
            parameters.hpCostMultiplier *= 1f - 0.5f * EffectLevelProgress;
            parameters.moneyCostMultiplier *= 1f + 0.2f * EffectLevelProgress;
        }
    }
}
