using System;
using System.Collections.Generic;
using UnityEngine;

public class Conveyor : MonoBehaviour
{
   [Header("Components")]
    [SerializeField] private MeshRenderer _meshRenderer;

    [Header("Settings")]
    [Tooltip("상자 물리 이동 속도")]
    [UnityEngine.Serialization.FormerlySerializedAs("_speed")]
    [SerializeField] private float _boxSpeed = 1.0f;

    [Tooltip("텍스처 스크롤 속도")]
    [SerializeField] private float _textureSpeed = 1.0f;

    public enum ScrollAxis { X, Y }
    [Tooltip("텍스처가 흘러가는 축")]
    [SerializeField] private ScrollAxis _scrollAxis = ScrollAxis.Y;

    [Tooltip("상자가 이동할 3D 월드/로컬 방향 (보통 forward(Z축) 또는 right(X축))")]
    [SerializeField] private Vector3 _pushDirection = Vector3.forward;

    [Tooltip("상자 이동 방향 반전 (+ / -)")]
    [UnityEngine.Serialization.FormerlySerializedAs("_reverseDirection")]
    [SerializeField] private bool _reverseBoxDirection = false;

    [Tooltip("텍스처 스크롤 방향 반전 (+ / -)")]
    [SerializeField] private bool _reverseTextureDirection = true;

    [Header("Layer Settings")]
    [Tooltip("감지할 상자 레이어 마스크 (미지정 시 Box 및 DefectBox 자동 설정)")]
    [SerializeField] private LayerMask _boxLayer;

    private Material _material;
    private Vector2 _currentOffset = Vector2.zero;
    private bool _lastRunState = false;
    private BoxCollider _boxCollider;
    private readonly HashSet<Rigidbody> _contactingBoxes = new HashSet<Rigidbody>();

    private void Awake()
    {
        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();

        _material = _meshRenderer.material;
        _boxCollider = GetComponent<BoxCollider>();

        if (_boxLayer.value == 0)
        {
            _boxLayer = LayerMask.GetMask("Box", "DefectBox");
            if (_boxLayer.value == 0)
            {
                _boxLayer = ~0; // 레이어가 없으면 모든 레이어 감지
            }
        }
    }

    private void Update()
    {
        bool currentRun = (PLCConnect.Instance != null && PLCConnect.Instance.isConveyorRun);

        // 컨베이어가 정지 -> 가동으로 바뀐 순간 (E-Stop 해제 및 재시작)
        if (currentRun && !_lastRunState)
        {
            WakeUpBoxesOnBelt();
        }

        _lastRunState = currentRun;

        if (currentRun)
        {
            RunTexture();
        }
    }

    private void FixedUpdate()
    {
        bool currentRun = (PLCConnect.Instance != null && PLCConnect.Instance.isConveyorRun);
        if (!currentRun || _contactingBoxes.Count == 0)
            return;

        // 파괴되었거나 비활성화된 오브젝트 정리
        _contactingBoxes.RemoveWhere(rb => rb == null || !rb.gameObject.activeInHierarchy);

        Vector3 worldDirection = transform.TransformDirection(_pushDirection.normalized);
        float boxDirectionSign = _reverseBoxDirection ? -1f : 1f;
        Vector3 movement = worldDirection * (boxDirectionSign * _boxSpeed * Time.fixedDeltaTime);

        foreach (var boxRb in _contactingBoxes)
        {
            if (boxRb != null && !boxRb.isKinematic)
            {
                if (boxRb.IsSleeping())
                {
                    boxRb.WakeUp();
                }

                boxRb.MovePosition(boxRb.position + movement);
            }
        }
    }

    private void WakeUpBoxesOnBelt()
    {
        if (_boxCollider == null) return;

        // 컨베이어 상단 영역 계산 (로컬 크기 및 회전 반영)
        Vector3 localCenter = _boxCollider.center + Vector3.up * (_boxCollider.size.y * 0.5f + 0.25f);
        Vector3 worldCenter = transform.TransformPoint(localCenter);
        Vector3 halfExtents = Vector3.Scale(_boxCollider.size, transform.lossyScale) * 0.5f;
        halfExtents.y += 0.5f; // 상자 높이만큼 위로 영역 확장

        // LayerMask는 int Layer가 아니라 비트마스크여야 함
        Collider[] hits = Physics.OverlapBox(worldCenter, halfExtents, transform.rotation, _boxLayer);

        foreach (var col in hits)
        {
            Rigidbody rb = col.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                rb.WakeUp(); // 잠든 Rigidbody 깨우기
                _contactingBoxes.Add(rb);
            }
        }
    }

    private void RunTexture()
    {
        float textureDirectionSign = _reverseTextureDirection ? -1f : 1f;
        float delta = _textureSpeed * textureDirectionSign * Time.deltaTime;

        if (_scrollAxis == ScrollAxis.Y) _currentOffset.y += delta;
        else _currentOffset.x += delta;

        _currentOffset.x %= 1f;
        _currentOffset.y %= 1f;

        _material.mainTextureOffset = _currentOffset;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody boxRb = collision.rigidbody;
        if (boxRb != null && !boxRb.isKinematic)
        {
            _contactingBoxes.Add(boxRb);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        Rigidbody boxRb = collision.rigidbody;
        if (boxRb != null && !boxRb.isKinematic)
        {
            _contactingBoxes.Add(boxRb);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        Rigidbody boxRb = collision.rigidbody;
        if (boxRb != null)
        {
            _contactingBoxes.Remove(boxRb);
        }
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }
}
