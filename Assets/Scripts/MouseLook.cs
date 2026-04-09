using Fusion;
using Unity.Cinemachine;
using UnityEngine;

public class MouseLook : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private string cameraHolderName = "CameraHolder";
    private Transform _cameraHolderTransform;
    public GameObject spine;
    public CinemachineCamera _vcam;
    private CinemachineThirdPersonFollow _tpFollow;

    [Header("Look Settings")]
    [Networked] public float _networkVerticalRotation { get; set; }
    public float mouseSensitivity = 80f;
    public float upperLookLimit = 55f;
    public float lowerLookLimit = -70f;
    public float POV = 60;

    [Header("Fixed Camera Settings")]
    // Khoảng cách camera cố định, bạn có thể chỉnh con số này trong Inspector
    public float fixedCameraDistance = 1.3f; 

    private float _verticalRotation = 0f;
    private bool _isCursorLocked = true;

    public override void Spawned()
    {
        if (!HasInputAuthority) return;

        _cameraHolderTransform = transform.Find(cameraHolderName);
        _vcam = FindFirstObjectByType<CinemachineCamera>();

        if (_vcam != null && _cameraHolderTransform != null)
        {
            _vcam.Target.TrackingTarget = _cameraHolderTransform;
            _vcam.Lens.FieldOfView = POV;
            
            _tpFollow = _vcam.GetComponent<CinemachineThirdPersonFollow>();
            
            // THIẾT LẬP KHOẢNG CÁCH CỐ ĐỊNH NGAY KHI SPAWN
            if (_tpFollow != null) 
            {
                _tpFollow.CameraDistance = fixedCameraDistance;
            }

            UpdateCursorState();
        }
    }

    void Update()
    {
        if (!HasInputAuthority) return;

        if (Input.GetKeyDown(KeyCode.L))
        {
            SetCursorLocked(!_isCursorLocked);
        }
    }

    public void SetCursorLocked(bool locked)
    {
        _isCursorLocked = locked;
        UpdateCursorState();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;
        if (!_isCursorLocked) return;

        // 1. XOAY CHUỘT DỌC
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Runner.DeltaTime;
        _verticalRotation -= mouseY;
        _verticalRotation = Mathf.Clamp(_verticalRotation, lowerLookLimit, upperLookLimit);

        _networkVerticalRotation = _verticalRotation;

        // 2. XOAY CHUỘT NGANG
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Runner.DeltaTime;
        transform.Rotate(Vector3.up * mouseX);
        
        // ĐÃ XÓA PHẦN CODE ZOOM Ở ĐÂY
    }

    void LateUpdate()
    {
        if (spine != null)
        {
            // Xoay xương sống đồng bộ qua mạng
            spine.transform.localRotation = Quaternion.Euler(_networkVerticalRotation - 20, 0, 0);
        }
        
        if (HasInputAuthority && _cameraHolderTransform != null)
        {
            _cameraHolderTransform.localRotation = Quaternion.Euler(_verticalRotation, 0, 0);
        }
    }

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