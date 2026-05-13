using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerTokenUI : MonoBehaviour
{
    public TMP_Text tokenNum;

    void Start()
    {
        if (Player.Instance != null) Player.Instance.tokenChanged += UpdateToken;
        UpdateToken(Player.Instance.spawnerToken);
    }

    void UpdateToken(int amount)
    {
        tokenNum.text = $"token : {amount.ToString()}";
    }
}