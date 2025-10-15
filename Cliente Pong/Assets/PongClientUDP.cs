using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Globalization;

public class PongClientUDP : MonoBehaviour
{
    UdpClient client;
    Thread receiveThread;
    IPEndPoint serverEP;

    public int myId = -1;
    private bool gameStarted = false;

    [Header("Configurações do Servidor")]
    public string serverIP = "127.0.0.1"; // você vai colocar o IP real
    public int serverPort = 5001;

    [Header("Referências do Jogo")]
    public GameObject player1Paddle; // paddle do servidor
    public GameObject player2Paddle; // paddle do cliente (controlável)
    public GameObject ball;
    public GameManager gameManager;

    // Dados recebidos da rede
    private float remotePlayerY = 0f;
    private Vector2 remoteBallPos = Vector2.zero;
    private Vector2 remoteBallVel = Vector2.zero;
    private bool updateRemoteBall = false;

    // Controle de envio
    private float sendRate = 0.03f; // 30 vezes por segundo
    private float lastSendTime = 0f;

    void Start()
    {
        // Conectar ao servidor
        ConnectToServer();
    }

    void ConnectToServer()
    {
        try
        {
            client = new UdpClient();
            serverEP = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
            client.Connect(serverEP);

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // Envia HELLO para se registrar
            SendMessage("HELLO");
            Debug.Log("[CLIENTE] Conectado ao servidor!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[CLIENTE] Erro ao conectar: " + e.Message);
        }
    }

    void Update()
    {
        if (myId == -1 || !gameStarted) return;

        // --- Movimento da raquete do cliente ---
        if (player2Paddle != null)
        {
            float move = Input.GetAxisRaw("Vertical"); // W/S ou ↑/↓
            Vector3 pos = player2Paddle.transform.position;
            pos.y += move * 8f * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, -4.5f, 4.5f);
            player2Paddle.transform.position = pos;
        }

        // Atualiza posição do paddle remoto (paddle do servidor)
        if (player1Paddle != null)
        {
            Vector3 targetPos = player1Paddle.transform.position;
            targetPos.y = remotePlayerY;
            player1Paddle.transform.position = Vector3.Lerp(
                player1Paddle.transform.position,
                targetPos,
                Time.deltaTime * 15f
            );
        }

        // Atualiza posição da bola
        if (ball != null && updateRemoteBall)
        {
            ball.transform.position = Vector3.Lerp(
                ball.transform.position,
                remoteBallPos,
                Time.deltaTime * 10f
            );

            Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = remoteBallVel;
        }

        // Envia posição da raquete do cliente
        SendPaddleData();
    }

    void SendPaddleData()
    {
        if (player2Paddle != null)
        {
            float y = player2Paddle.transform.position.y;
            string msg = $"PADDLE:{myId};{y.ToString("F3", CultureInfo.InvariantCulture)}";
            SendMessage(msg);
        }
    }

    void SendMessage(string message)
    {
        if (client != null)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length);
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
                    Debug.Log($"[CLIENTE] Meu ID = {myId}");
                }
                else if (msg.StartsWith("START"))
                {
                    gameStarted = true;
                    Debug.Log("[CLIENTE] Jogo iniciado!");
                }
                else if (msg.StartsWith("PADDLE:"))
                {
                    string[] parts = msg.Substring(7).Split(';');
                    if (parts.Length >= 2)
                    {
                        int id = int.Parse(parts[0]);
                        float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        if (id != myId)
                        {
                            remotePlayerY = y;
                        }
                    }
                }
                else if (msg.StartsWith("BALL:"))
                {
                    string[] parts = msg.Substring(5).Split(';');
                    if (parts.Length >= 4)
                    {
                        remoteBallPos.x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                        remoteBallPos.y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        remoteBallVel.x = float.Parse(parts[2], CultureInfo.InvariantCulture);
                        remoteBallVel.y = float.Parse(parts[3], CultureInfo.InvariantCulture);
                        updateRemoteBall = true;
                    }
                }
                else if (msg.StartsWith("GOAL:"))
                {
                    string[] parts = msg.Substring(5).Split(';');
                    if (parts.Length >= 2)
                    {
                        int scoringPlayer = int.Parse(parts[0]);
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            if (gameManager != null)
                            {
                                if (scoringPlayer == 1)
                                    gameManager.Player1Scored();
                                else
                                    gameManager.Player2Scored();
                            }
                        });
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CLIENTE] Erro ao receber: " + e.Message);
                break;
            }
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Abort();
        if (client != null)
            client.Close();
    }
}