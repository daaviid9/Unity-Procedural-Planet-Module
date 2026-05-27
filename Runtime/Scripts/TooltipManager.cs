using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [SerializeField] GameObject tooltipPanel;
    [SerializeField] TMP_Text tooltipText;

    [SerializeField] Vector2 mouseOffset = new(30f, -30f);
    [SerializeField] float hoverDelay = 0.5f;
    [SerializeField] float fadeDuration = 0.15f;

    CanvasGroup canvasGroup;
    Coroutine routine;
    bool visible;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        tooltipPanel.SetActive(false);
    }

    public void Show(string message)
    {
        tooltipText.text = message;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ShowRoutine());
    }

    public void Hide()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(HideRoutine());
    }

    IEnumerator ShowRoutine()
    {
        yield return new WaitForSeconds(hoverDelay);

        tooltipPanel.SetActive(true);
        visible = true;

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    IEnumerator HideRoutine()
    {
        visible = false;

        float start = canvasGroup.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        tooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (!visible || Mouse.current == null)
            return;

        tooltipPanel.transform.position =
            Mouse.current.position.ReadValue() + mouseOffset;
    }
}