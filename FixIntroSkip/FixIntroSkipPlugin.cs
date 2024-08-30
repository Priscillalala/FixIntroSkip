using System;
using System.Security.Permissions;
using System.Security;
using BepInEx;
using System.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using RoR2;
using UnityEngine;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]
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
            On.RoR2.RoR2Application.PrintSW += RoR2Application_PrintSW;
            On.RoR2.SplashScreenController.Finish += SplashScreenController_Finish;
            On.LoadHelper.LoadSceneAdditivelyAsync += LoadHelper_LoadSceneAdditivelyAsync;
            On.LoadHelper.StartLoadSceneAdditiveAsync += LoadHelper_StartLoadSceneAdditiveAsync;
        }

        private void RoR2Application_PrintSW(On.RoR2.RoR2Application.orig_PrintSW orig, string message)
        {
            orig(message);
            Debug.Log(message);
        }

        private void SplashScreenController_Finish(On.RoR2.SplashScreenController.orig_Finish orig, SplashScreenController self)
        {
            if (IntroCutsceneController.shouldSkip)
            {
                // stop the title scene from loading too early
                return;
            }
            orig(self);
        }

        public static bool ShouldSkipScene(string guid)
        {
            switch (guid)
            {
                case SPLASH_SCENE_GUID:
                    return SplashScreenController.cvSplashSkip.value;
                case INTRO_SCENE_GUID:
                    if (IntroCutsceneController.shouldSkip)
                    {
                        if (!RoR2Application.loadFinished)
                        {
                            RoR2Application.onLoad = (Action)Delegate.Combine(RoR2Application.onLoad, delegate ()
                            {
                                PlatformSystems.networkManager.ServerChangeScene("title");
                            });
                        }
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        private IEnumerator LoadHelper_LoadSceneAdditivelyAsync(On.LoadHelper.orig_LoadSceneAdditivelyAsync orig, string _sceneGuid)
        {
            if (ShouldSkipScene(_sceneGuid))
            {
                /*if (_sceneGuid == SPLASH_SCENE_GUID && !IntroCutsceneController.shouldSkip)
                {
                    return orig(INTRO_SCENE_GUID);
                }*/
                return Enumerable.Empty<object>().GetEnumerator();
            }
            return orig(_sceneGuid);
        }

        private AsyncOperationHandle LoadHelper_StartLoadSceneAdditiveAsync(On.LoadHelper.orig_StartLoadSceneAdditiveAsync orig, string _sceneGuid)
        {
            if (ShouldSkipScene(_sceneGuid))
            {
                return default;
            }
            if (_sceneGuid == INTRO_SCENE_GUID)
            {
                RoR2Application.instance?.StartCoroutine(LoadHelper.LoadSceneAdditivelyAsync(_sceneGuid));
                return default;
            }
            return orig(_sceneGuid);
        }
    }
}
