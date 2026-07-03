using UnityEngine;

public class CSVParseTest : MonoBehaviour
{
    private CardCSVLoader cardCSVLoader;

    private void Start()
    {
        cardCSVLoader = new CardCSVLoader();
        var hitters = cardCSVLoader.LoadHitters();
        var pitchers = cardCSVLoader.LoadPitchers();
        Debug.Log($"Å¸ÀÚ : {hitters[0].Name}, OVR: {hitters[0].OVR}, ÆÀ: {hitters[0].TeamName}");

        Debug.Log($"Åõ¼ö : {pitchers[0].Name}, OVR: {pitchers[0].OVR}, ÆÀ: {pitchers[0].TeamName}");
    }
}
