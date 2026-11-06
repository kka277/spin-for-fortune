namespace SpinForFortune
{
    public class RouletteGreedyWagerCharm : RouletteModifierCharm
    {
        public const int ItemId = 100003;

        protected override void Modify(RouletteParams parameters)
        {
            parameters.rewardMultiplier *= 1.5f;
            parameters.winChanceMultiplier *= 0.75f;
        }
    }
}
