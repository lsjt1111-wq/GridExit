using System.Collections;
using UnityEngine;

namespace GridTutorial
{
    public sealed class GridLoopAudio : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] GridMover playerMover;
        [SerializeField] AudioSource music, ui, feet;
        [SerializeField] AudioClip musicClip, hover, click, start;
        [SerializeField] AudioClip win, lose, reveal, back, foot;
        float nextFoot;
        bool returning;

        void OnEnable() => game.StateChanged += OnState;
        void OnDisable() => game.StateChanged -= OnState;
        void Start()
        {
            var settings = GridSettings.Instance;
            if (settings)
            {
                settings.Route(music, "Music");
                settings.Route(ui, "UI");
                settings.Route(feet, "SFX");
            }
            if (musicClip)
            {
                music.clip = musicClip;
                music.loop = true;
                music.Play();
            }
        }
        void OnState(LoopState state)
        {
            nextFoot = 0;
            if (state == LoopState.Playing) Play(start);
            else if (state == LoopState.Win) Play(win);
            else if (state == LoopState.Lose) Play(lose);
            if (state != LoopState.Playing) feet.Stop();
        }
        void Update()
        {
            if (game.State != LoopState.Playing || !playerMover.IsMoving) return;
            if (Time.time < nextFoot) return;
            nextFoot = Time.time + .3f;
            if (foot) feet.PlayOneShot(foot);
        }
        void Play(AudioClip clip)
        {
            if (clip) ui.PlayOneShot(clip);
        }
        public void PlayHover() => Play(hover);
        public void PlayClick() => Play(click);
        public void PlayReveal() => Play(reveal);
        public void SetMusicVolume(float value) => music.volume = Mathf.Clamp01(value);
        public void SetSfxVolume(float value)
        {
            ui.volume = Mathf.Clamp01(value);
            feet.volume = Mathf.Clamp01(value) * .4f;
        }
        public void ReturnWithSound()
        {
            if (returning) return;
            returning = true;
            StartCoroutine(ReturnRoutine());
        }
        IEnumerator ReturnRoutine()
        {
            Play(back);
            yield return new WaitForSecondsRealtime(back ? back.length : .15f);
            game.ReturnToMenu();
        }
    }
}

