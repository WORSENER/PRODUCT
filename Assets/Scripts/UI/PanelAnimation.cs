using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PanelAnimation : MonoBehaviour
{
    private Vector2 startPos;
    public Vector2 endPos;
    public  float duration=0.2f;
    private RectTransform rect;
    private Tween ani;
    private Sequence sequence;
    void Start()
    {
        rect = GetComponent<RectTransform>();
        GetComponent<CanvasGroup>().alpha = 0f;
        startPos = rect.anchoredPosition;
        // 创建动画并存储
        sequence=DOTween.Sequence().SetAutoKill(false).Pause();
        sequence.Join(rect.DOAnchorPos(endPos,duration));//移动到屏幕
        sequence.Join(rect.GetComponent<CanvasGroup>().DOFade(1f,duration));
        // 注册到 SceneManagerSelf
        SceneManagerSelf.Instance.RegisterTween(sequence);
    }
}
