namespace SpinForFortune
{
    public class RouletteFeverAmuletCharm : RouletteModifierCharm
    {
        public const int ItemId = 100006;

        protected override void Modify(RouletteParams parameters)
        {
            parameters.buffDurationMultiplier *= 1.5f;
            parameters.cooldownMultiplier *= 1.2f;
        }
    }
}
