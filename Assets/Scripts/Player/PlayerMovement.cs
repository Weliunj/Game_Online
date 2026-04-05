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
    private bool jumped = false;
    public float gravityValue = -9.81f;

    [Header("Stamina cost")]
    public float sprintMultiplier = 1.5f;
    public float jumpStaminaCost = 10f;
    public float sprintStaminaCost = 10f;

    private Vector3 playerVelocity;
    private bool isGrounded;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    public override void Spawned()
    {
        animator = GetComponentInChildren<Animator>();
        characterController = GetComponent<CharacterController>();
        stats = GetComponent<StatsHandler>();
    }

    public override void FixedUpdateNetwork()
    {
        // 1. Kiểm tra trạng thái chạm đất
        bool wasGrounded = isGrounded; // Lưu lại trạng thái frame trước
        isGrounded = characterController.isGrounded;

        if (isGrounded && playerVelocity.y < 0) 
        {
            // LOGIC MỚI: Nếu frame trước đang bay (vừa nhảy) mà frame này chạm đất
            if (!wasGrounded && jumped)
            {
                jumped = false;
                // Bắt đầu đếm ngược thời gian khựng (ví dụ: 0.5 giây)
                stats.LandingDelayTimer = TickTimer.CreateFromSeconds(Runner, 0.2f);
            }
            playerVelocity.y = -1f; 
        }
        if (stats.IsLandingLocked)
        {
            // Reset các giá trị di chuyển về 0
            stats.NetworkMoveSpeed = 0;
            // Vẫn áp dụng trọng lực để nhân vật không bị lơ lửng nếu đất dốc
            playerVelocity.y += gravityValue * Runner.DeltaTime;
            characterController.Move(playerVelocity * Runner.DeltaTime);
            return; // Thoát hàm, không đọc Input di chuyển bên dưới
        }

        // 2. Lấy Input di chuyển TRƯỚC (Để có dữ liệu tính toán Sprint và Animator)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 move = (transform.forward * v + transform.right * h).normalized;

        // 3. Tính toán trạng thái Sprint
        // Sử dụng move.magnitude > 0.1f để check xem người chơi có đang di chuyển không
        isSprinting = Input.GetKey(KeyCode.LeftShift) && stats.NetworkStamina > 0.1f && !stats.IsExhausted && v > 0.1f;
        float currSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;

        // 4. Trừ stamina khi chạy
        if (isSprinting)
        {
            stats.IsUsingStamina = true;
            stats.ConsumingStamina(sprintStaminaCost * Runner.DeltaTime);
        }

        // 5. Cập nhật giá trị Animator (0: Idle, 1: Walk, 2: Run)
        float moveMagnitude = 0;
        if (move.magnitude > 0.1f) 
        {
            moveMagnitude = isSprinting ? 2f : 1f;
        }
        stats.NetworkMoveSpeed = moveMagnitude;
        
        // 6. Logic Nhảy (Jump)
        if (Input.GetButton("Jump") && isGrounded && stats.NetworkStamina >= jumpStaminaCost && !stats.IsExhausted)
        {
            jumped = true;
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
            stats.ConsumingStamina(jumpStaminaCost);

            //Thông báo cho toàn mạng kích hoạt/thực thi một đoạn code cụ thể.
            RPC_TriggerJumpAnimation();
        }

        // 7. Áp dụng trọng lực
        playerVelocity.y += gravityValue * Runner.DeltaTime;

        // 8. Di chuyển tổng hợp
        Vector3 finalMove = (move * currSpeed) + Vector3.up * playerVelocity.y;
        characterController.Move(finalMove * Runner.DeltaTime);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_TriggerJumpAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Jump");
    }

    public override void Render()
    {
        if (animator != null)
        {
            // Sử dụng giá trị đã được đồng bộ qua mạng để chạy Animation
            // Tham số "Speed" phải khớp với tên trong Animator Blend Tree
            animator.SetFloat("Speed", stats.NetworkMoveSpeed, 0.1f, Time.deltaTime);
            //damp time: thoi gian lam muot
        }
    }
}