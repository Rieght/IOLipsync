using HarmonyLib;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace IOLipsync
{
    internal class FhHook
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
                _eyeOpenness = Mathf.SmoothStep(_eyeOpenness, 1f, Hook.LipsyncConfig.EyeSmoothness.Value);
                _mouthOpenness = Mathf.SmoothStep(_mouthOpenness, 0f, Hook.LipsyncConfig.MouthSmoothness.Value);

                _animator.SetLayerWeight(2, _eyeOpenness);
                _animator.SetLayerWeight(7, _mouthOpenness);
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
            if (_eyeVolume < Hook.LipsyncConfig.EyeThreshold.Value)
                _eyeOpenness = Mathf.SmoothStep(_eyeOpenness, 1f, Hook.LipsyncConfig.EyeSmoothness.Value);
            else
                _eyeOpenness = Mathf.SmoothStep(_eyeOpenness, 1f - _eyeVolume, Hook.LipsyncConfig.EyeSmoothness.Value);
            _eyeOpenness = Mathf.Clamp01(_eyeOpenness);

            // Calc mouth openness
            _mouthVolume = _volume * Hook.LipsyncConfig.MouthOpenness.Value;
            _mouthOpenness = Mathf.SmoothStep(_mouthOpenness, _mouthVolume, Hook.LipsyncConfig.MouthSmoothness.Value);
            _mouthOpenness = Mathf.Clamp01(_mouthOpenness);

            // Change mouth
            _animator.SetLayerWeight(7, _mouthOpenness);

            // Change eyes
            if (Hook.LipsyncConfig.EyeEnabled.Value)
                _animator.SetLayerWeight(2, _eyeOpenness);

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
}
