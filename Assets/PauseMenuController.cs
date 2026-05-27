using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    private bool isPaused;

    private void Awake()
    {
        EnsurePausePanel();
        SetPaused(false);
    }

    private void OnEnable()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void OnDisable()
    {
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(Resume);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);

        if (isPaused)
            Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetPaused(!isPaused);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pausePanel != null)
            pausePanel.SetActive(paused);
    }

    private void EnsurePausePanel()
    {
        if (pausePanel == null)
            pausePanel = GameObject.Find("PausePanel");

        if (pausePanel != null)
        {
            if (resumeButton == null)
                resumeButton = FindButtonInPanel("ResumeButton");

            if (quitButton == null)
                quitButton = FindButtonInPanel("QuitButton");

            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            canvas = CreateRuntimeCanvas();

        EnsureEventSystem();

        pausePanel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
        pausePanel.transform.SetParent(canvas.transform, false);

        var panelRect = pausePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = pausePanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.65f);

        CreateTitle(pausePanel.transform);
        resumeButton = CreateButton(pausePanel.transform, "ResumeButton", "Resume", new Vector2(0f, 20f));
        quitButton = CreateButton(pausePanel.transform, "QuitButton", "Quit", new Vector2(0f, -60f));
    }

    private Button FindButtonInPanel(string buttonName)
    {
        Transform buttonTransform = pausePanel.transform.Find(buttonName);
        return buttonTransform == null ? null : buttonTransform.GetComponent<Button>();
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

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void CreateTitle(Transform parent)
    {
        GameObject titleObject = new GameObject("PausedText", typeof(RectTransform));
        titleObject.transform.SetParent(parent, false);

        var titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 120f);
        titleRect.sizeDelta = new Vector2(360f, 80f);

        var titleText = titleObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "PAUSED";
        titleText.fontSize = 48f;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.raycastTarget = false;
    }

    private Button CreateButton(Transform parent, string objectName, string buttonText, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(220f, 54f);

        var buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.92f);

        GameObject labelObject = new GameObject("Text", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = buttonText;
        label.fontSize = 28f;
        label.color = Color.black;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return buttonObject.GetComponent<Button>();
    }
}
