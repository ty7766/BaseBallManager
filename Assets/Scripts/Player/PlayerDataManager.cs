using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    public string PlayerTeamName { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetPlayerTeam(string teamName)
    {
        if (string.IsNullOrEmpty(teamName))
        {
            Debug.LogWarning("[PlayerDataManager] : 팀 이름이 유효하지 않습니다");
            return; 
        }
        PlayerTeamName = teamName;
    }
}
