using ApacheTech.Common.Extensions.Harmony;
using HarmonyLib;
using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace firepitfix
{
    public class firepitfixModSystem : ModSystem
    {
        public Harmony harmony;

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }


        public override void StartServerSide(ICoreServerAPI api)
        {
            if (!Harmony.HasAnyPatches(Mod.Info.ModID))
            {
                harmony = new Harmony(Mod.Info.ModID);
                harmony.PatchAll(typeof(firepitfixModSystem).Assembly);
            }
        }


        [HarmonyPatch]
        public class PatchBlockEntityFirepit
        {
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(BlockEntityFirepit), "Initialize")]
            public static BlockEntityFirepit Init(BlockEntityFirepit __instance)
                {
                // its a stub so it has no initial content
                    __instance.CallBaseMethod<BlockEntityFirepit>("Initialize");

                //need to run a check in the reverse patch for whether the block code includes "construct"
                //base.Block.Code.Path.Contains("construct");

                    return __instance;
            }
        }

        [HarmonyPatch(typeof(BlockEntityFirepit), "Initialize")]
        public static class BlockEntityFirepit_Initialize_Patch
        {
            static Action<float> OnBurnTick = AccessTools.Method(typeof(BlockEntityFirepit), "OnBurnTick").CreateDelegate<Action<float>>();
            static Action<float> On500msTick = AccessTools.Method(typeof(BlockEntityFirepit), "On500msTick").CreateDelegate<Action<float>>();
            // Prefix: runs before the original Initialize
            public static bool Prefix(BlockEntityFirepit __instance, InventorySmelting ___inventory,FirepitContentsRenderer ___renderer )
            {
                // Equivalent to base.Initialize(api);
                __instance.GetType().BaseType
                    ?.GetMethod("Initialize", new[] { typeof(ICoreAPI) })
                    ?.Invoke(__instance, new object[] { __instance.Api });
                
                ___inventory.Pos = __instance.Pos;
                ___inventory.LateInitialize($"smelting-{__instance.Pos.X}/{__instance.Pos.Y}/{__instance.Pos.Z}", __instance.Api);
                if (!__instance.Block.Code.Path.Contains("construct"))
                {
                    __instance.RegisterGameTickListener(OnBurnTick, 100);
                    __instance.RegisterGameTickListener(On500msTick, 500);
                }

                if (__instance.Api is ICoreClientAPI capi)
                {
                    ___renderer = new FirepitContentsRenderer(capi, __instance.Pos);
                    capi.Event.RegisterRenderer(___renderer, EnumRenderStage.Opaque, "firepit");
                    __instance.CallMethod("updateRenderer");
                    //__instance.UpdateRenderer();
                }
                return false;
            }
        }
    }
}
