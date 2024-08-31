using System;
using System.Security.Permissions;
using System.Security;
using BepInEx;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using RoR2;
using UnityEngine;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace FixIntroSkip
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class FixIntroSkipPlugin : BaseUnityPlugin
    {
        public const string
            GUID = "groovesalad." + NAME,
            NAME = "FixIntroSkip",
            VERSION = "1.0.0";

        const string SPLASH_SCENE_GUID = "fe13e00695117284fb0d1b25422a0529";
        const string INTRO_SCENE_GUID = "330d792ae1727574e969e68ce8e966d2";

        public void Awake()
        {
            On.RoR2.SplashScreenController.Finish += SplashScreenController_Finish;
            On.LoadHelper.LoadSceneAdditivelyAsync += LoadHelper_LoadSceneAdditivelyAsync;
            On.LoadHelper.StartLoadSceneAdditiveAsync += LoadHelper_StartLoadSceneAdditiveAsync;
        }

        private void SplashScreenController_Finish(On.RoR2.SplashScreenController.orig_Finish orig, SplashScreenController self)
        {
            if (IntroCutsceneController.shouldSkip && RoR2Application.isLoading)
            {
                // stop the title scene from loading too early
                RoR2Application.onLoad = (Action)Delegate.Combine(RoR2Application.onLoad, delegate ()
                {
                    PlatformSystems.networkManager.ServerChangeScene("title");
                });
                return;
            }
            orig(self);
        }

        private IEnumerator LoadHelper_LoadSceneAdditivelyAsync(On.LoadHelper.orig_LoadSceneAdditivelyAsync orig, string _sceneGuid)
        {
            if (_sceneGuid == SPLASH_SCENE_GUID && SplashScreenController.cvSplashSkip.value)
            {
                static IEnumerator FinishSplashScreen()
                {
                    GameObject splash = new GameObject("Splash");
                    splash.SetActive(false);
                    SplashScreenController splashScreenController = splash.AddComponent<SplashScreenController>();
                    splash.AddComponent<TriggerIntroMusicStart>();
                    splash.SetActive(true);
                    splashScreenController.Finish();
                    yield break;
                }
                return FinishSplashScreen();
            }
            return orig(_sceneGuid);
        }

        private AsyncOperationHandle LoadHelper_StartLoadSceneAdditiveAsync(On.LoadHelper.orig_StartLoadSceneAdditiveAsync orig, string _sceneGuid)
        {
            // this seems to always brick the game if left unattended
            if (_sceneGuid == INTRO_SCENE_GUID && RoR2Application.isLoading)
            {
                return default;
            }
            return orig(_sceneGuid);
        }
    }
}
