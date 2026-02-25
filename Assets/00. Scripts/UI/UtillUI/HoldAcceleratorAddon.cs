using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoldAcceleratorAddon : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("가속 설정")]
    public float holdDelay = 1.0f;
    public float accelerationDuration = 3.0f;
    public float minTicksPerSecond = 2f;
    public float maxTicksPerSecond = 20f;

    private Button targetButton;

    private bool isPointerDown = false;
    private float timeHeld = 0f;
    private float timeSinceLastTick = 0f;

    private void Awake()
    {
        targetButton = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 버튼이 클릭 가능한 상태일 때만 작동
        if (!targetButton.interactable) return;

        isPointerDown = true;
        timeHeld = 0f;
        timeSinceLastTick = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerDown = false; // 마우스가 버튼 밖으로 나가면 홀드 취소
    }

    private void Update()
    {
        // 눌려있지 않거나, 도중에 버튼이 비활성화되면 리턴
        if (!isPointerDown || !targetButton.interactable) return;

        timeHeld += Time.deltaTime;

        // 지정된 시간(1초) 이상 눌렀을 때만 가속 로직 실행
        if (timeHeld >= holdDelay)
        {
            // 가속도 진행률 (0.0 ~ 1.0)
            float t = Mathf.Clamp01((timeHeld - holdDelay) / accelerationDuration);

            // 현재 초당 클릭 횟수 계산
            float currentTicksPerSec = Mathf.Lerp(minTicksPerSecond, maxTicksPerSecond, t);
            float currentTickInterval = 1f / currentTicksPerSec;

            timeSinceLastTick += Time.deltaTime;

            if (timeSinceLastTick >= currentTickInterval)
            {
                // 인스펙터에 연결해둔 기존 버튼의 이벤트를 코드로 실행
                targetButton.onClick.Invoke();

                // 오차 누적 방지를 위해 정확한 간격만큼만 차감
                timeSinceLastTick -= currentTickInterval;
            }
        }
    }
}