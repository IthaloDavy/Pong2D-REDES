using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public int player1Score = 0;
    public int player2Score = 0;

    public Text player1Text;
    public Text player2Text;

    public void Player1Scored()
    {
        player1Score++;
        UpdateUI();
    }

    public void Player2Scored()
    {
        player2Score++;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (player1Text) player1Text.text = player1Score.ToString();
        if (player2Text) player2Text.text = player2Score.ToString();
    }
}