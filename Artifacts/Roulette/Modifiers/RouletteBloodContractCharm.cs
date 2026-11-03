namespace SpinForFortune
{
    public class RouletteBloodContractCharm : RouletteModifierCharm
    {
        public const int ItemId = 2000000105;

        protected override void Modify(RouletteParams parameters)
        {
            parameters.hpCostMultiplier *= 0.5f;
            parameters.moneyCostMultiplier *= 1.2f;
        }
    }
}
