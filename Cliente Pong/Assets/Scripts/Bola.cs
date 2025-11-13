using UnityEngine;
using TMPro;

public class Bola : MonoBehaviour
{
    private Rigidbody2D rb;
    private UdpClientFourPlayers udpClient;
    private bool lancada = false;

    [Header("Pontuação dos Times")]
    public int pontosEsquerda = 0; // Player1 + Player4
    public int pontosDireita = 0;  // Player2 + Player3

    [Header("UI dos Pontos")]
    public TextMeshProUGUI textoEsquerda;
    public TextMeshProUGUI textoDireita;
    public TextMeshProUGUI textoVitoria;

    [Header("Configurações da Bola")]
    public float velocidade = 6f;
    public float fatorDesvio = 2f;
    public int pontuacaoMaxima = 10;

    private bool jogoEncerrado = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        udpClient = FindFirstObjectByType<UdpClientFourPlayers>();
        
        // 🔧 Atualiza UI no início
        AtualizarPontuacao();
        
        if (udpClient != null && udpClient.myId == 1)
            Invoke("LancarBola", 1f);
    }

    void Update()
    {
        if (udpClient == null || jogoEncerrado) return;

        if (!lancada && udpClient.myId == 1)
        {
            lancada = true;
            Invoke("LancarBola", 1f);
        }

        if (udpClient.myId == 1)
        {
            string msg = $"BALL:{transform.position.x.ToString(System.Globalization.CultureInfo.InvariantCulture)};" +
                         $"{transform.position.y.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            udpClient.SendUdpMessage(msg);
        }
    }

    void LancarBola()
    {
        Vector2 dir = Random.insideUnitCircle.normalized;
        rb.linearVelocity = dir * velocidade;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (jogoEncerrado) return;

        // 🔧 Apenas o Player 1 detecta colisões com gols
        if (udpClient == null || udpClient.myId != 1) return;

        if (col.gameObject.CompareTag("Raquete"))
        {
            float posYbola = transform.position.y;
            float posYraquete = col.transform.position.y;
            float alturaRaquete = col.collider.bounds.size.y;
            float diferenca = (posYbola - posYraquete) / (alturaRaquete / 2f);
            Vector2 direcao = new Vector2(Mathf.Sign(rb.linearVelocity.x), diferenca * fatorDesvio);
            rb.linearVelocity = direcao.normalized * velocidade;
        }
        else if (col.gameObject.CompareTag("Gol1"))
        {
            // Gol na esquerda → ponto pro time direito (P2 + P3)
            pontosDireita++;
            Debug.Log($"[Bola] GOL! Time Direito marcou. Placar: {pontosEsquerda} x {pontosDireita}");
            AtualizarPontuacao();
            VerificarVitoria(); // 🔧 DESCOMENTADO
            ResetBola();
        }
        else if (col.gameObject.CompareTag("Gol2"))
        {
            // Gol na direita → ponto pro time esquerdo (P1 + P4)
            pontosEsquerda++;
            Debug.Log($"[Bola] GOL! Time Esquerdo marcou. Placar: {pontosEsquerda} x {pontosDireita}");
            AtualizarPontuacao();
            VerificarVitoria(); // 🔧 DESCOMENTADO
            ResetBola();
        }
    }

    public void AtualizarPontuacao()
    {
        if (textoEsquerda != null)
            textoEsquerda.text = "Time Esquerdo: " + pontosEsquerda;
        
        if (textoDireita != null)
            textoDireita.text = "Time Direito: " + pontosDireita;

        // 🔧 Envia atualização para o servidor
        if (udpClient != null && udpClient.myId == 1)
        {
            string msg = $"SCORE:{pontosEsquerda};{pontosDireita}";
            udpClient.SendUdpMessage(msg);
            Debug.Log($"[Bola] Enviando placar ao servidor: {msg}");
        }
    }

    void VerificarVitoria()
    {
        if (pontosEsquerda >= pontuacaoMaxima)
        {
            jogoEncerrado = true;
            rb.linearVelocity = Vector2.zero;
            if (textoVitoria != null)
                textoVitoria.text = "Time Esquerdo (P1+P4) Venceu!";
            Debug.Log("Time Esquerdo venceu!");
        }
        else if (pontosDireita >= pontuacaoMaxima)
        {
            jogoEncerrado = true;
            rb.linearVelocity = Vector2.zero;
            if (textoVitoria != null)
                textoVitoria.text = "Time Direito (P2+P3) Venceu!";
            Debug.Log("Time Direito venceu!");
        }
    }

    void ResetBola()
    {
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
        
        if (!jogoEncerrado && udpClient != null && udpClient.myId == 1)
            Invoke("LancarBola", 1.5f);
    }
}