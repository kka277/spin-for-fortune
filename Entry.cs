using UnityEngine;

namespace SpinForFortune
{
    /// <summary>
    /// MODのエントリーポイント。metadata.jsonのentryClassが、このクラスの完全修飾名("SpinForFortune.Entry")を指定します。
    /// </summary>
    public class Entry : WorkshopModBase
    {
        private HarmonyLib.Harmony harmony;
        private static bool loaded = false;

        // プロセスごとに一度だけ呼び出されます。イベントの購読はここで一度だけ行ってください。
        protected override void OnModLoaded()
        {
            if (loaded)
                return;
            Debug.Log($"[SpinForFortune] OnModLoaded: {metadata.modName} v{metadata.modVersion} by {metadata.modAuthor} " +
                      $"| package={Context?.PackageRoot}");

            harmony = new HarmonyLib.Harmony(Context.Definition.ModId);
            harmony.PatchAll(typeof(Entry).Assembly);

            HorayModAPI.OnLoadKeywordDatabase += HandleKeywordDatabaseLoaded;
            HorayModAPI.OnLoadItemDatabase += HandleItemDatabaseLoaded;
            HorayModAPI.OnAllDatabasesReady += RouletteArtifact.TryRegisterPrefab;
            HorayModAPI.OnTitleMenuReady += HandleTitleMenuReady;
            HorayModAPI.OnPlayerAvatarSpawnedClientside += HandlePlayerSpawnedClient;
            loaded = true;
        }

        private void HandleItemDatabaseLoaded()
        {
            RouletteUI.PackageRoot = Context.PackageRoot;
            RouletteArtifact.Register(Context.Definition.ModId);
        }

        private void HandleKeywordDatabaseLoaded()
        {
            RouletteArtifact.RegisterComboInfoKeywords(Context.Definition.ModId);
        }

        protected override void OnModUnloaded()
        {
            harmony?.UnpatchAll(harmony.Id);
            HorayModAPI.OnLoadKeywordDatabase -= HandleKeywordDatabaseLoaded;
            HorayModAPI.OnLoadItemDatabase -= HandleItemDatabaseLoaded;
            HorayModAPI.OnAllDatabasesReady -= RouletteArtifact.TryRegisterPrefab;
            HorayModAPI.OnTitleMenuReady -= HandleTitleMenuReady;
            HorayModAPI.OnPlayerAvatarSpawnedClientside -= HandlePlayerSpawnedClient;
        }

        private void HandleTitleMenuReady(UI_TitleLobby _) => RouletteArtifact.TryRegisterPrefab();

        // MODのアイテムはゲーム側の解放処理(Resources読み込み)に含まれないため、サーバー開始時に解放済みにする。
        [HarmonyLib.HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.OnStartServer))]
        private static class PlayerSpawnerUnlockPatch
        {
            private static void Postfix(PlayerSpawner __instance) => RouletteArtifact.Unlock(__instance);
        }

        private static void HandlePlayerSpawnedClient(PlayerAvatar player)
        {
            if (player == null)
                return;
            RouletteArtifact.TryRegisterPrefab();
            if (player.isLocalPlayer)
            {
                RouletteProgression.ResetClientFever();
                player.OnCustomMessageReceivedClientside += RouletteCharm.HandleClientMessage;
                RouletteUI.Preload();
            }
        }
    }
}
