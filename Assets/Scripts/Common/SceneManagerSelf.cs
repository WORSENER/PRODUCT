using System;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using UnityEngine;
using DG.Tweening;
public class SceneManagerSelf : Singleton<SceneManagerSelf>
{
    [HideInInspector]
    public List<HighlightTrigger> highlightTriggers = new List<HighlightTrigger>();//物体高亮显示

    public bool isActivehighlightTrigger = true;
    public List<Sequence> dataPanelSequences=new List<Sequence>();
    private bool isDisplay=false;//panel是否展示的标志位
    public Camera mainCamera;
    public List<Camera> buildingCameras = new List<Camera>();
    private void Start()
    {
        Invoke(nameof(SetHighlightTrigger),0.5f);
        //Invoke(nameof(PlayAllSequences),0.5f);
        Application.runInBackground = true;

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
#endif
        }
    }

    public void SetHighlightTrigger()
    {
        SetHighlightTriggerActive(!isActivehighlightTrigger);
    }

    public void SetHighlightTriggerActive(bool active)
    {
        isActivehighlightTrigger = active;
        highlightTriggers.RemoveAll(item => item == null);
        foreach (var item in highlightTriggers)
        {
            if (!active)
            {
                // HighlightTrigger 禁用时不会自动清除 HighlightEffect 中已经生效的状态。
                // 临时取消淡出时间，确保进入楼层前当前高亮立即消失。
                HighlightEffect effect = item.highlightEffect;
                if (effect == null)
                {
                    effect = item.GetComponent<HighlightEffect>();
                }

                if (effect != null)
                {
                    float originalFadeOutDuration = effect.fadeOutDuration;
                    effect.fadeOutDuration = 0f;
                    effect.SetHighlighted(false);
                    effect.fadeOutDuration = originalFadeOutDuration;
                }
            }

            item.enabled = active;
        }
    }

    public void RegisterTween(Sequence sequence)
    {
        if (!dataPanelSequences.Contains(sequence))
        {
            dataPanelSequences.Add(sequence);
        }
    }
    // 统一控制所有动画的播放方向
    public void PlayAllSequences()
    {
        isDisplay = !isDisplay;
        string status=isDisplay?"<展示>":"<隐藏>";
        Debug.Log(status);
        foreach (var se in dataPanelSequences)
        {
            if (se == null) continue;
            if (isDisplay)
            {
                se.PlayForward(); // 正向播放
            }
            else
            {
                se.PlayBackwards(); // 倒放
            }
        }
    }

    public void PlayAllSequences(bool forward)
    {
        isDisplay = forward;
        foreach (var se in dataPanelSequences)
        {
            if (se == null) continue;
            if (isDisplay)
            {
                se.PlayForward(); // 正向播放
            }
            else
            {
                se.PlayBackwards(); // 倒放
            }
        }
    }

    public void ReturnMainCamera()
    {
        CameraRay cameraRay = mainCamera != null ? mainCamera.GetComponent<CameraRay>() : null;
        if (cameraRay != null && cameraRay.IsInsideBuilding)
        {
            cameraRay.ReturnToMainCamera();
            return;
        }

        mainCamera.gameObject.SetActive(true);
        foreach (var item in buildingCameras)
        {
            if (item == null) continue;
            if (item.gameObject.activeSelf)
            {
                StartCoroutine(MoveCameraToMain(item, mainCamera.transform.position, 0.5f, () =>
                {
                    item.gameObject.SetActive(false);
                }));
            }
            else
            {
                continue;
            }
            
        }
    }
    private IEnumerator MoveCameraToMain(Camera camera, Vector3 targetPosition, float duration, System.Action onComplete)
    {
        var chinaSmooth = camera.GetComponent<ChinarSmoothUi3DCamera>();
        chinaSmooth.enabled = false;
        Vector3 startPosition = camera.transform.position;
        Quaternion startRotation = camera.transform.rotation;
        Quaternion targetRotation = mainCamera.transform.rotation; // 确保旋转朝向主摄像机的方向

        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 平滑移动和旋转
            camera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            camera.transform.LookAt(startPosition);
            //camera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }
        camera.transform.position = chinaSmooth.startCameraPosition;
        camera.transform.rotation = chinaSmooth.startCameraRotation;
        chinaSmooth.distance = chinaSmooth.startDistance;

        chinaSmooth.enabled = true;
        //camera.gameObject.SetActive(false);
        
        // 执行完成后的回调
        onComplete?.Invoke();
    }

 
}

