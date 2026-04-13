using System.Collections;
using UnityEngine;

public class UIShakeOnDamage : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerHealth2D player;     // HP 이벤트를 보내는 대상(플레이어)
    [SerializeField] private RectTransform target;      // 흔들릴 UI(보통 HeartsBar 자기 자신)

    [Header("Shake")]
    [SerializeField] private float duration = 0.15f;    // 흔들리는 총 시간(초)
    [SerializeField] private float magnitude = 8f;      // 흔들림 강도(픽셀 정도로 생각)

    private int lastHp = -1;            // 이전 프레임의 HP(HP 감소 여부 판단용)
    private Coroutine shakeCo;          // 현재 진행 중인 흔들림 코루틴(중복 실행 방지)
    private Vector2 originalPos;        // 흔들기 시작 전 원래 UI 위치(끝나면 복구)

    private void Awake()
    {
        // target이 비어있으면 이 스크립트가 붙은 오브젝트(=HeartsBar)의 RectTransform을 사용
        if (target == null) target = (RectTransform)transform;

        // player를 인스펙터에 안 넣었으면 씬에서 PlayerHealth2D를 하나 찾아 자동 연결
        if (player == null) player = FindFirstObjectByType<PlayerHealth2D>();

        // 현재 UI 위치를 "원래 위치"로 저장해둠
        originalPos = target.anchoredPosition;
    }

    private void OnEnable()
    {
        // 오브젝트/컴포넌트가 활성화될 때 HP 변경 이벤트 구독(등록)
        if (player != null)
            player.OnHpChanged += OnHpChanged;
    }

    private void OnDisable()
    {
        // 오브젝트/컴포넌트가 비활성화될 때 이벤트 구독 해지(중복/오류 방지)
        if (player != null)
            player.OnHpChanged -= OnHpChanged;

        // 혹시 흔들리는 중이면 멈추고 원래 위치로 복구
        StopShakeAndRestore();
    }

    private void Start()
    {
        // 시작 시점 HP를 기준값으로 저장
        // (이 값과 다음 이벤트의 current를 비교해서 HP가 줄었는지 판단)
        if (player != null)
            lastHp = player.CurrentHP;
    }

    // PlayerHealth2D에서 HP가 바뀔 때마다 호출되는 함수(이벤트 리스너)
    private void OnHpChanged(int current, int max)
    {
        // 안전장치: 아직 기준값이 없으면(=초기 상태) current를 기준으로만 저장하고 끝
        if (lastHp < 0)
        {
            lastHp = current;
            return;
        }

        // HP가 줄었을 때만(피격) 흔들기
        if (current < lastHp)
            StartShake();

        // 다음 비교를 위해 기준값 갱신
        lastHp = current;
    }

    private void StartShake()
    {
        // 이미 흔들고 있으면 기존 코루틴을 끊고 새로 시작(연속 피격 시 깔끔)
        if (shakeCo != null) StopCoroutine(shakeCo);

        shakeCo = StartCoroutine(ShakeRoutine());
    }

    // 코루틴: 프레임에 걸쳐(duration 동안) UI를 랜덤하게 흔들었다가 원래 위치로 복구
    private IEnumerator ShakeRoutine()
    {
        // 흔들기 시작할 때의 위치를 다시 저장(중간에 UI 위치가 바뀌었을 수도 있어서)
        originalPos = target.anchoredPosition;

        float t = 0f;
        while (t < duration)
        {
            // Time.unscaledDeltaTime: 타임스케일(슬로우/일시정지)에 영향을 덜 받게 UI는 보통 unscaled 사용
            t += Time.unscaledDeltaTime;

            // -magnitude ~ +magnitude 사이의 랜덤 오프셋 생성
            float dx = Random.Range(-magnitude, magnitude);
            float dy = Random.Range(-magnitude, magnitude);

            // 원래 위치 + 랜덤 오프셋 = 흔들리는 위치
            target.anchoredPosition = originalPos + new Vector2(dx, dy);

            // 다음 프레임까지 대기(프레임마다 흔들리게 해줌)
            yield return null;
        }

        // 끝나면 원래 위치로 복구
        target.anchoredPosition = originalPos;
        shakeCo = null;
    }

    private void StopShakeAndRestore()
    {
        // 흔들림이 진행 중이면 중단
        if (shakeCo != null)
        {
            StopCoroutine(shakeCo);
            shakeCo = null;
        }

        // UI 위치를 원래 위치로 되돌림
        if (target != null)
            target.anchoredPosition = originalPos;
    }
}