# isaac-eden-finder

This is a LINQ script that finds specific seeds for [_The Binding of Isaac: Repentance_](https://store.steampowered.com/app/1426300/The_Binding_of_Isaac_Repentance/) where Eden starts with specific things. It does this by emulating the internal game logic.

---

## Reverse Engineering Journal

All offsets are virtual addresses in `isaac-ng.exe` (Repentance). PE32 x86.

### Section offsets
| Section | VA | Raw offset | Delta (VA→file) |
|---|---|---|---|
| `.text` | 0x401000 | 0x400 | `va - 0x400c00` |
| `.rdata` | 0xa06000 | 0x605000 | `va - 0x401000` |
| `.data` | 0xba9000 | — | `va - 0x401000` |

### RNG table at `0xa08aa0`
81 entries × 12 bytes each. Each entry is `(s1, s2, s3)` for xorshift: `seed ^= seed>>s1; seed ^= seed<<s2; seed ^= seed>>s3`.

Key entries:
| Entry | VA | s1 | s2 | s3 | Used by |
|---|---|---|---|---|---|
| 2 | 0xa08aa0+24 | 1 | 5 | 19 | `CalculateEdenItems` dropSeed Rng |
| 3 | 0xa08aa0+36 | 1 | 9 | 29 | entity xorshift in `0x63f660` |
| 11 | 0xa08aa0+132 | 2 | 7 | 7 | pre-xs in `0x6e7980` (other chars) |
| 22 | 0xa08ba8 | 3 | 5 | 20 | `pool_weighted_select` internal scoring |
| 55 | 0xa08d34 | 7 | 17 | 21 | pre-xs in `0x6e70b0` (Eden item dist) |

---

### `0x7a7d50` — `Rng::Next(uint n)`
`__thiscall`. ecx = Rng ptr (seed at [ecx+0], s1 at [+4], s2 at [+8], s3 at [+0xc]).
Applies xorshift then returns `seed % n`.

### `0x7a7e20` — `Rng::Next()`
`__thiscall`. Like above but no modulo — returns raw xorshifted seed.

---

### `0x91d680` — `pool_weighted_select(filter, seed, result_ptr)`
`__thiscall` (pool object in ecx). `ret 0xc` (stdcall args). Args: filter=`[ebp+8]`, seed=`[ebp+0xc]`, result_ptr=`[ebp+0x10]`.

**Algorithm:**
```
max_score = -INF (float)
winner = null
for each item_ptr in pool.items[0x10..0x14]:
    if [item+0x2c] != 0: continue     (blocked)
    if CheckAvailable(filter) <= 0: continue  (unavailable)
    seed = xs(seed, s1=3, s2=5, s3=20)  (entry 22 params, hardcoded at 0xa08ba8/0xa08bb0)
    score = float(seed) / 2^32
    if score > max_score: winner = item
*result_ptr = [winner+0x16a4] + filter*16  (pointer to 16-byte Rng slot)
```

**Key:** Only available items advance the seed. Iteration order matters because the shared seed advances per item.

**Return value:** pointer to the winner pool item's data slot for the given filter.

---

### `0x91d400` — `pool_select_v2(filter, seed, result_ptr)`
Like `pool_weighted_select` but result uses `[winner+0x1698]` instead of `[winner+0x16a4]`.

---

### `0x78d4c0` — `CheckAvailable(filter)`
`__thiscall` (pool item in ecx). Checks various flags on the pool item to determine if it's eligible:
- `[item+0x2c]` = block flag (checked first)
- `[item+0x1f1c]` and `[item+0x1f88]` — if nonzero, item is unavailable (returns 0 or neg)
- Filter-specific checks (e.g. filter=0x56 checks `[item+0x130c]`)
- Filter 0x15 triggers a recursive call

---

### `0x888070` — `AddToPool(pool_item)`
`__thiscall` (pool_item in ecx). Marks `[pool_item+0x198] = 1` (in pool). Iterates `[pool_item+0x190]` entries starting at `[pool_item+0x20]` (stride 0x38), calling `0x870460` for each to register in the global pool hashtable. Increments the pool's item count.

Called 22 times in the binary to add trinkets to the pool.

---

### `0x63f660` — Pickup entity init / trinket type selection
Called during pickup entity initialization. Determines what the pickup actually is.

```
pool_result = pool_weighted_select(filter=22, seed=[entity+0x344], pool=trinket_pool)
```

**Seed used:** `[entity+0x344]` — the entity's seed value at offset +0x344 — passed **directly** with **no pre-xorshift**.

After pool select, additional xorshift with entry-3 params (1,9,29) is applied for sub-variant/position randomness.

---

### `0x6e70b0` — Eden item distribution
Called from `0x55fa74` with `[edi+0x344]` as the seed. Handles characters type 7–10 (Azazel, Lazarus, Eden, Lost).

**Key code path for Eden trinket:**
1. At `0x6e72be`: loads xs params entry 55 = (7,17,21) from `0xa08d34`
2. At `0x6e72e8`: applies xs(7,17,21) to `esi` (intermediate seed) → `ebx`
3. At `0x6e7302`: `test bl, 1; jne 0x6e795f` — **if low bit of ebx == 1, SKIP trinket selection entirely**
4. At `0x6e7326`: applies xs(7,17,21) again to `ebx` → `edi`
5. At `0x6e7356`: calls `pool_weighted_select(filter=22, seed=edi, pool=trinket_pool)`

The result from step 5 is a pointer to winner's Rng slot. Then at `0x6e761f`:
- Calls `Rng::Next(5)` on winner's slot
- If result != 0: skip spawn (no trinket appears)

**This function spawns the trinket entity** but does NOT directly set the trinket type — that's done by `0x63f660` when the entity initializes.

---

### `0x6e7980` — Other-character trinket selection (Judas, etc.)
Applies xs with **entry 11 = (2,7,7)** then calls `pool_weighted_select(filter=22)`. This is for non-Eden characters and is **NOT** the relevant path for Eden.

---

### `0x63fa5d` — Player pickup-drop logic
Called when a player's pickup drops. Uses `[player+0x334]` Rng:
1. `Next(5)`: if != 0, no trinket/grab-bag
2. `Next(2)`: if == 0, trinket branch; else grab-bag branch
3. Trinket branch: `Next()` → entity seed → `SpawnEntity(type=5, variant=0x15e, seed=entity_seed)`

**This is the path modeled by `CalculateEdenItems`.**

---

## GetTrinket — Root Cause Analysis and Fix

### Root Cause (RESOLVED)

Two bugs in the original `GetTrinket`:

**Bug 1: Wrong pre-xorshift (xs(2,7,7))**
- Was using params from `0x6e7980` (non-Eden character path)
- Eden's path `0x63fa5d` passes `[entity+0x344]` directly to `pool_weighted_select` with no pre-transform
- `0x681a07` confirms `[entity+0x344]` is set to the raw entity spawn seed = `trinketSeed`

**Bug 2: Missing achievement-locked trinket exclusion**
- `CheckAvailable (0x78d4c0)` checks `[item+0x1f1c]` and `[item+0x1f88]` — if nonzero, item is UNAVAILABLE
- Unavailable trinkets do **not** advance the seed in `pool_weighted_select`
- Achievement-locked trinkets that haven't been earned are unavailable
- 96 trinkets in items.xml have `achievement=` attribute

**Bug 3 (minor): `[item+0x198]` check**
- Pool building at `0x703000` skips already-added trinkets; stride `0x1b8` = 440 bytes
- Pool IS in ID order (1, 2, 3, ... 189) — XML loading order confirmed

### Test Case Verification
Seed **"B918 HB47"** → `startSeed=0x9C7A`, `dropSeed=0x28ff86c0`, `trinketSeed=0xACDDEB28`.
- Game shows: **trinket 20 (Monkey Paw)**
- With IDs {13, 15, 17} locked (achievements 118, 85, 111): pool positions 0-19 = `[1..12, 14, 16, 18, 19, 20, ...]`
- xs^17(0xACDDEB28) is the maximum → 17th available item (0-indexed: 16) = ID 20 ✓

### `0x681a07` — Entity initialization
```asm
mov eax, [ebp+0x14]          ; arg4 = entity spawn seed
mov [esi+0x344], eax          ; [entity+0x344] = spawn seed (static copy, never advanced)
mov [esi+0x334], eax          ; [entity+0x334] = seed for entity's own Rng
movq [esi+0x338], xmm0        ; s1=4, s2=3 (entry 30 params)
mov [esi+0x340], eax          ; s3=17
```
**Key:** `[entity+0x344]` is a raw copy of the spawn seed. No pre-transform in `0x63fa5d` path.

### `0x6c3d90` — Mersenne Twister (MT19937)
Classic MT19937 RNG implementation. Index at `0xbc5304`, state at `0xbffe10` (N=624).
Tempering: shr 11, shl 7 & 0xff3a58ad, shl 15 & 0xffffdf8c, shr 18.
**Used by `0x6e70b0` Eden item distribution for MT-sourced entity seeds — NOT the `0x63fa5d` pickup path.**

### Pool building (`0x703000` + `0x787280-0x7876cc`)
- Main loop at `0x703000`: iterates items array, `imul eax, edx, 0x1b8` (trinket struct stride = 440 bytes)
- Adds trinkets in XML/ID order: 1, 2, 3, … 189
- Conditional adds at `0x787280`: IDs 7, 37, 55, 119, 132, 133, 150, 157, 170, 171 etc. — DLC/achievement gated

### Achievement-locked trinket IDs (from items.xml)
All have `achievement=` attribute; unavailable until the achievement is earned:
```
1, 13, 15, 17, 21, 22, 23, 28, 35, 42, 49, 50, 52, 54-61, 67, 74, 76,
80, 81, 83-85, 91, 92, 107, 110, 111, 113-128, 131, 138, 141-143, 146,
147, 150, 152-189
```
ID 1 (Swallowed Penny, ach 101) must be unlocked to appear. Adjust `LockedTrinkets` in code for your save file.
