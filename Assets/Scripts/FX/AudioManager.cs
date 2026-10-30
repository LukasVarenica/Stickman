using System;
using System.Collections.Generic;
using UnityEngine;

namespace StickmanWarriors
{
    public enum Sfx
    {
        Attack, Hit, Crit, Ability, PlayerHurt, Heal, Lifesteal, Death, Slam, Explosion, Dash,
        BossIntro, BossAttack, BossPhase, UiClick, Unlock, Coin
    }

    /// <summary>Plays SFX by id with random variation + volume settings. Assign clips in the inspector (all optional).</summary>
    public class AudioManager : MonoBehaviour
    {
        [Serializable]
        public class SfxEntry
        {
            public Sfx id;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 1f;
            public Vector2 pitch = new Vector2(0.95f, 1.05f);
            public float minInterval = 0.04f;
        }

        static AudioManager instance;
        [SerializeField] List<SfxEntry> entries = new List<SfxEntry>();
        [SerializeField] AudioSource musicSource;
        const int PoolSize = 10;
        AudioSource[] pool; int next;
        readonly Dictionary<Sfx, SfxEntry> map = new Dictionary<Sfx, SfxEntry>();
        readonly Dictionary<Sfx, float> last = new Dictionary<Sfx, float>();

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            foreach (var e in entries) map[e.id] = e;
            pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++) { pool[i] = gameObject.AddComponent<AudioSource>(); pool[i].playOnAwake = false; }
            if (musicSource == null) { musicSource = gameObject.AddComponent<AudioSource>(); musicSource.loop = true; musicSource.playOnAwake = false; }
            RefreshVolumes();
        }

        public static void RefreshVolumes()
        {
            if (instance == null || instance.musicSource == null) return;
            instance.musicSource.volume = SettingsManager.Master * SettingsManager.Music;
        }

        public static void PlayMusic(AudioClip clip)
        {
            if (instance == null || clip == null) return;
            instance.musicSource.clip = clip; instance.musicSource.Play();
        }

        public static void Play(Sfx id)
        {
            if (instance == null) return;
            SfxEntry e;
            if (!instance.map.TryGetValue(id, out e) || e.clips == null || e.clips.Length == 0) return;
            float lastT;
            if (instance.last.TryGetValue(id, out lastT) && Time.unscaledTime - lastT < e.minInterval) return;
            instance.last[id] = Time.unscaledTime;

            var src = instance.pool[instance.next++ % PoolSize];
            src.pitch = UnityEngine.Random.Range(e.pitch.x, e.pitch.y);
            src.PlayOneShot(e.clips[UnityEngine.Random.Range(0, e.clips.Length)], e.volume * SettingsManager.Master * SettingsManager.Sfx);
        }
    }
}

