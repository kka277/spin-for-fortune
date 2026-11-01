namespace SpinForFortune
{
    // アーティファクト「高速ルーレット」。回転の速さを1.5倍にする。
    public class RouletteSpeedUpCharm : RouletteModifierCharm
    {
        public const int ItemId = 2000000101;
        public const float SpeedMultiplier = 1.5f;

        protected override void Modify(RouletteParams parameters)
        {
            parameters.speed *= SpeedMultiplier;
        }
    }
}
