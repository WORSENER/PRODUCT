using System;
using UnityEngine;


/// <summary>
/// 脚本挂载新建相机上 ——
/// </summary>
public class ChinarSmoothUi3DCamera : MonoBehaviour
{
    public  Transform pivot;
    public  Vector3   pivotOffset = Vector3.zero;
    public  Transform target;
    public  float     distance       = 10.0f;
    public  float     minDistance    = 2f;
    public  float     maxDistance    = 15f;
    public  float     zoomSpeed      = 1f;
    public  float     xSpeed         = 250.0f;
    public  float     ySpeed         = 250.0f;
    public  bool      allowYTilt     = true;
    public  float     yMinLimit      = -90f;
    public  float     yMaxLimit      = 90f;
    private float     x              = 0.0f;
    private float     y              = 0.0f;
    private float     targetX        = 0f;
    private float     targetY        = 0f;
    public  float     targetDistance = 0f;
    private float     xVelocity      = 1f;
    private float     yVelocity      = 1f;
    private float     zoomVelocity   = 1f;

    public bool useSceneViewSettings = true;

    public Transform startCameraTransform;

    public Vector3 startCameraPosition;
    public Quaternion startCameraRotation;
    public float startDistance;
    private void Awake()
    {
        if (startCameraTransform == null)
        {
            startCameraTransform = transform;
        }

        startCameraPosition = startCameraTransform.position;
        startCameraRotation = startCameraTransform.rotation;
        startDistance = distance;
    }

    private void Start()
    {
        //startCameraTransform = transform;

        //if (pivot == null) return;
        UseStartValue(useSceneViewSettings);
        Debug.Log(transform.name);

        // 楼内摄像机在场景中已经默认设为未激活。
        // 不要在第一次启用后的 Start 中再次关闭自身，否则主摄像机关闭后会出现
        // “没有摄像机正在渲染”，并且只有返回后第二次进入才会正常。
    }
    public void UseStartValue(bool isUseSceneView)
    {
        if (!isUseSceneView)
        {
            // 如果不使用 Scene 视图设置，则初始化为默认值
            var angles = transform.eulerAngles;
            targetX = x = angles.y;
            targetY = y = ClampAngle(angles.x, yMinLimit, yMaxLimit);
            targetDistance = distance;
        }
        else
        {
            // 使用当前摄像机在 Scene 视图中的设置
            Transform viewTransform = startCameraTransform != null ? startCameraTransform : transform;
            targetX = x = viewTransform.eulerAngles.y;
            targetY = y = ClampAngle(viewTransform.eulerAngles.x, yMinLimit, yMaxLimit);
            targetDistance = pivot != null
                ? Vector3.Distance(viewTransform.position, pivot.position + pivotOffset)
                : distance;
            distance = targetDistance;
        }
    }
    private void OnEnable()
    {
        UseStartValue(useSceneViewSettings);

    }

    /// <summary>
    /// 返回园区主相机时恢复楼层相机的初始视角，下一次进入仍保留原旋转和缩放交互。
    /// </summary>
    public void ResetToStartView()
    {
        transform.SetPositionAndRotation(startCameraPosition, startCameraRotation);
        distance = startDistance;
        targetDistance = startDistance;
        xVelocity = 0f;
        yVelocity = 0f;
        zoomVelocity = 0f;
        UseStartValue(useSceneViewSettings);
    }

    /// <summary>
    /// 将运行时计算出的楼层入口视角设为新的初始视角，同时保留原有旋转、缩放逻辑。
    /// </summary>
    public void SetRuntimeStartView(
        Vector3 position,
        Quaternion rotation,
        Transform runtimePivot = null)
    {
        if (runtimePivot != null)
        {
            pivot = runtimePivot;
        }

        transform.SetPositionAndRotation(position, rotation);
        startCameraTransform = transform;
        startCameraPosition = position;
        startCameraRotation = rotation;

        float viewDistance = pivot != null
            ? Vector3.Distance(position, pivot.position + pivotOffset)
            : distance;
        distance = viewDistance;
        startDistance = viewDistance;
        targetDistance = viewDistance;
        xVelocity = 0f;
        yVelocity = 0f;
        zoomVelocity = 0f;
        UseStartValue(useSceneViewSettings);
    }

    private void LateUpdate()
    {
        if (!pivot) return;
        var scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll      > 0.0f) targetDistance -= zoomSpeed;
        else if (scroll < 0.0f)
            targetDistance += zoomSpeed;
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        if (Input.GetMouseButton(1) || (Input.GetMouseButton(0) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))))
        {
            targetX += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            if (allowYTilt)
            {
                targetY -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
                targetY =  ClampAngle(targetY, yMinLimit, yMaxLimit);
            }
        }

        x = Mathf.SmoothDampAngle(x, targetX, ref xVelocity, 0.3f);
        y = allowYTilt ? Mathf.SmoothDampAngle(y, targetY, ref yVelocity, 0.3f) : targetY;
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        distance = Mathf.SmoothDamp(distance, targetDistance, ref zoomVelocity, 0.5f);
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + pivot.position + pivotOffset;
        transform.rotation = rotation;
        transform.position = position;
    }


    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360) angle += 360;
        if (angle > 360) angle  -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}
