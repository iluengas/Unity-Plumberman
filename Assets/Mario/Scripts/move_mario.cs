using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

[RequireComponent(typeof(Rigidbody2D))]
public class move_mario : MonoBehaviour
{

    // Mario State Checks
    private bool isJumping;
    public bool is_mario_grounded { get; private set; }
    public bool jumping { get; private set; }
    public bool running => Mathf.Abs(velocity.x) > 0.25f || Mathf.Abs(inputAxis) > 0.25f;
    public bool sliding => (inputAxis > 0f && velocity.x < 0f) || (inputAxis < 0f && velocity.x > 0f);
    public bool falling => velocity.y < 0f && !is_mario_grounded;

    // Camera / physics Objects
    private new Camera camera;
    private new Rigidbody2D rigidbody;
    private new Collider2D collider;

    // 
    private Vector2 velocity;
    private float inputAxis;

    // Audio sounds
    public AudioSource superJumpAudioSource;
    public AudioSource normalJumpAudioSource;

    // Mario physics variables
    private float moveSpeed = 8f;
    private float maxJumpHeight = 5f;
    private float maxJumpTime = 1f;
    private float jumpForce => (2f * maxJumpHeight) / (maxJumpTime / 2f);
    private float gravity => (-2f * maxJumpHeight) / Mathf.Pow(maxJumpTime / 2f, 2f);

    // finger data thread
    Thread receiveThread;
    UdpClient client;
    public int finger_port = 3737;
    public bool startRecieving = true;
    public bool printToConsole = false;
    public static string data;

    // This function is called before the other functions
    public void Start()
    {
        // start receving the finger thread
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

    }

    // NEW CODE HERE
    private void Awake()
    {
        camera = Camera.main;
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        rigidbody.isKinematic = false;
        collider.enabled = true;
        velocity = Vector2.zero;
        jumping = false;
    }

    private void OnDisable()
    {
        rigidbody.isKinematic = true;
        collider.enabled = false;
        velocity = Vector2.zero;
        jumping = false;
    }

    // this function is called every frame
    private void Update()
    {
        // tells us if plumberman is on the ground 
        is_mario_grounded = rigidbody.Raycast(Vector2.down);
        // change HorizontalMovement() to make more fluid mario movement
        Mario_Horizontal_Movement(data);

        if (is_mario_grounded) {
            // change this function to make more fluid mario movement
            Mario_Grounded_Movement(data);
        }

        // we can tweak this to make Mario feel more floaty
        ApplyGravity();

    }

    // this function is called every period of time
    private void FixedUpdate()
    {
        // move mario based on his velocity
        Vector2 position = rigidbody.position;
        position += velocity * Time.fixedDeltaTime;

        // clamp within the screen bounds
        Vector2 leftEdge = camera.ScreenToWorldPoint(Vector2.zero);
        Vector2 rightEdge = camera.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        position.x = Mathf.Clamp(position.x, leftEdge.x + 0.5f, rightEdge.x - 0.5f);

        rigidbody.MovePosition(position);
    }

    // this is how Unity receives the hand landmarks from our Python handtracking script
    private void ReceiveData()
    {
        using (client = new UdpClient(finger_port))
        {
            while (startRecieving)
            {

                try
                {
                    IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] dataByte = client.Receive(ref anyIP);
                    data = Encoding.UTF8.GetString(dataByte);

                    if (printToConsole)
                    {
                        print(data);
                    }
                }
                catch (Exception err)
                {
                    data = "";
                    print(err.ToString());
                }
            }
        }
    }

    private void Mario_Horizontal_Movement(string data)
    {

        // if thumb sticking out move mario forward
        if(data == "[1, 0, 0, 0, 0]")
        {
            velocity.x = 30 * moveSpeed * Time.deltaTime;
        }

        // if all fingers out stop Mario
        if(data == "[1, 1, 1, 1, 1]")
        {
            velocity.x = 0 * moveSpeed * Time.deltaTime;
        }

        // if pinky out move mario backwards
        if(data == "[0, 0, 0, 0, 1]")
        {
            velocity.x = -30 * moveSpeed * Time.deltaTime;
        }

        // check if mario is running into a wall
        if (rigidbody.Raycast(Vector2.right * velocity.x)) {
            velocity.x = 0f;
        }

        // flip mario sprite to face direction
        if (velocity.x > 0f) {
            transform.eulerAngles = Vector3.zero;
        } else if (velocity.x < 0f) {
            transform.eulerAngles = new Vector3(0f, 180f, 0f);
        }
    }

    private void Mario_Grounded_Movement(string data)
    {
        // prevent gravity from infinitly building up
        velocity.y = Mathf.Max(velocity.y, 0f);
        jumping = velocity.y > 0f;

        // when index finger is up then make mario jump
        if (data == "[0, 1, 0, 0, 0]")
        {
            velocity.y = (jumpForce * 10 / 7);
            jumping = true;
            normalJumpAudioSource.Play();
        }

        // if thumb and index then make mario leap forward
        if (data == "[1, 1, 0, 0, 0]")
        {
            velocity.y = (jumpForce * 10 / 7);
            velocity.x = 60 * moveSpeed * Time.deltaTime;
            jumping = true;
            
            superJumpAudioSource.Play();
        }
        
        // if pinky and index then make mario leap backwards
        if (data == "[0, 1, 0, 0, 1]")
        {
            velocity.y = (jumpForce * 10 / 7);
            velocity.x = -60 * moveSpeed * Time.deltaTime;
            jumping = true;
            
           superJumpAudioSource.Play();
        }
    }

    private void ApplyGravity()
    {
        // check if falling
        bool falling = velocity.y < 0f || !Input.GetButton("Jump");
        float multiplier = falling ? 2f : 1f;

        // apply gravity and terminal velocity
        velocity.y += gravity * multiplier * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, gravity / 2f);
    }

    // checks to see if Mario is colliding an object
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            // bounce off enemy head
            if (transform.DotTest(collision.transform, Vector2.down))
            {
                velocity.y = jumpForce / 2f;
                jumping = true;
            }
        }
        else if (collision.gameObject.layer != LayerMask.NameToLayer("PowerUp"))
        {
            // stop vertical movement if mario bonks his head
            if (transform.DotTest(collision.transform, Vector2.up)) {
                velocity.y = 0f;
            }
        }
    }

}
