# Spin for Fortune

A Sephiria mod featuring the **Roulette** artifact, the **Gambling** combo, and six artifacts that modify Roulette.

## Roulette

![Roulette artifact](images/roulette/icon.png)

Roulette is a Legendary artifact that turns weapon attacks into slot spins. Hitting a normal enemy with a direct weapon attack starts a spin. It does not trigger on scarecrows or training damage dummies, and attacks are ignored while the reels are spinning or during the 0.5-second cooldown.

Each paid spin costs **30 gold**. If you do not have enough gold, it costs **5 HP** instead; this cost cannot reduce you below 1 HP. The spin will not start if you cannot pay either cost.

### Spin results

| Result | Chance | Reels | Reward |
|---|---:|---|---|
| Small win | 30% | CHERRY | Gain 50 gold and damage nearby enemies. |
| Mid win | 12% | BELL | Gain +50% to all damage and +20% attack speed for 15 seconds, and damage nearby enemies. |
| Big win | 6% | BAR | Restore 50% HP, gain +70% to all damage for 15 seconds, and damage nearby enemies. |
| Jackpot | 2% | 7 | Gain 500 gold, become invulnerable for 20 seconds, gain +75% to all damage for 25 seconds, and damage nearby enemies. |
| Miss | 50% | No winning match | No reward. 15% of all spins are near-misses, where the first two reels match but the last reel does not. |

Every win damages enemies within 14 units. Damage is based on 4× your highest elemental damage, multiplied by the result tier: Small 0.5×, Mid 1×, Big 1.5×, and Jackpot 3×. Gambling bonuses and Fever can further increase rewards.

The three reels stop one at a time. When the first two match, the spin shows a near-miss: the last reel slows down, with visual and audio effects. The displayed result always matches the reward awarded.

### Heat, streaks, and Fever

- These progression effects are provided by the Gambling combo, which activates at **2 or more Gambling artifacts**. If the combo activates while you do not own Roulette, you gain Roulette.
- Every win increases your **Heat**: Small +1, Mid +2, Big +3, and Jackpot +5.
- Consecutive wins build a streak and add bonus Heat: starting with the third consecutive win, every two consecutive wins add +1 Heat, up to +2 bonus Heat per win.
- Each consecutive win also adds 5 Luck for subsequent Roulette spin odds while the combo is active. A miss resets the streak and its bonus Luck.
- A miss ends the streak but does not reduce Heat. Heat is capped at 10; reaching 10 resets it to 0 and activates **Fever** for 10 seconds.
- Fever increases Roulette rewards by 50% on subsequent spins; the spin that activates Fever does not receive this bonus.
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

Roulette, Roulette Speed Up, and the five Roulette modifiers belong to the **Gambling** category. Collecting 2, 4, or 6 Gambling artifacts boosts Roulette rewards and buff durations by **20%, 40%, or 60%**, respectively. Only the highest reached tier applies; the bonuses do not stack. The combo also enables Heat, streaks, Fever, and free-spin chains at 2 or more pieces.

The set bonus improves gold rewards, healing, area damage, damage-buff strength and duration, and Jackpot invulnerability duration. These reward tiers do not change spin costs or odds; Heat, streaks, Fever, and free-spin chains are enabled by the combo at 2 or more pieces. Disabling the combo disables those progression effects. Win counts are tracked internally and are not displayed.

## Roulette Modifiers

These artifacts modify Roulette while owned. Multiple modifiers stack multiplicatively.

| Icon | Artifact | Rarity / Price | Effect |
|---|---|---|---|
| ![Roulette Speed Up](images/roulette/speedup_icon.png) | Roulette Speed Up | Rare / 500 gold | Reel speed increases with level, up to 1.5× at level 4. |
| ![Lucky Horseshoe](images/roulette/lucky_horseshoe_icon.png) | Lucky Horseshoe | Rare / 600 gold | Win chance and reward penalty scale with level, up to 1.25× win chance and 0.8× rewards at level 5. |
| ![Greedy Wager](images/roulette/greedy_wager_icon.png) | Greedy Wager | Legendary / 900 gold | Reward and win chance penalty scale with level, up to 1.5× rewards and 0.75× win chance at level 3. |
| ![Miser's Purse](images/roulette/miser_purse_icon.png) | Miser's Purse | Rare / 500 gold | Gold savings and cooldown penalty scale with level, up to 0.6× gold cost and 1.2× cooldown at level 4. |
| ![Blood Contract](images/roulette/blood_contract_icon.png) | Blood Contract | Rare / 500 gold | HP savings and gold cost penalty scale with level, up to 0.5× HP cost and 1.2× gold cost at level 4. |
| ![Fever Amulet](images/roulette/fever_amulet_icon.png) | Fever Amulet | Legendary / 800 gold | Buff duration and cooldown penalty scale with level, up to 1.5× buff duration and 1.2× cooldown at level 3. |

The Gambling category includes Roulette, Roulette Speed Up, and these five modifiers. Collecting 2, 4, or 6 pieces activates the corresponding Gambling set bonus.
