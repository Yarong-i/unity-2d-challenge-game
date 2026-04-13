using TMPro;
using UnityEngine;

public class HpTextUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth2D player;
    [SerializeField] private TMP_Text text;

    private void Awake()
    {
        if (text == null) text = GetComponent<TMP_Text>();
        if (player == null) player = FindFirstObjectByType<PlayerHealth2D>();
    }

    private void Update()
    {
        if (player == null || text == null) return;
        text.text = $"HP: {player.CurrentHP}/{player.MaxHP}";
    }
}