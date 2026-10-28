using UnityEngine;
using System.Collections;

namespace ClawbearGames
{
    public class SoundManager : MonoBehaviour
    {
        [Header("Audio Source References")]
        [SerializeField] private AudioSource soundSource = null;
        [SerializeField] private AudioSource musicSource = null;


        [Header("Audio Clips References")]
        [SerializeField] private AudioClip button = null;
        public AudioClip Button => button;

        [SerializeField] private AudioClip collectCoin = null;
        public AudioClip CollectCoin => collectCoin;
        [SerializeField] private AudioClip selectPot = null;
        public AudioClip SelectPot => selectPot;
        [SerializeField] private AudioClip potJumped = null;
        public AudioClip PotJumped => potJumped;
        [SerializeField] private AudioClip potCleared = null;
        public AudioClip PotCleared => potCleared;
        [SerializeField] private AudioClip cubeJumped = null;
        public AudioClip CubeJumped => cubeJumped;


        [SerializeField] private AudioClip frozenBreak = null;
        public AudioClip FrozenBreak => frozenBreak;
        [SerializeField] private AudioClip hammerSmash = null;
        public AudioClip HammerSmash => hammerSmash;
        [SerializeField] private AudioClip potUnlocked = null;
        public AudioClip PotUnlocked => potUnlocked;
        [SerializeField] private AudioClip potBurned = null;
        public AudioClip PotBurned => potBurned;
        [SerializeField] private AudioClip potCoolOff = null;
        public AudioClip PotCoolOff => potCoolOff;
        [SerializeField] private AudioClip magnetPull = null;
        public AudioClip MagnetPull => magnetPull;


        [SerializeField] private AudioClip countStar = null;
        public AudioClip CountStar => countStar;


        [SerializeField] private AudioClip levelCompleted = null;
        public AudioClip LevelCompleted => levelCompleted;
        [SerializeField] private AudioClip levelFailed = null;
        public AudioClip LevelFailed => levelFailed;



        [SerializeField] private AudioClip rewarded = null;
        public AudioClip Rewarded => rewarded;


        private IEnumerator Start()
        {
            yield return null;
            UpdateVolumes();
        }



        /// <summary>
        /// Update the volume of sound and music based on data.
        /// </summary>
        public void UpdateVolumes()
        {
            soundSource.volume = PlayerDataHandler.GetSoundValue();
            musicSource.volume = PlayerDataHandler.GetMusicValue();
        }

        /// <summary>
        /// Play one audio clip as sound.
        /// </summary>
        /// <param name="audioClip"></param>
        public void PlaySound(AudioClip audioClip)
        {
            soundSource.PlayOneShot(audioClip);
        }


        /// <summary>
        /// Plays the given sound clip as music clip (automatically loop).
        /// The music will volume up from 0 to 0.3 with given increase volume time.
        /// </summary>
        /// <param name="audioClip"></param>
        /// <param name="increaseVolumeTime"></param>
        public void PlayMusic(AudioClip audioClip, float increaseVolumeTime)
        {
            musicSource.clip = audioClip;
            musicSource.loop = true;
            musicSource.Play();
            StartCoroutine(CRIncreaseVolume(increaseVolumeTime));
        }


        /// <summary>
        /// Coroutine increase volume from 0 to 0.3 with increasing time.
        /// </summary>
        /// <param name="increasingTime"></param>
        /// <returns></returns>
        private IEnumerator CRIncreaseVolume(float increasingTime)
        {
            musicSource.volume = 0f;
            float value = PlayerDataHandler.GetMusicValue();
            float t = 0;
            while (t < increasingTime)
            {
                t += Time.deltaTime;
                float factor = t / increasingTime;
                musicSource.volume = Mathf.Lerp(0f, value, factor);
                yield return null;
            }
        }


        /// <summary>
        /// Stop the current music with given decrease volume time.
        /// </summary>
        /// <param name="decreaseVolumeTime"></param>
        public void StopMusic(float decreaseVolumeTime)
        {
            StartCoroutine(CRDecreaseVolume(decreaseVolumeTime));
        }


        /// <summary>
        /// Coroutine decrease volume from 0.3 to 0 with given decreasing time.
        /// </summary>
        /// <param name="decreasingTime"></param>
        /// <returns></returns>
        private IEnumerator CRDecreaseVolume(float decreasingTime)
        {
            float t = 0;
            float currentVolume = musicSource.volume;
            while (t < decreasingTime)
            {
                t += Time.deltaTime;
                float factor = t / decreasingTime;
                musicSource.volume = Mathf.Lerp(currentVolume, 0, factor);
                yield return null;
            }
            musicSource.Stop();
        }




        /// <summary>
        /// Pauses the music with given decrease volume time.
        /// </summary>
        /// <param name="decreaseVolumeTime"></param>
        public void PauseMusic(float decreaseVolumeTime)
        {
            StartCoroutine(CRPauseMusic(decreaseVolumeTime));
        }


        /// <summary>
        /// Coroutine pause the music.
        /// </summary>
        /// <param name="decreasingTime"></param>
        /// <returns></returns>
        private IEnumerator CRPauseMusic(float decreasingTime)
        {
            float t = 0;
            float currentVolume = musicSource.volume;
            while (t < decreasingTime)
            {
                t += Time.deltaTime;
                float factor = t / decreasingTime;
                musicSource.volume = Mathf.Lerp(currentVolume, 0, factor);
                yield return null;
            }

            musicSource.Pause();
            musicSource.volume = currentVolume;
        }



        /// <summary>
        /// Resumes the music with increase volume time.
        /// </summary>
        /// <param name="increaseVolumeTime"></param>
        public void ResumeMusic(float increaseVolumeTime)
        {
            StartCoroutine(CRResumeMusic(increaseVolumeTime));
        }


        /// <summary>
        /// Coroutine resume the music.
        /// </summary>
        /// <param name="increasingTime"></param>
        /// <returns></returns>
        private IEnumerator CRResumeMusic(float increasingTime)
        {
            musicSource.UnPause();
            musicSource.volume = 0f;
            float value = PlayerDataHandler.GetMusicValue();
            float t = 0;
            while (t < increasingTime)
            {
                t += Time.deltaTime;
                float factor = t / increasingTime;
                musicSource.volume = Mathf.Lerp(0f, value, factor);
                yield return null;
            }
        }

    }
}
