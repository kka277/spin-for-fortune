using System;

namespace SpinForFortune
{
    public enum RouletteTier
    {
        Miss,
        Small,
        Mid,
        Big,
        Jackpot
    }

    public class RouletteSpin
    {
        public RouletteTier tier;
        public int[] reels;

        // 最初の2リールが揃った状態。当たりでもハズレでも発生する。
        public bool reach;

        // 回転の速さ(1が標準)。リールの停止時刻とリーチ演出の長さに反映される。
        public float speed = 1f;

        public float Reel1StopTime => RouletteTable.Reel1StopTime / speed;
        public float Reel2StopTime => RouletteTable.Reel2StopTime / speed;
        public float Reel3StopTime => reach ? Reel2StopTime + RouletteTable.ReachSlowTime / speed : Reel2StopTime + 0.6f / speed;
    }

    // 抽選テーブルと調整用の定数。数値を変える場合はここだけを触る。
    public static class RouletteTable
    {
        public const int ItemId = 2000000100;
        public const int MoneyCost = 30;
        public const int HpCost = 5;
        public const float Cooldown = 0.5f;

        public const float Reel1StopTime = 0.6f;
        public const float Reel2StopTime = 1.2f;
        public const float ReachSlowTime = 2.8f;

        // 抽選確率(%)。残りがハズレ。
        public const float JackpotPercent = 1.5f;
        public const float BigPercent = 4.5f;
        public const float MidPercent = 9f;
        public const float SmallPercent = 15f;
        // ハズレのうちリーチになる割合(全体に対する%)。
        public const float ReachMissPercent = 15f;

        public const int SmallMoney = 50;
        public const int MidBuffDamageBonus = 30;
        public const float MidBuffSeconds = 10f;
        public const float BigHealPercent = 25f;
        public const float BigDamageRadius = 14f;
        public const float BigBaseDamage = 100f;
        public const int JackpotMoney = 500;
        public const float JackpotInvincibleSeconds = 20f;
        public const int JackpotBuffDamageBonus = 50;
        public const float JackpotBuffSeconds = 20f;

        // 0..3 は当たり図柄(Small/Mid/Big/Jackpot)、4..5 はハズレ専用の図柄。
        public static readonly string[] SymbolNames = { "CHERRY", "BELL", "BAR", "7", "LEMON", "PLUM" };
        public static readonly string[] SymbolFileNames = { "cherry", "bell", "bar", "seven", "lemon", "plum" };
        public const int SymbolCount = 6;

        public static int SymbolOf(RouletteTier tier)
        {
            switch (tier)
            {
                case RouletteTier.Small: return 0;
                case RouletteTier.Mid: return 1;
                case RouletteTier.Big: return 2;
                case RouletteTier.Jackpot: return 3;
                default: return -1;
            }
        }

        public static RouletteSpin Roll(Random rng, RouletteParams parameters = null)
        {
            parameters = parameters ?? new RouletteParams();
            float m = parameters.winChanceMultiplier;
            double r = rng.NextDouble() * 100.0;
            var tier = RouletteTier.Miss;
            double threshold = JackpotPercent * m;
            if (r < threshold) tier = RouletteTier.Jackpot;
            else if (r < (threshold += BigPercent * m)) tier = RouletteTier.Big;
            else if (r < (threshold += MidPercent * m)) tier = RouletteTier.Mid;
            else if (r < (threshold += SmallPercent * m)) tier = RouletteTier.Small;

            var spin = new RouletteSpin { tier = tier, speed = Math.Max(0.1f, parameters.speed) };
            if (tier != RouletteTier.Miss)
            {
                int s = SymbolOf(tier);
                spin.reels = new[] { s, s, s };
                spin.reach = true;
                return spin;
            }

            if (r < threshold + ReachMissPercent)
            {
                int s = rng.Next(SymbolCount);
                int other = (s + 1 + rng.Next(SymbolCount - 1)) % SymbolCount;
                spin.reels = new[] { s, s, other };
                spin.reach = true;
                return spin;
            }

            int a = rng.Next(SymbolCount);
            int b = (a + 1 + rng.Next(SymbolCount - 1)) % SymbolCount;
            spin.reels = new[] { a, b, rng.Next(SymbolCount) };
            return spin;
        }
    }
}
