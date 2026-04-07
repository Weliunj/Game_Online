using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private CharacterController characterController;
    private StatsHandler stats;

    [Header("Movement Settings")]
    [HideInInspector]
    [SerializeField] public bool isSprinting;
    [SerializeField] public bool isCrouching;
    public float moveSpeed = 5f;
    public float jumpHeight = 2f;
    private bool jumped = false;
    public float gravityValue = -9.81f;

    [Header("Stamina cost")]
    public float sprintMultiplier = 1.5f;
    public float jumpStaminaCost = 15f;
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

        if (stats.IsDead)
        {
            if (!isGrounded)
            {
                playerVelocity.y += gravityValue * Runner.DeltaTime;
            }
            else if (playerVelocity.y < 0f)
            {
                playerVelocity.y = -1f;
            }

            characterController.Move(Vector3.up * playerVelocity.y * Runner.DeltaTime);
            return;
        }

        // 2. Lấy Input di chuyển TRƯỚC (Để có dữ liệu tính toán Sprint và Animator)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 move = (transform.forward * v + transform.right * h).normalized;

        // 3. LOGIC NGỒI (CROUCH)
        // Giữ phím LeftControl để ngồi
        bool wantToCrouch = Input.GetKey(KeyCode.LeftControl);
        stats.IsCrouching = wantToCrouch;

        // 4. TÍNH TOÁN TỐC ĐỘ
        var combat = GetComponent<PlayerCombat>();
        bool isZooming = combat != null && combat.IsZooming;
        isSprinting = Input.GetKey(KeyCode.LeftShift) && stats.NetworkStamina > 0.1f && !stats.IsExhausted && v > 0.1f && !stats.IsCrouching && !isZooming;
        
        float speedMod = 1f;
        if (stats.IsCrouching) speedMod = 0.7f; // Đi chậm khi ngồi
        else if (isSprinting) 
        {
            speedMod = sprintMultiplier;
            stats.IsUsingStamina = true; 
            stats.ConsumingStamina(sprintStaminaCost * Runner.DeltaTime);
        }

        float currSpeed = moveSpeed * speedMod;

        // 5. CẬP NHẬT BIẾN ANIMATOR MẠNG
        float moveMagnitude = (move.magnitude > 0.1f) ? (isSprinting ? 2f : 1f) : 0f;
        stats.NetworkMoveSpeed = moveMagnitude;

        //Crouch move
        float crouchMoveVal = (stats.IsCrouching && move.magnitude > 0.1f) ? 1f : 0f;
        stats.NetworkCrouchMove = crouchMoveVal;
        
        // 6. NHẢY & TRỌNG LỰC (Giữ nguyên, lưu ý: thường không cho nhảy khi đang ngồi)
        if (Input.GetButton("Jump") && isGrounded && !stats.IsCrouching && !isZooming && stats.NetworkStamina >= jumpStaminaCost)
        {
            jumped = true;
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
            stats.ConsumingStamina(jumpStaminaCost);

            //Thông báo cho toàn mạng kích hoạt/thực thi một đoạn code cụ thể.
            RPC_TriggerJumpAnimation();
        }

        // 7. DI CHUYỂN TỔNG HỢP
        playerVelocity.y += gravityValue * Runner.DeltaTime;
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
            // Đồng bộ các thông số Animator
            animator.SetBool("isCrouching", stats.IsCrouching);
            
            // Speed dành cho chạy/đi bộ bình thường
            animator.SetFloat("Speed", stats.NetworkMoveSpeed, 0.1f, Time.deltaTime);
            
            // CrouchMove: 0 là Idle ngồi, 1 là Walk ngồi (Sử dụng Float để dùng Blend Tree)
            animator.SetFloat("CrouchMove", stats.NetworkCrouchMove, 0.1f, Time.deltaTime);
        }
    }
}