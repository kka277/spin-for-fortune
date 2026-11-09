namespace SpinForFortune
{
    // アーティファクト「高速ルーレット」。回転の速さを1.5倍にする。
    public class RouletteSpeedUpCharm : RouletteModifierCharm
    {
        public const float SpeedMultiplier = 1.5f;

        protected override void Modify(RouletteParams parameters)
        {
            parameters.speed *= 1f + (SpeedMultiplier - 1f) * EffectLevelProgress;
        }
    }
}
