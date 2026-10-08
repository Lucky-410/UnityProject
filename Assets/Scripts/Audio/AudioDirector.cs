using Relicfall.Combat;
using Relicfall.Player;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Relicfall.Audio
{
    public sealed class AudioDirector : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup effectsGroup;
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameMusic;
        [SerializeField] private AudioClip attack;
        [SerializeField] private AudioClip enemyHit;
        [SerializeField] private AudioClip playerHit;
        [SerializeField] private AudioClip dash;
        [SerializeField] private AudioClip pickup;
        [SerializeField] private AudioClip click;

        private AudioSource musicSource;
        private AudioSource effectsSource;
        public static AudioDirector Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.38f;
            musicSource.outputAudioMixerGroup = musicGroup;
            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            effectsSource.volume = 0.70f;
            effectsSource.outputAudioMixerGroup = effectsGroup;
        }

        private void OnEnable()
        {
            if (Instance != this) return;
            SceneManager.activeSceneChanged += ChangeMusic;
            Health.OnAnyDamaged += PlayDamage;
        }

        private void Start() => ChangeMusic(default, SceneManager.GetActiveScene());

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= ChangeMusic;
            Health.OnAnyDamaged -= PlayDamage;
        }

        private void ChangeMusic(Scene oldScene, Scene nextScene)
        {
            AudioClip next = nextScene.name == "GameScene" ? gameMusic : menuMusic;
            if (next == null || musicSource.clip == next) return;
            musicSource.Stop();
            musicSource.clip = next;
            musicSource.Play();
        }

        private void PlayDamage(Health target, DamageInfo info)
        {
            bool player = target.TryGetComponent<PlayerMotor>(out _);
            Play(player ? playerHit : enemyHit, player ? 0.8f : 0.65f);
        }

        private void Play(AudioClip clip, float volume = 1f)
        {
            if (clip != null && effectsSource != null) effectsSource.PlayOneShot(clip, volume);
        }

        public void PlayAttack() => Play(attack, 0.55f);
        public void PlayDash() => Play(dash, 0.55f);
        public void PlayPickup() => Play(pickup, 0.75f);
        public void PlayClick() => Play(click, 0.55f);

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

#if UNITY_EDITOR
        public void Configure(AudioMixerGroup music, AudioMixerGroup effects,
            AudioClip menu, AudioClip game, AudioClip swing, AudioClip hitEnemy,
            AudioClip hitPlayer, AudioClip move, AudioClip collect, AudioClip ui)
        {
            musicGroup = music;
            effectsGroup = effects;
            menuMusic = menu;
            gameMusic = game;
            attack = swing;
            enemyHit = hitEnemy;
            playerHit = hitPlayer;
            dash = move;
            pickup = collect;
            click = ui;
        }
#endif
    }
}
