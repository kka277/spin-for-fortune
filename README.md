# Spin for Fortune

A Sephiria mod featuring the **Roulette** artifact and its upgrade, **Roulette Speed Up**.

## Roulette

Roulette is a Legendary artifact that turns weapon attacks into slot spins. Hitting a normal enemy with a direct weapon attack starts a spin. It does not trigger on scarecrows or training damage dummies, and attacks are ignored while the reels are spinning or during the 0.5-second cooldown.

Each paid spin costs **30 gold**. If you do not have enough gold, it costs **5 HP** instead; this cost cannot reduce you below 1 HP. The spin will not start if you cannot pay either cost.

### Spin results

| Result | Chance | Reels | Reward |
|---|---:|---|---|
| Small win | 30% | CHERRY | Gain 50 gold. |
| Mid win | 12% | BELL | Gain +30% to all damage for 10 seconds. |
| Big win | 6% | BAR | Restore 25% HP and deal damage to nearby enemies (radius 14; 100 + 5× your Physical Damage). |
| Jackpot | 2% | 7 | Gain 500 gold, become invulnerable for 20 seconds, and gain +50% to all damage for 20 seconds. |
| Miss | 50% | No winning match | No reward. 15% of all spins are near-misses, where the first two reels match but the last reel does not. |

The three reels stop one at a time. When the first two match, the spin shows a near-miss: the last reel slows down, with visual and audio effects. The displayed result always matches the reward awarded.

### Heat, streaks, and Fever

- These progression effects are provided by the Gambling combo, which activates at **2 or more Gambling artifacts**. If the combo activates while you do not own Roulette, you gain Roulette.
- Every win increases your **Heat**: Small +1, Mid +2, Big +3, and Jackpot +5.
- Consecutive wins build a streak and add bonus Heat: starting with the third consecutive win, every two consecutive wins add +1 Heat, up to +2 bonus Heat per win.
- Each consecutive win also adds 5 Luck for subsequent Roulette spin odds while the combo is active. A miss resets the streak and its bonus Luck.
- A miss ends the streak. A regular miss removes 2 Heat; a near-miss removes only 1. Heat is capped at 10.
- At 10 Heat, you enter **Fever** for 10 seconds. Fever increases Roulette rewards by 50% on subsequent spins; the spin that activates Fever does not receive this bonus.
- Reaching 10 Heat again during Fever adds 10 seconds, up to a maximum of 15 seconds remaining.
- Fever affects Roulette rewards, but does not extend the Jackpot's invulnerability duration.

The Roulette UI displays your current Heat, win streak, and remaining Fever time.

### Free-spin chains

With the Gambling combo active, a Mid win, Big win, or Jackpot grants a free spin. Free spins do not cost gold or HP, and can continue the chain if they also win Mid or higher.

- A paid spin can start a chain of up to **3 free spins**.
- A free-spin Miss or Small win ends the chain.
- Free spins still count toward Heat and streak progression and receive applicable reward bonuses.
- While a chain is in progress, further attacks cannot start paid spins.

### Gambling set bonuses

Roulette and Roulette Speed Up belong to the **Gambling** category. Collecting 2, 4, or 6 Gambling artifacts boosts Roulette rewards and buff durations by **20%, 40%, or 60%**, respectively. Only the highest reached tier applies; the bonuses do not stack. The combo also enables Heat, streaks, Fever, and free-spin chains at 2 or more pieces.

The set bonus improves gold rewards, healing, area damage, damage-buff strength and duration, and Jackpot invulnerability duration. These reward tiers do not change spin costs, odds, or the progression rules; Heat, streaks, Fever, and free-spin chains are enabled by the combo at 2 or more pieces. Disabling the combo resets that progression. The combo description also tracks your Small, Mid, Big, and Jackpot win counts.

## Roulette Speed Up

This Rare artifact increases Roulette's reel speed by **1.5×**. It affects the spin animation and timing, not the odds or rewards.
