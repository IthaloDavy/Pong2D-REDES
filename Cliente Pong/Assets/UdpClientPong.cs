using System.Collections.Concurrent;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Globalization;

public class UdpClientPong : MonoBehaviour
{
    UdpClient client;
    Thread receiveThread;
    IPEndPoint serverEP;

    public GameObject localCube;
    public GameObject remoteCube;
    public GameObject ball;

    int myId = -1;

    private ConcurrentQueue<Vector3> remotePositionsQueue = new ConcurrentQueue<Vector3>();
    private ConcurrentQueue<Vector3> ballPositionsQueue = new ConcurrentQueue<Vector3>();

    void Start()
    {
        client = new UdpClient(5002);
        serverEP = new IPEndPoint(IPAddress.Parse("10.57.1."), 5001);
        client.Connect(serverEP);

        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();

        byte[] hello = Encoding.UTF8.GetBytes("HELLO");
        client.Send(hello, hello.Length);
    }

    void Update()
    {
        // === Movimento da raquete local ===
        float v = Input.GetAxisRaw("Vertical");
        Vector3 pos = localCube.transform.position;
        pos.y += v * 25f * Time.deltaTime; // velocidade bem maior
        localCube.transform.position = pos;

        // Envia posição para o servidor
        string msg = "POS:" +
            pos.x.ToString("F2", CultureInfo.InvariantCulture) + ";" +
            pos.y.ToString("F2", CultureInfo.InvariantCulture);
        byte[] data = Encoding.UTF8.GetBytes(msg);
        client.Send(data, data.Length);

        // === Atualiza oponente ===
        if (remotePositionsQueue.TryDequeue(out Vector3 remotePos))
        {
            remoteCube.transform.position = remotePos; // sem suavização
        }

        // === Atualiza bola ===
        if (ballPositionsQueue.TryDequeue(out Vector3 ballPos))
        {
            ball.transform.position = ballPos; // sem suavização
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            try
            {
                byte[] data = client.Receive(ref remoteEP);
                string msg = Encoding.UTF8.GetString(data);

                if (msg.StartsWith("ASSIGN:"))
                {
                    myId = int.Parse(msg.Substring(7));
                    Debug.Log("[Cliente] Recebi ID = " + myId);
                }
                else if (msg.StartsWith("PLAYER:"))
                {
                    string[] parts = msg.Split(':');
                    int id = int.Parse(parts[1]);
                    if (id == myId) continue;

                    string[] coords = parts[2].Split(';');
                    float x = float.Parse(coords[0], CultureInfo.InvariantCulture);
                    float y = float.Parse(coords[1], CultureInfo.InvariantCulture);

                    remotePositionsQueue.Enqueue(new Vector3(x, y, 0));
                }
                else if (msg.StartsWith("BALL:"))
                {
                    string[] coords = msg.Substring(5).Split(';');
                    float x = float.Parse(coords[0], CultureInfo.InvariantCulture);
                    float y = float.Parse(coords[1], CultureInfo.InvariantCulture);

                    ballPositionsQueue.Enqueue(new Vector3(x, y, 0));
                }
            }
            catch (SocketException ex)
            {
                Debug.LogWarning("Socket encerrado ou erro: " + ex.Message);
                break;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Erro no cliente: " + ex.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        receiveThread?.Abort();
        client?.Close();
    }
}
