using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageClearController : MonoBehaviour
{
    [SerializeField] private RunTimerUI runTimerUI;
    [SerializeField] private RespawnManager2D respawnManager;

    private Canvas clearCanvas;
    private TMP_Text clearTimeText;
    private bool stageCleared;

    private void Awake()
    {
        ResolveReferences();
        CreateClearUI();
        HideClearUI();
    }

    public void StageClear()
    {
        if (stageCleared)
            return;

        stageCleared = true;
        ResolveReferences();

        if (runTimerUI != null)
            runTimerUI.StopTimer();

        UpdateClearTimeText();
        ShowClearUI();
        Time.timeScale = 0f;
    }

    public void RestartStage()
    {
        Time.timeScale = 1f;
        stageCleared = false;
        ResolveReferences();

        if (respawnManager != null)
            respawnManager.Respawn();

        if (runTimerUI != null)
        {
            runTimerUI.ResetTimer();
            runTimerUI.StartTimer();
        }

        foreach (StageGoal2D goal in FindObjectsByType<StageGoal2D>(FindObjectsSortMode.None))
        {
            goal.ResetGoal();
        }

        HideClearUI();
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

    private void ResolveReferences()
    {
        if (runTimerUI == null)
            runTimerUI = FindFirstObjectByType<RunTimerUI>();

        if (respawnManager == null)
            respawnManager = FindFirstObjectByType<RespawnManager2D>();
    }

    private void CreateClearUI()
    {
        if (clearCanvas != null)
            return;

        GameObject canvasObject = new GameObject(
            "StageClearUI",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        clearCanvas = canvasObject.GetComponent<Canvas>();
        clearCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        clearCanvas.sortingOrder = 100;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = CreateUIObject("Panel", canvasObject.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject content = CreateUIObject("Content", panel.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(520f, 360f);

        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 24f;

        var titleText = CreateText("Title", content.transform, "STAGE CLEAR", 56f);
        titleText.fontStyle = FontStyles.Bold;

        clearTimeText = CreateText("ClearTime", content.transform, "Clear Time 00:00.00", 32f);

        Button restartButton = CreateButton("RestartButton", content.transform, "Restart");
        restartButton.onClick.AddListener(RestartStage);

        Button quitButton = CreateButton("QuitButton", content.transform, "Quit");
        quitButton.onClick.AddListener(QuitGame);
    }

    private GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private TMP_Text CreateText(string objectName, Transform parent, string text, float fontSize)
    {
        GameObject textObject = CreateUIObject(objectName, parent);
        var layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = fontSize + 18f;

        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private Button CreateButton(string objectName, Transform parent, string label)
    {
        GameObject buttonObject = CreateUIObject(objectName, parent);
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(260f, 58f);

        var layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 260f;
        layoutElement.preferredHeight = 58f;

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 0.85f, 0.16f, 1f);

        var button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.92f, 0.35f, 1f);
        colors.pressedColor = new Color(0.86f, 0.68f, 0.08f, 1f);
        button.colors = colors;

        TMP_Text labelText = CreateText("Label", buttonObject.transform, label, 28f);
        labelText.color = Color.black;
        labelText.fontStyle = FontStyles.Bold;

        var labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Destroy(labelText.GetComponent<LayoutElement>());

        return button;
    }

    private void UpdateClearTimeText()
    {
        if (clearTimeText == null)
            return;

        string clearTime = runTimerUI != null ? runTimerUI.FormattedElapsedTime : "00:00.00";
        clearTimeText.text = $"Clear Time {clearTime}";
    }

    private void ShowClearUI()
    {
        if (clearCanvas != null)
            clearCanvas.gameObject.SetActive(true);
    }

    private void HideClearUI()
    {
        if (clearCanvas != null)
            clearCanvas.gameObject.SetActive(false);
    }
}
