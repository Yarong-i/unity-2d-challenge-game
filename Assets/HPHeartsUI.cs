using TMPro;
using UnityEngine;

public class HpHeartsUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth2D player;
    [SerializeField] private TMP_Text text;

    [Header("Hearts")]
    [SerializeField] private string fullHeart = "♥";
    [SerializeField] private string emptyHeart = "♡";
    [SerializeField] private bool showNumbers = true; // (3/5) 같이 표시

    private void Awake()
    {
        if (text == null) text = GetComponent<TMP_Text>();
        if (player == null) player = FindFirstObjectByType<PlayerHealth2D>();
    }

    private void OnEnable()
    {
        if (player != null)
            player.OnHpChanged += UpdateUI;
    }

    private void OnDisable()
    {
        if (player != null)
            player.OnHpChanged -= UpdateUI;
    }

    private void Start()
    {
        // 처음 표시(이벤트는 "변할 때"만 오니까 시작 시 한번 그려줘야 함)
        if (player != null)
            UpdateUI(player.CurrentHP, player.MaxHP);
    }

    private void UpdateUI(int current, int max)
    {
        if (text == null) return;

        // ♥♥♥♡♡ 만들기
        int clamped = Mathf.Clamp(current, 0, max);
        System.Text.StringBuilder sb = new System.Text.StringBuilder(max * 2 + 10);

        for (int i = 0; i < max; i++)
            sb.Append(i < clamped ? fullHeart : emptyHeart);

        if (showNumbers)
            sb.Append($" ({clamped}/{max})");

        text.text = sb.ToString();
    }
}
