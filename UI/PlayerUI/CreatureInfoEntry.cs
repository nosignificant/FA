using UnityEngine;
using TMPro;

public class CreatureInfoEntry : MonoBehaviour
{
    public TMP_Text nameText;
    //public GameObject learnedMark;
    //public GameObject unknownMark;

    public void SetData(CreatureData data, bool isLearned, bool isCurrent)
    {
        if (isLearned)
        {
            nameText.text = $"{data.creatureName} learned";
            //learnedMark.SetActive(true);
            //unknownMark.SetActive(false);
        }
        else
        {
            nameText.text = "???";
            //learnedMark.SetActive(false);
            //unknownMark.SetActive(true);
        }
    }
}
