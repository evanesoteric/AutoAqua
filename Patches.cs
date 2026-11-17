using HarmonyLib;

namespace AutoAqua
{
    // Runs the routine when the game starts a new day (after the overnight purification has happened).
    [HarmonyPatch(typeof(PlayerStore), "BeginDay")]
    public static class BeginDayPatch
    {
        public static void Postfix() { Logic.RunMorning(); }
    }
}
