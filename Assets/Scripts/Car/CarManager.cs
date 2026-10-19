using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class CarManager : MonoBehaviour
{
    public List<CarPath> carPaths;
    public GameObject[] car;
    public float gapTimer;

    [SerializeField, Min(0)] private int prewarmPerPrefab = 10;

    private Queue<GameObject>[] carPools;
    private Coroutine spawnCoroutine;
    private WaitForSeconds spawnWait;

    private void Start()
    {
        if (!CanSpawnCars())
        {
            enabled = false;
            return;
        }

        BuildPools();
        spawnWait = new WaitForSeconds(Mathf.Max(0.1f, gapTimer));
        spawnCoroutine = StartCoroutine(InitializedCar());
    }

    IEnumerator InitializedCar()
    {
        while (true)
        {
            int carIndex = UnityEngine.Random.Range(0, car.Length);
            int pathIndex = UnityEngine.Random.Range(0, carPaths.Count);
            CarPath path = carPaths[pathIndex];

            if (path != null && path.point != null && path.point.Count >= 2
                && path.point[0] != null && path.point[1] != null
                && car[carIndex] != null)
            {
                GameObject instance = TakeFromPool(carIndex);
                Transform instanceTransform = instance.transform;
                instanceTransform.SetParent(transform, false);
                instanceTransform.position = path.point[0].position;
                instanceTransform.rotation = Quaternion.identity;
                instanceTransform.LookAt(path.point[1].position);
                instance.SetActive(true);

                instanceTransform.DOKill();
                instanceTransform.DOMove(path.point[1].position, 30f)
                    .SetEase(Ease.Linear)
                    .OnComplete(() => ReturnToPool(carIndex, instance));
            }

            yield return spawnWait;
        }
    }

    private bool CanSpawnCars()
    {
        if (car == null || car.Length == 0 || carPaths == null || carPaths.Count == 0)
        {
            Debug.LogWarning("CarManager 未配置车辆或路径，已停止生成车辆。", this);
            return false;
        }

        return true;
    }

    private void BuildPools()
    {
        carPools = new Queue<GameObject>[car.Length];
        for (int carIndex = 0; carIndex < car.Length; carIndex++)
        {
            carPools[carIndex] = new Queue<GameObject>(prewarmPerPrefab);
            if (car[carIndex] == null) continue;

            for (int i = 0; i < prewarmPerPrefab; i++)
            {
                carPools[carIndex].Enqueue(CreatePooledCar(carIndex));
            }
        }
    }

    private GameObject TakeFromPool(int carIndex)
    {
        Queue<GameObject> pool = carPools[carIndex];
        while (pool.Count > 0)
        {
            GameObject instance = pool.Dequeue();
            if (instance != null) return instance;
        }

        return CreatePooledCar(carIndex);
    }

    private GameObject CreatePooledCar(int carIndex)
    {
        GameObject instance = Instantiate(car[carIndex], transform);
        instance.name = $"{car[carIndex].name}_Pooled";
        instance.SetActive(false);
        return instance;
    }

    private void ReturnToPool(int carIndex, GameObject instance)
    {
        if (instance == null || carPools == null || carIndex < 0 || carIndex >= carPools.Length)
        {
            return;
        }

        instance.transform.DOKill();
        instance.SetActive(false);
        carPools[carIndex].Enqueue(instance);
    }

    private void OnDisable()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).DOKill();
        }
    }

}
[Serializable]
public class CarPath
{
    public List<Transform> point;
}
