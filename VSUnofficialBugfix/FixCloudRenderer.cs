// namespace UnofficialBugfix.FixCloudRenderer;

// using System.Reflection.Emit;
// using FluffyClouds;

// [HarmonyPatch(typeof(CloudRendererMap), nameof(CloudRendererMap.CloudTick))]
// public static class FixCloudRenderer
// {
//     public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
//     {
//         return new CodeMatcher(instructions, generator)
//             // Move to before the second deltaTime store
//             .MatchEndForward(new CodeMatch(OpCodes.Starg_S, "deltaTime"))
//             .InsertAfter(
//                 new CodeInstruction(OpCodes.Ldarg_1),
//                 new CodeInstruction(OpCodes.Ldc_R4, 1.0f),
//                 new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Min), [typeof(float), typeof(float)])),
//                 new CodeInstruction(OpCodes.Starg_S, "deltaTime")
//             )
//             .MatchEndForward(new CodeMatch(OpCodes.Starg_S, "deltaTime"))
//             //.Advance(0x86)
//             // Re-min the value
//             .InsertAfter(
//                 new CodeInstruction(OpCodes.Ldarg_1),
//                 new CodeInstruction(OpCodes.Ldc_R4, 1.0f),
//                 new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Min), [typeof(float), typeof(float)])),
//                 new CodeInstruction(OpCodes.Starg_S, "deltaTime")
//             )
//             .InstructionEnumeration();
//     }
// }


namespace UnofficialBugfix.FixCloudRenderer;

using System.Threading;
using FluffyClouds;

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixCloudRenderer
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "UpdateCloudTiles")]
    private static extern void UpdateCloudTiles(CloudRendererMap self);


    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "UpdateCloudTilesOffThread")]
    private static extern void UpdateCloudTilesOffThread(CloudRendererMap self, int changeSpeed);


    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "WriteTexture")]
    private static extern void WriteTexture(CloudRendererMap self);


    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "InitCloudTiles")]
    private static extern void InitCloudTiles(CloudRendererMap self);


    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "InitCloudTiles")]
    private static extern void InitCloudTiles(CloudRendererMap self, int viewDistance);



    [HarmonyPrefix()]
    [HarmonyPatch(typeof(CloudRendererMap), nameof(CloudRendererMap.CloudTick))]
    public static bool FixCloudTick(
        CloudRendererMap __instance,
        float deltaTime,
        ref ICoreClientAPI ___capi,
        ref float ___blendedCloudDensity,
        ref float ___blendedGlobalCloudBrightness,
        ref bool ___isFirstTick,
        ref WeatherSystemClient ___weatherSys,
        ref Thread ___cloudTileUpdThread,
        ref long ___windChangeTimer,
        ref float ___targetCloudSpeedX,
        ref float ___targetCloudSpeedZ,
        ref Random ___rand,
        ref float ___cloudSpeedX,
        ref float ___cloudSpeedZ,
        ref object ___cloudStateLock,
        ref double ___windOffsetX,
        ref double ___windOffsetZ,
        ref CloudTilesState ___mainThreadState,
        ref bool ___newStateRready,
        ref CloudTilesState ___offThreadState,
        ref CloudTilesState ___committedState,
        ref bool ___requireTileRebuild,
        ref bool ___instantTileBlend)
    {
        ___blendedCloudDensity = ___capi.Ambient.BlendedCloudDensity;
        ___blendedGlobalCloudBrightness = ___capi.Ambient.BlendedCloudBrightness;

        if (___isFirstTick)
        {
            ___weatherSys.ProcessWeatherUpdates();
            UpdateCloudTilesOffThread(__instance, short.MaxValue);
            ___cloudTileUpdThread.Start();
            ___isFirstTick = false;
        }

        deltaTime *= ___capi.World.Calendar.SpeedOfTime / 60f;
        deltaTime = Math.Min(deltaTime, 1);

        if (deltaTime > 0)
        {
            if (___windChangeTimer - ___capi.ElapsedMilliseconds < 0)
            {
                ___windChangeTimer = ___capi.ElapsedMilliseconds + ___rand.Next(20000, 120000);
                ___targetCloudSpeedX = (float)___rand.NextDouble() * 5f;
                ___targetCloudSpeedZ = (float)___rand.NextDouble() * 0.5f;
            }

            //float windspeedx = 3 * (float)wreaderpreload.GetWindSpeed(capi.World.Player.Entity.Pos.Y); - likely wrong
            float windspeedx = 3 * (float)___weatherSys.WeatherDataAtPlayer.GetWindSpeed(___capi.World.Player.Entity.Pos.Y);

            // Wind speed direction change smoothing 
            ___cloudSpeedX = ___cloudSpeedX + (___targetCloudSpeedX + windspeedx - ___cloudSpeedX) * deltaTime;
            ___cloudSpeedZ = ___cloudSpeedZ + (___targetCloudSpeedZ - ___cloudSpeedZ) * deltaTime;
        }

        lock (___cloudStateLock)
        {
            if (deltaTime > 0)
            {
                ___windOffsetX += ___cloudSpeedX * deltaTime;
                ___windOffsetZ += ___cloudSpeedZ * deltaTime;
            }

            ___mainThreadState.CenterTilePos.X = (int)(___capi.World.Player.Entity.Pos.X) / __instance.CloudTileSize;
            ___mainThreadState.CenterTilePos.Z = (int)(___capi.World.Player.Entity.Pos.Z) / __instance.CloudTileSize;
        }

        if (___newStateRready)
        {
            int dx = ___offThreadState.WindTileOffsetX - ___committedState.WindTileOffsetX;
            int dz = ___offThreadState.WindTileOffsetZ - ___committedState.WindTileOffsetZ;

            ___committedState.Set(___offThreadState);

            ___mainThreadState.WindTileOffsetX = ___committedState.WindTileOffsetX;
            ___mainThreadState.WindTileOffsetZ = ___committedState.WindTileOffsetZ;

            ___windOffsetX -= dx * __instance.CloudTileSize;
            ___windOffsetZ -= dz * __instance.CloudTileSize;

            ___weatherSys.ProcessWeatherUpdates();

            if (___requireTileRebuild)
            {
                InitCloudTiles(__instance, 8 * ___capi.World.Player.WorldData.DesiredViewDistance);
                UpdateCloudTiles(__instance);
                ___requireTileRebuild = false;
                ___instantTileBlend = true;
            }

            WriteTexture(__instance);

            ___newStateRready = false;
        }

        ___capi.World.FrameProfiler.Mark("gt-clouds");

        return false;
    }
}
