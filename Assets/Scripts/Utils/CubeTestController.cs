using UnityEngine;

/// <summary>
/// 测试方块控制器 - 键盘控制
/// Player1: WASD + Space
/// Player2: 方向键 + Enter
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CubeTestController : MonoBehaviour
{
    [Header("控制配置")]
    public int playerIndex = 0; // 0 = Player1, 1 = Player2
    public float moveSpeed = 5f;
    public float jumpForce = 8f;

    private Rigidbody rb;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
    }

    private void Update()
    {
        // 地面检测
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.6f);
        Debug.DrawRay(transform.position, Vector3.down * 0.6f, isGrounded ? Color.green : Color.red);
    }

    private void FixedUpdate()
    {
        float horizontal = 0f;
        float vertical = 0f;
        bool jump = false;

        if (playerIndex == 0)
        {
            // Player1: WASD + Space
            if (Input.GetKey(KeyCode.D)) horizontal = 1f;
            else if (Input.GetKey(KeyCode.A)) horizontal = -1f;

            if (Input.GetKey(KeyCode.W)) vertical = 1f;
            else if (Input.GetKey(KeyCode.S)) vertical = -1f;

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
                jump = true;
        }
        else
        {
            // Player2: 方向键 + Enter
            if (Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;
            else if (Input.GetKey(KeyCode.LeftArrow)) horizontal = -1f;

            if (Input.GetKey(KeyCode.UpArrow)) vertical = 1f;
            else if (Input.GetKey(KeyCode.DownArrow)) vertical = -1f;

            if (Input.GetKeyDown(KeyCode.Return) && isGrounded)
                jump = true;
        }

        // 移动
        Vector3 velocity = new Vector3(horizontal * moveSpeed, rb.velocity.y, vertical * moveSpeed);
        rb.velocity = velocity;

        // 跳跃
        if (jump)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.name.Contains("Ground"))
        {
            isGrounded = true;
        }
    }
}