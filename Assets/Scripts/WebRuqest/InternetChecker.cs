using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class InternetChecker : MonoBehaviour
{
    public string URL = "https://www.baidu.com"; // 检查连接的URL
    public GameObject LoginCanvas; // 登录界面画布
    public Image loadingImage; // 加载旋转的图片
    private bool isConnected = false; // 是否连接成功标志

    void Start()
    {
        StartCoroutine(CheckInternetConnection());
    }
    
    IEnumerator CheckInternetConnection()
    {
        while (!isConnected)
        {
            // 检查指定的URL
            UnityWebRequest request = UnityWebRequest.Head(URL);
            yield return request.SendWebRequest();

            // 如果连接成功，设置为已连接
            if (request.result == UnityWebRequest.Result.Success)
            {
                isConnected = true;
            }
            else
            {
                yield return new WaitForSeconds(1); // 如果连接失败，等待一秒后重试
            }
        }

        // 连接成功后，可以跳转到下一页面
        Debug.Log("Internet connection is available. Loading next page...");
        LoadNextPage();
    }

    void LoadNextPage()
    {
        LoginCanvas.SetActive(true); // 启用登录界面
        transform.gameObject.SetActive(false); // 禁用当前对象
    }

    private void Update()
    {
        if (!isConnected)
        {
            // 让加载图片每帧旋转
            loadingImage.rectTransform.Rotate(0, 0, -20 * Time.deltaTime);
        }
    }
}