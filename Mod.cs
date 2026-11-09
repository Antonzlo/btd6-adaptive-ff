using System;
using System.Diagnostics;
using System.Globalization;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Helpers;
using HarmonyLib;
using MelonLoader;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.Stats;
using Il2CppTMPro;
using UnityEngine;

[assembly: MelonInfo(typeof(AdaptiveFastForward.Mod), AdaptiveFastForward.ModHelperData.Name, AdaptiveFastForward.ModHelperData.Version, AdaptiveFastForward.ModHelperData.Author)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace AdaptiveFastForward;

// While fast forwarding, lowers the fast forward speed when the simulation can't keep up
// (more ticks per frame than fit the budget), then raises it back to the chosen speed.
// Every tick is simulated exactly as before, only fewer of them run per frame.
//
// The slowdown is a factor on Time.timeScale applied after Mod Helper sets it, so the chosen speed
// (TimeHelper.OverrideFastForwardTimeScale) is never touched: Coop Speed Sync only sees the players' own changes.
public class Mod : BloonsTD6Mod
{
    const double BudgetMs = 10; // sim time per frame we aim for
    const double MinShare = 0.5; // never slower than half the chosen speed
    static readonly double ToMs = 1000.0 / Stopwatch.Frequency;

    // Coop Speed Sync reads LocalFactor and writes PeerFactor by reflection (keep the names): in co-op everyone runs
    // at the slowest player's factor instead of stalling for them. 1 = chosen speed, below 1 = slowed down.
    public static double LocalFactor = 1; // what this machine keeps up with
    public static double PeerFactor = 1;  // lowest of the other players' LocalFactor, 1 without Coop Speed Sync
    static double Factor => Math.Min(LocalFactor, PeerFactor);
    static double avgSimMs;
    static long simStart;

    static double Chosen => TimeHelper.OverrideFastForwardTimeScale;
    // same condition Mod Helper uses to apply the fast forward speed
    static bool Fast => TimeManager.FastForwardActive && !TimeManager.inBetweenRounds && InGame.instance != null;

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
        double chosen = Chosen;
        if (!Fast || chosen <= 1)
        {
            LocalFactor = 1;
            return;
        }
        double current = chosen * LocalFactor;
        if (avgSimMs > BudgetMs) current = Math.Max(Math.Max(1, chosen * MinShare), current * 0.9);
        else if (avgSimMs < BudgetMs * 0.7) current = Math.Min(chosen, current * 1.05);
        LocalFactor = current / chosen;
    }

    // Mod Helper sets Time.timeScale from the chosen speed in its own TimeManager.Update postfix; run after it
    [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.Update))]
    static class ApplyFactor
    {
        [HarmonyPriority(HarmonyLib.Priority.Last)]
        static void Postfix()
        {
            var f = Factor;
            if (f < 1 && Fast) Time.timeScale *= (float)f;
        }
    }

    // "x5" left of the round counter, the mod's icon and the slowed speed ("x2.5") while slowed down
    [HarmonyPatch(typeof(RoundDisplay), nameof(RoundDisplay.OnUpdate))]
    static class SpeedLabel
    {
        const float Gap = 30;
        static ModHelperText label;
        static ModHelperImage icon;
        static string shown;

        static void Postfix(RoundDisplay __instance)
        {
            var round = __instance.text;
            if (round == null) return;
            if (label == null) Build(round);  // also rebuilt after a scene change destroyed it

            bool on = TimeManager.FastForwardActive && InGame.instance != null && Chosen > 1;
            bool slowed = on && Factor < 0.999;
            string text = on ? "x" + (Chosen * Factor).ToString("0.#", CultureInfo.InvariantCulture) : "";
            if (text != shown)
            {
                label.SetText(text);
                shown = text;
            }
            icon.gameObject.SetActive(slowed);

            // left edge of the round text's box, in its own local space (textBounds put the label on top of the digits)
            var box = round.rectTransform.rect;
            // the text is right aligned and grows left past its box ("40/80"), so measure from the right edge
            float right = Mathf.Min(box.xMin, box.xMax - round.preferredWidth) - Gap;
            label.transform.localPosition = new Vector3(right, box.center.y, 0);
            icon.transform.localPosition = new Vector3(right - label.Text.preferredWidth - Gap / 2, box.center.y, 0);
        }

        static void Build(TMP_Text round)
        {
            float size = round.fontSize;
            label = ModHelperText.Create(new Info("AdaptiveSpeed", 0, 0, size * 4, size * 1.2f), "", size * 0.8f, TextAlignmentOptions.Right);
            label.transform.SetParent(round.transform, false);
            label.RectTransform.pivot = new Vector2(1, 0.5f);
            icon = ModHelperImage.Create(new Info("AdaptiveIcon", 0, 0, size, size), ModContent.GetTextureGUID<Mod>("Icon"));
            icon.transform.SetParent(round.transform, false);
            icon.RectTransform.pivot = new Vector2(1, 0.5f);
            shown = null;
            MelonLogger.Msg($"speed label built on {round.name} text={round.text} size={size} rect={round.rectTransform.rect} align={round.alignment} pos={round.transform.localPosition}");
        }
    }

    public override void OnSceneWasUnloaded(int buildIndex, string sceneName) => LocalFactor = PeerFactor = 1;
}
