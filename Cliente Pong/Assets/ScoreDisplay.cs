using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    public TMP_Text leftScoreText;
    public TMP_Text rightScoreText;

    public void UpdateScore(int left, int right)
    {
        leftScoreText.text = left.ToString();
        rightScoreText.text = right.ToString();
    }
}