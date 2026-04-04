using UnityEngine;
using Fusion;

// Cấu trúc dữ liệu để gửi qua mạng
public struct NetworkInputData : INetworkInput
{
    public Vector3 direction; // Hướng di chuyển
    public NetworkBool isFiring; // Trạng thái bấm chuột (bắn)
}
public class GunController : NetworkBehaviour
{
    [Header("Cấu hình Prefab")]
    public NetworkObject bulletPrefab; 
    public Transform firePoint;
    public float bulletForce = 20f;

    [Header("Tốc độ bắn")]
    public float fireRate = 0.2f; // Khoảng cách giữa các viên đạn (giây)
    
    // Biến này để đếm thời gian trên Network (phải dùng [Networked])
    [Networked] private TickTimer shootTimer { get; set; }

    public override void FixedUpdateNetwork()
    {
        // Lấy Input từ NetworkInputData bạn đã tạo
        if (GetInput(out NetworkInputData data))
        {
            // Kiểm tra: 1. Có bấm chuột không? 2. Đã hết thời gian chờ (Cooldown) chưa?
            if (data.isFiring && shootTimer.ExpiredOrNotRunning(Runner))
            {
                // Reset lại bộ đếm thời gian chờ
                shootTimer = TickTimer.CreateFromSeconds(Runner, fireRate);
                
                Shoot();
            }
        }
    }

    void Shoot()
    {
        // Chỉ Spawn trên Server (State Authority) để đảm bảo đồng bộ tuyệt đối
        Runner.Spawn(bulletPrefab, firePoint.position, firePoint.rotation, Object.InputAuthority, (runner, obj) => {
            
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Reset vận tốc cũ nếu có để đạn bay chuẩn hơn
                rb.linearVelocity = Vector3.zero; 
                rb.AddForce(firePoint.forward * bulletForce, ForceMode.Impulse);
            }
        });
    }
}