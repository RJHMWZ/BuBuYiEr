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
        [SerializeField] private AudioClip countStar = null;
        public AudioClip CountStar => countStar;


        [SerializeField] private AudioClip levelCompleted = null;
        public AudioClip LevelCompleted => levelCompleted;
        [SerializeField] private AudioClip levelFailed = null;
        public AudioClip LevelFailed => levelFailed;



        [SerializeField] private AudioClip rewarded = null;
        public AudioClip Rewarded => rewarded;


        private Coroutine musicFade;
        private enum FadeEnd { KeepPlaying, Stop, Pause }

        private IEnumerator Start()
        {
            yield return null;
            UpdateVolumes();
        }

        public void UpdateVolumes()
        {
            if (soundSource != null) soundSource.volume = PlayerDataHandler.GetSoundValue();
            if (musicSource != null) musicSource.volume = PlayerDataHandler.GetMusicValue();
        }

        public void PlaySound(AudioClip audioClip)
        {
            if (soundSource != null && audioClip != null) soundSource.PlayOneShot(audioClip);
        }

        public void PlayMusic(AudioClip audioClip, float increaseVolumeTime)
        {
            if (musicSource == null || audioClip == null) return;
            CancelFade();
            musicSource.clip = audioClip;
            musicSource.loop = true;
            musicSource.Play();
            FadeMusic(0, PlayerDataHandler.GetMusicValue(), increaseVolumeTime, FadeEnd.KeepPlaying);
        }

        public void StopMusic(float decreaseVolumeTime)
        {
            if (musicSource != null) FadeMusic(musicSource.volume, 0, decreaseVolumeTime, FadeEnd.Stop);
        }

        public void PauseMusic(float decreaseVolumeTime)
        {
            if (musicSource != null) FadeMusic(musicSource.volume, 0, decreaseVolumeTime, FadeEnd.Pause);
        }

        public void ResumeMusic(float increaseVolumeTime)
        {
            if (musicSource == null) return;
            CancelFade();
            musicSource.UnPause();
            FadeMusic(0, PlayerDataHandler.GetMusicValue(), increaseVolumeTime, FadeEnd.KeepPlaying);
        }

        private void FadeMusic(float from, float to, float duration, FadeEnd end)
        {
            // ?????????????????????????????
            CancelFade();
            if (duration <= 0)
            {
                musicSource.volume = to;
                FinishFade(end, from);
                return;
            }
            musicFade = StartCoroutine(FadeVolume(from, to, duration, end));
        }

        private IEnumerator FadeVolume(float from, float to, float duration, FadeEnd end)
        {
            musicSource.volume = from;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            musicSource.volume = to;
            musicFade = null;
            FinishFade(end, from);
        }

        private void FinishFade(FadeEnd end, float previousVolume)
        {
            if (end == FadeEnd.Stop) musicSource.Stop();
            else if (end == FadeEnd.Pause)
            {
                musicSource.Pause();
                musicSource.volume = previousVolume;
            }
        }

        private void CancelFade()
        {
            if (musicFade == null) return;
            StopCoroutine(musicFade);
            musicFade = null;
        }

        private void OnDisable() => CancelFade();
    }
}
