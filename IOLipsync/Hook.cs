using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IOLipsync
{
    [BepInEx.BepInPlugin(_guid, "IOLipsync", "1.2.0")]
    public class Hook : BaseUnityPlugin
    {
        private const string _guid = "rieght.insultorder.iolipsync";

        public static LipsyncConfig LipsyncConfig = new LipsyncConfig();

        public Hook()
        {
            var harmony = new Harmony(_guid);
            harmony.PatchAll(typeof(DebugHook));
            harmony.PatchAll(typeof(UpdateBlendShapeHook));

            this.AddConfigs();
        }

        public void AddConfigs()
        {
            LipsyncConfig.DebugMenu = Config.Bind<bool>(new ConfigDefinition("Debug", "Debug Menu"), false);
            LipsyncConfig.MouthOpenness = Config.Bind<float>(new ConfigDefinition("Mouth Movement", "Mouth Openness"), 200f);
            LipsyncConfig.MouthSmoothness = Config.Bind<float>(new ConfigDefinition("Mouth Movement", "Mouth Smoothness"), 20f); LipsyncConfig.MouthOpenness = Config.Bind<float>(new ConfigDefinition("Mouth", "Mouth Openness"), 200f);
            LipsyncConfig.EyeOpenness = Config.Bind<float>(new ConfigDefinition("Eye Movement", "Eye Openness"), 200f); LipsyncConfig.MouthOpenness = Config.Bind<float>(new ConfigDefinition("Mouth", "Mouth Openness"), 200f);
            LipsyncConfig.EyeSmoothness = Config.Bind<float>(new ConfigDefinition("Eye Movement", "Eye Smoothness"), 10f);
        }
    }

    public class LipsyncConfig
    {
        public ConfigEntry<bool> DebugMenu { get; set; }
        public ConfigEntry<float> MouthOpenness { get; set; }
        public ConfigEntry<float> MouthSmoothness { get; set; }
        public ConfigEntry<float> EyeOpenness { get; set; }
        public ConfigEntry<float> EyeSmoothness { get; set; }
    }

    public static class LipsyncDebug
    {
        public static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("LipSync");
    }

    public static class UpdateBlendShapeHook
    {
        private static AudioSource _audioSource;
        private static Animator _animator;

        // Value must be a power of 2. Min = 64. Max = 8192.
        private static readonly int _numSamples = 1024;
        private static float[] _samples = new float[_numSamples];
        private static float _volume = 0f;

        private static float _eyeVolume = 0f;
        private static float _eyeOpenness = 1f;

        private static float _mouthVolume = 0f;
        private static float _mouthOpenness = 0f;


        private static readonly string _nekoPath = "CH01/CH0001/HS_kiten/bip01/bip01 Pelvis/bip01 Spine/bip01 Spine1/bip01 Spine2/bip01 Neck/bip01 Head/HS_HeadScale/HS_Head";
        private static readonly string _tomoePath = "CH02/CH0002/HS_kiten/bip01/bip01 Pelvis/bip01 Spine/bip01 Spine1/bip01 Spine2/bip01 Neck/bip01 Head/HS_HeadScale/HS_Head";

        // Update mouth and eye openness
        [HarmonyPatch(typeof(FaceMotion), "Update")]
        [HarmonyPostfix]
        public static void PostUpdate()
        {
            if (SceneManager.GetActiveScene().name != "FH") return;
            if (_audioSource == null || !_audioSource.isPlaying || _audioSource.time > _audioSource.clip.length)
            {
                if (_animator == null) return;

                // Reset mouth and eyes at the end of each line.
                _animator.SetLayerWeight(2, 1f);
                _animator.SetLayerWeight(7, 0f);
                return;
            }

            // Calc volume
            _audioSource.GetOutputData(_samples, 0);
            _volume = 0f;
            for (int i = 0; i < _numSamples; i++)
            {
                _volume = _samples[i] * _samples[i];
            }
            _volume = Mathf.Sqrt(_volume / _numSamples);

            // Calc eye openness
            _eyeVolume = _volume * Hook.LipsyncConfig.EyeOpenness.Value;
            if (_eyeVolume < 0.5f)
                _eyeOpenness = Mathf.SmoothStep(_eyeOpenness, 1f, Time.deltaTime * Hook.LipsyncConfig.EyeSmoothness.Value);
            else
                _eyeOpenness = Mathf.SmoothStep(_eyeOpenness, 1f - _eyeVolume, Time.deltaTime * Hook.LipsyncConfig.EyeSmoothness.Value);
            _eyeOpenness = Mathf.Clamp01(_eyeOpenness);

            // Calc mouth openness
            _mouthVolume = _volume * Hook.LipsyncConfig.MouthOpenness.Value;
            _mouthOpenness = Mathf.SmoothStep(_mouthOpenness, _mouthVolume, Time.deltaTime * Hook.LipsyncConfig.MouthSmoothness.Value);
            _mouthOpenness = Mathf.Clamp01(_mouthOpenness);

            // Change eyes
            _animator.SetLayerWeight(2, _eyeOpenness);
            // Change mouth
            _animator.SetLayerWeight(7, _mouthOpenness);
        }

        // Get animator and audiosource when Neko/Tomoe says a new line.
        [HarmonyPatch(typeof(FH_AnimeController), "WordPlay")]
        [HarmonyPostfix]
        public static void PostWordPlay(int No)
        {
            // TODO: Don't update animator on every line.
            GameObject gameObjectWithAudioSources;

            var neko = GameObject.Find("CH0001");
            if (neko)
            {
                _animator = neko.GetComponent<Animator>();
                gameObjectWithAudioSources = GameObject.Find(_nekoPath);
                _audioSource = gameObjectWithAudioSources.GetComponents<AudioSource>()[1];
                return;
            }

            var tomoe = GameObject.Find("CH0002");
            if (tomoe)
            {
                _animator = tomoe.GetComponent<Animator>();
                gameObjectWithAudioSources = GameObject.Find(_tomoePath);
                _audioSource = gameObjectWithAudioSources.GetComponents<AudioSource>()[1];
                return;
            }

            LipsyncDebug.Logger.LogError("Couldn't get Audiosource!");
        }
    }

    // Creates a Debugging Window for the control of animation layers.
    // Code by lynzrand https://github.com/lynzrand/kk-lipsync/blob/master/IOLipSync/Hook.cs
    public static class DebugHook
    {
        private struct EnabledToggleKey : IEquatable<EnabledToggleKey>
        {
            public int id;
            public int layer;

            #region toggle

            public override bool Equals(object obj)
            {
                return obj is EnabledToggleKey key && Equals(key);
            }

            public bool Equals(EnabledToggleKey other)
            {
                return id == other.id &&
                       layer == other.layer;
            }

            public override int GetHashCode()
            {
                int hashCode = 2005647062;
                hashCode = hashCode * -1521134295 + id.GetHashCode();
                hashCode = hashCode * -1521134295 + layer.GetHashCode();
                return hashCode;
            }

            public static bool operator ==(EnabledToggleKey left, EnabledToggleKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(EnabledToggleKey left, EnabledToggleKey right)
            {
                return !(left == right);
            }

            #endregion toggle
        }

        private struct EnabledToggleValue
        {
            public bool enabled;
            public float value;
        }

        private static readonly Dictionary<int, Rect> windows = new Dictionary<int, Rect>();
        private static readonly Dictionary<EnabledToggleKey, EnabledToggleValue> enabledKeys = new Dictionary<EnabledToggleKey, EnabledToggleValue>();

        [HarmonyPatch(typeof(FaceMotion), "Awake")]
        [HarmonyPostfix]
        public static void AddDebugGui(FaceMotion __instance, Animator ___animator)
        {
            var guiComponent = __instance.gameObject.AddComponent<KutiPakuDebugGui>();
            guiComponent.target = ___animator;
        }

        private class KutiPakuDebugGui : MonoBehaviour
        {
            public Animator target;

            public void OnGUI()
            {
                if (!Hook.LipsyncConfig.DebugMenu.Value) return;
                var id = target.GetHashCode() + 0x14243;

                var animator = target;
                string title = string.Format("DEBUG/IOLipsync/{0}", target.gameObject.name);

                if (!windows.TryGetValue(id, out var lastWindow))
                {
                    lastWindow = new Rect(20, 20, 480, 480);
                }

                var window = GUILayout.Window(
                    id, lastWindow,
                (id1) =>
                {
                    GUI.FocusWindow(id1);
                    //GUILayout.BeginArea(new Rect(0, 0, 240, 480));
                    GUILayout.BeginVertical();
                    GUILayout.Label(title);
                    var cnt = animator.layerCount;
                    for (var i = 0; i < cnt; i++)
                    {
                        var toggle = new EnabledToggleKey { id = id, layer = i };
                        if (!enabledKeys.TryGetValue(toggle, out var value))
                        {
                            value = new EnabledToggleValue { enabled = false, value = 0 };
                        }

                        GUILayout.BeginHorizontal();
                        float layerWeight = animator.GetLayerWeight(i);
                        value.enabled = GUILayout.Toggle(value.enabled, string.Format("{0}", animator.GetLayerName(i), layerWeight), GUILayout.Width(90));
                        GUILayout.Label(string.Format("{0}", layerWeight), GUILayout.Width(90));
                        value.value = GUILayout.HorizontalSlider(value.value, 0, 1);
                        GUILayout.EndHorizontal();

                        if (value.enabled) animator.SetLayerWeight(i, value.value);
                        else value.value = layerWeight;

                        enabledKeys[toggle] = value;
                    }

                    GUILayout.EndVertical();
                    //GUILayout.EndArea();
                },
                title);

                windows[id] = window;
            }
        }
    }
}
