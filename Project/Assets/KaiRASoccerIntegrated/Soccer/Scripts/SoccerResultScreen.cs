using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SoccerResultScreen : MonoBehaviour
{
    string m_PlayerName;
    string m_Message = "名前を入力してランキングに登録してください（20文字まで）。";
    bool m_Registered;
    bool m_FocusName = true;
    bool m_ClearNameFocus;
    int m_Difficulty;
    int m_PlayerScore;
    int m_OpponentScore;
    Vector2 m_Scroll;
    List<SoccerLeaderboard.Entry> m_Entries;

    void Start()
    {
        m_Difficulty = SoccerLeaderboard.SelectedDifficulty;
        var blue = SoccerEnvController.HumanTeam == Team.Blue;
        m_PlayerScore = blue ? SoccerEnvController.BlueScore : SoccerEnvController.PurpleScore;
        m_OpponentScore = blue ? SoccerEnvController.PurpleScore : SoccerEnvController.BlueScore;
        m_PlayerName = PlayerPrefs.GetString("SoccerLeaderboard.PlayerName", "");
        m_Entries = SoccerLeaderboard.Load(m_Difficulty);
        if (!SoccerEnvController.HasRankingResult)
            m_Message = "登録できる試合結果がありません。";
    }

    void Update()
    {
        if (m_Entries == null) return;
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;
        var back = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        back |= gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        if (back)
        {
            ReturnToStartScreen();
            return;
        }

        var submit = keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
        submit |= gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
        if (submit) Register();
    }

    void ReturnToStartScreen()
    {
        SoccerEnvController.HasRankingResult = false;
        SceneManager.LoadScene("SoccerStartMenu");
    }

    void Register()
    {
        if (m_Registered || !SoccerEnvController.HasRankingResult || string.IsNullOrWhiteSpace(m_PlayerName)) return;
        var rank = SoccerLeaderboard.Register(m_Difficulty, m_PlayerName, m_PlayerScore, m_OpponentScore);
        m_Registered = true;
        SoccerEnvController.HasRankingResult = false;
        m_Message = rank > 0 ? $"{rank}位に登録しました！" : "今回は50位圏外でした。";
        m_Entries = SoccerLeaderboard.Load(m_Difficulty);
        if (rank > 0) m_Scroll.y = (rank - 1) * Mathf.Ceil(44f * SoccerLeaderboard.UiScale);
        m_ClearNameFocus = true;
    }

    void OnGUI()
    {
        if (m_Entries == null) return;
        SoccerLeaderboard.BeginPanel();
        if (m_ClearNameFocus)
        {
            GUI.FocusControl(null);
            m_ClearNameFocus = false;
        }
        var titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(28f * SoccerLeaderboard.UiScale), fontStyle = FontStyle.Bold };
        GUILayout.Label(SoccerEnvController.WinningTeam == Team.Blue ? "BLUE TEAM WINS!" : "PURPLE TEAM WINS!", titleStyle);
        GUILayout.Label($"{SoccerLeaderboard.DifficultyNames[m_Difficulty]}  |  あなた {m_PlayerScore} - {m_OpponentScore} 相手  |  得失点差 {m_PlayerScore - m_OpponentScore:+0;-0;0}");
        GUILayout.Label(m_Message);
        if (!m_Registered && SoccerEnvController.HasRankingResult)
        {
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("PlayerName");
            m_PlayerName = GUILayout.TextField(m_PlayerName, 20, GUILayout.Height(36f * SoccerLeaderboard.UiScale));
            if (m_FocusName)
            {
                GUI.FocusControl("PlayerName");
                m_FocusName = false;
            }
            GUI.enabled = !string.IsNullOrWhiteSpace(m_PlayerName);
            var clicked = GUILayout.Button("登録（Enter / 〇）", GUILayout.Width(210f * SoccerLeaderboard.UiScale), GUILayout.Height(36f * SoccerLeaderboard.UiScale));
            GUI.enabled = true;
            if (clicked)
            {
                Register();
            }
            GUILayout.EndHorizontal();
        }
        SoccerLeaderboard.Draw(m_Entries, ref m_Scroll);
        if (GUILayout.Button("スタート画面へ戻る（Esc / ×）", GUILayout.Height(44f * SoccerLeaderboard.UiScale)))
        {
            ReturnToStartScreen();
        }
        SoccerLeaderboard.EndPanel();
    }
}
