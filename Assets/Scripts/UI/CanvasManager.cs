using DataClass;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using System.Linq;

/// <summary>
/// 关于画布的管理脚本，挂载到主画布上
/// </summary>
public class CanvasManager : MonoBehaviour
{
    public Text water;//水
    public Text electricity;//电
    public Text gas;//气
    public Text C;//碳

    [Header("Welcome Message Settings")]
    public RectTransform welcomePanel;
    public RectTransform welcomeContent;
    public TMP_Text topMessageTex;
    public TMP_Text middleMessageTex;
    public TMP_Text bottomMessageTex;
    public float switchGapTimer;
    [SerializeField, Min(1f)] private float welcomeRefreshInterval = 20f;
    private Coroutine _switchGapCoroutine;
    private WaitForSeconds _welcomeRefreshWait;
    private WaitForSeconds _switchMessageWait;
    private Vector3 _welcomePanelVisibleScale = Vector3.one;

    private void OnEnable()
    {
        ConnectMySQL.DataRefreshed += RefreshYearSummary;
        RefreshYearSummary();
    }

    private void Start()
    {
        if (welcomePanel != null && welcomePanel.localScale.sqrMagnitude > Mathf.Epsilon)
        {
            _welcomePanelVisibleScale = welcomePanel.localScale;
        }

        _switchMessageWait = new WaitForSeconds(Mathf.Max(0.1f, switchGapTimer));
        _welcomeRefreshWait = new WaitForSeconds(Mathf.Max(1f, welcomeRefreshInterval));
        ShowMessagePanel();
    }
    public void SwitchActive(GameObject gameObject)
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    public void RefreshYearSummary()
    {
        if (ConnectMySQL.instance == null || ConnectMySQL.instance.dataSO == null)
        {
            SetSummaryText(water, null, "水");
            SetSummaryText(electricity, null, "电");
            SetSummaryText(gas, null, "燃气");
            SetSummaryText(C, null, "碳排放");
            return;
        }

        int currentYear = DateTime.Now.Year;
        var allYearData = ConnectMySQL.instance.dataSO.yearEnergyConsumptions;

        SetSummaryText(water, allYearData
            .Where(item => item != null
                           && EnergyDataUtility.MatchesEnergyType(item.Type, "水")
                           && EnergyDataUtility.MatchesDatePart(item.Year, currentYear))
            .OrderByDescending(item => item.Id)
            .FirstOrDefault(), "水");

        SetSummaryText(electricity, allYearData
            .Where(item => item != null
                           && EnergyDataUtility.MatchesEnergyType(item.Type, "电")
                           && EnergyDataUtility.MatchesDatePart(item.Year, currentYear))
            .OrderByDescending(item => item.Id)
            .FirstOrDefault(), "电");

        SetSummaryText(gas, allYearData
            .Where(item => item != null
                           && EnergyDataUtility.MatchesEnergyType(item.Type, "燃气")
                           && EnergyDataUtility.MatchesDatePart(item.Year, currentYear))
            .OrderByDescending(item => item.Id)
            .FirstOrDefault(), "燃气");

        SetSummaryText(C, allYearData
            .Where(item => item != null
                           && EnergyDataUtility.MatchesEnergyType(item.Type, "碳排放")
                           && EnergyDataUtility.MatchesDatePart(item.Year, currentYear))
            .OrderByDescending(item => item.Id)
            .FirstOrDefault(), "碳排放");
    }

    private static void SetSummaryText(Text target, YearEnergyConsumption data, string energyType)
    {
        if (target == null) return;

        if (data == null)
        {
            target.text = $"--<size=30>{EnergyDataUtility.DefaultUnit(energyType)}</size>";
            return;
        }

        string unit = string.IsNullOrWhiteSpace(data.Unit)
            ? EnergyDataUtility.DefaultUnit(energyType)
            : data.Unit.Trim();
        target.text = $"{data.ActualValue}<size=30>{unit}</size>";
    }

    public void ShowMessagePanel()
    {
        if (welcomePanel == null) return;

        bool shouldShow = !welcomePanel.gameObject.activeSelf;
        Debug.Log($"display {shouldShow}");

        if (shouldShow)
        {
            // 背景位于 welcomePanel 下，不参与缩放动画；重新打开时恢复父面板的设计缩放。
            welcomePanel.localScale = _welcomePanelVisibleScale;
            welcomePanel.gameObject.SetActive(true);

            // 只有文字容器从 0 开始播放缩放动画。
            if (welcomeContent != null)
            {
                welcomeContent.DOKill();
                welcomeContent.localScale = Vector3.zero;
            }

            if (_switchGapCoroutine == null)
            {
                _switchGapCoroutine = StartCoroutine(SwitchWelcomeMessage());
            }

            return;
        }

        StopWelcomeMessageLoop();
        if (welcomeContent != null)
        {
            welcomeContent.localScale = Vector3.zero;
        }

        welcomePanel.gameObject.SetActive(false);
    }

    IEnumerator SwitchWelcomeMessage()
    {
        bool needsInitialRefresh = true;

        while (true)
        {
            if (ConnectMySQL.instance == null || ConnectMySQL.instance.dataSO == null)
            {
                welcomeContent.localScale = Vector3.zero;
                yield return _welcomeRefreshWait;
                continue;
            }

            // 首次进入轮播时读取一次。之后仅在一整轮播放完毕后刷新。
            if (needsInitialRefresh)
            {
                yield return ConnectMySQL.instance.RefreshWelcomeMessagesAsync();
                needsInitialRefresh = false;
            }

            var allMessages = ConnectMySQL.instance.dataSO.welcomeMessages;

            // 1. 获取今天日期，转为和数据一致格式 yyyy/MM/dd
            string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
            Debug.Log($"今日日期：{todayStr}");

            // 2. 筛选出当天日期匹配的消息
            var todayMessages = (allMessages ?? new System.Collections.Generic.List<WelcomeMessage>())
                .Where(msg => msg != null && msg.date == todayStr)
                .ToList();

            // 当天列表为空时才使用刷新间隔，避免空数据状态持续访问数据库。
            if (todayMessages.Count == 0)
            {
                welcomeContent.localScale = Vector3.zero;
                yield return _welcomeRefreshWait;
                yield return ConnectMySQL.instance.RefreshWelcomeMessagesAsync();
                continue;
            }

            // 使用本轮快照完整播放；中途数据库内容变化不会打断当前列表。
            for (int index = 0; index < todayMessages.Count; index++)
            {
                var targetMsg = todayMessages[index];
                if (topMessageTex != null) topMessageTex.text = targetMsg.topMessage;
                if (middleMessageTex != null) middleMessageTex.text = targetMsg.middleMessage;
                if (bottomMessageTex != null) bottomMessageTex.text = targetMsg.bottomMessage;
                yield return welcomeContent.DOScale(Vector3.one, 0.2f).WaitForCompletion();

                yield return _switchMessageWait;
                yield return welcomeContent.DOScale(Vector3.zero, 0.2f).WaitForCompletion();
            }

            // 完整一轮结束后刷新一次，刷新完成即使用新快照进入下一轮。
            yield return ConnectMySQL.instance.RefreshWelcomeMessagesAsync();
        }
    }

    private void StopWelcomeMessageLoop()
    {
        if (welcomeContent != null)
        {
            welcomeContent.DOKill();
        }

        if (_switchGapCoroutine == null)
        {
            return;
        }

        StopCoroutine(_switchGapCoroutine);
        _switchGapCoroutine = null;
    }

    private void OnDisable()
    {
        ConnectMySQL.DataRefreshed -= RefreshYearSummary;
        StopWelcomeMessageLoop();
    }
}
