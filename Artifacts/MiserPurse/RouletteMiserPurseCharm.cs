namespace SpinForFortune
{
    public class RouletteMiserPurseCharm : RouletteModifierCharm
    {
        public const int ItemId = 100004;

        protected override void Modify(RouletteParams parameters)
        {
            parameters.moneyCostMultiplier *= 0.6f;
            parameters.cooldownMultiplier *= 1.2f;
        }
    }
}
