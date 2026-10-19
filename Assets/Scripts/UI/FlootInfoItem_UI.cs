using DG.Tweening;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class FlootInfoItem_UI : MonoBehaviour
{
    [SerializeField] private FloorType _floorType;
    public TMP_Text infoText;
    public Transform floorTrans;

    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private float floatDistance = 8f;
    [SerializeField] private float duration = 1f;

    private RectTransform selfRect;
    private CanvasGroup canvasGroup;
    private Tween floatTween;
    private float visualRootStartY;
    private bool hasFloorInfo;

    public float CameraDistanceSqr { get; private set; } = float.PositiveInfinity;

    private void Awake()
    {
        selfRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (visualRoot == null && transform.childCount > 0)
        {
            visualRoot = transform.GetChild(0) as RectTransform;
        }

        if (visualRoot != null)
        {
            visualRootStartY = visualRoot.localPosition.y;
        }
    }

    private void OnEnable()
    {
        ConnectMySQL.DataRefreshed += RefreshFloorInfo;
        RefreshFloorInfo();

        if (visualRoot == null) return;
        floatTween?.Kill();
        Vector3 localPosition = visualRoot.localPosition;
        localPosition.y = visualRootStartY;
        visualRoot.localPosition = localPosition;
        floatTween = visualRoot.DOLocalMoveY(visualRootStartY + floatDistance, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDisable()
    {
        ConnectMySQL.DataRefreshed -= RefreshFloorInfo;
        floatTween?.Kill();
        floatTween = null;
        if (visualRoot != null)
        {
            Vector3 localPosition = visualRoot.localPosition;
            localPosition.y = visualRootStartY;
            visualRoot.localPosition = localPosition;
        }
    }

    private void LateUpdate()
    {
        Camera activeCamera = Camera.main;
        if (!hasFloorInfo || activeCamera == null || floorTrans == null || selfRect == null)
        {
            SetVisible(false);
            CameraDistanceSqr = float.PositiveInfinity;
            return;
        }

        Vector3 viewportPoint = activeCamera.WorldToViewportPoint(floorTrans.position);
        bool isVisible = viewportPoint.z > 0f;
        SetVisible(isVisible);
        if (!isVisible)
        {
            CameraDistanceSqr = float.PositiveInfinity;
            return;
        }

        selfRect.position = activeCamera.WorldToScreenPoint(floorTrans.position);
        CameraDistanceSqr = (activeCamera.transform.position - floorTrans.position).sqrMagnitude;
    }

    private void RefreshFloorInfo()
    {
        var floorInfo = ConnectMySQL.instance != null
            ? ConnectMySQL.instance.dataSO?.floorInfo
            : null;
        string value = FlootInfoItem.GetFloorValue(floorInfo, _floorType);
        hasFloorInfo = infoText != null && !string.IsNullOrWhiteSpace(value);
        if (!hasFloorInfo)
        {
            if (infoText != null)
            {
                infoText.text = string.Empty;
            }
            SetVisible(false);
            return;
        }

        string[] cnAndEn = value.Split(',');
        infoText.text = cnAndEn.Length >= 2
            ? $"{cnAndEn[0].Trim()}\n{cnAndEn[1].Trim()}"
            : value.Trim();
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}
