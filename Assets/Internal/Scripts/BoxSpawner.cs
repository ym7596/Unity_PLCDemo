using UnityEngine;

public class BoxSpawner : MonoBehaviour
{
    [Header("Prefabs & Points")]
    [SerializeField] private GameObject boxPrefab;   // 상자 프리팹
    [SerializeField] private Transform spawnPoint;    // 상자가 떨어질 위치

    private bool lastSpawnSignal = false;

    void Update()
    {
        if (PLCConnect.Instance == null) return;

        bool currentSignal = PLCConnect.Instance.isSpawnTrigger;

        // PLC의 M15 신호가 꺼져있다가 켜지는 순간(상승 에지 감지)
        if (currentSignal && !lastSpawnSignal)
        {
            SpawnBox();
        }

        lastSpawnSignal = currentSignal;
    }

    private void SpawnBox()
    {
        if (boxPrefab != null && spawnPoint != null)
        {
            Instantiate(boxPrefab, spawnPoint.position, spawnPoint.rotation);
        }
    }
}
