using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpinForFortune
{
    public class RouletteGamblingComboEffect : ComboEffectBase
    {
        protected override void OnRequestComboData(UnitAvatar avatar, List<ComboEffectElement> elements)
        {
            if (avatar == null)
                return;

            var snapshot = RouletteProgression.GetDisplaySnapshot(avatar);

            string key = $"{RouletteArtifact.ModId}:roulette-gambling-combo-2";
            string format = new LocalizedString(key).ToString();
            elements.Add(new ComboEffectElement
            {
                comboCount = 2,
                effectName = string.Format(
                    CultureInfo.InvariantCulture,
                    format,
                    RouletteProgression.RewardBonusAtTwoPieces,
                    RouletteProgression.RewardBonusAtTwoPieces,
                    snapshot.SmallWins,
                    snapshot.MidWins,
                    snapshot.BigWins,
                    snapshot.JackpotWins,
                    snapshot.Heat,
                    snapshot.Streak,
                    snapshot.FeverSeconds)
            });

            key = $"{RouletteArtifact.ModId}:roulette-gambling-combo-4";
            elements.Add(new ComboEffectElement
            {
                comboCount = 4,
                effectName = string.Format(
                    CultureInfo.InvariantCulture,
                    new LocalizedString(key).ToString(),
                    RouletteProgression.RewardBonusAtFourPieces,
                    RouletteProgression.RewardBonusAtFourPieces)
            });

            key = $"{RouletteArtifact.ModId}:roulette-gambling-combo-6";
            elements.Add(new ComboEffectElement
            {
                comboCount = 6,
                effectName = string.Format(
                    CultureInfo.InvariantCulture,
                    new LocalizedString(key).ToString(),
                    RouletteProgression.RewardBonusAtSixPieces,
                    RouletteProgression.RewardBonusAtSixPieces)
            });
        }

        public override bool Weaved()
        {
            return true;
        }
    }

    internal struct RouletteProgressSnapshot
    {
        public int SmallWins;
        public int MidWins;
        public int BigWins;
        public int JackpotWins;
        public int Heat;
        public int Streak;
        public float FeverSeconds;
    }

    internal static class RouletteProgression
    {
        private const string ProgressMessagePrefix = "SpinForFortune:roulette-progress:";
        private static readonly Dictionary<UnitAvatar, ProgressState> States = new Dictionary<UnitAvatar, ProgressState>();
        private static RouletteProgressSnapshot clientSnapshot;
        private static bool hasClientSnapshot;

        private sealed class ProgressState
        {
            public readonly HashSet<RouletteCharm> ActiveCharms = new HashSet<RouletteCharm>();
            public RouletteProgressSnapshot Snapshot;
            public bool ResetForDeath;
            public int LastTickFrame = -1;
        }

        internal const int RewardBonusAtTwoPieces = 20;
        internal const int RewardBonusAtFourPieces = 40;
        internal const int RewardBonusAtSixPieces = 60;
        internal const int MaxHeat = 10;
        internal const float FeverDuration = 10f;
        internal const float MaxFeverDuration = 15f;
        internal const float FeverRewardMultiplier = 1.5f;

        internal static float GetRewardMultiplier(int pieces)
        {
            int bonusPercent = pieces >= 6
                ? RewardBonusAtSixPieces
                : pieces >= 4
                    ? RewardBonusAtFourPieces
                    : pieces >= 2
                        ? RewardBonusAtTwoPieces
                        : 0;
            return 1f + bonusPercent / 100f;
        }

        internal static void RegisterCharm(RouletteCharm charm, UnitAvatar owner)
        {
            if (charm == null || owner == null)
                return;

            if (!States.TryGetValue(owner, out var state))
            {
                state = new ProgressState();
                States.Add(owner, state);
            }
            state.ActiveCharms.Add(charm);
            state.ResetForDeath = false;
            Publish(owner, state.Snapshot, true);
        }

        internal static void UnregisterCharm(RouletteCharm charm, UnitAvatar owner)
        {
            if (charm == null || owner == null || !States.TryGetValue(owner, out var state))
                return;

            state.ActiveCharms.Remove(charm);
            if (state.ActiveCharms.Count == 0)
                Reset(owner, state, true);
            else
                Publish(owner, state.Snapshot, true);
        }

        internal static void ResetOnDeath(UnitAvatar owner)
        {
            if (owner == null || !States.TryGetValue(owner, out var state))
                return;

            if (!owner.IsDead)
            {
                state.ResetForDeath = false;
                return;
            }

            if (state.ResetForDeath)
                return;

            state.ResetForDeath = true;
            Reset(owner, state, false);
        }

        internal static void Tick(UnitAvatar owner, float deltaTime)
        {
            if (owner == null || !States.TryGetValue(owner, out var state) ||
                state.LastTickFrame == UnityEngine.Time.frameCount)
                return;

            state.LastTickFrame = UnityEngine.Time.frameCount;
            if (state.Snapshot.FeverSeconds <= 0f)
                return;

            state.Snapshot.FeverSeconds = Math.Max(0f, state.Snapshot.FeverSeconds - deltaTime);
            if (state.Snapshot.FeverSeconds <= 0f)
                Publish(owner, state.Snapshot, state.ActiveCharms.Count > 0);
        }

        internal static float GetFeverSeconds(UnitAvatar owner)
        {
            return owner != null && States.TryGetValue(owner, out var state)
                ? state.Snapshot.FeverSeconds
                : 0f;
        }

        internal static void RecordOutcome(UnitAvatar owner, RouletteTier tier, bool reach)
        {
            if (owner == null || !States.TryGetValue(owner, out var state))
                return;

            if (tier == RouletteTier.Miss)
            {
                state.Snapshot.Streak = 0;
                state.Snapshot.Heat = Math.Max(0, state.Snapshot.Heat - (reach ? 1 : 2));
            }
            else
            {
                state.Snapshot.Streak = Increment(state.Snapshot.Streak);
                int baseHeat = HeatForTier(tier);
                int streakBonus = Math.Min(2, Math.Max(0, state.Snapshot.Streak - 1) / 2);
                state.Snapshot.Heat = Math.Min(MaxHeat, state.Snapshot.Heat + baseHeat + streakBonus);

                int pieces = owner.Inventory == null
                    ? 0
                    : owner.Inventory.GetSetEffectCount(RouletteArtifact.GamblingCategoryId);
                if (pieces >= 2)
                {
                    if (tier == RouletteTier.Small)
                        state.Snapshot.SmallWins = Increment(state.Snapshot.SmallWins);
                    else if (tier == RouletteTier.Mid)
                        state.Snapshot.MidWins = Increment(state.Snapshot.MidWins);
                    else if (tier == RouletteTier.Big)
                        state.Snapshot.BigWins = Increment(state.Snapshot.BigWins);
                    else if (tier == RouletteTier.Jackpot)
                        state.Snapshot.JackpotWins = Increment(state.Snapshot.JackpotWins);
                }

                if (state.Snapshot.Heat >= MaxHeat)
                {
                    state.Snapshot.Heat = 0;
                    state.Snapshot.FeverSeconds = Math.Min(
                        MaxFeverDuration,
                        state.Snapshot.FeverSeconds + FeverDuration);
                }
            }

            state.ResetForDeath = false;
            Publish(owner, state.Snapshot, state.ActiveCharms.Count > 0);
        }

        private static int HeatForTier(RouletteTier tier)
        {
            switch (tier)
            {
                case RouletteTier.Small: return 1;
                case RouletteTier.Mid: return 2;
                case RouletteTier.Big: return 3;
                case RouletteTier.Jackpot: return 5;
                default: return 0;
            }
        }

        internal static RouletteProgressSnapshot GetDisplaySnapshot(UnitAvatar avatar)
        {
            if (avatar != null && avatar.isLocalPlayer && hasClientSnapshot)
                return clientSnapshot;
            if (avatar != null && States.TryGetValue(avatar, out var state))
                return state.Snapshot;
            return new RouletteProgressSnapshot();
        }

        internal static void ResetClientSnapshot()
        {
            clientSnapshot = new RouletteProgressSnapshot();
            hasClientSnapshot = true;
            RouletteUI.SetProgression(0, 0, 0f, false);
        }

        internal static bool HandleClientMessage(string message)
        {
            if (message == null || !message.StartsWith(ProgressMessagePrefix, StringComparison.Ordinal))
                return false;

            var values = message.Substring(ProgressMessagePrefix.Length).Split('|');
            if (values.Length != 8 ||
                !TryParseNonNegative(values[0], out int smallWins) ||
                !TryParseNonNegative(values[1], out int midWins) ||
                !TryParseNonNegative(values[2], out int bigWins) ||
                !TryParseNonNegative(values[3], out int jackpotWins) ||
                !TryParseNonNegative(values[4], out int heat) ||
                heat >= MaxHeat ||
                !TryParseNonNegative(values[5], out int streak) ||
                !float.TryParse(values[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float feverSeconds) ||
                feverSeconds < 0f || feverSeconds > MaxFeverDuration ||
                float.IsNaN(feverSeconds) || float.IsInfinity(feverSeconds) ||
                (values[7] != "0" && values[7] != "1"))
            {
                UnityEngine.Debug.LogWarning("[SpinForFortune] ルーレット成長メッセージの形式が不正です。");
                return true;
            }

            clientSnapshot = new RouletteProgressSnapshot
            {
                SmallWins = smallWins,
                MidWins = midWins,
                BigWins = bigWins,
                JackpotWins = jackpotWins,
                Heat = heat,
                Streak = streak,
                FeverSeconds = feverSeconds
            };
            hasClientSnapshot = true;
            RouletteUI.SetProgression(heat, streak, feverSeconds, values[7] == "1");
            return true;
        }

        private static void Reset(UnitAvatar owner, ProgressState state, bool removeState)
        {
            state.Snapshot = new RouletteProgressSnapshot();
            Publish(owner, state.Snapshot, state.ActiveCharms.Count > 0);
            if (removeState)
                States.Remove(owner);
        }

        private static void Publish(UnitAvatar owner, RouletteProgressSnapshot snapshot, bool active)
        {
            if (owner == null)
                return;

            if (owner.isLocalPlayer)
            {
                clientSnapshot = snapshot;
                hasClientSnapshot = true;
                RouletteUI.SetProgression(snapshot.Heat, snapshot.Streak, snapshot.FeverSeconds, active);
                return;
            }

            if (owner.connectionToClient != null)
                owner.TargetSendCustomMessage(owner.connectionToClient, Serialize(snapshot, active));
        }

        private static string Serialize(RouletteProgressSnapshot snapshot, bool active)
        {
            return ProgressMessagePrefix +
                snapshot.SmallWins.ToString(CultureInfo.InvariantCulture) + "|" +
                snapshot.MidWins.ToString(CultureInfo.InvariantCulture) + "|" +
                snapshot.BigWins.ToString(CultureInfo.InvariantCulture) + "|" +
                snapshot.JackpotWins.ToString(CultureInfo.InvariantCulture) + "|" +
                snapshot.Heat.ToString(CultureInfo.InvariantCulture) + "|" +
                snapshot.Streak.ToString(CultureInfo.InvariantCulture) + "|" +
                snapshot.FeverSeconds.ToString("R", CultureInfo.InvariantCulture) + "|" +
                (active ? "1" : "0");
        }

        private static bool TryParseNonNegative(string value, out int result)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result >= 0;
        }

        private static int Increment(int value)
        {
            return value == int.MaxValue ? value : value + 1;
        }
    }
}
