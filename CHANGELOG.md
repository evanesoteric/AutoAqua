
# Changelog
 
## 1.0.0
 
First public release.
 
- At the start of each in-game day, pours finished (99.00% to 99.89%) water pitchers into a collection jug and refills them from the faucet.
- Never touches Pure-tier water (99.90%+), never pours into an empty jug on its own, never mixes tiers, never pours into a jug without room for the whole pitcher.
- Automatic overflow: with two or more collection jugs, the fuller jug is filled until the next pitcher no longer fits, then the next one.
- Only handles pitchers sitting in a machine slot by default (`OnlyInMachines`).
- Quiet log by default (`Verbose` for per-pitcher detail); optional setup report (`Report`) that lists pitchers, jugs and their uids.
- Optional settings: `DryRun`, `RequireFaucet`, `MinPurity`, `PureAt`, `HqJugUids`, `IgnoreUids`, `TriggerKey`.
- Optional hotkey (default F9) that runs the routine immediately.
Tested with game build 0.46D (Unity 2021.3.45f2) and MelonLoader 0.7.3.
