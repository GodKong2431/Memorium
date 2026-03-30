using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SynergyUI : MonoBehaviour
{
    private const int DustItemId = 3450001;

    public const string atkText = "공격력";
    public const string atkSPDText = "공격 속도";
    public const string defText = "저항력";
    public const string moveSPDText = "이동 속도";
    public const string glodGainText = "골드 획득량";
    public const string hpText = "체력";
    public const string normalDmgText = "일반 데미지";
    public const string bossDmgText = "보스 데미지";
    public const string nullText = "비어 있음";
    
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] List<SynergyItem> synergyItems = new List<SynergyItem>();
    [SerializeField] private TextMeshProUGUI currentSynergyText;
    [SerializeField] private SynergyItem _currentItem;
    [SerializeField] private RetryUI retryUI;
    [SerializeField] private Button synergyChangeButton;
    [SerializeField] private TextMeshProUGUI currencyText;
    
    [SerializeField] private CanvasGroup BingoBoard;
    
    [SerializeField] private Toggle dismantleToggle;

    [SerializeField] private bool _isDismantleMode;    
    private bool isSynergyGachaRunning;
    private bool isBingoGachaRunning;
    private readonly HashSet<ParticleSystem> activeDismantleEffects = new HashSet<ParticleSystem>();
    private int pendingDismantleDustAmount;
    
    [SerializeField] private TextMeshProUGUI dismantleText;
    
    [SerializeField] private RectTransform dustTransform;

    [Header("Dismantle Effect")]
    [SerializeField] private int dustAmountPerEffect = 20;
    [SerializeField, Min(1)] private int maxDismantleEffectCount = 20;
    [SerializeField] private float effectSpawnInterval = 0.02f;
    [FormerlySerializedAs("initialFlightSpeed")]
    [SerializeField, Min(0f)] private float initialFlightSpeedMin = 850f;
    [SerializeField, Min(0f)] private float initialFlightSpeedMax = 850f;
    [FormerlySerializedAs("flightAcceleration")]
    [SerializeField, Min(0f)] private float flightAccelerationMin = 1500f;
    [SerializeField, Min(0f)] private float flightAccelerationMax = 1500f;
    [SerializeField] private float curveHeight = 180f;
    [SerializeField] private float curveWidth = 140f;
    [SerializeField] private float pathNoiseAmplitude = 28f;
    [SerializeField] private float pathNoiseFrequency = 5f;

    [Header("Dismantle Spawn Range")]
    [SerializeField, Min(0f)] private float spawnRangeX = 28f;
    [SerializeField, Min(0f)] private float spawnRangeY = 14f;
    [SerializeField, Min(0f)] private float spawnClampPadding = 4f;
    [SerializeField] private RectTransform spawnClampRect;

    private readonly Vector3[] spawnClampCorners = new Vector3[4];
    
    public static event Action<int> OnSynergyGachaButton;
    
    public SynergyItem currentItem
    {
        get{return _currentItem;}
        set
        {
            if(_currentItem != null && _currentItem == value)
            {
                currentItem = null;
                return;
            }
            
            _currentItem = value;
            SynergyManager.Instance.item = _currentItem;
            currentSynergyText.text = _currentItem?
                                        $"{GetSynergyText(_currentItem.synergyData.synergyStat)} {_currentItem.synergyData.statUp1 * 100}% 증가"
                                        : _isDismantleMode? "분해 모드" : $"{GetSynergyText(SynergyStat.None)}";
        }
    }
    
    
    void Start()
    {
        EnsureDismantleTarget();
        SynergyManager.Instance.retryUI = retryUI;
        currencyText.text = $"{InventoryManager.Instance.GetItemAmount(3450001).ToFloat()}";
        synergyChangeButton.onClick.AddListener(()=>SynergyManager.Instance.TestSyer());
        dismantleToggle.onValueChanged.AddListener(_ => SetButton(_));
        RefreshDismantleToggleInteractable();
        
        SetSynergy();
    }
    
    public void SetButton(bool toggle)
    {
        synergyChangeButton.onClick.RemoveAllListeners();
        if (!toggle)
        {
            _isDismantleMode = false;
            dismantleText.text = "분해";
            currentItem = null;
            synergyChangeButton.onClick.AddListener(()=>SynergyManager.Instance.TestSyer());
        }
        
        else
        {
            _isDismantleMode = true;
            dismantleText.text = "취소";
            currentItem = null;
            synergyChangeButton.onClick.AddListener(()=>StartSynergyDist());
        }
        
    }

    void OnEnable()
    {
        BingoBoard.blocksRaycasts = false;
        InventoryManager.Instance.OnItemAmountChanged += UpdateDustCurreny;
        SynergyManager.OnSynergyGachaRunningChanged += HandleSynergyGachaRunningChanged;
        BingoBoardManager.OnBingoGachaRunningChanged += HandleBingoGachaRunningChanged;
        isSynergyGachaRunning = SynergyManager.Instance != null && SynergyManager.Instance.IsSynergyGachaRunning;
        isBingoGachaRunning = BingoBoardManager.Instance != null && BingoBoardManager.Instance.IsBingoGachaRunning;
        RefreshDismantleToggleInteractable();
    }


    void OnDisable()
    {
        SynergyManager.OnSynergyGachaRunningChanged -= HandleSynergyGachaRunningChanged;
        BingoBoardManager.OnBingoGachaRunningChanged -= HandleBingoGachaRunningChanged;
        FlushDismantleEffectsImmediately();

        if (InventoryManager.Instance != null)
        {
            BingoBoard.blocksRaycasts = true;
            InventoryManager.Instance.OnItemAmountChanged -= UpdateDustCurreny;
            currentItem = null;
            dismantleToggle.isOn = false;
            gameObject.SetActive(false);
        }

    }
    
    public void StartSynergyDist()
    {
        foreach(var synergy in synergyItems)
        {
            synergy.DismantleSynergy(this);
        }
    }

    public void PlaySynergyDismantleDust(Transform source, int totalDustAmount)
    {
        if (totalDustAmount <= 0)
            return;

        ReservePendingDust(totalDustAmount);
        EnsureDismantleTarget();
        StartCoroutine(PlaySynergyDismantleDustRoutine(source, totalDustAmount));
    }

    private IEnumerator PlaySynergyDismantleDustRoutine(Transform source, int totalDustAmount)
    {
        int unitAmount = Mathf.Max(1, dustAmountPerEffect);
        int effectCount = Mathf.CeilToInt(totalDustAmount / (float)unitAmount);
        int remainingDust = totalDustAmount;
        Transform sourceParent = source != null ? source : transform;

        for (int i = 0; i < effectCount; i++)
        {
            int gainAmount = Mathf.Min(unitAmount, remainingDust);
            remainingDust -= gainAmount;

            if (gainAmount <= 0)
                break;

            if (activeDismantleEffects.Count >= Mathf.Max(1, maxDismantleEffectCount))
            {
                GrantReservedDust(gainAmount);
                continue;
            }

            ParticleSystem effectInstance = SpawnDismantleEffect(sourceParent, out Vector3 startLocalPosition);
            if (effectInstance == null)
            {
                GrantReservedDust(gainAmount);
            }
            else
            {
                Vector3 targetLocalPosition = ResolveDismantleTargetLocalPosition(effectInstance.transform.parent);
                StartCoroutine(FlyDismantleEffect(effectInstance, startLocalPosition, targetLocalPosition, gainAmount));
            }

            if (effectSpawnInterval > 0f)
                yield return new WaitForSecondsRealtime(effectSpawnInterval);
        }
    }
    public static string GetSynergyText(SynergyStat synergyStat)
    {
        return synergyStat switch
        {
            SynergyStat.ATK => atkText,
            SynergyStat.ATK_SPEED => atkSPDText,
            SynergyStat.DEF => defText,
            SynergyStat.BOSS_DMG => bossDmgText,
            SynergyStat.NORMAL_DMG => normalDmgText,
            SynergyStat.HP => hpText,
            SynergyStat.GOLD_GAIN => glodGainText,
            SynergyStat.MOVE_SPEED => moveSPDText,
            _ => nullText
        };
    }

    public void SetSynergy()
    {
        ToggleGroup toggleGroup = rectTransform != null ? rectTransform.GetComponent<ToggleGroup>() : null;

        foreach(var synergyStat in SynergyManager.Instance.synergyDataSo.SynergyDataDict)
        {
            foreach(var synergy in synergyStat.Value)
            {
                var item = Instantiate(SynergyManager.Instance.synergyDataSo.SynergyItems[synergy.Key], rectTransform);
                item.LoadSynergy(synergy.Key,synergyStat.Key);
                item.SetButton(this, false);
                dismantleToggle.onValueChanged.AddListener(_ => item.SetButton(this,_));
                
                
                // Toggle itemToggle = item.GetComponent<Toggle>();
                // if (itemToggle == null)
                // {
                //     itemToggle = item.GetComponentInChildren<Toggle>(true);
                // }

                // if (itemToggle != null && toggleGroup != null)
                // {
                //     itemToggle.group = toggleGroup;
                // }

                synergyItems.Add(item);
            }
        }
    }
    
    public void UpdateDustCurreny(InventoryItemContext item, BigDouble amount)
    {
        if (item.ItemId != 3450001)
            return;
            
        currencyText.text = $"{InventoryManager.Instance.GetItemAmount(item.ItemId).ToFloat()}";
    }

    public void OnClickGachaButton(int index)
    {
        OnSynergyGachaButton?.Invoke(index);
    }

    private void HandleSynergyGachaRunningChanged(bool isRunning)
    {
        isSynergyGachaRunning = isRunning;
        RefreshDismantleToggleInteractable();
    }

    private void HandleBingoGachaRunningChanged(bool isRunning)
    {
        isBingoGachaRunning = isRunning;
        RefreshDismantleToggleInteractable();
    }

    private void RefreshDismantleToggleInteractable()
    {
        bool canInteract = !isSynergyGachaRunning && !isBingoGachaRunning;
        if (dismantleToggle != null)
            dismantleToggle.interactable = canInteract;

        if (synergyChangeButton != null)
            synergyChangeButton.interactable = canInteract;
    }

    private ParticleSystem SpawnDismantleEffect(Transform source, out Vector3 startLocalPosition)
    {
        startLocalPosition = Vector3.zero;

        if (BingoEffectManager.Instance == null)
            return null;

        Transform sourceTransform = source != null ? source : transform;
        Transform parent = ResolveEffectSpawnParent(sourceTransform);
        ParticleSystem effectInstance = BingoEffectManager.Instance.PlaySynergyDismantleEffectManual(parent);
        if (effectInstance == null)
            return null;

        Transform effectTransform = effectInstance.transform;
        if (effectTransform != null)
        {
            Vector3 baseLocalPosition = parent != null
                ? parent.InverseTransformPoint(sourceTransform.position)
                : Vector3.zero;
            baseLocalPosition.z = 0f;

            Vector3 spawnLocalPosition = baseLocalPosition + SampleSpawnOffset(parent);
            spawnLocalPosition = ClampSpawnLocalPosition(spawnLocalPosition, parent);
            effectTransform.localPosition = spawnLocalPosition;
            startLocalPosition = spawnLocalPosition;
        }

        effectInstance.transform.SetAsLastSibling();
        activeDismantleEffects.Add(effectInstance);
        return effectInstance;
    }

    private Transform ResolveEffectSpawnParent(Transform sourceTransform)
    {
        if (spawnClampRect != null)
            return spawnClampRect;

        if (rectTransform != null)
            return rectTransform;

        return sourceTransform != null ? sourceTransform : transform;
    }

    private Vector3 ResolveDismantleTargetLocalPosition(Transform localSpace)
    {
        if (localSpace == null)
            return Vector3.zero;

        Vector3 targetWorldPosition = dustTransform != null ? dustTransform.position : transform.position;
        Vector3 targetLocalPosition = localSpace.InverseTransformPoint(targetWorldPosition);
        targetLocalPosition.z = 0f;
        return targetLocalPosition;
    }

    private IEnumerator FlyDismantleEffect(
        ParticleSystem effectInstance,
        Vector3 startLocalPosition,
        Vector3 targetLocalPosition,
        int gainAmount)
    {
        if (effectInstance == null)
            yield break;

        Transform effectTransform = effectInstance.transform;
        if (effectTransform == null)
            yield break;

        float fixedZ = startLocalPosition.z;

        float totalDistance = Mathf.Max(0.01f, Vector3.Distance(startLocalPosition, targetLocalPosition));
        float traveledDistance = 0f;
        float currentSpeed = SampleInRange(initialFlightSpeedMin, initialFlightSpeedMax);
        float acceleration = SampleInRange(flightAccelerationMin, flightAccelerationMax);
        float elapsed = 0f;
        const float MaxFlightSeconds = 5f;
        Vector3 direction = (targetLocalPosition - startLocalPosition).normalized;
        Vector3 perpendicular = direction.sqrMagnitude <= 0f ? Vector3.right : Vector3.Cross(direction, Vector3.forward).normalized;
        // Inspector 값이 실제로 경로 모양을 제어하도록 거리 기반 최소 강제값을 제거.
        float dynamicCurveHeight = Mathf.Max(0f, curveHeight);
        float dynamicCurveWidth = Mathf.Max(0f, curveWidth);
        float dynamicNoiseAmplitude = Mathf.Min(Mathf.Max(0f, pathNoiseAmplitude), totalDistance * 0.35f);

        float sideSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        float launchSide = sideSign * UnityEngine.Random.Range(dynamicCurveWidth * 0.45f, dynamicCurveWidth * 1.1f);
        float returnSide = -sideSign * UnityEngine.Random.Range(dynamicCurveWidth * 0.1f, dynamicCurveWidth * 0.65f);

        Vector3 controlPointA = startLocalPosition +
                                (direction * UnityEngine.Random.Range(totalDistance * 0.1f, totalDistance * 0.24f)) +
                                (Vector3.up * UnityEngine.Random.Range(dynamicCurveHeight * 0.7f, dynamicCurveHeight * 1.25f)) +
                                (perpendicular * launchSide);
        Vector3 controlPointB = targetLocalPosition -
                                (direction * UnityEngine.Random.Range(totalDistance * 0.16f, totalDistance * 0.35f)) +
                                (Vector3.up * UnityEngine.Random.Range(dynamicCurveHeight * 0.3f, dynamicCurveHeight * 0.9f)) +
                                (perpendicular * returnSide);

        float waveFrequency = UnityEngine.Random.Range(1.8f, 3.2f);
        float waveSeed = UnityEngine.Random.Range(0f, 1000f);
        float noiseSeed = UnityEngine.Random.Range(0f, 2000f);

        while (traveledDistance < totalDistance && elapsed < MaxFlightSeconds && effectTransform != null)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;

            float deltaDistance = (currentSpeed * dt) + (0.5f * acceleration * dt * dt);
            if (deltaDistance > 0f)
                traveledDistance = Mathf.Min(totalDistance, traveledDistance + deltaDistance);

            currentSpeed = Mathf.Max(0f, currentSpeed + (acceleration * dt));

            float t = Mathf.Clamp01(traveledDistance / totalDistance);
            float curveT = t * t * (3f - (2f * t));

            Vector3 curveWorldPoint = EvaluateCubicBezier(startLocalPosition, controlPointA, controlPointB, targetLocalPosition, curveT);
            float noiseWeight = 4f * t * (1f - t);
            float sideWave = Mathf.Sin((t * waveFrequency + waveSeed) * Mathf.PI * 2f) * (dynamicNoiseAmplitude * noiseWeight);
            float noiseX = (Mathf.PerlinNoise(noiseSeed, t * pathNoiseFrequency) - 0.5f) * 2f;
            float noiseY = (Mathf.PerlinNoise(noiseSeed + 73.1f, t * pathNoiseFrequency) - 0.5f) * 2f;
            curveWorldPoint += new Vector3(noiseX, noiseY, 0f) * (dynamicNoiseAmplitude * noiseWeight * 0.45f);
            curveWorldPoint += perpendicular * sideWave;
            curveWorldPoint.z = fixedZ;

            effectTransform.localPosition = curveWorldPoint;
            yield return null;
        }

        if (effectTransform != null)
        {
            Vector3 finalPosition = targetLocalPosition;
            finalPosition.z = fixedZ;
            effectTransform.localPosition = finalPosition;
        }

        GrantReservedDust(gainAmount);

        if (effectInstance != null)
        {
            activeDismantleEffects.Remove(effectInstance);
            ReturnDismantleEffectToPool(effectInstance);
        }
    }

    private static float SampleInRange(float minValue, float maxValue)
    {
        float min = Mathf.Min(minValue, maxValue);
        float max = Mathf.Max(minValue, maxValue);
        return min == max ? min : UnityEngine.Random.Range(min, max);
    }

    private static Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float oneMinusT = 1f - t;
        return (oneMinusT * oneMinusT * oneMinusT * p0) +
               (3f * oneMinusT * oneMinusT * t * p1) +
               (3f * oneMinusT * t * t * p2) +
               (t * t * t * p3);
    }

    private Vector3 SampleSpawnOffset(Transform parent)
    {
        float halfRangeX = spawnRangeX;
        float halfRangeY = spawnRangeY;

        if (parent is RectTransform parentRect)
        {
            Rect rect = parentRect.rect;
            if (rect.width > 0f)
                halfRangeX = Mathf.Min(halfRangeX, rect.width * 0.45f);

            if (rect.height > 0f)
                halfRangeY = Mathf.Min(halfRangeY, rect.height * 0.45f);
        }

        return new Vector3(
            UnityEngine.Random.Range(-halfRangeX, halfRangeX),
            UnityEngine.Random.Range(-halfRangeY, halfRangeY),
            0f);
    }

    private Vector3 ClampSpawnLocalPosition(Vector3 localPosition, Transform localSpace)
    {
        if (localSpace == null)
            return localPosition;

        RectTransform clampRect = ResolveSpawnClampRect(localSpace);
        if (clampRect == null)
            return localPosition;

        clampRect.GetWorldCorners(spawnClampCorners);
        float minX = Mathf.Min(spawnClampCorners[0].x, spawnClampCorners[2].x) + spawnClampPadding;
        float maxX = Mathf.Max(spawnClampCorners[0].x, spawnClampCorners[2].x) - spawnClampPadding;
        float minY = Mathf.Min(spawnClampCorners[0].y, spawnClampCorners[2].y) + spawnClampPadding;
        float maxY = Mathf.Max(spawnClampCorners[0].y, spawnClampCorners[2].y) - spawnClampPadding;

        if (minX > maxX)
        {
            float centerX = (minX + maxX) * 0.5f;
            minX = centerX;
            maxX = centerX;
        }

        if (minY > maxY)
        {
            float centerY = (minY + maxY) * 0.5f;
            minY = centerY;
            maxY = centerY;
        }

        Vector3 worldPosition = localSpace.TransformPoint(localPosition);
        worldPosition.x = Mathf.Clamp(worldPosition.x, minX, maxX);
        worldPosition.y = Mathf.Clamp(worldPosition.y, minY, maxY);

        Vector3 clampedLocalPosition = localSpace.InverseTransformPoint(worldPosition);
        clampedLocalPosition.z = localPosition.z;
        return clampedLocalPosition;
    }

    private RectTransform ResolveSpawnClampRect(Transform localSpace)
    {
        if (spawnClampRect != null)
            return spawnClampRect;

        if (localSpace is RectTransform localRect)
            return localRect;

        return rectTransform;
    }

    private void EnsureDismantleTarget()
    {
        if (dustTransform != null)
            return;

        if (currencyText != null && currencyText.transform is RectTransform currencyRect)
        {
            dustTransform = currencyRect;
            return;
        }

        dustTransform = rectTransform != null ? rectTransform : transform as RectTransform;
    }

    private void ReservePendingDust(int amount)
    {
        if (amount <= 0)
            return;

        pendingDismantleDustAmount += amount;
    }

    private void GrantReservedDust(int amount)
    {
        if (amount <= 0)
            return;

        int grantAmount = Mathf.Min(amount, pendingDismantleDustAmount);
        if (grantAmount <= 0)
            return;

        pendingDismantleDustAmount -= grantAmount;
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(DustItemId, grantAmount);
    }

    private void FlushDismantleEffectsImmediately()
    {
        StopAllCoroutines();

        int remainingDust = pendingDismantleDustAmount;
        pendingDismantleDustAmount = 0;
        if (remainingDust > 0 && InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(DustItemId, remainingDust);

        if (activeDismantleEffects.Count <= 0)
            return;

        foreach (ParticleSystem effect in activeDismantleEffects)
        {
            if (effect == null)
                continue;

            ReturnDismantleEffectToPool(effect);
        }

        activeDismantleEffects.Clear();
    }

    private static void ReturnDismantleEffectToPool(ParticleSystem effectInstance)
    {
        if (effectInstance == null)
            return;

        if (BingoEffectManager.Instance != null)
            BingoEffectManager.Instance.ReturnSynergyDismantleEffect(effectInstance);
        else
            Destroy(effectInstance.gameObject);
    }
    
}
