using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace AutoAqua
{
    public static class Logic
    {
        // Item identifiers used by the game (the same in every save).
        public const string PitcherId = "water_pitcher";
        public const string JugId = "water_tank";
        public const string FaucetId = "faucet";

        public static MelonPreferences_Entry<bool> DryRun;
        public static MelonPreferences_Entry<bool> Verbose;
        public static MelonPreferences_Entry<bool> Report;
        public static MelonPreferences_Entry<bool> RequireFaucet;
        public static MelonPreferences_Entry<bool> OnlyInMachines;
        public static MelonPreferences_Entry<int> MinPurity;
        public static MelonPreferences_Entry<int> PureAt;
        public static MelonPreferences_Entry<string> HqJugUids;
        public static MelonPreferences_Entry<string> IgnoreUids;
        public static MelonPreferences_Entry<string> TriggerKey;

        private static readonly MethodInfo InvParentMethod = AccessTools.Method(typeof(GameInventory), "GetParentItem");
        private static readonly PropertyInfo FaucetProp = AccessTools.Property(typeof(EmporiumEntry), "faucet");
        private static readonly FieldInfo FaucetField = AccessTools.Field(typeof(EmporiumEntry), "<faucet>k__BackingField");

        public static void InitPrefs()
        {
            var cat = MelonPreferences.CreateCategory("AutoAqua");
            DryRun = cat.CreateEntry("DryRun", false, description: "true = only log what would happen, change nothing");
            Verbose = cat.CreateEntry("Verbose", false, description: "true = log every pitcher and why it was skipped");
            Report = cat.CreateEntry("Report", false, description: "true = log a setup report (pitchers, jugs, uids) 45 s after the shop loads");
            RequireFaucet = cat.CreateEntry("RequireFaucet", true, description: "true = do nothing if the faucet is not in the room");
            OnlyInMachines = cat.CreateEntry("OnlyInMachines", true, description: "true = only handle pitchers sitting in a machine slot (ignores spare pitchers in storage or bags)");
            MinPurity = cat.CreateEntry("MinPurity", 9900, description: "pour threshold in hundredths of a percent (9900 = 99.00%, start of High-Quality)");
            PureAt = cat.CreateEntry("PureAt", 9990, description: "purity at which water counts as Pure (99.90%); Pure water is never moved or mixed");
            HqJugUids = cat.CreateEntry("HqJugUids", "", description: "optional override: comma-separated uids of jugs to fill, in priority order. Empty = automatic (jugs that already hold high-quality water)");
            IgnoreUids = cat.CreateEntry("IgnoreUids", "", description: "optional: comma-separated uids of pitchers or machines to leave alone");
            TriggerKey = cat.CreateEntry("TriggerKey", "F9", description: "key that runs the routine right now (any UnityEngine KeyCode name), empty = disabled");
        }

        private static void Vlog(string message)
        {
            if (Verbose.Value) MelonLogger.Msg(message);
        }

        private static List<int> ParseUids(string text)
        {
            var list = new List<int>();
            foreach (var part in (text ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int n;
                if (int.TryParse(part, out n) && !list.Contains(n)) list.Add(n);
            }
            return list;
        }

        private static List<GameItem> Own()
        {
            var store = PlayerStore.Instance;
            if (store == null) return new List<GameItem>();
            return store.FindAllItem(true) ?? new List<GameItem>();
        }

        private static GameItem MachineOf(GameItem i)
        {
            try
            {
                var inv = i.parentInventory;
                if (inv == null || InvParentMethod == null) return null;
                return InvParentMethod.Invoke(inv, null) as GameItem;
            }
            catch { return null; }
        }

        private static bool InMachine(GameItem i)
        {
            try
            {
                var inv = i.parentInventory;
                return inv != null && inv.GetType().Name == "GameSlotInventory";
            }
            catch { return false; }
        }

        private static string Describe(GameItem i)
        {
            var m = MachineOf(i);
            return "id=" + i.identifier + " uid=" + i.uniqueId +
                   " purity=" + WaterHelper.GetWaterPurity(i) +
                   " ml=" + WaterHelper.GetCurrentCapacityML(i) +
                   " free_uL=" + WaterHelper.GetFreeCapacity(i) +
                   " inMachine=" + InMachine(i) +
                   " machine=" + (m == null ? "none" : m.identifier + "#" + m.uniqueId);
        }

        private static bool IsHq(int purity) { return purity >= MinPurity.Value && purity < PureAt.Value; }

        private static GameItem GetFaucet()
        {
            try
            {
                var entry = UnityEngine.Object.FindObjectOfType(typeof(EmporiumEntry));
                if (entry == null) return null;
                object v = FaucetProp != null ? FaucetProp.GetValue(entry, null)
                         : (FaucetField != null ? FaucetField.GetValue(entry) : null);
                var item = v as GameItem;
                return (item != null && item.identifier == FaucetId) ? item : null;
            }
            catch { return null; }
        }

        // A jug is a collection jug when it already holds high-quality (but not Pure) water.
        private static bool IsCollectionJug(GameItem j)
        {
            return !WaterHelper.IsEmpty(j) && IsHq(WaterHelper.GetWaterPurity(j));
        }

        private static int FreeAfter(GameItem j, Dictionary<int, int> reserved)
        {
            int res; reserved.TryGetValue(j.uniqueId, out res);
            return WaterHelper.GetFreeCapacity(j) - res;
        }

        private static bool Fits(GameItem jug, int needUl, Dictionary<int, int> reserved)
        {
            if (FreeAfter(jug, reserved) < needUl) return false;
            if (WaterHelper.IsEmpty(jug)) return true;
            return IsHq(WaterHelper.GetWaterPurity(jug));
        }

        private static GameItem PickJug(List<GameItem> jugs, int pitcherMl, Dictionary<int, int> reserved)
        {
            int need = pitcherMl * 1000; // free capacity is reported in microlitres
            var designated = ParseUids(HqJugUids.Value);
            IEnumerable<GameItem> candidates;
            if (designated.Count > 0)
                candidates = designated.Select(uid => jugs.FirstOrDefault(j => j.uniqueId == uid)).Where(j => j != null);
            else
                candidates = jugs.Where(IsCollectionJug).OrderBy(j => FreeAfter(j, reserved)); // fullest jug first, then overflow
            foreach (var j in candidates)
                if (Fits(j, need, reserved)) return j;
            return null;
        }

        public static void LogReport()
        {
            try
            {
                var items = Own();
                MelonLogger.Msg("[report] items=" + items.Count + " (the uids below can be used in IgnoreUids / HqJugUids)");
                foreach (var id in new[] { PitcherId, JugId })
                {
                    var list = items.Where(i => i.identifier == id).ToList();
                    MelonLogger.Msg("[report] " + id + " x" + list.Count);
                    foreach (var i in list) MelonLogger.Msg("[report]   " + Describe(i));
                }
                MelonLogger.Msg("[report] faucet found: " + (GetFaucet() != null));
            }
            catch (Exception e) { MelonLogger.Error("[report] " + e); }
        }

        public static void RunMorning()
        {
            try
            {
                var faucet = GetFaucet();
                if (faucet == null && RequireFaucet.Value)
                {
                    MelonLogger.Msg("[morning] no faucet found in the room, doing nothing (RequireFaucet=true)");
                    return;
                }

                var items = Own();
                var ignored = new HashSet<int>(ParseUids(IgnoreUids.Value));
                var jugs = items.Where(i => i.identifier == JugId).ToList();
                var allPitchers = items.Where(i => i.identifier == PitcherId).ToList();
                var pitchers = OnlyInMachines.Value ? allPitchers.Where(InMachine).ToList() : allPitchers;
                var reserved = new Dictionary<int, int>();
                bool auto = string.IsNullOrEmpty(HqJugUids.Value);
                int done = 0, noRoom = 0;

                MelonLogger.Msg("[morning] pitchers=" + pitchers.Count +
                                (allPitchers.Count != pitchers.Count ? " (+" + (allPitchers.Count - pitchers.Count) + " outside machines, ignored)" : "") +
                                " jugs=" + jugs.Count + " faucet=" + (faucet != null) +
                                " dry=" + DryRun.Value + " mode=" + (auto ? "auto" : "override"));

                if (Verbose.Value)
                {
                    var collection = jugs.Where(IsCollectionJug).ToList();
                    MelonLogger.Msg("[morning] collection jugs: " + (collection.Count == 0 ? "none"
                        : string.Join(", ", collection.Select(j => "uid=" + j.uniqueId + " " +
                            WaterHelper.GetCurrentCapacityML(j) + "ml held, " +
                            (WaterHelper.GetFreeCapacity(j) / 1000) + "ml free").ToArray())));
                }

                foreach (var pitcher in pitchers)
                {
                    var machine = MachineOf(pitcher);
                    int purity = WaterHelper.GetWaterPurity(pitcher);
                    int ml = WaterHelper.GetCurrentCapacityML(pitcher);

                    if (ignored.Contains(pitcher.uniqueId) || (machine != null && ignored.Contains(machine.uniqueId)))
                    { Vlog("[morning] skip (ignored): " + Describe(pitcher)); continue; }
                    if (ml <= 0) { Vlog("[morning] skip (empty): " + Describe(pitcher)); continue; }
                    if (purity >= PureAt.Value) { Vlog("[morning] skip (pure tier): " + Describe(pitcher)); continue; }
                    if (purity < MinPurity.Value) { Vlog("[morning] skip (still filtering): " + Describe(pitcher)); continue; }

                    var jug = PickJug(jugs, ml, reserved);
                    if (jug == null)
                    {
                        noRoom++;
                        Vlog("[morning] waiting (no jug has room): " + Describe(pitcher));
                        continue;
                    }

                    if (DryRun.Value)
                    {
                        int res; reserved.TryGetValue(jug.uniqueId, out res);
                        reserved[jug.uniqueId] = res + ml * 1000;
                        done++;
                        MelonLogger.Msg("[morning] WOULD pour pitcher uid=" + pitcher.uniqueId + " (" + ml + " ml, purity " + purity +
                                        ") into jug uid=" + jug.uniqueId + ", then refill from the faucet");
                        continue;
                    }

                    if (!pitcher.CanTarget(jug))
                    {
                        MelonLogger.Warning("[morning] the game does not allow pitcher uid=" + pitcher.uniqueId +
                                            " to pour into jug uid=" + jug.uniqueId);
                        continue;
                    }

                    pitcher.Target(jug);
                    done++;

                    if (!WaterHelper.IsEmpty(pitcher))
                    {
                        MelonLogger.Warning("[morning] pitcher uid=" + pitcher.uniqueId + " still holds water after pouring, not refilling it");
                        continue;
                    }

                    if (faucet != null && pitcher.CanTarget(faucet))
                    {
                        pitcher.Target(faucet);
                        MelonLogger.Msg("[morning] pitcher uid=" + pitcher.uniqueId + ": poured " + ml + " ml (purity " + purity +
                                        ") into jug uid=" + jug.uniqueId + ", refilled from the faucet");
                    }
                    else
                    {
                        MelonLogger.Warning("[morning] pitcher uid=" + pitcher.uniqueId + ": poured " + ml + " ml into jug uid=" +
                                            jug.uniqueId + " but the game would not let it use the faucet; it was left empty");
                    }
                }

                if (noRoom > 0)
                {
                    MelonLogger.Warning("[morning] " + noRoom + " pitcher(s) are ready but no collection jug has room");
                    if (auto)
                    {
                        if (!jugs.Any(IsCollectionJug))
                            MelonLogger.Msg("[morning] hint: no jug holds high-quality water yet. Pour a little 99%+ water into the jug you want used and it will be filled from then on");
                        else if (jugs.Any(j => WaterHelper.IsEmpty(j)))
                            MelonLogger.Msg("[morning] hint: empty jugs are never used automatically (one might be your Pure Water jug). To use one as overflow, pour a little 99%+ water into it, or list its uid in HqJugUids");
                    }
                }

                MelonLogger.Msg("[morning] summary: " + done + (DryRun.Value ? " would be poured" : " poured") +
                                (noRoom > 0 ? ", " + noRoom + " waiting for jug space" : ""));
            }
            catch (Exception e) { MelonLogger.Error("[morning] " + e); }
        }
    }
}
