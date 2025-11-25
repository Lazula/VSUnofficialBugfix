using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

using HarmonyLib;

namespace UnofficialBugfix
{
    public class UnofficialBugfixModSystem : ModSystem
    {
        public static ILogger Logger { get; private set; }
        public static ICoreAPI Api { get; private set; }
        private Harmony patcher;

        public override void StartPre(ICoreAPI api) {
            Logger = Mod.Logger;
            Api = api;
        }

        public override void Start(ICoreAPI api)
        {
            if (!Harmony.HasAnyPatches(Mod.Info.ModID)) {
                patcher = new Harmony(Mod.Info.ModID);
                patcher.PatchCategory(Mod.Info.ModID);

                if (api.ModLoader.IsModSystemEnabled("SlowTox.SlowToxSystem")) {
                    Mod.Logger.Notification(Lang.Get("unofficialbugfix:tox-patches-off"));
                } else {
                    patcher.PatchCategory($"{Mod.Info.ModID}-tox");
                    Mod.Logger.Notification(Lang.Get("unofficialbugfix:tox-patches-on"));
                }

                Mod.Logger.Notification(Lang.Get("unofficialbugfix:loading"));
            }
        }

        public override void Dispose() {
            patcher?.UnpatchAll(Mod.Info.ModID);
        }
    }
}
