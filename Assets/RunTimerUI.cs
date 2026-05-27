using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunTimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private string label = "Time";

    private float elapsedTime;

    private void Awake()
    {
        EnsureTimerText();
        UpdateTimerText();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        UpdateTimerText();
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateTimerText();
    }

    private void EnsureTimerText()
    {
        if (timerText != null)
            return;

        GameObject existingText = GameObject.Find("TimerText");
        if (existingText != null)
            timerText = existingText.GetComponent<TMP_Text>();

        if (timerText != null)
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            canvas = CreateRuntimeCanvas();

        GameObject textObject = new GameObject("TimerText", typeof(RectTransform));
        textObject.transform.SetParent(canvas.transform, false);

        var rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(20f, -20f);
        rectTransform.sizeDelta = new Vector2(240f, 48f);

        timerText = textObject.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 28f;
        timerText.color = Color.white;
        timerText.alignment = TextAlignmentOptions.TopLeft;
        timerText.raycastTarget = false;
    }

    private Canvas CreateRuntimeCanvas()
    {
        GameObject canvasObject = new GameObject(
            "RuntimeUI",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        float seconds = elapsedTime - minutes * 60f;
        timerText.text = $"{label} {minutes:00}:{seconds:00.00}";
    }
}
