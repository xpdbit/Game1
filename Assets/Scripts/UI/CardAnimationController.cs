using System.Collections;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// Card animation controller using Unity coroutines.
    /// Handles card flip/reveal animations for the card panel.
    /// </summary>
    public class CardAnimationController : MonoBehaviour
    {
        [Header("Animation Settings")]
        public float cardFlipDuration = 0.4f;
        public float cardRevealDelay = 0.15f;
        public float glowPulseDuration = 0.6f;

        /// <summary>
        /// Play a card reveal animation on a RectTransform
        /// </summary>
        public IEnumerator PlayCardReveal(RectTransform cardRect, CardRarity rarity, System.Action onComplete = null)
        {
            if (cardRect == null) yield break;

            // Step 1: Start small and scale up with bounce effect
            cardRect.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < cardFlipDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / cardFlipDuration);
                // Ease out bounce effect
                float scale = 1f - Mathf.Pow(1f - t, 3f);
                cardRect.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }
            cardRect.localScale = Vector3.one;

            // Step 2: Pulse glow effect (simulate rarity glow)
            var image = cardRect.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                Color originalColor = image.color;
                Color glowColor = GetRarityGlowColor(rarity);

                float pulseElapsed = 0f;
                while (pulseElapsed < glowPulseDuration)
                {
                    pulseElapsed += Time.deltaTime;
                    float t = Mathf.PingPong(pulseElapsed * 2f, 1f);
                    image.color = Color.Lerp(originalColor, glowColor, t * 0.5f);
                    yield return null;
                }
                image.color = originalColor;
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Play sequential reveals for multiple cards (fan-out effect).
        /// Note: Call via UICardPanel.StartCoroutine(controller.PlayMultiCardReveal(...))
        /// since UICardPanel is a MonoBehaviour that can start coroutines.
        /// </summary>
        public IEnumerator PlayMultiCardReveal(RectTransform[] cardRects, CardRarity[] rarities, MonoBehaviour starter, System.Action onComplete = null)
        {
            if (cardRects == null || cardRects.Length == 0 || starter == null) yield break;

            for (int i = 0; i < cardRects.Length; i++)
            {
                if (i > 0) yield return new WaitForSeconds(cardRevealDelay);

                var index = i; // capture for closure
                starter.StartCoroutine(PlayCardReveal(cardRects[index], rarities[index], null));
            }

            yield return new WaitForSeconds(cardFlipDuration + cardRevealDelay);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Get glow color for card rarity
        /// </summary>
        private Color GetRarityGlowColor(CardRarity rarity)
        {
            var c = GetRarityColor(rarity);
            c.a = 0.3f;
            return c;
        }

        /// <summary>
        /// Get base color for card rarity
        /// </summary>
        private Color GetRarityColor(CardRarity rarity) => rarity switch
        {
            CardRarity.N => new Color(0.6f, 0.6f, 0.6f),
            CardRarity.R => new Color(0.2f, 0.6f, 1.0f),
            CardRarity.SR => new Color(0.6f, 0.2f, 1.0f),
            CardRarity.SSR => new Color(1.0f, 0.7f, 0.0f),
            CardRarity.UR => new Color(1.0f, 0.3f, 0.0f),
            CardRarity.GR => new Color(1.0f, 0.0f, 0.5f),
            _ => Color.white
        };
    }
}