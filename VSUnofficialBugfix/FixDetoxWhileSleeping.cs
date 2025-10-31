using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

using VSSurvivalMod;
using HarmonyLib;
using UnofficialBugfix;

namespace UnofficialBugfix.FixDetoxWhileSleeping
{

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixDetoxWhileSleeping {
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ReduceSaturation")]
    private static extern bool ReduceSaturation(EntityBehaviorHunger instance, float satLossMultiplier);

    /// BUG: Detoxification does not scale with game time
    /// and only passes relative to real time.
    /// FIX: Scale detox value by SpeedOfTime and CalenderSpeedMul
    /// 
    /// Unfortunately, detox is private so we have to completely
    /// override OnGameTick, which sucks for compatibility.
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(EntityBehaviorHunger))]
    [HarmonyPatch("OnGameTick")]
    public static bool UseTimeScaleForDetox(
        ref EntityBehaviorHunger __instance,
        ref Entity ___entity,
        ref EntityAgent ___entityAgent,
        ref float ___lastMoveMs,
        ref float ___detoxCounter,
        ref int ___sprintCounter,
        ref float ___hungerCounter,
        float deltaTime
    ) {
        if (___entity is EntityPlayer)
        {
            EntityPlayer entityPlayer = (EntityPlayer)___entity;
            EnumGameMode currentGameMode = ___entity.World.PlayerByUid(entityPlayer.PlayerUID).WorldData.CurrentGameMode;

            // start inlined detox()
            ___detoxCounter += deltaTime;
            if (___detoxCounter > 1) {
                float intox = ___entity.WatchedAttributes.GetFloat("intoxication");
                if (intox > 0) {
                    // 60 * 0,5 = 30 (SpeedOfTime * CalendarSpeedMul) is the default, so we scale according to the default time multiplier
                    var intoxLoss = 0.005f * ___entity.Api.World.Calendar.SpeedOfTime * ___entity.Api.World.Calendar.CalendarSpeedMul / 30;
                    ___entity.WatchedAttributes.SetFloat("intoxication", Math.Max(0, intox - intoxLoss));
                    UnofficialBugfixModSystem.Logger.Notification("[FixDetoxWhileSleeping] intox now {0}", ___entity.WatchedAttributes.GetFloat("intoxication"));
                }
                ___detoxCounter = 0f;
            }
            // end inlined detox()

            if (currentGameMode == EnumGameMode.Creative || currentGameMode == EnumGameMode.Spectator)
            {
                return false;
            }

            if (entityPlayer.Controls.TriesToMove || entityPlayer.Controls.Jump || entityPlayer.Controls.LeftMouseDown || entityPlayer.Controls.RightMouseDown)
            {
                ___lastMoveMs = ___entity.World.ElapsedMilliseconds;
            }
        }

        if (___entityAgent != null && ___entityAgent.Controls.Sprint)
        {
            ___sprintCounter++;
        }

        ___hungerCounter += deltaTime;
        if (___hungerCounter > 10f)
        {
            bool num = ___entity.World.ElapsedMilliseconds - ___lastMoveMs > 3000;
            float num2 = ___entity.Api.World.Calendar.SpeedOfTime * ___entity.Api.World.Calendar.CalendarSpeedMul;
            float num3 = GlobalConstants.HungerSpeedModifier / 30f;
            if (num)
            {
                num3 /= 4f;
            }

            num3 *= 1.2f * (8f + (float)___sprintCounter / 15f) / 10f;
            num3 *= ___entity.Stats.GetBlended("hungerrate");
            ReduceSaturation(__instance, num3 * num2);
            ___hungerCounter = 0f;
            ___sprintCounter = 0;

            // start inlined detox()
            ___detoxCounter += deltaTime;
            if (___detoxCounter > 1) {
                float intox = ___entity.WatchedAttributes.GetFloat("intoxication");
                if (intox > 0) {
                    // 60 * 0,5 = 30 (SpeedOfTime * CalendarSpeedMul) is the default, so we scale according to the default time multiplier
                    var intoxLoss = 0.005f * ___entity.Api.World.Calendar.SpeedOfTime * ___entity.Api.World.Calendar.CalendarSpeedMul / 30;
                    ___entity.WatchedAttributes.SetFloat("intoxication", Math.Max(0, intox - intoxLoss));
                    UnofficialBugfixModSystem.Logger.Notification("[FixDetoxWhileSleeping] intox now {0}", ___entity.WatchedAttributes.GetFloat("intoxication"));
                }
                ___detoxCounter = 0f;
            }
            // end inlined detox()
        }

        return false;
    }
}

}
