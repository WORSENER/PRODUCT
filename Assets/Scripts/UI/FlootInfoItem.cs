using System;
using DataClass;
using DG.Tweening;
using TMPro;
using UnityEngine;

[Serializable]
public enum FloorType
{
    A,
    B_Ya,
    B_Zhong,
    C,
    D,
    E,
    F,
    G,
    H,
    Kfh_Jing,
    Kfh_Hou,
    Kfh_Ya,
    Comprehensive_Office
}

public class FlootInfoItem : MonoBehaviour
{
    [SerializeField] private FloorType _floorType;
    public Transform texPanel;
    public TMP_Text infoText;

    [SerializeField] private float floatDistance = 0.2f;
    [SerializeField] private float duration = 1f;

    private Tween floatTween;

    private void OnEnable()
    {
        ConnectMySQL.DataRefreshed += RefreshFloorInfo;
        RefreshFloorInfo();
    }

    private void Start()
    {
        Vector3 startPos = transform.localPosition;
        floatTween = transform.DOLocalMoveY(startPos.y + floatDistance, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDisable()
    {
        ConnectMySQL.DataRefreshed -= RefreshFloorInfo;
        floatTween?.Kill();
        floatTween = null;
    }

    private void LateUpdate()
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null) return;

        transform.forward = activeCamera.transform.forward.normalized * -1f;
    }

    private void RefreshFloorInfo()
    {
        FloorInfo floorInfo = ConnectMySQL.instance != null
            ? ConnectMySQL.instance.dataSO?.floorInfo
            : null;

        string value = GetFloorValue(floorInfo, _floorType);
        bool hasValue = !string.IsNullOrWhiteSpace(value);
        SetMarkerVisible(hasValue);
        if (!hasValue || infoText == null)
        {
            if (infoText != null)
            {
                infoText.text = string.Empty;
            }
            return;
        }

        string[] cnAndEn = value.Split(',');
        infoText.text = cnAndEn.Length >= 2
            ? $"{cnAndEn[0].Trim()}\n<size=11>{cnAndEn[1].Trim()}</size>"
            : value.Trim();
    }

    private void SetMarkerVisible(bool visible)
    {
        if (texPanel != null)
        {
            texPanel.gameObject.SetActive(visible);
        }
        else if (infoText != null)
        {
            infoText.gameObject.SetActive(visible);
        }
    }

    public static string GetFloorValue(FloorInfo info, FloorType floorType)
    {
        if (info == null) return null;

        switch (floorType)
        {
            case FloorType.A: return info.A;
            case FloorType.B_Ya: return info.B_Ya;
            case FloorType.B_Zhong: return info.B_Zhong;
            case FloorType.C: return info.C;
            case FloorType.D: return info.D;
            case FloorType.E: return info.E;
            case FloorType.F: return info.F;
            case FloorType.G: return info.G;
            case FloorType.H: return info.H;
            case FloorType.Kfh_Jing: return info.Kfh_Jing;
            case FloorType.Kfh_Hou: return info.Kfh_Hou;
            case FloorType.Kfh_Ya: return info.Kfh_Ya;
            case FloorType.Comprehensive_Office: return info.Comprehensive_Office;
            default: return null;
        }
    }

    public static string GetDefaultFloorName(FloorType floorType)
    {
        switch (floorType)
        {
            case FloorType.B_Ya: return "B楼压铸";
            case FloorType.B_Zhong: return "B楼重铸";
            case FloorType.Kfh_Jing: return "库房精加工";
            case FloorType.Kfh_Hou: return "库房后处理";
            case FloorType.Kfh_Ya: return "库房压铸";
            case FloorType.Comprehensive_Office: return "综合办公楼";
            default: return floorType + "楼";
        }
    }
}
