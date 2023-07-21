using HarmonyLib;
using OCUnion;
using RimWorld.Planet;
using System;
using System.Collections;
using UnityEngine;
using Verse;

namespace MapRenderer
{
    // Autor AaronCRobinson https://github.com/AaronCRobinson/MapRenderer
    // https://forum.unity3d.com/threads/render-texture-to-png-arbg32-no-opaque-pixels.317451/

    // NOTE: creating a new camera would be a better solution (how?)
    public class RenderMap : MonoBehaviour
    {
        private const int defaultPixelOnCell = 15;
        private const int defaultQuality = 80;
        
        public int SettingsPixelOnCell = defaultPixelOnCell;
        public int SettingsQuality = defaultQuality;
        public bool SettingsShowWeather = true;
        public Action<Func<byte[]>> ImageReady;

        private static bool isRendering;

        private Camera camera;
        private Map map;

        private Map rememberedMap;
        private bool switchedMap;
        private Vector3 rememberedRootPos;
        private float rememberedRootSize;
        private bool rememberedWorldRendered;
        private bool rememberedShowZones;
        private bool rememberedShowRoofOverlay;
        private bool rememberedShowFertilityOverlay;
        private bool rememberedShowTerrainAffordanceOverlay;
        private bool rememberedShowPollutionOverlay;
        private bool rememberedShowTemperatureOverlay;

        private int viewWidth;
        private int viewHeight;

        private RenderTexture rt;
        private Texture2D tempTexture;

        public static bool IsRendering { get => isRendering; set => isRendering = value; }

        // NOTE: unity is not calling the constructor, so we manually call it
        public RenderMap() { }

        public void Initialize(Map bymap)
        {
            if (bymap == null) bymap = Find.CurrentMap;
            map = bymap;
            viewWidth = map.Size.x * SettingsPixelOnCell;
            viewHeight = map.Size.z * SettingsPixelOnCell;
        }

        public void Render() => Find.CameraDriver.StartCoroutine(Renderer());

        private IEnumerator Renderer()
        {
            yield return new WaitForFixedUpdate();

            IsRendering = true;

            /// {
            switchedMap = false;
            rememberedMap = Find.CurrentMap;
            if (map != rememberedMap)
            {
                switchedMap = true;
                Current.Game.CurrentMap = map;
            }
            rememberedWorldRendered = WorldRendererUtility.WorldRenderedNow;
            if (rememberedWorldRendered)
            {
                CameraJumper.TryHideWorld();
            }
            var settings = Find.PlaySettings;
            rememberedShowZones = settings.showZones;
            rememberedShowRoofOverlay = settings.showRoofOverlay;
            rememberedShowFertilityOverlay = settings.showFertilityOverlay;
            rememberedShowTerrainAffordanceOverlay = settings.showTerrainAffordanceOverlay;
            rememberedShowPollutionOverlay = settings.showPollutionOverlay;
            rememberedShowTemperatureOverlay = settings.showTemperatureOverlay;
            settings.showZones = false;
            settings.showRoofOverlay = false;
            settings.showFertilityOverlay = false;
            settings.showTerrainAffordanceOverlay = false;
            settings.showPollutionOverlay = false;
            settings.showTemperatureOverlay = false;
            rememberedRootPos = map.rememberedCameraPos.rootPos;
            rememberedRootSize = map.rememberedCameraPos.rootSize;
            /// }

            rt = RenderTexture.GetTemporary(viewWidth, viewHeight, 24);
            tempTexture = new Texture2D(viewWidth, viewHeight, TextureFormat.RGB24, false);
            camera = Find.Camera;
            var camDriver = camera.GetComponent<CameraDriver>();
            camDriver.enabled = false;
            var rememberedFarClipPlane = camera.farClipPlane;

            var camViewRect = camDriver.CurrentViewRect;
            var camRectMinX = Math.Min(0, camViewRect.minX);
            var camRectMinZ = Math.Min(0, camViewRect.minZ);
            var camRectMaxX = Math.Max(map.Size.x, camViewRect.maxX);
            var camRectMaxZ = Math.Max(map.Size.z, camViewRect.maxZ);
            var camDriverTraverse = Traverse.Create(camDriver);
            camDriverTraverse.Field("lastViewRect").SetValue(CellRect.FromLimits(camRectMinX, camRectMinZ, camRectMaxX, camRectMaxZ));
            camDriverTraverse.Field("lastViewRectGetFrame").SetValue(Time.frameCount);

            yield return RenderCurrentView();

            /// {
            camera.farClipPlane = rememberedFarClipPlane;
            camDriver.SetRootPosAndSize(rememberedRootPos, rememberedRootSize);
            camDriver.enabled = true;
            RenderTexture.ReleaseTemporary(rt);
            Find.PlaySettings.showZones = rememberedShowZones;
            Find.PlaySettings.showRoofOverlay = rememberedShowRoofOverlay;
            Find.PlaySettings.showFertilityOverlay = rememberedShowFertilityOverlay;
            Find.PlaySettings.showTerrainAffordanceOverlay = rememberedShowTerrainAffordanceOverlay;
            Find.PlaySettings.showPollutionOverlay = rememberedShowPollutionOverlay;
            Find.PlaySettings.showTemperatureOverlay = rememberedShowTemperatureOverlay;
            if (rememberedWorldRendered)
            {
                CameraJumper.TryShowWorld();
            }
            if (switchedMap)
            {
                Current.Game.CurrentMap = rememberedMap;
            }
            /// }

            Func<byte[]> getImage = () =>
            {
                var encodedImage = tempTexture.EncodeToJPG(SettingsQuality);
                Destroy(this.tempTexture);
                return encodedImage;
            };
            if (ImageReady != null)
            {
                ImageReady(getImage);
            }
            else
            {
                Destroy(this.tempTexture);
            }

            Destroy(this.rt);

            IsRendering = false;

            yield return null;
        }

        private void RestoreCamera() => RenderTexture.active = this.camera.targetTexture = null;

        private void SetCamera() => RenderTexture.active = this.camera.targetTexture = this.rt;

        private IEnumerator RenderCurrentView()
        {
#if DEBUG
            Log.Message("Start of RenderCurrentView");
#endif
            yield return new WaitForEndOfFrame();
#if DEBUG
            Log.Message("After WaitForEndOfFrame");
#endif
            try
            {
                var cameraPosX = map.Size.x / 2;
                var cameraPosZ = map.Size.z / 2;
                var orthographicSize = cameraPosZ;
                var cameraBasePos = new Vector3(cameraPosX, 15f + (orthographicSize - 11f) / 49f * 50f, cameraPosZ);
                camera.orthographicSize = orthographicSize;
                camera.farClipPlane = cameraBasePos.y + 6.5f;
                SetCamera();
                RenderTexture.active = rt;
                if (SettingsShowWeather)
                {
                    map.weatherManager.DrawAllWeather();
                }
                camera.transform.position = new Vector3(cameraBasePos.x, cameraBasePos.y, cameraBasePos.z);
                camera.Render();
#if DEBUG
            Log.Message("After Render");
#endif
                tempTexture.ReadPixels(new Rect(0, 0, viewWidth, viewHeight), 0, 0, false);

                RenderTexture.active = null;
                RestoreCamera();
#if DEBUG
            Log.Message("End of RenderCurrentView");
#endif
            }
            catch (Exception exp)
            {
                Log.Error(exp.Message);
            }
        }
    }
}

