using System;
using System.Diagnostics;
using BTD_Mod_Helper.Api.Helpers;
using HarmonyLib;
using MelonLoader;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;

[assembly: MelonInfo(typeof(AdaptiveFastForward.Mod), AdaptiveFastForward.ModHelperData.Name, AdaptiveFastForward.ModHelperData.Version, AdaptiveFastForward.ModHelperData.Author)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace AdaptiveFastForward;

// While fast forwarding, lowers the fast forward speed when the simulation can't keep up
// (more ticks per frame than fit the budget), then raises it back to the chosen speed.
// Every tick is simulated exactly as before, only fewer of them run per frame.
public class Mod : MelonMod
{
    const double BudgetMs = 10; // sim time per frame we aim for
    static readonly double ToMs = 1000.0 / Stopwatch.Frequency;

    static double chosen;   // speed set by the player (vanilla 3x or FasterForward hotkeys)
    static double current;  // speed we last set, 0 = not adapting
    static double avgSimMs;
    static long simStart;

    [HarmonyPatch(typeof(InGame), "UpdateSimulation")]
    static class SimStep
    {
        static void Prefix() => simStart = Stopwatch.GetTimestamp();

        static void Postfix()
        {
            double ms = (Stopwatch.GetTimestamp() - simStart) * ToMs;
            avgSimMs = avgSimMs * 0.8 + ms * 0.2;
            Adapt();
        }
    }

    static void Adapt()
    {
        double scale = TimeHelper.OverrideFastForwardTimeScale;

        // Someone else changed the speed (player hotkey), take it as the new target
        if (current == 0 || Math.Abs(scale - current) > 0.001)
        {
            chosen = scale;
            current = scale;
        }

        // Co-op keeps players in lockstep, don't touch its speed
        if (!TimeManager.FastForwardActive || InGame.instance == null || InGame.instance.IsCoop)
        {
            Restore();
            return;
        }

        double next = current;
        if (avgSimMs > BudgetMs) next = Math.Max(1, current * 0.9);
        else if (avgSimMs < BudgetMs * 0.7) next = Math.Min(chosen, current * 1.05);

        if (Math.Abs(next - current) > 0.001)
        {
            current = next;
            TimeHelper.OverrideFastForwardTimeScale = next;
        }
    }

    static void Restore()
    {
        if (current != 0 && Math.Abs(current - chosen) > 0.001)
            TimeHelper.OverrideFastForwardTimeScale = chosen;
        current = chosen;
    }

    public override void OnSceneWasUnloaded(int buildIndex, string sceneName) => Restore();
}
