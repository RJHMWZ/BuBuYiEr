using UnityEngine;
using System.Collections;

namespace ClawbearGames
{
    public enum GameSoundCue
    {
        UiClick, UiOpen, UiClose, BrickHit, BrickBreak, WallBounce, Purchase, Upgrade,
        RoundStart, Pause, Resume, Coin, Star, Reward, LevelComplete, LevelFail, StoryUnlock, Count
    }

    public class SoundManager : MonoBehaviour
    {
        [Header("Audio Source References")]
        [SerializeField] private AudioSource soundSource = null;
        [SerializeField] private AudioSource musicSource = null;

        [Header("Music / Effects Balance")]
        [SerializeField, Range(0f, 1f)] private float musicMixGain = .25f;
        [SerializeField, Range(0f, 1f)] private float collisionMusicGain = .65f;
        public float MusicMixGain => musicMixGain;


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
        private AudioSource outgoingMusic;
        private AudioClip homeMusic;
        private readonly AudioClip[] themeEffects = new AudioClip[(int)GameSoundCue.Count];
        private readonly AudioClip[] hitVariants = new AudioClip[3], breakVariants = new AudioClip[3];
        private bool effectsLoaded;
        private int hitSequence, breakSequence;
        private FadeEnd fadeEnd;
        private float musicEnvelope = 1, outgoingInitialEnvelope, crossfadeProgress = 1;
        private float musicDuck = 1, duckUntil;
        private static readonly string[] CueAssets = {
            "UI_Click", "UI_Open", "UI_Close", "BrickHit1", "BrickBreak1", "WallBounce", "Purchase", "Upgrade",
            "RoundStart", "Pause", "Resume", "Coin", "Star", "Reward", "LevelComplete", "LevelFail", "StoryUnlock"
        };
        private enum FadeEnd { KeepPlaying, Stop, Pause }

        private void Awake()
        {
            if(soundSource!=null)
            {
                soundSource.spatialBlend=0;soundSource.dopplerLevel=0;
                soundSource.ignoreListenerPause=true;soundSource.priority=64;
            }
            if(musicSource!=null){musicSource.spatialBlend=0;musicSource.dopplerLevel=0;musicSource.priority=160;musicSource.ignoreListenerPause=false;}
        }

        private IEnumerator Start()
        {
            EnsureThemeEffects();
            yield return null;
            UpdateVolumes();
        }

        public void UpdateVolumes()
        {
            if (soundSource != null) soundSource.volume = PlayerDataHandler.GetSoundValue();
            ApplyMusicVolumes();
        }

        private void Update()
        {
            float target=Time.unscaledTime<duckUntil?collisionMusicGain:1;
            float duration=target<musicDuck?.025f:.28f;
            float duck=Mathf.MoveTowards(musicDuck,target,Mathf.Max(.05f,1-collisionMusicGain)*Time.unscaledDeltaTime/duration);
            if(Mathf.Approximately(duck,musicDuck))return;
            musicDuck=duck;
            ApplyMusicVolumes();
        }

        private void ApplyMusicVolumes()
        {
            float level=PlayerDataHandler.GetMusicValue()*musicMixGain*musicDuck;
            if(musicSource!=null)musicSource.volume=level*musicEnvelope;
            if(outgoingMusic!=null)
                outgoingMusic.volume=level*Mathf.Lerp(outgoingInitialEnvelope,0,crossfadeProgress);
        }

        public void PlaySound(AudioClip audioClip)
        {
            if (soundSource != null && audioClip != null) soundSource.PlayOneShot(audioClip);
        }

        public AudioClip GetCue(GameSoundCue cue)
        {
            if(cue<0 || cue>=GameSoundCue.Count)return null;
            EnsureThemeEffects();
            return themeEffects[(int)cue];
        }

        public void PlayCue(GameSoundCue cue)
        {
            AudioClip clip=GetCue(cue);
            if(cue==GameSoundCue.BrickHit){clip=hitVariants[hitSequence] ?? clip;hitSequence=(hitSequence+1)%hitVariants.Length;}
            else if(cue==GameSoundCue.BrickBreak){clip=breakVariants[breakSequence] ?? clip;breakSequence=(breakSequence+1)%breakVariants.Length;}
            // 只为实际可听到的碰撞短暂让出音乐空间，不改变玩家保存的音量。
            if(clip!=null && soundSource!=null && soundSource.volume>0 &&
                (cue==GameSoundCue.BrickHit || cue==GameSoundCue.BrickBreak || cue==GameSoundCue.WallBounce))
                duckUntil=Time.unscaledTime+.08f;
            PlaySound(clip);
        }

        public void PlayHomeMusic()
        {
            if(homeMusic==null)homeMusic=Resources.Load<AudioClip>("BearAudio/HomeLoop");
            if(homeMusic!=null)PlayMusic(homeMusic,.6f);
        }

        private void EnsureThemeEffects()
        {
            if(effectsLoaded)return;
            effectsLoaded=true;
            for(int i=0;i<CueAssets.Length;i++)themeEffects[i]=Resources.Load<AudioClip>("BearAudio/"+CueAssets[i]);
            hitVariants[0]=themeEffects[(int)GameSoundCue.BrickHit];
            breakVariants[0]=themeEffects[(int)GameSoundCue.BrickBreak];
            for(int i=1;i<3;i++)
            {
                hitVariants[i]=Resources.Load<AudioClip>("BearAudio/BrickHit"+(i+1));
                breakVariants[i]=Resources.Load<AudioClip>("BearAudio/BrickBreak"+(i+1));
            }
        }

        public void PlayMusic(AudioClip audioClip, float increaseVolumeTime)
        {
            if (musicSource == null || audioClip == null) return;
            if(musicSource.clip==audioClip && musicSource.isPlaying && (musicFade==null || fadeEnd==FadeEnd.KeepPlaying))return;
            CancelFade();
            StopOutgoing();
            if(musicSource.isPlaying && musicSource.volume>0 && musicSource.clip!=null && increaseVolumeTime>0)
            {
                if(outgoingMusic==null)
                {
                    outgoingMusic=gameObject.AddComponent<AudioSource>();
                    outgoingMusic.playOnAwake=false;outgoingMusic.loop=true;outgoingMusic.spatialBlend=0;
                    outgoingMusic.priority=musicSource.priority;outgoingMusic.outputAudioMixerGroup=musicSource.outputAudioMixerGroup;
                }
                outgoingMusic.clip=musicSource.clip;
                outgoingMusic.volume=musicSource.volume;
                outgoingInitialEnvelope=musicEnvelope;
                crossfadeProgress=0;
                int position=musicSource.timeSamples;
                outgoingMusic.Play();
                if(position>=0 && position<outgoingMusic.clip.samples)outgoingMusic.timeSamples=position;
            }
            musicSource.clip = audioClip;
            musicSource.loop = true;
            musicSource.Play();
            FadeMusic(0, 1, increaseVolumeTime, FadeEnd.KeepPlaying);
        }

        public void StopMusic(float decreaseVolumeTime)
        {
            if (musicSource != null) FadeMusic(musicEnvelope, 0, decreaseVolumeTime, FadeEnd.Stop);
        }

        public void PauseMusic(float decreaseVolumeTime)
        {
            if (musicSource != null) FadeMusic(musicEnvelope, 0, decreaseVolumeTime, FadeEnd.Pause);
        }

        public void ResumeMusic(float increaseVolumeTime)
        {
            if (musicSource == null) return;
            CancelFade();
            musicSource.UnPause();
            FadeMusic(0, 1, increaseVolumeTime, FadeEnd.KeepPlaying);
        }

        private void FadeMusic(float from, float to, float duration, FadeEnd end)
        {
            // 一个过渡协程同时管理淡入淡出，切换曲目时保留旧曲的尾音。
            CancelFade();
            fadeEnd=end;
            outgoingInitialEnvelope=Mathf.Lerp(outgoingInitialEnvelope,0,crossfadeProgress);
            crossfadeProgress=0;
            if (duration <= 0)
            {
                musicEnvelope=to;
                ApplyMusicVolumes();
                FinishFade(end, from);
                return;
            }
            musicFade = StartCoroutine(FadeVolume(from, to, duration, end));
        }

        private IEnumerator FadeVolume(float from, float to, float duration, FadeEnd end)
        {
            musicEnvelope=from;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                crossfadeProgress=Mathf.Clamp01(elapsed/duration);
                musicEnvelope=Mathf.Lerp(from,to,crossfadeProgress);
                ApplyMusicVolumes();
                yield return null;
            }
            musicEnvelope=to;
            ApplyMusicVolumes();
            musicFade = null;
            FinishFade(end, from);
        }

        private void FinishFade(FadeEnd end, float previousVolume)
        {
            StopOutgoing();
            if (end == FadeEnd.Stop) musicSource.Stop();
            else if (end == FadeEnd.Pause)
            {
                musicSource.Pause();
                musicEnvelope=previousVolume;
                ApplyMusicVolumes();
            }
        }

        private void CancelFade()
        {
            if (musicFade == null) return;
            StopCoroutine(musicFade);
            musicFade = null;
        }

        private void StopOutgoing()
        {
            crossfadeProgress=1;outgoingInitialEnvelope=0;
            if(outgoingMusic==null)return;
            outgoingMusic.Stop();outgoingMusic.clip=null;
        }

        private void OnDisable() { CancelFade();StopOutgoing(); }
    }
}
