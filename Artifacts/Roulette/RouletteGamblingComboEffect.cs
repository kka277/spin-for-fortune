using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpinForFortune
{
    public class RouletteGamblingComboEffect : ComboEffectBase
    {
        private bool rouletteGrantPending;

        public override EComboEnableResult Enable(int comboCount, out int appliedComboEffectCount)
        {
            var result = base.Enable(comboCount, out appliedComboEffectCount);
            if (isEnabled)
            {
                RouletteProgression.EnableCombo(avatar);
                rouletteGrantPending = true;
            }
            return result;
        }

        protected override void OnDisableEffect()
        {
            base.OnDisableEffect();
            RouletteProgression.DisableCombo(avatar);
        }

        private void Update()
        {
            if (!Mirror.NetworkServer.active || !isEnabled)
                return;

            if (rouletteGrantPending)
            {
                rouletteGrantPending = false;
                RouletteArtifact.GrantRouletteIfMissing(avatar);
            }

            RouletteProgression.ResetOnDeath(avatar);
            RouletteProgression.Tick(avatar, UnityEngine.Time.deltaTime);
        }

        protected override void OnRequestComboData(UnitAvatar avatar, List<ComboEffectElement> elements)
        {
            if (avatar == null)
                return;

            string key = $"{RouletteArtifact.ModId}:roulette-gambling-combo-2";
            string format = new LocalizedString(key).ToString();
            string effectName = string.Format(
                CultureInfo.InvariantCulture,
                format,
                RouletteProgression.RewardBonusAtTwoPieces,
                RouletteProgression.RewardBonusAtTwoPieces);
            elements.Add(new ComboEffectElement
            {
                comboCount = 2,
                effectName = $"{effectName}\n<tag={RouletteArtifact.HeatDetailKeyword}>\n<tag={RouletteArtifact.StreakDetailKeyword}>"
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

        private sealed class ProgressState
        {
            public readonly HashSet<RouletteCharm> ActiveCharms = new HashSet<RouletteCharm>();
            public RouletteProgressSnapshot Snapshot;
            public bool ComboActive;
            public bool ResetForDeath;
            public int LastTickFrame = -1;
            public float DisabledAt = -1f;
        }

        // 装備変更などで効果が一瞬だけ無効→有効に切り替わっても進行を保持する猶予時間。
        private const float DisableGraceSeconds = 2f;

        private static void RestoreOrResetAfterDisable(ProgressState state)
        {
            if (state.DisabledAt >= 0f &&
                UnityEngine.Time.unscaledTime - state.DisabledAt > DisableGraceSeconds)
                state.Snapshot = new RouletteProgressSnapshot();
            state.DisabledAt = -1f;
        }

        internal const int RewardBonusAtTwoPieces = 20;
        internal const int RewardBonusAtFourPieces = 40;
        internal const int RewardBonusAtSixPieces = 60;
        internal const int MaxHeat = 10;
        internal const float FeverDuration = 10f;
        internal const float MaxFeverDuration = 15f;
        internal const float FeverRewardMultiplier = 1.5f;
        internal const int LuckPerStreak = 5;

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

        internal static void EnableCombo(UnitAvatar owner)
        {
            if (owner == null)
                return;

            if (!States.TryGetValue(owner, out var state))
            {
                state = new ProgressState();
                States.Add(owner, state);
            }
            state.ComboActive = true;
            state.ResetForDeath = false;
            RestoreOrResetAfterDisable(state);
            Publish(owner, state.Snapshot, true);
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
            RestoreOrResetAfterDisable(state);
            Publish(owner, state.Snapshot, state.ComboActive);
        }

        internal static void UnregisterCharm(RouletteCharm charm, UnitAvatar owner)
        {
            if (charm == null || owner == null || !States.TryGetValue(owner, out var state))
                return;

            state.ActiveCharms.Remove(charm);
            if (state.ActiveCharms.Count == 0)
            {
                state.DisabledAt = UnityEngine.Time.unscaledTime;
                Publish(owner, state.Snapshot, false);
            }
            else
                Publish(owner, state.Snapshot, state.ComboActive);
        }

        internal static void DisableCombo(UnitAvatar owner)
        {
            if (owner == null || !States.TryGetValue(owner, out var state))
                return;

            state.ComboActive = false;
            state.DisabledAt = UnityEngine.Time.unscaledTime;
            Publish(owner, state.Snapshot, false);
        }

        internal static bool IsComboActive(UnitAvatar owner)
        {
            return owner != null &&
                States.TryGetValue(owner, out var state) &&
                state.ComboActive;
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
            Reset(owner, state);
        }

        internal static void Tick(UnitAvatar owner, float deltaTime)
        {
            if (owner == null || !States.TryGetValue(owner, out var state) ||
                !state.ComboActive ||
                state.LastTickFrame == UnityEngine.Time.frameCount)
                return;

            state.LastTickFrame = UnityEngine.Time.frameCount;
            if (state.Snapshot.FeverSeconds <= 0f)
                return;

            state.Snapshot.FeverSeconds = Math.Max(0f, state.Snapshot.FeverSeconds - deltaTime);
            if (state.Snapshot.FeverSeconds <= 0f)
                Publish(owner, state.Snapshot, state.ComboActive);
            else if (owner.isServer)
                SyncFeverBuff(owner, owner.GetBuffInstanceByPrefab(RouletteArtifact.FeverProgressBuff) as RouletteProgressBuff);
        }

        internal static void SyncFeverBuff(UnitAvatar owner, RouletteProgressBuff buff)
        {
            if (buff != null && States.TryGetValue(owner, out var state) && state.Snapshot.FeverSeconds > 0f)
                SyncFeverTimer(buff, state.Snapshot.FeverSeconds);
        }

        internal static float GetFeverSeconds(UnitAvatar owner)
        {
            return owner != null && States.TryGetValue(owner, out var state)
                ? state.Snapshot.FeverSeconds
                : 0f;
        }

        internal static float GetStreakLuckBonus(UnitAvatar owner)
        {
            return owner != null &&
                States.TryGetValue(owner, out var state) &&
                state.ComboActive
                    ? state.Snapshot.Streak * LuckPerStreak
                    : 0f;
        }

        internal static void RecordOutcome(UnitAvatar owner, RouletteTier tier)
        {
            if (owner == null || !States.TryGetValue(owner, out var state) ||
                !state.ComboActive)
                return;

            if (tier == RouletteTier.Miss)
            {
                state.Snapshot.Streak = 0;
            }
            else
            {
                state.Snapshot.Streak = Increment(state.Snapshot.Streak);
                int baseHeat = HeatForTier(tier);
                int streakBonus = Math.Min(2, Math.Max(0, state.Snapshot.Streak - 1) / 2);
                state.Snapshot.Heat = Math.Min(MaxHeat, state.Snapshot.Heat + baseHeat + streakBonus);

                switch (tier)
                {
                    case RouletteTier.Small:
                        state.Snapshot.SmallWins = Increment(state.Snapshot.SmallWins);
                        break;
                    case RouletteTier.Mid:
                        state.Snapshot.MidWins = Increment(state.Snapshot.MidWins);
                        break;
                    case RouletteTier.Big:
                        state.Snapshot.BigWins = Increment(state.Snapshot.BigWins);
                        break;
                    case RouletteTier.Jackpot:
                        state.Snapshot.JackpotWins = Increment(state.Snapshot.JackpotWins);
                        break;
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
            Publish(owner, state.Snapshot, state.ComboActive);
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

        internal static void ResetClientFever()
        {
            RouletteUI.SetFever(0f);
        }

        internal static bool HandleClientMessage(string message)
        {
            if (message == null || !message.StartsWith(ProgressMessagePrefix, StringComparison.Ordinal))
                return false;

            var values = message.Substring(ProgressMessagePrefix.Length).Split('|');
            if (values.Length != 1 ||
                !float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float feverSeconds) ||
                feverSeconds < 0f || feverSeconds > MaxFeverDuration ||
                float.IsNaN(feverSeconds) || float.IsInfinity(feverSeconds))
            {
                UnityEngine.Debug.LogWarning("[SpinForFortune] ルーレット成長メッセージの形式が不正です。");
                return true;
            }

            RouletteUI.SetFever(feverSeconds);
            return true;
        }

        private static void Reset(UnitAvatar owner, ProgressState state)
        {
            state.Snapshot = new RouletteProgressSnapshot();
            Publish(owner, state.Snapshot, state.ComboActive);
        }

        private static void Publish(UnitAvatar owner, RouletteProgressSnapshot snapshot, bool active)
        {
            if (owner == null)
                return;

            if (owner.isServer)
                UpdateProgressBuffs(owner, snapshot, active);

            if (owner.isLocalPlayer)
            {
                RouletteUI.SetFever(snapshot.FeverSeconds);
                return;
            }

            if (owner.connectionToClient != null)
                owner.TargetSendCustomMessage(owner.connectionToClient, Serialize(snapshot));
        }

        private static void UpdateProgressBuffs(UnitAvatar owner, RouletteProgressSnapshot snapshot, bool active)
        {
            UpdateProgressBuff(owner, RouletteArtifact.HeatProgressBuff, snapshot.Heat, active);
            UpdateProgressBuff(owner, RouletteArtifact.StreakProgressBuff, snapshot.Streak, active);
            UpdateProgressBuff(owner, RouletteArtifact.FeverProgressBuff, 1, active && snapshot.FeverSeconds > 0f, snapshot.FeverSeconds);
        }

        private static void UpdateProgressBuff(
            UnitAvatar owner,
            RouletteProgressBuff prefab,
            int stack,
            bool active,
            float duration = 1000000f)
        {
            if (prefab == null)
            {
                UnityEngine.Debug.LogError("[SpinForFortune] ルーレット進行バフのプレハブが登録されていません。");
                return;
            }

            var buff = owner.GetBuffInstanceByPrefab(prefab) as RouletteProgressBuff;
            if (!active || stack <= 0)
            {
                if (buff != null)
                    owner.ForcedEndBuff(buff);
                return;
            }

            if (buff == null)
            {
                buff = owner.ApplyBuff(prefab, 1f, owner);
                if (buff == null)
                {
                    UnityEngine.Debug.LogError($"[SpinForFortune] ルーレット進行バフを付与できませんでした: {prefab.ID}");
                    return;
                }
            }

            buff.SetProgressStack(stack);
            if (duration < 1000000f)
                SyncFeverTimer(buff, duration);
        }

        private static readonly System.Reflection.FieldInfo BuffTimerField =
            typeof(CharacterBuff).GetField("buffTimer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        // バフのタイマーは経過時間/全体時間で動くため、残り時間から経過割合を逆算して合わせる。
        private static void SyncFeverTimer(RouletteProgressBuff buff, float remaining)
        {
            var timer = BuffTimerField?.GetValue(buff) as Timer;
            if (timer == null)
                return;

            timer.time = MaxFeverDuration;
            timer.Ratio = 1f - UnityEngine.Mathf.Clamp01(remaining / MaxFeverDuration);
        }

        private static string Serialize(RouletteProgressSnapshot snapshot)
        {
            return ProgressMessagePrefix + snapshot.FeverSeconds.ToString("R", CultureInfo.InvariantCulture);
        }

        private static int Increment(int value)
        {
            return value == int.MaxValue ? value : value + 1;
        }
    }
}
