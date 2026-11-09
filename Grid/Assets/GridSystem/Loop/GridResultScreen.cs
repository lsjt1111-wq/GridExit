using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GridTutorial
{
    public sealed class GridResultScreen : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] GameObject panel;
        [SerializeField] RectTransform card;
        [SerializeField] CanvasGroup cardGroup, buttons;
        [SerializeField] TMP_Text title, reason, stats, score;
        [SerializeField] UnityEvent revealCue = new UnityEvent();
        Vector2 home;

        void Awake() => home = card.anchoredPosition;
        void OnEnable()
        {
            game.StateChanged += OnState;
            OnState(game.State);
        }
        void OnDisable()
        {
            game.StateChanged -= OnState;
            StopAllCoroutines();
        }
        void OnState(LoopState state)
        {
            StopAllCoroutines();
            if (state != LoopState.Win && state != LoopState.Lose)
            {
                panel.SetActive(false);
                return;
            }
            bool won = state == LoopState.Win;
            title.text = won ? "탈출 성공" : "탈출 실패";
            title.color = won ? new Color(.3f, 1, .7f) : new Color(1, .35f, .3f);
            reason.text = game.ResultReason;
            stats.text = $"플레이 시간 {game.Elapsed:0.0}초";
            score.text = "점수 0";
            cardGroup.alpha = buttons.alpha = 0;
            cardGroup.interactable = false;
            buttons.interactable = buttons.blocksRaycasts = false;
            card.localScale = Vector3.one * .9f;
            card.anchoredPosition = home + Vector2.down * 48;
            panel.SetActive(true);
            StartCoroutine(Reveal(game.FinalScore));
        }
        IEnumerator Reveal(int finalScore)
        {
            float elapsed = 0;
            while (elapsed < .3f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / .3f);
                float ease = 1 - Mathf.Pow(1 - t, 3);
                cardGroup.alpha = ease;
                card.localScale = Vector3.one * Mathf.Lerp(.9f, 1, ease);
                card.anchoredPosition = Vector2.Lerp(home + Vector2.down * 48, home, ease);
                yield return null;
            }
            cardGroup.alpha = 1;
            card.localScale = Vector3.one;
            card.anchoredPosition = home;
            revealCue.Invoke();
            elapsed = 0;
            while (elapsed < .6f)
            {
                elapsed += Time.unscaledDeltaTime;
                int shown = Mathf.RoundToInt(Mathf.Lerp(0, finalScore,
                    Mathf.Clamp01(elapsed / .6f)));
                score.text = $"점수 {shown:N0}";
                yield return null;
            }
            score.text = $"점수 {finalScore:N0}";
            elapsed = 0;
            while (elapsed < .2f)
            {
                elapsed += Time.unscaledDeltaTime;
                buttons.alpha = Mathf.Clamp01(elapsed / .2f);
                yield return null;
            }
            buttons.alpha = 1;
            cardGroup.interactable = true;
            buttons.interactable = buttons.blocksRaycasts = true;
        }
    }
}
