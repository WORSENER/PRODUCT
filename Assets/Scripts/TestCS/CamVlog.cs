using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CamVlog : MonoBehaviour
{
    public MoveLine[] moveLines;
    public Image hide;

    void Start()
    {
        StartCoroutine(CamMove());
        Application.runInBackground = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    IEnumerator CamMove()
    {
        for (int i = 0; i < moveLines.Length; i++) 
        {
            hide.DOFade(0f,1.5f);
            transform.position = moveLines[i].start.position;
            transform.eulerAngles = moveLines[i].start.eulerAngles;
            transform.DOMove(moveLines[i].end.position, 11f).SetEase(Ease.Linear);
            transform.DORotate(moveLines[i].end.eulerAngles,10f).SetEase(Ease.Linear);
            hide.DOFade(1f,2f).SetDelay(9f);
            yield return new WaitForSeconds(11f);
        }
    }
}
[Serializable]
public struct MoveLine
{
    public Transform start;
    public Transform end;
}
