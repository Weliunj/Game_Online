using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    public override void FixedUpdateNetwork()
    {
        // Có thể xử lý di chuyển ở đây nếu không dùng Rigidbody
    }

    // Với Fusion, việc hủy Object phải do Server hoặc Runner xử lý
    void OnCollisionEnter(Collision collision)
    {
        // Chỉ máy chủ hoặc người có quyền mới được ra lệnh hủy để tránh xung đột
        if (Object.HasStateAuthority)
        {
            // Runner.Despawn thay thế cho Destroy
            Runner.Despawn(Object);
        }
    }
}