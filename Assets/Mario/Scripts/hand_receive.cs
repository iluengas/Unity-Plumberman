using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class hand_receive : MonoBehaviour
{
    // hand data thread
    Thread receiveThread2;
    UdpClient client;
    public int finger_port = 5757;
    public bool startRecieving = true;
    public bool printToConsole = false;
    public static string data;

    // game objects
    public GameObject[] hand_points;
    public GameObject mario_;

    // NEW CODE
    public Material lineMaterial; // The material to use for the line renderer
    public Vector2[] linePoints; // The points of the line

    // This function is called before the other functions
    public void Start()
    {
        // start receving the finger thread
        receiveThread2 = new Thread(new ThreadStart(ReceiveData));
        receiveThread2.IsBackground = true;
        receiveThread2.Start();

        // NEW CODE
        // Create a new GameObject with a LineRenderer component
        GameObject lineObject = new GameObject("Line");
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        // Set the material for the line renderer
        lineRenderer.material = lineMaterial;

        // Set the number of points in the line
        lineRenderer.positionCount = linePoints.Length;

    }
    void Update()
    {
        if(!string.IsNullOrEmpty(data)){
            print(data);
            
            if (data.Length < 0 || !data.Contains("[") || !data.Contains("]"))
            {
                return;
            }
            
            data = data.Remove(0, 1);
            data = data.Remove(data.Length - 1, 1);
            string[] points = data.Split(',');
            print(points[0]);
            
            float ymax = 11f;
            float ymin = -5f;
            // this move all the game object balls to the landmarks of the players hand
            for (int i = 0; i < 21; i++)
            {
                // this gets mario's position
                Vector3 mario_position = mario_.transform.position;
                // we have to remember points looks like this x1 y1 z1 x2 y2 z2...
                float x = float.Parse(points[i * 3])/80-8;
                float y = float.Parse(points[i * 3 + 1])/80-5;
                float z = 0;
                
                // Constrain y value between minY and maxY
                if (y < ymin) {
                    y = ymin;
                } else if (y > ymax) {
                    y = ymax;
                }

                Vector3 hand_position = mario_position;
                hand_position.y = 0;
                hand_points[i].transform.localPosition = new Vector3(x, y, z) + hand_position - new Vector3(20, 5, 0);
                // Calculate the target position based on elapsed time
                // Vector3 targetPosition = new Vector3(x, y, z) + mario_position - new Vector3(25, 11, 0);
                // Vector3 currentPosition = hand_points[i].transform.localPosition;
                // Vector3 newPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime *10f);
        
                // Update the position of the hand point
                //hand_points[i].transform.localPosition = newPosition;
            }
        }
    }

    // this is how Unity receives the hand landmarks from our Python handtracking script
    private void ReceiveData()
    {

        using (client = new UdpClient(finger_port)) {
            while (startRecieving)
            {
                try
                {
                    IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] dataByte = client.Receive(ref anyIP);
                    data = Encoding.UTF8.GetString(dataByte);
                    if (true) { print(data); }
                }
                catch (Exception err)
                {
                    print(err.ToString());
                }
            }
        }
    }
}
