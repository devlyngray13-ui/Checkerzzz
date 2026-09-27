using System;
using System.Reflection;
using BoneLib.BoneMenu;
using MelonLoader;

[assembly: MelonInfo(typeof(QuestFOV.MinimalFOVMod), "Quest FOV", "1.1.0", "OpenAI")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace QuestFOV
{
    public sealed class MinimalFOVMod : MelonMod
    {
        private const float Min = 40f;
        private const float Max = 140f;
        private float fov = 90f;

        public override void OnInitializeMelon()
        {
            var page = Page.Root.CreatePage("Quest FOV", System.Drawing.Color.White, 0, true);

            page.CreateFloat(
                "FOV",
                System.Drawing.Color.Cyan,
                fov,
                1f,
                Min,
                Max,
                value =>
                {
                    fov = value;
                    Apply();
                });

            page.CreateFunction("Reset (90)", System.Drawing.Color.White, () =>
            {
                fov = 90f;
                Apply();
            });
        }

        public override void OnUpdate()
        {
            // Keep this light: no direct Unity/IL2CPP compile-time references.
            Apply();
        }

        private void Apply()
        {
            try
            {
                // Everything below is reflection so the project does not directly
                // reference UnityEngine or Assembly-CSharp at compile time.
                Type cameraType = FindType("UnityEngine.Camera");
                if (cameraType == null) return;

                PropertyInfo allCameras = cameraType.GetProperty(
                    "allCameras",
                    BindingFlags.Public | BindingFlags.Static);

                Array cameras = allCameras?.GetValue(null) as Array;
                if (cameras == null) return;

                foreach (object camera in cameras)
                {
                    if (camera == null) continue;

                    PropertyInfo stereoEnabled = cameraType.GetProperty("stereoEnabled");
                    bool stereo = stereoEnabled != null &&
                                  (bool)stereoEnabled.GetValue(camera);

                    PropertyInfo fovProperty = cameraType.GetProperty("fieldOfView");
                    if (fovProperty != null)
                        fovProperty.SetValue(camera, fov);

                    // Some Quest/XR camera setups use their own projection.
                    // Resetting the stereo matrices lets the active XR runtime
                    // use the camera's current projection rather than leaving a
                    // stale custom matrix behind.
                    if (stereo)
                    {
                        MethodInfo resetStereo = cameraType.GetMethod(
                            "ResetStereoProjectionMatrices",
                            BindingFlags.Public | BindingFlags.Instance);

                        resetStereo?.Invoke(camera, null);
                    }
                }
            }
            catch
            {
                // Camera implementations vary between BONELAB/Quest versions.
            }
        }

        private static Type FindType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type t = assembly.GetType(name, false);
                    if (t != null) return t;
                }
                catch { }
            }

            return null;
        }
    }
}
