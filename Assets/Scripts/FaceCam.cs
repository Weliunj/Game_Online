using UnityEngine;

public class FaceCam : MonoBehaviour
{
    // Biến để lưu tham chiếu đến Camera chính (Local Camera)
    private Transform _mainCameraTransform;

    private void Start()
    {
        // Tìm Camera chính trong cảnh.
        // Trong game Multiplayer, mỗi máy client chỉ có 1 MainCamera hoạt động.
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("Không tìm thấy Main Camera trong Scene! Text sẽ không quay được.");
            enabled = false; // Tắt script nếu không có camera
        }
    }

    // Dùng LateUpdate để đảm bảo Camera đã di chuyển xong trước khi Text quay theo
    private void LateUpdate()
    {
        if (_mainCameraTransform != null)
        {
            transform.LookAt(_mainCameraTransform);
            transform.Rotate(0, 180, 0);
        }
    }
}