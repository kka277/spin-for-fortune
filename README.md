# Spin for Fortune

A Sephiria mod featuring the **Roulette** artifact and its upgrade, **Roulette Speed Up**.

## Roulette

Roulette is a Legendary artifact that turns weapon attacks into slot spins. Hitting a normal enemy with a direct weapon attack starts a spin. It does not trigger on scarecrows or training damage dummies, and attacks are ignored while the reels are spinning or during the 0.5-second cooldown.

Each paid spin costs **30 gold**. If you do not have enough gold, it costs **5 HP** instead; this cost cannot reduce you below 1 HP. The spin will not start if you cannot pay either cost.

### Spin results

| Result | Chance | Reels | Reward |
|---|---:|---|---|
| Small win | 15% | CHERRY | Gain 50 gold. |
| Mid win | 9% | BELL | Gain +30% to all damage for 10 seconds. |
| Big win | 4.5% | BAR | Restore 25% HP and deal damage to nearby enemies (radius 14; 100 + 5× your Physical Damage). |
| Jackpot | 1.5% | 7 | Gain 500 gold, become invulnerable for 20 seconds, and gain +50% to all damage for 20 seconds. |
| Miss | 70% | No winning match | No reward. 15% of all spins are near-misses, where the first two reels match but the last reel does not. |

The three reels stop one at a time. When the first two match, the spin shows a near-miss: the last reel slows down, with visual and audio effects. The displayed result always matches the reward awarded.

### Heat, streaks, and Fever

- Every win increases your **Heat**: Small +1, Mid +2, Big +3, and Jackpot +5.
- Consecutive wins build a streak and add bonus Heat: starting with the third consecutive win, every two consecutive wins add +1 Heat, up to +2 bonus Heat per win.
- A miss ends the streak. A regular miss removes 2 Heat; a near-miss removes only 1. Heat is capped at 10.
- At 10 Heat, you enter **Fever** for 10 seconds. Fever increases Roulette rewards by 50% on subsequent spins; the spin that activates Fever does not receive this bonus.
- Reaching 10 Heat again during Fever adds 10 seconds, up to a maximum of 15 seconds remaining.
- Fever affects Roulette rewards, but does not extend the Jackpot's invulnerability duration.

The Roulette UI displays your current Heat, win streak, and remaining Fever time.

### Free-spin chains

A Mid win, Big win, or Jackpot grants a free spin. Free spins do not cost gold or HP, and can continue the chain if they also win Mid or higher.

- A paid spin can start a chain of up to **3 free spins**.
- A free-spin Miss or Small win ends the chain.
- Free spins still count toward Heat and streak progression and receive applicable reward bonuses.
- While a chain is in progress, further attacks cannot start paid spins.

### Gambling set bonuses

Roulette and Roulette Speed Up belong to the **Gambling** category. Collecting 2, 4, or 6 Gambling artifacts boosts Roulette rewards and buff durations by **20%, 40%, or 60%**, respectively. Only the highest reached tier applies; the bonuses do not stack.

The set bonus improves gold rewards, healing, area damage, damage-buff strength and duration, and Jackpot invulnerability duration. It does not change spin costs, odds, Heat, streaks, Fever, or free-spin chains. With at least 2 Gambling artifacts, the combo description also tracks your Small, Mid, Big, and Jackpot win counts.

## Roulette Speed Up

This Rare artifact increases Roulette's reel speed by **1.5×**. It affects the spin animation and timing, not the odds or rewards.
