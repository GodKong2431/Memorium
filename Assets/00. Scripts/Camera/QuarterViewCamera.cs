using UnityEngine;
using UnityEngine.SceneManagement;

public class QuarterViewCamera : MonoBehaviour
{
    private static QuarterViewCamera instance;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool findOnStart = true;

    [Header("View")]
    [SerializeField] private float distance = 14f;
    [SerializeField] private Vector2 angle = new Vector2(57f, 45f);
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, 0f);

    [Header("Bottom Sheet")]
    [SerializeField] private BottomPanelController bottomPanelController;
    [SerializeField] private Vector3 sheetOpenLocalPositionOffset = new Vector3(0f, -2f, 0f);

    [Header("Smoothing")]
    [SerializeField] private float moveSmooth = 10f;
    [SerializeField] private float turnSmooth = 12f;

    private bool shouldSnapToResolvedTarget = true;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GameEventManager.OnPlayerSpawned += OnPlayerSpawned;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEventManager.OnPlayerSpawned -= OnPlayerSpawned;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Start()
    {
        if (findOnStart)
            FindTarget();

        shouldSnapToResolvedTarget = false;
        Snap();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            if (!FindTarget())
                return;

            if (shouldSnapToResolvedTarget)
            {
                shouldSnapToResolvedTarget = false;
                Snap();
                return;
            }
        }

        UpdateCamera(Time.deltaTime);
    }

    public bool FindTarget()
    {
        if (target != null)
            return true;

        if (ScenePlayerLocator.TryGetPlayerTransform(out Transform playerTransform))
        {
            target = playerTransform;
            return true;
        }

        GameObject go = GameObject.FindGameObjectWithTag(targetTag);
        if (go == null)
            return false;

        target = go.transform;
        return true;
    }

    public void Snap()
    {
        if (target == null && !FindTarget())
            return;

        Vector3 pivotPos = GetPivotPos();
        Quaternion targetRot = GetViewRot();
        Vector3 camPos = GetCamPos(pivotPos, targetRot);

        transform.SetPositionAndRotation(camPos, targetRot);
    }

    private void UpdateCamera(float deltaTime)
    {
        Vector3 pivotPos = GetPivotPos();
        Quaternion targetRot = GetViewRot();
        Vector3 camPos = GetCamPos(pivotPos, targetRot);

        float moveT = 1f - Mathf.Exp(-Mathf.Max(0f, moveSmooth) * deltaTime);
        float turnT = 1f - Mathf.Exp(-Mathf.Max(0f, turnSmooth) * deltaTime);

        transform.position = Vector3.Lerp(transform.position, camPos, moveT);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnT);
    }

    private Vector3 GetPivotPos()
    {
        return target.position + offset;
    }

    private Quaternion GetViewRot()
    {
        return Quaternion.Euler(angle.x, angle.y, 0f);
    }

    private Vector3 GetCamPos(Vector3 pivotPos, Quaternion rot)
    {
        Vector3 basePosition = pivotPos + rot * (Vector3.back * Mathf.Max(0f, distance));
        if (!IsBottomSheetOpen())
            return basePosition;

        return basePosition + (rot * sheetOpenLocalPositionOffset);
    }

    private bool IsBottomSheetOpen()
    {
        if (bottomPanelController == null)
            bottomPanelController = Object.FindFirstObjectByType<BottomPanelController>();

        return bottomPanelController != null && bottomPanelController.IsSheetOpen;
    }

    private void OnPlayerSpawned(Transform playerTransform)
    {
        if (playerTransform == null)
            return;

        target = playerTransform;
        shouldSnapToResolvedTarget = false;
        Snap();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        target = null;
        bottomPanelController = null;
        shouldSnapToResolvedTarget = true;
    }
}
