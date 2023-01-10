using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IOLipsync
{
   [BepInEx.BepInPlugin(_guid, "IOLipsync", "1.0.0")]
    public class Hook : BaseUnityPlugin
    {
        private const string _guid = "rieght.insultorder.iolipsync";

        public static LipsyncConfig LipsyncConfig = new LipsyncConfig();

        public Hook()
        {
            var harmony = new Harmony(_guid);
            harmony.PatchAll(typeof(TestHook));
            harmony.PatchAll(typeof(UpdateBlendShapeHook));

            this.AddConfigs();
        }

        public void AddConfigs()
        {
            LipsyncConfig.DebugMenu = Config.Bind<bool>(new ConfigDefinition("Debug", "Debug Menu"), false);
            LipsyncConfig.MouthOpenness = Config.Bind<float>(new ConfigDefinition("Debug", "Mouth Openness"), 200f);
        }
    }

    public class LipsyncConfig
    {
        public ConfigEntry<bool> DebugMenu { get; set; }
        public ConfigEntry<float> MouthOpenness { get; set; }
    }

    public static class LipsyncDebug
    {
        public static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("LipSync");
    }

    public static class UpdateBlendShapeHook
    {
        private static bool _inDialogue;

        private static AudioSource _audioSource;
        private static Animator _animator;

        // Value must be a power of 2. Min = 64. Max = 8192.
        private static readonly int _numSamples = 1024;
        private static float[] _samples = new float[_numSamples];
        private static float _sum;
        private static float _rms;
        private static float _openness;

        private static readonly string _nekoPath = "CH01/CH0001/HS_kiten/bip01/bip01 Pelvis/bip01 Spine/bip01 Spine1/bip01 Spine2/bip01 Neck/bip01 Head/HS_HeadScale/HS_Head";
        private static readonly string _tomoePath = "CH02/CH0002/HS_kiten/bip01/bip01 Pelvis/bip01 Spine/bip01 Spine1/bip01 Spine2/bip01 Neck/bip01 Head/HS_HeadScale/HS_Head";

        // Update mouth openness
        [HarmonyPatch(typeof(FaceMotion), "Update")]
        [HarmonyPostfix]
        public static void PostUpdate()
        {
            if (!_inDialogue) return;
            if (_audioSource == null || !_audioSource.isPlaying || _audioSource.time > _audioSource.clip.length) return;

            _audioSource.GetOutputData(_samples, 0);
            _sum = 0f;
            for (int i = 0; i < _numSamples; i++)
            {
                _sum = _samples[i] * _samples[i];
            }
            _rms = Mathf.Sqrt(_sum / _numSamples);
            _openness = Mathf.Clamp01(_rms * Hook.LipsyncConfig.MouthOpenness.Value);

            // TODO: Maybe add some movement to the face (layer 2, default 1f)

            _animator.SetLayerWeight(7, _openness);
        }

        // Get animator and audiosource when Neko/Tomoe says a new line.
        [HarmonyPatch(typeof(FH_AnimeController), "WordPlay")]
        [HarmonyPostfix]
        public static void PostWordPlay(int No)
        {
            // TODO: Don't update animator on every line.
            _inDialogue = true;

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
    public static class TestHook
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
