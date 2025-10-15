using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Globalization;

public class UdpClientWithID : MonoBehaviour
    {
        UdpClient client; 
        Thread receiveThread;
        IPEndPoint serverEP;
        int myId = -1;
        public GameObject localCube;
        void Start()
        {
            client = new UdpClient(); 
            serverEP = new IPEndPoint(IPAddress.Parse("10.57.1.35"), 5001);
            client.Connect(serverEP); 
            
            // Thread para ouvir respostas do servidor
            receiveThread = new Thread(ReceiveData);
            receiveThread.Start();

            // Envia mensagem inicial para o servidor
            byte[] hello = Encoding.UTF8.GetBytes("HELLO");
            client.Send(hello, hello.Length); 
        }
        void Update()
        { 
            // Movimenta o cubo local
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            localCube.transform.Translate(new Vector3(h, v, 0) * Time.deltaTime * 5); 
            
            // Envia posição formatada
            string msg = "POS:" + localCube.transform.position.x.ToString("F2", CultureInfo.InvariantCulture) + ";" + localCube.transform.position.y.ToString("F2", CultureInfo.InvariantCulture);
            byte[] data = Encoding.UTF8.GetBytes(msg);
            client.Send(data, data.Length);
        }
        void ReceiveData()
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0); 
            while (true)
            {
                byte[] data = client.Receive(ref remoteEP);
                string msg = Encoding.UTF8.GetString(data);
                
                if (msg.StartsWith("ASSIGN:"))
                {
                    myId = int.Parse(msg.Substring(7));
                    Debug.Log("[Cliente] Recebi ID = " + myId); 
                }
            }
        }
        void OnApplicationQuit()
        {
            receiveThread.Abort(); 
            client.Close(); 
        } 
    }