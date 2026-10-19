using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityTemplateProjects;

public class ScrollViewScript : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Range(0, 10)]
    public float speed;
    public float waitTime=30f;//等待间隔时间
    private float waitTimeCount=0f;//等待时间计时
    public static float autoScrollInterval = 10f; // 自动滑动时的间隔时间
    private ScrollRect scrollRect;
    public float[] posArray;
    private float targetPos;
    private bool isDrag = false;
    private int index = 0;
    private float timeSinceLastDrag = 0f;
    public static SimpleCameraController cameraController;
    public bool isDebug;
    public bool isInteractive;//可交互
    public void OnBeginDrag(PointerEventData eventData)
    {
        if(isInteractive)
        {
            cameraController.enabled = false;
            isDrag = true;
            timeSinceLastDrag = 0f;
            waitTimeCount = 0f;
        }

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isInteractive)
        {
            cameraController.enabled = true;
            isDrag = false;

            Vector2 pos = scrollRect.normalizedPosition;
            float x = Mathf.Abs(pos.x - posArray[0]);
            for (int i = 0; i < posArray.Length; i++)
            {
                float temp = Mathf.Abs(posArray[i] - pos.x);
                if (temp <= x)
                {
                    x = temp;
                    index = i;
                }

            }
            targetPos = posArray[index];
        }
    }

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        cameraController = GameObject.Find("Main Camera").GetComponent<SimpleCameraController>();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //测试水平位置posArray的数据
        //Debug.Log(scrollRect.horizontalNormalizedPosition.ToString());
        if(isDebug)
        {
            Debug.Log(scrollRect.horizontalNormalizedPosition.ToString("F2"));
        }
        if (!isDrag)
        {
            timeSinceLastDrag += Time.deltaTime;
            waitTimeCount += Time.deltaTime;
            if(waitTimeCount>waitTime)
            {
                // 每隔 autoScrollInterval 秒自动滚动到下一个位置
                if (timeSinceLastDrag >= autoScrollInterval)
                {
                    index = (index + 1) % posArray.Length; // 循环到下一个位置
                    targetPos = posArray[index];
                    timeSinceLastDrag = 0f; // 重置计时器
                }
            }
            

            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
                scrollRect.horizontalNormalizedPosition,
                targetPos,
                Time.deltaTime * speed
            );
        }

    }
}
