using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;


public class CurrentSelected : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text foodCText;
    public TMP_Text enemyCText;
    public TMP_Text friendCText;


    public Image creatureImage;
    // public void SetData(CreatureData data, bool isLearned)
    // {
    //     nameText.text = isLearned ? data.creatureName : "???";
    //     foodCText.text = FormatList(data.foodCreatures);
    //     enemyCText.text = FormatList(data.enemyCreatures);
    //     friendCText.text = FormatList(data.friendCreatures);
    // }

    // private string FormatList(List<CreatureData> list)
    // {
    //     if (list == null || list.Count == 0) return "NONE";
    //     return string.Join(", ", list.Select(c => c.creatureName));
    // }
}
