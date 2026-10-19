using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityTemplateProjects;

public class CameraRay : MonoBehaviour
{
    public Camera mainCamera; // 当前的主摄像机
    public float moveDuration = 1.0f; // 摄像机移动的持续时间
    private float lastClickTime; // 上次点击的时间
    private const float doubleClickThreshold = 0.3f; // 双击的时间间隔
    private bool isMoving = false; // 判断摄像机是否正在移动
    private Camera activeBuildingCamera;
    private Vector3 mainCameraStartPosition;
    private Quaternion mainCameraStartRotation;
    private CameraLensState mainCameraStartLens;
    private CinemachineBrain cinemachineBrain;
    private SimpleCameraController mainCameraController;
    private bool mainCameraControllerWasEnabled;
    private GameObject transitionSourceObject;
    private GameObject transitionDestinationObject;

    [Header("Cinemachine 镜头过渡")]
    [SerializeField] private CinemachineBlendDefinition.Style blendStyle =
        CinemachineBlendDefinition.Style.EaseInOut;
    [SerializeField] private int transitionPriority = 1000;

    [Header("场景中预设的返回界面")]
    [SerializeField] private GameObject returnCanvasObject;
    [SerializeField] private Button returnMainCameraButton;
    private GameObject returnDisplayObject;

    [Header("主视角快捷入口")]
    [SerializeField] private Button systemTitleButton;
    private const string SystemTitleText = "HoYang智能制造运营平台";

    public bool IsInsideBuilding => activeBuildingCamera != null;

    private struct CameraLensState
    {
        public bool orthographic;
        public float fieldOfView;
        public float orthographicSize;
        public float nearClipPlane;
        public float farClipPlane;
        public Vector2 lensShift;
        public Camera.GateFitMode gateFit;
    }

    // 手动设置楼层与对应的摄像机和目标位置
    [System.Serializable]
    public struct BuildingData
    {
        public string tagName;      // 楼层的标签
        public Camera camera;       // 专属摄像机
        public Transform targetPos; // 摄像机的目标位置
    }

    public List<BuildingData> buildings = new List<BuildingData>();

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
        }

        if (mainCamera != null)
        {
            mainCameraController = mainCamera.GetComponent<SimpleCameraController>();
            CaptureMainCameraDefaultView();
            mainCameraStartLens = CaptureLens(mainCamera);
            mainCameraControllerWasEnabled =
                mainCameraController != null && mainCameraController.enabled;
            EnsureCinemachineBrain();
        }

        InitializeSceneReturnUI();
        InitializeSystemTitleButton();
    }

    void Update()
    {
        if (mainCamera == null || IsInsideBuilding)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && !isMoving)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 1000f))
            {
                if (hitInfo.collider.tag == "图标")
                {
                    Debug.Log("点击到了图标");
                    hitInfo.transform.GetComponent<SceneDetailsInformation>().SwitchImagePanel(); //转换显示
                }
            }
        }

        if (Input.GetMouseButtonDown(0) && !isMoving && SceneManagerSelf.Instance.isActivehighlightTrigger)
        {
            // 检测双击
            if (Time.time - lastClickTime <= doubleClickThreshold)
            {
                HandleDoubleClick();

            }

            lastClickTime = Time.time;
        }
    }

    private void HandleDoubleClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 1000f))
        {
            string tag = hitInfo.collider.tag; // 获取点击物体的 Tag
            foreach (var building in buildings)
            {
                if (building.tagName == tag)
                {
                    if (building.camera == null)
                    {
                        Debug.LogError($"{tag} 没有配置楼内摄像机，无法进入。");
                        return;
                    }

                    Debug.Log($"双击到了 {tag}");
                    if (SceneManagerSelf.Instance != null)
                    {
                        // 双击确认进入楼层时立即关闭高亮，避免过渡期间及楼内物体继续响应高亮。
                        SceneManagerSelf.Instance.SetHighlightTriggerActive(false);
                        SceneManagerSelf.Instance.PlayAllSequences(false);//在进入楼层摄影机之前，先吧所以的UI隐藏
                    }
                    StartCoroutine(MoveCameraToBuilding(building));
                    return;
                }
            }
        }
    }


    private IEnumerator MoveCameraToBuilding(BuildingData building)
    {
        if (mainCamera == null || building.camera == null)
        {
            yield break;
        }

        isMoving = true;

        Vector3 startPosition = mainCamera.transform.position;

        DisableOtherBuildingCameras(building.camera);
        AlignBuildingCameraWithEntryLine(building, startPosition);

        // 主相机控制器与 Cinemachine 会同时写 Transform，过渡期间必须暂停手动控制。
        if (mainCameraController != null)
        {
            mainCameraControllerWasEnabled = mainCameraController.enabled;
            mainCameraController.enabled = false;
        }

        EnsureCinemachineBrain();
        if (cinemachineBrain == null)
        {
            Debug.LogError("主相机无法创建 CinemachineBrain，已取消进入楼层。");
            RestoreMainCameraController();
            if (SceneManagerSelf.Instance != null)
            {
                SceneManagerSelf.Instance.SetHighlightTriggerActive(true);
            }
            isMoving = false;
            yield break;
        }

        CleanupTransitionCameras();
        CinemachineVirtualCamera sourceCamera = CreateTransitionCamera(
            "CM_FloorTransition_Source",
            mainCamera.transform,
            mainCamera,
            transitionPriority);
        transitionSourceObject = sourceCamera.gameObject;

        // 楼层实体相机已经被放到“主相机 -> 楼层 target”的直线上。
        // Cinemachine 因此沿这条线直接推进，不再混合到场景中原先的任意观察角度。
        CinemachineVirtualCamera destinationCamera = CreateTransitionCamera(
            $"CM_FloorTransition_{building.camera.name}",
            building.camera.transform,
            building.camera,
            transitionPriority - 1);
        transitionDestinationObject = destinationCamera.gameObject;

        cinemachineBrain.m_UpdateMethod = CinemachineBrain.UpdateMethod.SmartUpdate;
        cinemachineBrain.m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.LateUpdate;
        cinemachineBrain.m_DefaultBlend = new CinemachineBlendDefinition(
            blendStyle,
            Mathf.Max(0f, moveDuration));
        cinemachineBrain.enabled = true;

        // 先让 Brain 接管与当前画面完全一致的起点相机，再提升终点优先级触发混合。
        yield return null;
        destinationCamera.Priority = transitionPriority + 1;
        yield return null;

        float waitTime = 0f;
        float blendTimeout = Mathf.Max(1f, moveDuration + 1f);
        while (cinemachineBrain != null && cinemachineBrain.IsBlending && waitTime < blendTimeout)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = false;
        }

        CleanupTransitionCameras();

        // Cinemachine 只负责过渡。最终仍启用原楼层相机，从而完整保留它自己的
        // Culling Mask、URP 设置、显示层级和 ChinarSmoothUi3DCamera 交互。
        activeBuildingCamera = building.camera;
        RestoreMainCameraView();

        SetReturnButtonVisible(true);
        SetSystemTitleShortcutEnabled(false);

        isMoving = false;

        // 先停止主相机渲染，再激活楼层相机，确保不存在两个视角叠加的帧。
        mainCamera.enabled = false;
        building.camera.gameObject.SetActive(true);
        mainCamera.gameObject.SetActive(false);
    }

    private void AlignBuildingCameraWithEntryLine(BuildingData building, Vector3 mainCameraPosition)
    {
        ChinarSmoothUi3DCamera controller =
            building.camera.GetComponent<ChinarSmoothUi3DCamera>();

        Transform targetTransform = building.targetPos;
        if (targetTransform == null && controller != null)
        {
            targetTransform = controller.pivot;
        }

        if (targetTransform == null)
        {
            Debug.LogWarning($"{building.tagName} 没有可用的 target，继续使用楼层相机原视角。");
            return;
        }

        Vector3 targetPosition = targetTransform.position;
        if (controller != null)
        {
            targetPosition += controller.pivotOffset;
        }

        Vector3 mainToTarget = targetPosition - mainCameraPosition;
        float mainToTargetDistance = mainToTarget.magnitude;
        if (mainToTargetDistance < 0.001f)
        {
            Debug.LogWarning($"{building.tagName} 的 target 与主相机位置重合，无法计算直线进入视角。");
            return;
        }

        Vector3 lineDirection = mainToTarget / mainToTargetDistance;
        // 保留该楼层相机原本到观察中心的距离，只改变它位于 target 的哪一侧。
        float floorViewDistance = controller != null && controller.distance > 0.001f
            ? controller.distance
            : Vector3.Distance(building.camera.transform.position, targetPosition);
        if (controller != null)
        {
            floorViewDistance = Mathf.Clamp(
                floorViewDistance,
                controller.minDistance,
                controller.maxDistance);
        }
        floorViewDistance = Mathf.Max(0.001f, floorViewDistance);

        Vector3 alignedPosition = targetPosition - lineDirection * floorViewDistance;
        Quaternion alignedRotation = Quaternion.LookRotation(
            lineDirection,
            mainCamera.transform.up);

        if (controller != null)
        {
            controller.SetRuntimeStartView(
                alignedPosition,
                alignedRotation,
                targetTransform);
        }
        else
        {
            building.camera.transform.SetPositionAndRotation(alignedPosition, alignedRotation);
        }
    }

    private void DisableOtherBuildingCameras(Camera selectedCamera)
    {
        foreach (BuildingData building in buildings)
        {
            if (building.camera != null && building.camera != selectedCamera)
            {
                building.camera.gameObject.SetActive(false);
            }
        }
    }

    public void ReturnToMainCamera()
    {
        if (mainCamera == null || isMoving)
        {
            return;
        }

        if (activeBuildingCamera == null)
        {
            ReturnToDefaultMainView();
            return;
        }

        BeginBuildingReturnTransition(activeBuildingCamera);
    }

    private void BeginBuildingReturnTransition(Camera buildingCamera)
    {
        isMoving = true;
        SetReturnButtonVisible(false);
        SetSystemTitleShortcutEnabled(false);

        CaptureMainCameraDefaultView();
        CameraLensState buildingLens = CaptureLens(buildingCamera);
        int originalMainCullingMask = mainCamera.cullingMask;

        EnsureCinemachineBrain();
        CleanupTransitionCameras();

        CinemachineVirtualCamera sourceCamera = CreateTransitionCamera(
            "CM_Return_Source",
            buildingCamera.transform.position,
            buildingCamera.transform.rotation,
            buildingCamera,
            transitionPriority);
        transitionSourceObject = sourceCamera.gameObject;

        // 使用保存的默认镜头参数创建终点虚拟相机。
        ApplyLens(mainCamera, mainCameraStartLens);
        CinemachineVirtualCamera destinationCamera = CreateTransitionCamera(
            "CM_Return_DefaultView",
            mainCameraStartPosition,
            mainCameraStartRotation,
            mainCamera,
            transitionPriority - 1);
        transitionDestinationObject = destinationCamera.gameObject;

        // 首帧由主相机无缝接替楼层相机画面，避免切换时闪回园区全景。
        mainCamera.transform.SetPositionAndRotation(
            buildingCamera.transform.position,
            buildingCamera.transform.rotation);
        ApplyLens(mainCamera, buildingLens);
        mainCamera.cullingMask = originalMainCullingMask | buildingCamera.cullingMask;

        ConfigureCinemachineBlend(DefaultViewMoveDuration);
        ResetBuildingCamera(buildingCamera);
        buildingCamera.gameObject.SetActive(false);

        mainCamera.enabled = true;
        mainCamera.gameObject.SetActive(true);
        StartCoroutine(CompleteBuildingReturnTransition(
            destinationCamera,
            buildingCamera,
            originalMainCullingMask));
    }

    private IEnumerator CompleteBuildingReturnTransition(
        CinemachineVirtualCamera destinationCamera,
        Camera buildingCamera,
        int originalMainCullingMask)
    {
        yield return PlayCinemachineBlend(destinationCamera, DefaultViewMoveDuration);

        cinemachineBrain.enabled = false;
        CleanupTransitionCameras();
        mainCamera.cullingMask = originalMainCullingMask;
        RestoreMainCameraView();

        buildingCamera.enabled = true;
        activeBuildingCamera = null;
        RestoreMainCameraController();
        SetSystemTitleShortcutEnabled(true);

        if (SceneManagerSelf.Instance != null)
        {
            SceneManagerSelf.Instance.SetHighlightTriggerActive(true);
            // 返回主视角后保持数据面板隐藏，需要时由“数据面板”按钮手动展开。
            SceneManagerSelf.Instance.PlayAllSequences(false);
        }

        isMoving = false;
    }

    /// <summary>
    /// 左上角系统名称的快捷入口。楼层专属相机生效时明确禁用该功能。
    /// </summary>
    public void ReturnToDefaultMainView()
    {
        if (mainCamera == null || IsInsideBuilding || isMoving ||
            !mainCamera.gameObject.activeInHierarchy)
        {
            return;
        }

        StartCoroutine(SmoothMainCameraToDefaultView());
    }

    private IEnumerator SmoothMainCameraToDefaultView()
    {
        isMoving = true;
        bool controllerEnabled =
            mainCameraController != null && mainCameraController.enabled;
        if (controllerEnabled)
        {
            mainCameraController.enabled = false;
        }

        CaptureMainCameraDefaultView();
        CameraLensState currentLens = CaptureLens(mainCamera);
        EnsureCinemachineBrain();
        CleanupTransitionCameras();

        CinemachineVirtualCamera sourceCamera = CreateTransitionCamera(
            "CM_DefaultView_Source",
            mainCamera.transform.position,
            mainCamera.transform.rotation,
            mainCamera,
            transitionPriority);
        transitionSourceObject = sourceCamera.gameObject;

        ApplyLens(mainCamera, mainCameraStartLens);
        CinemachineVirtualCamera destinationCamera = CreateTransitionCamera(
            "CM_DefaultView_Destination",
            mainCameraStartPosition,
            mainCameraStartRotation,
            mainCamera,
            transitionPriority - 1);
        transitionDestinationObject = destinationCamera.gameObject;
        ApplyLens(mainCamera, currentLens);

        ConfigureCinemachineBlend(DefaultViewMoveDuration);
        yield return PlayCinemachineBlend(destinationCamera, DefaultViewMoveDuration);

        cinemachineBrain.enabled = false;
        CleanupTransitionCameras();
        RestoreMainCameraView();

        if (controllerEnabled)
        {
            // 重新启用会触发 SimpleCameraController.OnEnable，令其内部状态同步默认视角。
            mainCameraController.enabled = true;
        }

        isMoving = false;
    }

    private static void ResetBuildingCamera(Camera camera)
    {
        ChinarSmoothUi3DCamera controller = camera.GetComponent<ChinarSmoothUi3DCamera>();
        if (controller == null)
        {
            return;
        }

        controller.ResetToStartView();
    }

    private void EnsureCinemachineBrain()
    {
        if (mainCamera == null)
        {
            return;
        }

        if (cinemachineBrain == null)
        {
            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
        }

        if (cinemachineBrain == null)
        {
            cinemachineBrain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
        }

        // 普通园区浏览继续由原 SimpleCameraController 控制，只有进入楼层时才启用 Brain。
        cinemachineBrain.enabled = false;
    }

    private static CinemachineVirtualCamera CreateTransitionCamera(
        string objectName,
        Transform viewTransform,
        Camera lensSource,
        int priority)
    {
        return CreateTransitionCamera(
            objectName,
            viewTransform.position,
            viewTransform.rotation,
            lensSource,
            priority);
    }

    private static CinemachineVirtualCamera CreateTransitionCamera(
        string objectName,
        Vector3 position,
        Quaternion rotation,
        Camera lensSource,
        int priority)
    {
        GameObject cameraObject = new GameObject(objectName);
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        cameraObject.transform.SetPositionAndRotation(position, rotation);

        CinemachineVirtualCamera virtualCamera = cameraObject.AddComponent<CinemachineVirtualCamera>();
        virtualCamera.Priority = priority;
        virtualCamera.m_Lens = LensSettings.FromCamera(lensSource);
        return virtualCamera;
    }

    private float DefaultViewMoveDuration => mainCameraController != null
        ? Mathf.Max(0.1f, mainCameraController.defaultViewMoveDuration)
        : Mathf.Max(0.1f, moveDuration);

    private void ConfigureCinemachineBlend(float duration)
    {
        cinemachineBrain.m_UpdateMethod = CinemachineBrain.UpdateMethod.SmartUpdate;
        cinemachineBrain.m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.LateUpdate;
        cinemachineBrain.m_DefaultBlend = new CinemachineBlendDefinition(
            blendStyle,
            duration);
        cinemachineBrain.enabled = true;
    }

    private IEnumerator PlayCinemachineBlend(
        CinemachineVirtualCamera destinationCamera,
        float duration)
    {
        // 先稳定在当前画面一帧，再提升默认视角的优先级开始混合。
        yield return null;
        destinationCamera.Priority = transitionPriority + 1;
        yield return null;

        float waitTime = 0f;
        float blendTimeout = Mathf.Max(1f, duration + 1f);
        while (cinemachineBrain != null && cinemachineBrain.IsBlending &&
               waitTime < blendTimeout)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void CancelCinemachineTransition()
    {
        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = false;
        }

        CleanupTransitionCameras();
    }

    private void CleanupTransitionCameras()
    {
        DestroyTransitionObject(ref transitionSourceObject);
        DestroyTransitionObject(ref transitionDestinationObject);
    }

    private static void DestroyTransitionObject(ref GameObject transitionObject)
    {
        if (transitionObject == null)
        {
            return;
        }

        Destroy(transitionObject);
        transitionObject = null;
    }

    private void RestoreMainCameraView()
    {
        CaptureMainCameraDefaultView();
        mainCamera.transform.SetPositionAndRotation(mainCameraStartPosition, mainCameraStartRotation);
        ApplyLens(mainCamera, mainCameraStartLens);
    }

    private void CaptureMainCameraDefaultView()
    {
        if (mainCameraController != null)
        {
            mainCameraController.GetDefaultView(
                out mainCameraStartPosition,
                out mainCameraStartRotation);
            return;
        }

        mainCameraStartPosition = mainCamera.transform.position;
        mainCameraStartRotation = mainCamera.transform.rotation;
    }

    private void RestoreMainCameraController()
    {
        if (mainCameraController != null)
        {
            mainCameraController.enabled = mainCameraControllerWasEnabled;
        }
    }

    private static CameraLensState CaptureLens(Camera camera)
    {
        return new CameraLensState
        {
            orthographic = camera.orthographic,
            fieldOfView = camera.fieldOfView,
            orthographicSize = camera.orthographicSize,
            nearClipPlane = camera.nearClipPlane,
            farClipPlane = camera.farClipPlane,
            lensShift = camera.lensShift,
            gateFit = camera.gateFit
        };
    }

    private static void ApplyLens(Camera camera, CameraLensState lens)
    {
        camera.orthographic = lens.orthographic;
        camera.fieldOfView = lens.fieldOfView;
        camera.orthographicSize = lens.orthographicSize;
        camera.nearClipPlane = lens.nearClipPlane;
        camera.farClipPlane = lens.farClipPlane;
        camera.lensShift = lens.lensShift;
        camera.gateFit = lens.gateFit;
    }

    private void InitializeSceneReturnUI()
    {
        if (returnMainCameraButton == null)
        {
            returnMainCameraButton = FindSceneReturnButton();
        }

        if (returnCanvasObject == null)
        {
            returnCanvasObject = FindNamedReturnCanvas();
        }

        // 优先控制用户预设的独立 Canvas；没有配置时只控制返回按钮本身，
        // 避免误关包含其他园区 UI 的公共 Canvas。
        returnDisplayObject = returnCanvasObject != null
            ? returnCanvasObject
            : returnMainCameraButton != null ? returnMainCameraButton.gameObject : null;

        if (returnMainCameraButton == null)
        {
            Debug.LogWarning("没有找到 ReturnMainCam/ReturnMaimCam 返回按钮，请在 CameraRay 中指定。");
        }
        else if (!HasPersistentReturnHandler(returnMainCameraButton))
        {
            // 场景按钮没有绑定时才补充运行时监听，避免重复执行返回逻辑。
            returnMainCameraButton.onClick.AddListener(ReturnToMainCamera);
        }

        SetReturnButtonVisible(false);
    }

    private void InitializeSystemTitleButton()
    {
        if (systemTitleButton == null)
        {
            Text[] sceneTexts = Resources.FindObjectsOfTypeAll<Text>();
            foreach (Text sceneText in sceneTexts)
            {
                if (sceneText == null || sceneText.gameObject.scene != gameObject.scene ||
                    sceneText.text != SystemTitleText)
                {
                    continue;
                }

                systemTitleButton = sceneText.GetComponent<Button>();
                if (systemTitleButton == null)
                {
                    systemTitleButton = sceneText.gameObject.AddComponent<Button>();
                }

                systemTitleButton.targetGraphic = sceneText;
                systemTitleButton.transition = Selectable.Transition.None;
                break;
            }
        }

        if (systemTitleButton == null)
        {
            Debug.LogWarning($"没有找到左上角系统名称：{SystemTitleText}");
            return;
        }

        systemTitleButton.onClick.RemoveListener(ReturnToDefaultMainView);
        systemTitleButton.onClick.AddListener(ReturnToDefaultMainView);
        SetSystemTitleShortcutEnabled(!IsInsideBuilding);
    }

    private void SetSystemTitleShortcutEnabled(bool enabled)
    {
        if (systemTitleButton != null)
        {
            systemTitleButton.interactable = enabled;
        }
    }

    private Button FindSceneReturnButton()
    {
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button button in buttons)
        {
            if (button == null || button.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            string objectName = button.gameObject.name;
            if (objectName == "ReturnMainCam" || objectName == "ReturnMaimCam")
            {
                return button;
            }
        }

        return null;
    }

    private GameObject FindNamedReturnCanvas()
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            string canvasName = canvas.gameObject.name.ToLowerInvariant();
            if (canvasName.Contains("return") &&
                (canvasName.Contains("main") || canvasName.Contains("maim")))
            {
                return canvas.gameObject;
            }
        }

        return null;
    }

    private static bool HasPersistentReturnHandler(Button button)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == "ReturnMainCamera")
            {
                return true;
            }
        }

        return false;
    }

    private void SetReturnButtonVisible(bool visible)
    {
        if (returnDisplayObject != null)
        {
            returnDisplayObject.SetActive(visible);
        }
    }
}
