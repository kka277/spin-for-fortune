namespace SpinForFortune
{
    // ルーレット1回分の調整値。所持中の強化アーティファクトが倍率を掛け合わせる。
    // 新しい調整項目を足すときは、ここにフィールドを足し、RouletteCharm / RouletteTable で使う。
    public class RouletteParams
    {
        // 回転の速さ。2なら所要時間が半分になる。
        public float speed = 1f;

        public float moneyCostMultiplier = 1f;
        public float hpCostMultiplier = 1f;
        public float cooldownMultiplier = 1f;

        // 当たり(小〜ジャックポット)の確率にかかる倍率。
        public float winChanceMultiplier = 1f;

        // 獲得金額・バフ量などの報酬にかかる倍率。
        public float rewardMultiplier = 1f;

        // カテゴリコンボで強化するバフ時間の倍率。
        public float buffDurationMultiplier = 1f;
    }
}
