using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;

namespace IOLipsync
{
    [BepInEx.BepInPlugin(_guid, "IOLipsync", "1.3.0")]
    public class Hook : BaseUnityPlugin
    {
        private const string _guid = "rieght.insultorder.iolipsync";

        public static LipsyncConfig LipsyncConfig = new LipsyncConfig();

        public Hook()
        {
            var harmony = new Harmony(_guid);
            harmony.PatchAll(typeof(DebugHook));
            harmony.PatchAll(typeof(FhHook));
            harmony.PatchAll(typeof(AdvHook));

            this.AddConfigs();
        }

        public void AddConfigs()
        {
            LipsyncConfig.DebugMenu = Config.Bind<bool>(new ConfigDefinition("Debug", "Debug Menu"), false);
            LipsyncConfig.MouthOpenness = Config.Bind<float>(new ConfigDefinition("Mouth Movement", "Mouth Openness"), 100f, new ConfigDescription("", new FloatValueBase()));
            LipsyncConfig.MouthSmoothness = Config.Bind<float>(new ConfigDefinition("Mouth Movement", "Mouth Smoothness"), 0.5f, new ConfigDescription("", new FloatValueBase()));
            LipsyncConfig.EyeEnabled = Config.Bind<bool>(new ConfigDefinition("Eye Movement", "Eye Enabled"), true);
            LipsyncConfig.EyeOpenness = Config.Bind<float>(new ConfigDefinition("Eye Movement", "Eye Openness"), 400.0f, new ConfigDescription("", new FloatValueBase()));
            LipsyncConfig.EyeSmoothness = Config.Bind<float>(new ConfigDefinition("Eye Movement", "Eye Smoothness"), 0.1f, new ConfigDescription("", new FloatValueBase()));
            LipsyncConfig.EyeThreshold = Config.Bind<float>(new ConfigDefinition("Eye Movement", "Eye Threshold"), 1f, new ConfigDescription("", new FloatValueBase()));

        }
    }

    public class FloatValueBase : AcceptableValueBase
    {
        public FloatValueBase() : base(typeof(float))
        {
        }

        public override object Clamp(object value)
        {
            float f = (float)value;
            return f;
        }

        public override bool IsValid(object value)
        {
            return true;
        }

        public override string ToDescriptionString()
        {
            return "Float value between " + float.MinValue.ToString() + " and " + float.MaxValue.ToString();
        }
    }

    public class LipsyncConfig
    {
        public ConfigEntry<bool> DebugMenu { get; set; }
        public ConfigEntry<float> MouthOpenness { get; set; }
        public ConfigEntry<float> MouthSmoothness { get; set; }
        public ConfigEntry<bool> EyeEnabled { get; set; }
        public ConfigEntry<float> EyeOpenness { get; set; }
        public ConfigEntry<float> EyeSmoothness { get; set; }
        public ConfigEntry<float> EyeThreshold { get; set; }
    }

    public static class LipsyncDebug
    {
        public static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("LipSync");
    }
}
