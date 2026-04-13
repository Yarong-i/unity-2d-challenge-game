using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HpHeartsImagesUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerHealth2D player;
    [SerializeField] private Image heartPrefab;     // 방금 만든 Heart 프리팹(Image)
    [SerializeField] private Transform container;   // 보통 이 오브젝트(HeartsBar)

    [Header("Sprites")]
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;

    private readonly List<Image> hearts = new();
    private int lastMax = -1;

    private void Awake()
    {
        if (container == null) container = transform;
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
        if (player == null) return;

        // 처음 한 번 그려주기
        RebuildIfNeeded(player.MaxHP);
        UpdateUI(player.CurrentHP, player.MaxHP);
    }

    private void RebuildIfNeeded(int maxHp)
    {
        if (heartPrefab == null || container == null) return;
        if (maxHp == lastMax) return;

        // 기존 하트 삭제
        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] != null) Destroy(hearts[i].gameObject);
        }
        hearts.Clear();

        // maxHp 개수만큼 생성
        for (int i = 0; i < maxHp; i++)
        {
            Image img = Instantiate(heartPrefab, container);
            img.sprite = emptyHeart; // 기본은 빈 하트
            hearts.Add(img);
        }

        lastMax = maxHp;
    }

    private void UpdateUI(int current, int max)
    {
        RebuildIfNeeded(max);
        int clamped = Mathf.Clamp(current, 0, max);

        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] == null) continue;
            hearts[i].sprite = (i < clamped) ? fullHeart : emptyHeart;
        }
    }
}