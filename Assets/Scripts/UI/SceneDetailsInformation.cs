using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
/// <summary>
/// 此脚本应该挂载到场景中的定位标志上，点击显示此位置的信息
/// </summary>
public class SceneDetailsInformation : MonoBehaviour
{
    public Transform imagePanel;
    Vector3 screenPos;
    public Vector3 offestScreenPos;
    //public float distance=300f;
    private string screenRes;//屏幕分辨率
    private void Awake()
    {
        screenRes=Screen.width.ToString()+Screen.height.ToString();
        switch (screenRes)
        {
            case "38402160":
                offestScreenPos = new Vector3(-1.26999998f,345f,0f);
                break;
            case "25601440":
                offestScreenPos=new Vector3(-1.26999998f,239.600006f,0);
                break;
            case "19201080":
                offestScreenPos= new Vector3(-1.26999998f,178f,0);
                break;
            case "1366768":
                offestScreenPos= new Vector3(-1.26999998f,131.300003f,0f);
                break;
            default:
                offestScreenPos = new Vector3(-1.26999998f,178f,0);
                break;
        }
    }
    public void SwitchImagePanel()
    {
        imagePanel.gameObject.SetActive(!imagePanel.gameObject.activeSelf);
    }
    private void Start()
    {
        Debug.Log("Width=" +Screen.width + "Height=" + Screen.height);
        
    }
    private void Update()
    {
        //if(Vector3.Distance(transform.position,Camera.main.transform.position)>distance)
        //{
        //    imagePanel.gameObject.SetActive(false);
        //}
        if(imagePanel.gameObject.activeSelf)
        {
            screenPos=Camera.main.WorldToScreenPoint(transform.position);
            imagePanel.position = screenPos + offestScreenPos;
        }
    }

}
