using UnityEngine;

public class CSVParseTest : MonoBehaviour
{
    private CardCSVLoader cardCSVLoader;

    private void Start()
    {
        cardCSVLoader = new CardCSVLoader();
        var hitters = cardCSVLoader.LoadHitters();
        var pitchers = cardCSVLoader.LoadPitchers();
        Debug.Log($"타자 : {hitters[0].Name}, OVR: {hitters[0].OVR}, 팀: {hitters[0].TeamName}");

        Debug.Log($"투수 : {pitchers[0].Name}, OVR: {pitchers[0].OVR}, 팀: {pitchers[0].TeamName}");
    }
}
