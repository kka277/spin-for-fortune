using System;
using System.Globalization;
using UnityEngine;

namespace SpinForFortune
{
    // サーバー抽選の確定結果を所有者クライアントへ送り、演出と報酬を一致させる。
    public class RouletteCharm : Charm_Basic
    {
        private const string SpinMessagePrefix = "SpinForFortune:roulette-start:";
        private const int MaxFreeSpins = 3;
        private const float FreeSpinDelay = 3f;
        private readonly System.Random rng = new System.Random();
        private RouletteSpin pending;
        private RouletteParams pendingParams;
        private float pendingTimer;
        private float cooldown;
        private float freeSpinDelay;
        private int freeSpinsIssued;
        private bool freeChainActive;

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            RouletteProgression.RegisterCharm(this, NetworkAvatar);
            NetworkAvatar.OnAttackUnit += HandleAttackUnit;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            if (NetworkAvatar != null)
                NetworkAvatar.OnAttackUnit -= HandleAttackUnit;
            if (pending != null)
                FinishSpin(false);
            freeChainActive = false;
            freeSpinsIssued = 0;
            pendingParams = null;
            RouletteProgression.UnregisterCharm(this, NetworkAvatar);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            RouletteProgression.ResetOnDeath(NetworkAvatar);
            if (NetworkAvatar == null || NetworkAvatar.IsDead)
            {
                pending = null;
                pendingParams = null;
                freeChainActive = false;
                freeSpinsIssued = 0;
                return;
            }

            float dt = Time.deltaTime;
            RouletteProgression.Tick(NetworkAvatar, dt);
            if (cooldown > 0f)
                cooldown -= dt;

            if (pending != null)
            {
                pendingTimer -= dt;
                if (pendingTimer <= 0f)
                    FinishSpin();
            }
            else if (freeChainActive)
            {
                freeSpinDelay -= dt;
                if (freeSpinDelay <= 0f)
                {
                    if (freeSpinsIssued >= MaxFreeSpins)
                    {
                        FinishFreeChain(pendingParams);
                        return;
                    }

                    freeSpinsIssued++;
                    StartSpin(pendingParams, freeSpinsIssued);
                }
            }
        }

        private void HandleAttackUnit(UnitAvatar target, DamageInstance damage)
        {
            if (!IsEffectEnabled || NetworkAvatar == null || NetworkAvatar.IsDead || target == null)
                return;
            if (target is DamageDummy || target is Unit_Scarecrow)
                return;
            if (damage.fromType != EDamageFromType.DirectAttack ||
                pending != null || freeChainActive || cooldown > 0f)
                return;

            var parameters = RouletteModifierCharm.Collect(NetworkAvatar);
            int gamblingPieces = NetworkAvatar.Inventory == null
                ? 0
                : NetworkAvatar.Inventory.GetSetEffectCount(RouletteArtifact.GamblingCategoryId);
            float comboMultiplier = RouletteProgression.GetRewardMultiplier(gamblingPieces);
            parameters.rewardMultiplier *= comboMultiplier;
            parameters.buffDurationMultiplier *= comboMultiplier;

            int moneyCost = Mathf.Max(0, Mathf.RoundToInt(RouletteTable.MoneyCost * parameters.moneyCostMultiplier));
            int hpCost = Mathf.Max(0, Mathf.RoundToInt(RouletteTable.HpCost * parameters.hpCostMultiplier));

            // お金が無く、HPも支払えない場合は回さない(HPは1より下がらない)。
            if (NetworkAvatar.HasMoney(moneyCost))
                NetworkAvatar.SubMoney(moneyCost);
            else if (NetworkAvatar.hp > hpCost)
                NetworkAvatar.Networkhp = NetworkAvatar.hp - hpCost;
            else
                return;

            freeSpinsIssued = 0;
            freeChainActive = false;
            pendingParams = parameters;
            StartSpin(parameters, 0);
        }

        internal static void HandleClientMessage(string message)
        {
            if (RouletteProgression.HandleClientMessage(message))
                return;
            if (message == null || !message.StartsWith(SpinMessagePrefix, StringComparison.Ordinal))
                return;

            var values = message.Substring(SpinMessagePrefix.Length).Split('|');
            if (values.Length != 7 ||
                !int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int tierValue) ||
                tierValue < (int)RouletteTier.Miss || tierValue > (int)RouletteTier.Jackpot ||
                !int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int reel1) ||
                reel1 < 0 || reel1 >= RouletteTable.SymbolCount ||
                !int.TryParse(values[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int reel2) ||
                reel2 < 0 || reel2 >= RouletteTable.SymbolCount ||
                !int.TryParse(values[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int reel3) ||
                reel3 < 0 || reel3 >= RouletteTable.SymbolCount ||
                (values[4] != "0" && values[4] != "1") ||
                !float.TryParse(values[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float speed) ||
                speed <= 0f || float.IsNaN(speed) || float.IsInfinity(speed) ||
                !int.TryParse(values[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int freeSpinNumber) ||
                freeSpinNumber < 0 || freeSpinNumber > MaxFreeSpins)
            {
                Debug.LogWarning("[SpinForFortune] ルーレット演出開始メッセージの形式が不正です。");
                return;
            }

            var spin = new RouletteSpin
            {
                tier = (RouletteTier)tierValue,
                reels = new[] { reel1, reel2, reel3 },
                reach = values[4] == "1",
                speed = speed
            };
            PlayPresentation(spin, freeSpinNumber);
        }

        private void StartSpin(RouletteParams parameters, int freeSpinNumber)
        {
            pendingParams = parameters ?? new RouletteParams();
            pending = RouletteTable.Roll(rng, pendingParams);
            pendingTimer = pending.Reel3StopTime;

            if (NetworkAvatar.isLocalPlayer)
                PlayPresentation(pending, freeSpinNumber);
            else if (NetworkAvatar.connectionToClient != null)
                NetworkAvatar.TargetSendCustomMessage(
                    NetworkAvatar.connectionToClient,
                    SerializeSpinStart(pending, freeSpinNumber));
            else
                Debug.LogWarning("[SpinForFortune] ルーレット演出を送信できません。所有者の接続がありません。");
        }

        private static void PlayPresentation(RouletteSpin spin, int freeSpinNumber)
        {
            RouletteUI.Play(spin, freeSpinNumber, MaxFreeSpins);
        }

        private static string SerializeSpinStart(RouletteSpin spin, int freeSpinNumber)
        {
            return SpinMessagePrefix +
                ((int)spin.tier).ToString(CultureInfo.InvariantCulture) + "|" +
                spin.reels[0].ToString(CultureInfo.InvariantCulture) + "|" +
                spin.reels[1].ToString(CultureInfo.InvariantCulture) + "|" +
                spin.reels[2].ToString(CultureInfo.InvariantCulture) + "|" +
                (spin.reach ? "1" : "0") + "|" +
                spin.speed.ToString("R", CultureInfo.InvariantCulture) + "|" +
                freeSpinNumber.ToString(CultureInfo.InvariantCulture);
        }

        private void FinishSpin(bool allowFreeChain = true)
        {
            var spin = pending;
            var parameters = pendingParams ?? new RouletteParams();
            pending = null;
            if (spin == null || NetworkAvatar == null || NetworkAvatar.IsDead)
                return;

            float reward = parameters.rewardMultiplier;
            if (RouletteProgression.GetFeverSeconds(NetworkAvatar) > 0f)
                reward *= RouletteProgression.FeverRewardMultiplier;

            RouletteProgression.RecordOutcome(NetworkAvatar, spin.tier, spin.reach);
            switch (spin.tier)
            {
                case RouletteTier.Small:
                    NetworkAvatar.AddMoney(Scale(RouletteTable.SmallMoney, reward));
                    break;
                case RouletteTier.Mid:
                {
                    var midBuff = NetworkAvatar.ApplyBuff(RouletteArtifact.MidDamageBuff, reward, NetworkAvatar);
                    if (midBuff == null)
                        Debug.LogError("[SpinForFortune] 中当たりの攻撃力バフを付与できませんでした。");
                    else
                        midBuff.BuffDuration *= parameters.buffDurationMultiplier;
                    break;
                }
                case RouletteTier.Big:
                    NetworkAvatar.HealPercent(RouletteTable.BigHealPercent * reward);
                    DamageNearbyEnemies(reward);
                    break;
                case RouletteTier.Jackpot:
                {
                    NetworkAvatar.AddMoney(Scale(RouletteTable.JackpotMoney, reward));
                    var invulnerabilityBuff = NetworkAvatar.ApplyBuff(RouletteArtifact.InvulnerabilityBuff, 1f, NetworkAvatar);
                    if (invulnerabilityBuff == null)
                        Debug.LogError("[SpinForFortune] ジャックポットの無敵バフを付与できませんでした。");
                    else
                        invulnerabilityBuff.BuffDuration *= parameters.buffDurationMultiplier;

                    var jackpotBuff = NetworkAvatar.ApplyBuff(RouletteArtifact.JackpotDamageBuff, reward, NetworkAvatar);
                    if (jackpotBuff == null)
                        Debug.LogError("[SpinForFortune] ジャックポットの攻撃力バフを付与できませんでした。");
                    else
                        jackpotBuff.BuffDuration *= parameters.buffDurationMultiplier;
                    break;
                }
            }

            if (allowFreeChain && spin.tier >= RouletteTier.Mid && freeSpinsIssued < MaxFreeSpins)
            {
                freeChainActive = true;
                freeSpinDelay = FreeSpinDelay;
                return;
            }

            FinishFreeChain(parameters);
        }

        private void FinishFreeChain(RouletteParams parameters = null)
        {
            freeChainActive = false;
            freeSpinsIssued = 0;
            pendingParams = null;
            cooldown = RouletteTable.Cooldown * (parameters ?? new RouletteParams()).cooldownMultiplier;
        }

        private static int Scale(int value, float multiplier) => Mathf.RoundToInt(value * multiplier);

        private void DamageNearbyEnemies(float rewardMultiplier)
        {
            float damage = (RouletteTable.BigBaseDamage + NetworkAvatar.GetCustomStat(ECustomStat.PhysicalDamage) * 5f) * rewardMultiplier;
            Vector3 origin = NetworkAvatar.transform.position;
            long hostile = NetworkAvatar.GetHostileFactionLayers(EDamageFromType.None);
            foreach (var unit in FindObjectsByType<UnitAvatar>(FindObjectsSortMode.None))
            {
                if (unit == null || unit == NetworkAvatar || unit.IsDead)
                    continue;
                if ((unit.transform.position - origin).sqrMagnitude > RouletteTable.BigDamageRadius * RouletteTable.BigDamageRadius)
                    continue;

                var instance = DamageInstance.GetDamage(NetworkAvatar, "roulette-big", unit.transform.position, hostile, damage,
                    EDamageType.Projectile, EDamageFromType.None, Vector2.zero, 1, 1f);
                instance.elementalType = EDamageElementalType.Physical;
                unit.ApplyDamage(instance);
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}
