using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SoccerEnvController : MonoBehaviour
{
    const int k_ScoreToWin = 10;
    const string k_ResultSceneName = "SoccerResult";

    public static bool HasRankingResult { get; set; }
    public static Team HumanTeam { get; private set; }
    public static Team WinningTeam { get; private set; }
    public static int BlueScore { get; private set; }
    public static int PurpleScore { get; private set; }

    [System.Serializable]
    public class PlayerInfo
    {
        public AgentSoccer Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
    }


    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;

    /// <summary>
    /// The area bounds.
    /// </summary>

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>

    public GameObject ball;
    public bool isPlayerMatch;
    [HideInInspector]
    public Rigidbody ballRb;
    Vector3 m_BallStartingPos;

    //List of Agents On Platform
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    private SoccerSettings m_SoccerSettings;


    private SimpleMultiAgentGroup m_BlueAgentGroup;
    private SimpleMultiAgentGroup m_PurpleAgentGroup;

    private int m_ResetTimer;
    private bool m_MatchFinished;
    private GUIStyle m_ScoreStyle;
    private bool m_IsPaused;
    private float m_TimeScaleBeforePause;

    void Update()
    {
        if (!isPlayerMatch || m_MatchFinished) return;
        var gamepad = Gamepad.current;
        var keyboard = Keyboard.current;
        // DualShock: west = square, south = cross.
        var togglePause = gamepad != null && gamepad.buttonWest.wasPressedThisFrame;
        togglePause |= keyboard != null && keyboard.pKey.wasPressedThisFrame;
        if (togglePause)
        {
            if (m_IsPaused) ResumeMatch();
            else
            {
                m_TimeScaleBeforePause = Time.timeScale;
                m_IsPaused = true;
                Time.timeScale = 0f;
            }
            return;
        }
        var returnToStart = gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        returnToStart |= keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        if (m_IsPaused && returnToStart)
        {
            ReturnToStartMenu();
        }
    }

    void ResumeMatch()
    {
        if (!m_IsPaused) return;
        Time.timeScale = m_TimeScaleBeforePause;
        m_IsPaused = false;
    }

    void ReturnToStartMenu()
    {
        ResumeMatch();
        HasRankingResult = false;
        m_MatchFinished = true;
        SceneManager.LoadScene("SoccerStartMenu");
    }

    void OnDisable()
    {
        // Restore time even if another script unloads the scene while paused.
        ResumeMatch();
    }

    void Start()
    {
        if (isPlayerMatch)
        {
            HasRankingResult = false;
            BlueScore = 0;
            PurpleScore = 0;
            WinningTeam = Team.Blue;
            m_MatchFinished = false;
        }

        m_SoccerSettings = FindFirstObjectByType<SoccerSettings>();
        // Initialize TeamManager
        m_BlueAgentGroup = new SimpleMultiAgentGroup();
        m_PurpleAgentGroup = new SimpleMultiAgentGroup();
        ballRb = ball.GetComponent<Rigidbody>();
        m_BallStartingPos = new Vector3(ball.transform.position.x, ball.transform.position.y, ball.transform.position.z);
        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            if (item.Agent.team == Team.Blue)
            {
                m_BlueAgentGroup.RegisterAgent(item.Agent);
            }
            else
            {
                m_PurpleAgentGroup.RegisterAgent(item.Agent);
            }
        }
        ResetScene();
    }

    void FixedUpdate()
    {
        if (m_IsPaused || m_MatchFinished) return;
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_BlueAgentGroup.GroupEpisodeInterrupted();
            m_PurpleAgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }


    public void ResetBall()
    {
        var randomPosX = Random.Range(-2.5f, 2.5f);
        var randomPosZ = Random.Range(-2.5f, 2.5f);

        ball.transform.position = m_BallStartingPos + new Vector3(randomPosX, 0f, randomPosZ);
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

    }

    public void GoalTouched(Team scoredTeam)
    {
        if (m_MatchFinished || m_IsPaused)
        {
            return;
        }

        if (isPlayerMatch && scoredTeam == Team.Blue)
        {
            BlueScore++;
        }
        else if (isPlayerMatch)
        {
            PurpleScore++;
        }

        if (scoredTeam == Team.Blue)
        {
            m_BlueAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            m_PurpleAgentGroup.AddGroupReward(-1);
        }
        else
        {
            m_PurpleAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            m_BlueAgentGroup.AddGroupReward(-1);
        }
        m_PurpleAgentGroup.EndGroupEpisode();
        m_BlueAgentGroup.EndGroupEpisode();

        if (isPlayerMatch &&
            (BlueScore >= k_ScoreToWin || PurpleScore >= k_ScoreToWin))
        {
            m_MatchFinished = true;
            WinningTeam = scoredTeam;
            HumanTeam = Team.Blue;
            foreach (var player in AgentsList)
            {
                if (player.Agent.humanControlled)
                {
                    HumanTeam = player.Agent.team;
                    break;
                }
            }
            HasRankingResult = true;
            SceneManager.LoadScene(k_ResultSceneName);
            return;
        }

        ResetScene();

    }

    void OnGUI()
    {
        if (!isPlayerMatch)
        {
            return;
        }

        if (m_ScoreStyle == null)
        {
            m_ScoreStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
        }

        GUI.Box(
            new Rect((Screen.width - 300f) * 0.5f, 20f, 300f, 48f),
            $"BLUE  {BlueScore}  -  {PurpleScore}  PURPLE",
            m_ScoreStyle
        );
        var hintScale = Mathf.Clamp(Screen.height / 720f, 0.6f, 3f);
        var hintStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Max(12, Mathf.RoundToInt(18f * hintScale)),
            wordWrap = true
        };
        var hintHeight = 64f * hintScale;
        GUI.Box(new Rect(12f, Screen.height - hintHeight - 12f,
                Mathf.Min(Screen.width - 24f, 560f * hintScale), hintHeight),
            "〇 / Space（押している間）：ダッシュ\n□ / P：一時停止・再開", hintStyle);
        if (m_IsPaused) DrawPauseMenu();
    }

    void DrawPauseMenu()
    {
        var previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;

        var scale = Mathf.Clamp(Screen.height / 720f, 0.6f, 3f);
        var width = Mathf.Min(Screen.width - 24f, 480f * scale);
        var height = Mathf.Min(Screen.height - 24f, 280f * scale);
        var panel = new Rect(Mathf.Round((Screen.width - width) / 2f),
            Mathf.Round((Screen.height - height) / 2f), width, height);
        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(30f * scale),
            fontStyle = FontStyle.Bold
        };
        var buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.Max(14, Mathf.RoundToInt(20f * scale))
        };
        GUILayout.BeginArea(panel, GUI.skin.box);
        GUILayout.Space(16f * scale);
        GUILayout.Label("一時停止", titleStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("□ / P：試合を再開", buttonStyle, GUILayout.Height(52f * scale)))
            ResumeMatch();
        GUILayout.Space(10f * scale);
        if (GUILayout.Button("× / Esc：スタート画面へ戻る", buttonStyle, GUILayout.Height(52f * scale)))
            ReturnToStartMenu();
        GUILayout.Space(16f * scale);
        GUILayout.EndArea();
    }


    public void ResetScene()
    {
        m_ResetTimer = 0;

        //Reset Agents
        foreach (var item in AgentsList)
        {
            var randomPosX = Random.Range(-5f, 5f);
            var newStartPos = item.Agent.initialPos + new Vector3(randomPosX, 0f, 0f);
            var rot = item.Agent.rotSign * Random.Range(80.0f, 100.0f);
            var newRot = Quaternion.Euler(0, rot, 0);
            item.Agent.transform.SetPositionAndRotation(newStartPos, newRot);

            item.Rb.linearVelocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }

        //Reset Ball
        ResetBall();
    }
}
