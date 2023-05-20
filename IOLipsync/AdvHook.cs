using HarmonyLib;
using UnityEngine;

namespace IOLipsync
{
    internal class AdvHook
    {
        // Value must be a power of 2. Min = 64. Max = 8192.
        private static readonly int _numSamples = 1024;
        private static float[] _samples = new float[_numSamples];
        private static float _volume = 0f;

        private static float _mouthVolume = 0f;
        private static float _mouthOpenness = 0f;

        private static float _eyeVolume = 0f;
        private static float _eyeOpenness = 1f;

        private static Animator _nekoAnimator;
        private static Animator _tomoeAnimator;

        [HarmonyPatch(typeof(KutiPaku), "Start")]
        [HarmonyPrefix]
        public static void Start(ref KutiPaku __instance)
        {
            if (__instance.gameObject.name == "CH01")
            {
                _nekoAnimator = GameObject.Find("CH01/CH0001").GetComponent<Animator>();
            }
            else if (__instance.gameObject.name == "CH02")
            {
                _tomoeAnimator = GameObject.Find("CH02/CH0002").GetComponent<Animator>();

            }
        }

        [HarmonyPatch(typeof(KutiPaku), "VoiceRip")]
        [HarmonyPrefix]
        public static bool VoiceRip(ref KutiPaku __instance)
        {
            // Calc volume
            GameObject.Find("MainSystem").GetComponent<ADV_Loader>().Voice.GetOutputData(_samples, 0);

            _volume = 0f;
            for (int i = 0; i < _numSamples; i++)
            {
                _volume = _samples[i] * _samples[i];
            }
            _volume = Mathf.Sqrt(_volume / _numSamples);

            // Calc mouth openness
            _mouthVolume = _volume * Hook.LipsyncConfig.MouthOpenness.Value;
            _mouthOpenness = Mathf.SmoothStep(_mouthOpenness, _mouthVolume, Hook.LipsyncConfig.MouthSmoothness.Value);
            _mouthOpenness = Mathf.Clamp01(_mouthOpenness);

            // Calc eye openness
            _eyeVolume = _volume * Hook.LipsyncConfig.EyeOpenness.Value;
            if (_eyeVolume < Hook.LipsyncConfig.EyeThreshold.Value)
                _eyeOpenness = Mathf.SmoothStep(_eyeOpenness, 1f, Hook.LipsyncConfig.EyeSmoothness.Value);
            else
                _eyeOpenness = Mathf.SmoothStep(_eyeOpenness, 1f - _eyeVolume, Hook.LipsyncConfig.EyeSmoothness.Value);
            _eyeOpenness = Mathf.Clamp01(_eyeOpenness);

            // Change mouth and eyes
            if (__instance.gameObject.name == "CH01")
            {
                _nekoAnimator.SetLayerWeight(3, _mouthOpenness);
                if(Hook.LipsyncConfig.EyeEnabled.Value)
                    _nekoAnimator.SetLayerWeight(1, _eyeOpenness);
            }
            else
            {
                _tomoeAnimator.SetLayerWeight(3, _mouthOpenness);
                if (Hook.LipsyncConfig.EyeEnabled.Value)
                    _tomoeAnimator.SetLayerWeight(1, _eyeOpenness);
            }

            return false;
        }
    }
}
