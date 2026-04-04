using Fusion;
using Unity.Cinemachine;
using UnityEngine;

public class MouseLook : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private string cameraHolderName = "CameraHolder";
    private Transform _cameraHolderTransform;
    private CinemachineCamera _vcam;
    private CinemachineThirdPersonFollow _tpFollow;

    [Header("Look Settings")]
    public float mouseSensitivity = 100f;
    public float upperLookLimit = 80f;
    public float lowerLookLimit = -70f;

    [Header("Zoom Settings")]
    public float zoomSensitivity = 2f;
    public float minDistance = 2f;
    public float maxDistance = 10f;

    private float _verticalRotation = 0f;
    private float _currentDistance = 5f;
    
    // Biến lưu trạng thái khóa chuột
    private bool _isCursorLocked = true;

    public override void Spawned()
    {
        if (!HasInputAuthority) return;

        _cameraHolderTransform = transform.Find(cameraHolderName);
        _vcam = FindFirstObjectByType<CinemachineCamera>();

        if (_vcam != null && _cameraHolderTransform != null)
        {
            _vcam.Target.TrackingTarget = _cameraHolderTransform;
            _tpFollow = _vcam.GetComponent<CinemachineThirdPersonFollow>();
            if (_tpFollow != null) _currentDistance = _tpFollow.CameraDistance;

            // Khởi tạo trạng thái ban đầu
            UpdateCursorState();
        }
    }

    // Dùng Update để bắt phím L mượt mà hơn cho UI
    void Update()
    {
        if (!HasInputAuthority) return;

        // --- BẬT/TẮT KHÓA CHUỘT KHI BẤM L ---
        if (Input.GetKeyDown(KeyCode.L))
        {
            _isCursorLocked = !_isCursorLocked;
            UpdateCursorState();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;

        // Nếu chuột đang mở khóa (hiện con trỏ) thì không xoay camera
        if (!_isCursorLocked) return;

        // --- 1. XOAY CHUỘT ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Runner.DeltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Runner.DeltaTime;

        transform.Rotate(Vector3.up * mouseX);

        _verticalRotation -= mouseY;
        _verticalRotation = Mathf.Clamp(_verticalRotation, lowerLookLimit, upperLookLimit);

        if (_cameraHolderTransform != null)
        {
            _cameraHolderTransform.localRotation = Quaternion.Euler(_verticalRotation, 0, 0);
        }

        // --- 2. ZOOM (CAMERA DISTANCE) ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0 && _tpFollow != null)
        {
            _currentDistance -= scroll * zoomSensitivity;
            _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
            _tpFollow.CameraDistance = _currentDistance;
        }
    }

    // Hàm cập nhật trạng thái con trỏ chuột
    private void UpdateCursorState()
    {
        if (_isCursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}