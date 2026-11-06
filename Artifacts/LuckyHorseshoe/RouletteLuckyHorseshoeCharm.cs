namespace SpinForFortune
{
    public class RouletteLuckyHorseshoeCharm : RouletteModifierCharm
    {
        public const int ItemId = 100002;

        protected override void Modify(RouletteParams parameters)
        {
            parameters.winChanceMultiplier *= 1.25f;
            parameters.rewardMultiplier *= 0.8f;
        }
    }
}
