using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class VLOG : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform end;
    void Start()
    {
        transform.DOMove(end.position,10f).SetEase(Ease.Linear);
        transform.DORotate(end.eulerAngles,10f).SetEase(Ease.Linear);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
