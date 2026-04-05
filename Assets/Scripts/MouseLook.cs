using Fusion;
using Unity.Cinemachine;
using UnityEngine;

public class MouseLook : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private string cameraHolderName = "CameraHolder";
    private Transform _cameraHolderTransform;
    public GameObject spine;
    private CinemachineCamera _vcam;
    private CinemachineThirdPersonFollow _tpFollow;

    [Header("Look Settings")]
    [Networked] public float _networkVerticalRotation { get; set; }
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
        if (!_isCursorLocked) return;

        // Tính toán góc xoay dọc
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Runner.DeltaTime;
        _verticalRotation -= mouseY;
        _verticalRotation = Mathf.Clamp(_verticalRotation, lowerLookLimit, upperLookLimit);

        // Đẩy giá trị lên Network để mọi người cùng nhận được
        _networkVerticalRotation = _verticalRotation;

        // Xoay thân nhân vật (ngang) thì vẫn làm ở đây
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Runner.DeltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // --- 2. ZOOM (CAMERA DISTANCE) ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0 && _tpFollow != null)
        {
            _currentDistance -= scroll * zoomSensitivity;
            _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
            _tpFollow.CameraDistance = _currentDistance;
        }
    }

    void LateUpdate()
    {
        if (spine != null)
        {
            // Sử dụng giá trị từ Network để máy mình và máy khách đều thấy spine xoay
            spine.transform.localRotation = Quaternion.Euler(_networkVerticalRotation - 20, 0, 0);
        }
        
        // Camera Holder thì chỉ cần xoay trên máy mình để nhìn cho chuẩn
        if (HasInputAuthority && _cameraHolderTransform != null)
        {
            _cameraHolderTransform.localRotation = Quaternion.Euler(_verticalRotation, 0, 0);
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