using UnityEngine;
using System.Net.Sockets;
using System.Text;

public class UdpClientPosition : MonoBehaviour
{
    UdpClient client;
    Vector3 remotePos = Vector3.zero;
    
    void Start()
    {
        client = new UdpClient();
        client.Connect("127.0.0.1", 5001);
    }

    void Update()
    {
        // Movimento local
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical"); 
        transform.Translate(new Vector3(h, v, 0) * Time.deltaTime * 5);

        // Enviar posição
        string msg = transform.position.x + "," + transform.position.y;
        byte[] data = Encoding.UTF8.GetBytes(msg);
        client.Send(data, data.Length);

    }
}