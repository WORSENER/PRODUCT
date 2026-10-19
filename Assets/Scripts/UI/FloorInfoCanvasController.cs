using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maintains the dedicated floor marker canvas and renders nearer markers above farther markers.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class FloorInfoCanvasController : MonoBehaviour
{
    [SerializeField] private int sortingOrder = -10;

    private readonly List<FlootInfoItem_UI> items = new List<FlootInfoItem_UI>();
    private Canvas floorCanvas;
    private bool itemsDirty = true;

    private void Awake()
    {
        floorCanvas = GetComponent<Canvas>();
        ConfigureCanvas();
        CacheItems();
    }

    private void OnEnable()
    {
        itemsDirty = true;
    }

    private void OnTransformChildrenChanged()
    {
        itemsDirty = true;
    }

    private void LateUpdate()
    {
        if (itemsDirty)
        {
            CacheItems();
        }

        // UGUI draws later siblings last. Farther items go first, nearest items go last/on top.
        items.Sort(CompareFarToNear);
        for (int i = 0; i < items.Count; i++)
        {
            FlootInfoItem_UI item = items[i];
            if (item != null && item.transform.GetSiblingIndex() != i)
            {
                item.transform.SetSiblingIndex(i);
            }
        }
    }

    private void ConfigureCanvas()
    {
        if (floorCanvas == null) return;

        floorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        floorCanvas.overrideSorting = true;
        floorCanvas.sortingOrder = sortingOrder;
    }

    private void CacheItems()
    {
        items.Clear();
        FlootInfoItem_UI[] foundItems = GetComponentsInChildren<FlootInfoItem_UI>(true);
        items.AddRange(foundItems);
        itemsDirty = false;
    }

    private static int CompareFarToNear(FlootInfoItem_UI left, FlootInfoItem_UI right)
    {
        if (ReferenceEquals(left, right)) return 0;
        if (left == null) return -1;
        if (right == null) return 1;

        int distanceCompare = right.CameraDistanceSqr.CompareTo(left.CameraDistanceSqr);
        return distanceCompare != 0
            ? distanceCompare
            : left.GetInstanceID().CompareTo(right.GetInstanceID());
    }
}
