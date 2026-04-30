global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Reflection;
global using System.Runtime.CompilerServices;

global using Vintagestory.API.Client;
global using Vintagestory.API.Common;
global using Vintagestory.API.Common.Entities;
global using Vintagestory.API.Config;
global using Vintagestory.API.Datastructures;
global using Vintagestory.API.MathTools;
global using Vintagestory.API.Server;
global using Vintagestory.API.Util;
global using Vintagestory.GameContent;
global using Vintagestory.GameContent.Mechanics;

global using HarmonyLib;

namespace UnofficialBugfix;

public class UnofficialBugfixModSystem : ModSystem
{
    public static ILogger Logger { get; private set; }
    public static ICoreAPI Api { get; private set; }
    private Harmony patcher;

    public class CompatInfo
    {
        public string CategoryId { get; }
        public string ModName { get; }
        public bool ModFound { get; }

        public CompatInfo(string categoryId, string name)
        {
            CategoryId = categoryId;
            ModName = name;
            ModFound = Api.ModLoader.IsModEnabled(name);
        }

        public CompatInfo(string categoryId, string name, string systemName)
        {
            CategoryId = categoryId;
            ModName = name;
            ModFound = Api.ModLoader.IsModSystemEnabled($"{name}.{systemName}");
        }

        public void PrintCompatString()
        {
            Logger.Notification(Lang.Get(
                "unofficial-bugfix:compat-notif",
                ModName,
                Lang.Get(ModFound ? "unofficial-bugfix:found" : "unofficial-bugfix:not found"),
                Lang.Get(ModFound ? "unofficial-bugfix:disabled" : "unofficial-bugfix:enabled")
            ));
        }
    }

    public override void StartPre(ICoreAPI api)
    {
        Logger = Mod.Logger;
        Api = api;
    }

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        if (!Harmony.HasAnyPatches(Mod.Info.ModID))
        {
            patcher = new Harmony(Mod.Info.ModID);
            patcher.PatchCategory(Mod.Info.ModID);

            List<CompatInfo> allCompat = new List<CompatInfo>([
                new CompatInfo("noncompat-slowtox", "SlowTox", "SlowToxSystem"),
                new CompatInfo("noncompat-xskills", "XSkills", "XSkills")
            ]);

            foreach (CompatInfo ci in allCompat)
            {
                if (!ci.ModFound)
                {
                    patcher.PatchCategory($"{Mod.Info.ModID}-{ci.CategoryId}");
                }

                ci.PrintCompatString();
            }

            Mod.Logger.Notification(Lang.Get("unofficial-bugfix:loaded"));
        }
    }

    public override void Dispose()
    {
        patcher?.UnpatchAll(Mod.Info.ModID);
    }
}
