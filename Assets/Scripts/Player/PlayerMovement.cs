using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private CharacterController characterController;
    private StatsHandler stats;

    [Header("Movement Settings")]
    [HideInInspector]
    [SerializeField] public bool isSprinting;
    public float moveSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravityValue = -9.81f;

    [Header("Stamina cost")]
    public float sprintMultiplier = 1.5f;
    public float jumpStaminaCost = 10f;
    public float sprintStaminaCost = 10f;

    private Vector3 playerVelocity;
    private bool isGrounded;

    public override void Spawned()
    {
        characterController = GetComponent<CharacterController>();
        stats = GetComponent<StatsHandler>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;

        // 1. Kiểm tra trạng thái chạm đất
        isGrounded = characterController.isGrounded;
        if (isGrounded && playerVelocity.y < 0) 
        {
            // Giữ một lực hút nhẹ để nhân vật bám sàn mượt mà
            playerVelocity.y = -1f; 
        }

        // 2. Tính toán tốc độ di chuyển ngang (Sprint/Walk)
        isSprinting = Input.GetKey(KeyCode.LeftShift) && stats.NetworkStamina > 0.1f && !stats.IsExhausted;
        float currSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;

        // 3. Lấy Input di chuyển
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 move = (transform.forward * v + transform.right * h).normalized;

        // 4. Trừ stamina khi chạy
        if (isSprinting && move.magnitude > 0.1f)
        {
            stats.IsUsingStamina = true;
            stats.ConsumingStamina(sprintStaminaCost * Runner.DeltaTime);
        }

        // 5. Logic Nhảy (Jump)
        if (Input.GetButton("Jump") && isGrounded && stats.NetworkStamina >= jumpStaminaCost && !stats.IsExhausted)
        {
            // Công thức tính vận tốc nhảy: v = sqrt(h * -2 * g)
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
            stats.ConsumingStamina(jumpStaminaCost);
        }

        // 6. Áp dụng trọng lực theo thời gian
        // Nếu không có dòng này, nhân vật sẽ không bao giờ rơi xuống
        playerVelocity.y += gravityValue * Runner.DeltaTime;

        // 7. Di chuyển tổng hợp (Gom ngang và dọc vào 1 lần gọi duy nhất)
        // Lưu ý: move * currSpeed là vận tốc ngang, playerVelocity là vận tốc dọc
        Vector3 finalMove = (move * currSpeed) + Vector3.up * playerVelocity.y;
        characterController.Move(finalMove * Runner.DeltaTime);
    }
}