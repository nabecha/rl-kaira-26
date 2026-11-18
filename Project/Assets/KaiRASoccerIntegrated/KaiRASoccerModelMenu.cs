using UnityEngine.InputSystem;
using Unity.InferenceEngine;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KaiRASoccerModelMenu : MonoBehaviour
{
    const string k_GameSceneName = "SoccerHumanVsAI";

    GUIStyle m_TitleStyle;
    GUIStyle m_InstructionStyle;
    GUIStyle m_ButtonStyle;
    GUIStyle m_NoteStyle;
    static readonly long[] s_DifficultySteps = { 499706, 1999860, 3999998, 7999989, 18858676 };
    static int RankingMenuIndex => SoccerLeaderboard.DifficultyNames.Length;
    static int ModelMenuIndex => RankingMenuIndex + 1;
    static int MenuItemCount => RankingMenuIndex + 2;
    [SerializeField] ModelAsset[] models;
    [SerializeField] KaiRASoccerStadiumPreview stadiumPreview;
    ModelAsset m_PreviewModel;
    static int s_SelectedModel;
    static ModelAsset s_GameModel;
    bool m_ShowModels;
    Vector2 m_ModelScroll;

    void Awake()
    {
        // Filter before navigation so mouse and controller see the same options.
        models = System.Array.FindAll(models ?? new ModelAsset[0],
            model => model != null && TryGetSteps(model.name, out _));
        System.Array.Sort(models, (a, b) =>
        {
            TryGetSteps(a.name, out var aSteps);
            TryGetSteps(b.name, out var bSteps);
            return aSteps.CompareTo(bSteps);
        });
        s_SelectedModel = Mathf.Clamp(s_SelectedModel, 0, Mathf.Max(0, models.Length - 1));
        m_SelectedMenuItem = Mathf.Clamp(SoccerLeaderboard.SelectedDifficulty, 0, s_DifficultySteps.Length - 1);
        SelectDifficulty(m_SelectedMenuItem);
    }

    bool SelectDifficulty(int difficulty)
    {
        var steps = s_DifficultySteps[difficulty];
        var modelIndex = System.Array.FindIndex(models,
            model => TryGetSteps(model.name, out var modelSteps) && modelSteps == steps);
        if (modelIndex < 0)
        {
            Debug.LogError($"レベル{difficulty + 1}のモデル SoccerTwos-{steps} が見つかりません。", this);
            return false;
        }

        m_SelectedDifficulty = difficulty;
        s_SelectedModel = modelIndex;
        UpdatePreviewModel();
        return true;
    }

    void UpdatePreviewModel()
    {
        if (stadiumPreview == null || models.Length == 0) return;
        var model = models[s_SelectedModel];
        if (m_PreviewModel == model) return;
        m_PreviewModel = model;
        stadiumPreview.SetModel(model);
    }

    static bool TryGetSteps(string name, out long steps)
    {
        var separator = name.LastIndexOf('-');
        steps = 0;
        return separator >= 0 && long.TryParse(name.Substring(separator + 1), out steps) && steps > 0;
    }

    static string ModelLabel(ModelAsset model)
    {
        if (!TryGetSteps(model.name, out var steps)) return model.name;
        var value = steps >= 1000000 ? steps / 1000000d : steps / 1000d;
        var suffix = steps >= 1000000 ? "M" : "K";
        return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + suffix + " ステップ";
    }

    static void ApplyModel(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= ApplyModel;
        if (s_GameModel == null || scene.name != k_GameSceneName) return;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var agent in root.GetComponentsInChildren<AgentSoccer>())
        {
            if (agent.humanControlled) continue;
            var behavior = agent.GetComponent<BehaviorParameters>();
            agent.SetModel(behavior.BehaviorName, s_GameModel, behavior.InferenceDevice);
            behavior.BehaviorType = BehaviorType.InferenceOnly;
        }
    }

    void DrawModels()
    {
        var width = Mathf.Min(Screen.width * 0.94f, 700f);
        var height = Screen.height * 0.9f;
        GUILayout.BeginArea(new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height), GUI.skin.box);
        GUILayout.Label("デモ用の学習済みモデルを選択", m_InstructionStyle);
        GUILayout.Label("↑↓ / W・S / 左スティック：選択　Enter / 〇：決定　Esc / ×：戻る", m_NoteStyle);
        m_ModelScroll = GUILayout.BeginScrollView(m_ModelScroll);
        for (var i = 0; i < models.Length; i++)
        {
            if (models[i] == null) continue;
            if (GUILayout.Button((s_SelectedModel == i ? "▶ " : "") + ModelLabel(models[i]), GUILayout.Height(38f)))
            {
                s_SelectedModel = i;
                m_ShowModels = false;
            }
        }
        GUILayout.EndScrollView();
        GUILayout.Label("選択したモデルをデモのAI全員に適用します。\n対戦ではレベルごとのモデルを使用します。\nK = 千ステップ ／ M = 百万ステップ（表示は小数第2位まで）", m_NoteStyle);
        if (GUILayout.Button("決定 / 戻る", GUILayout.Height(38f))) m_ShowModels = false;
        GUILayout.EndArea();
    }
    int m_SelectedMenuItem;
    int m_SelectedDifficulty;
    bool m_ShowRanking;
    Vector2 m_RankingScroll;
    System.Collections.Generic.List<SoccerLeaderboard.Entry> m_Ranking;
    bool m_StickNavigationReady = true;
    int m_RankingScrollDirection;
    int m_RankingStickDirection;
    float m_NextRankingScrollTime;

    void Update()
    {
        UpdatePreviewModel();
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;
        if (m_ShowRanking)
        {
            UpdateRankingInput(keyboard, gamepad);
            return;
        }
        var direction = 0;

        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
            {
                direction = 1;
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
            {
                direction = -1;
            }
        }

        if (gamepad != null)
        {
            if (gamepad.dpad.up.wasPressedThisFrame)
            {
                direction = 1;
            }
            else if (gamepad.dpad.down.wasPressedThisFrame)
            {
                direction = -1;
            }

            var stickY = gamepad.leftStick.ReadValue().y;
            if (Mathf.Abs(stickY) < 0.4f)
            {
                m_StickNavigationReady = true;
            }
            else if (m_StickNavigationReady && Mathf.Abs(stickY) >= 0.6f)
            {
                direction = stickY > 0f ? 1 : -1;
                m_StickNavigationReady = false;
            }
        }

        if (m_ShowModels)
        {
            if (direction != 0 && models.Length > 0)
            {
                s_SelectedModel = (s_SelectedModel - direction + models.Length) % models.Length;
                m_ModelScroll.y = Mathf.Max(0f, s_SelectedModel * 42f - Screen.height * 0.25f);
            }
            if ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)) ||
                (gamepad != null && (gamepad.buttonEast.wasPressedThisFrame || gamepad.buttonSouth.wasPressedThisFrame)))
                m_ShowModels = false;
            return;
        }

        if (direction != 0)
        {
            m_SelectedMenuItem =
                (m_SelectedMenuItem - direction + MenuItemCount) % MenuItemCount;
            if (m_SelectedMenuItem < RankingMenuIndex)
            {
                SelectDifficulty(m_SelectedMenuItem);
            }
        }

        var submit =
            keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame ||
             keyboard.numpadEnterKey.wasPressedThisFrame ||
             keyboard.spaceKey.wasPressedThisFrame);
        // DualShock: east = circle (confirm), south = cross (back).
        submit |= gamepad != null && gamepad.buttonEast.wasPressedThisFrame;

        if (submit)
        {
            if (m_SelectedMenuItem == ModelMenuIndex)
            {
                m_ShowModels = true;
            }
            else if (m_SelectedMenuItem == RankingMenuIndex)
            {
                OpenRanking();
            }
            else
            {
                m_SelectedDifficulty = m_SelectedMenuItem;
                StartGame();
            }
        }
    }

    void OnGUI()
    {
        EnsureStyles();
        if (m_ShowModels) { DrawModels(); return; }
        if (m_ShowRanking)
        {
            SoccerLeaderboard.BeginPanel();
            var rankingTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(28f * SoccerLeaderboard.UiScale),
                fontStyle = FontStyle.Bold
            };
            GUILayout.Label("レベル別ランキング", rankingTitleStyle);
            var selected = GUILayout.Toolbar(m_SelectedDifficulty, SoccerLeaderboard.DifficultyTabNames);
            if (selected != m_SelectedDifficulty)
            {
                SelectRankingDifficulty(selected);
            }
            GUILayout.Label(SoccerLeaderboard.DifficultyNames[m_SelectedDifficulty]);
            SoccerLeaderboard.Draw(m_Ranking, ref m_RankingScroll);
            GUILayout.Label("十字キー / 左スティック：左右でレベル・上下でスクロール   × / Esc：戻る");
            if (GUILayout.Button("戻る（× / Esc）", GUILayout.Height(44f * SoccerLeaderboard.UiScale))) m_ShowRanking = false;
            SoccerLeaderboard.EndPanel();
            return;
        }

        var previousMatrix = GUI.matrix;
        var menuScale = KaiRASoccerStadiumPreview.MenuScale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(menuScale, menuScale, 1f));
        const float panelWidth = 420f;
        const float panelHeight = 700f;
        var panel = new Rect(
            16f / menuScale,
            (Screen.height / menuScale - panelHeight) * 0.5f,
            panelWidth,
            panelHeight
        );

        GUI.Box(panel, GUIContent.none);

        var padding = panelWidth * 0.1f;
        var contentWidth = panelWidth - padding * 2f;
        var buttonHeight = Mathf.Clamp(panelHeight * 0.075f, 36f, 54f);
        var buttonSpacing = panelHeight * 0.015f;
        var y = panel.y + panelHeight * 0.06f;

        GUI.Label(
            new Rect(panel.x + padding, y, contentWidth, panelHeight * 0.12f),
            "進化するAIサッカー",
            m_TitleStyle
        );
        y += panelHeight * 0.14f;

        GUI.Label(
            new Rect(panel.x + padding, y, contentWidth, panelHeight * 0.07f),
            "レベルを選択してください",
            m_InstructionStyle
        );
        y += panelHeight * 0.09f;

        for (var i = 0; i < SoccerLeaderboard.DifficultyNames.Length; i++)
        {
            DrawDifficultyButton(panel, padding, contentWidth, buttonHeight, y,
                SoccerLeaderboard.DifficultyNames[i], i);
            y += buttonHeight + buttonSpacing;
        }

        var originalColor = GUI.backgroundColor;
        if (m_SelectedMenuItem == RankingMenuIndex)
        {
            GUI.backgroundColor = new Color(0.65f, 0.85f, 1f);
        }
        if (GUI.Button(new Rect(panel.x + padding, y, contentWidth, 30f),
            m_SelectedMenuItem == RankingMenuIndex ? "▶ ランキングを見る" : "ランキングを見る"))
        {
            m_SelectedMenuItem = RankingMenuIndex;
            OpenRanking();
        }
        GUI.backgroundColor = originalColor;
        y += 34f;
        GUI.backgroundColor = m_SelectedMenuItem == ModelMenuIndex ? new Color(0.65f, 0.85f, 1f) : originalColor;
        var modelName = models.Length > 0 && models[s_SelectedModel] != null ? ModelLabel(models[s_SelectedModel]) : "モデルなし";
        if (GUI.Button(new Rect(panel.x + padding, y, contentWidth, 36f), "デモのモデル：" + modelName))
        {
            m_SelectedMenuItem = ModelMenuIndex;
            m_ShowModels = true;
        }
        GUI.backgroundColor = originalColor;
        y += 40f;
        GUI.Label(
            new Rect(panel.x + padding, y, contentWidth, panelHeight * 0.12f),
            "↑↓ / W・S / コントローラーで選択\nEnter・Space / 〇で決定\nレベルに応じたAIと対戦します",
            m_NoteStyle
        );
        GUI.matrix = previousMatrix;
    }

    void EnsureStyles()
    {
        if (m_TitleStyle != null)
        {
            return;
        }

        m_TitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 32,
            fontStyle = FontStyle.Bold
        };
        m_InstructionStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22
        };
        m_ButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
        m_NoteStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            wordWrap = true
        };
    }

    void DrawDifficultyButton(
        Rect panel,
        float padding,
        float contentWidth,
        float buttonHeight,
        float y,
        string label,
        int difficultyIndex
    )
    {
        var originalColor = GUI.backgroundColor;
        if (difficultyIndex == m_SelectedMenuItem)
        {
            GUI.backgroundColor = new Color(0.65f, 0.85f, 1f);
        }

        if (GUI.Button(
            new Rect(panel.x + padding, y, contentWidth, buttonHeight),
            difficultyIndex == m_SelectedMenuItem ? "▶ " + label : label,
            m_ButtonStyle
        ))
        {
            m_SelectedMenuItem = difficultyIndex;
            m_SelectedDifficulty = difficultyIndex;
            StartGame();
        }

        GUI.backgroundColor = originalColor;
    }

    void SelectRankingDifficulty(int difficulty)
    {
        m_SelectedDifficulty = difficulty;
        m_Ranking = SoccerLeaderboard.Load(difficulty);
        m_RankingScroll = Vector2.zero;
        m_RankingScrollDirection = 0;
    }

    void UpdateRankingInput(Keyboard keyboard, Gamepad gamepad)
    {
        var back = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        back |= gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        if (back)
        {
            m_ShowRanking = false;
            m_SelectedMenuItem = RankingMenuIndex;
            m_RankingScrollDirection = 0;
            m_StickNavigationReady = false;
            return;
        }

        var previousStickDirection = m_RankingStickDirection;
        var stick = gamepad != null ? gamepad.leftStick.ReadValue() : Vector2.zero;
        // Use the dominant axis so a diagonal tilt does not switch tabs and scroll together.
        // Separate engage/release thresholds prevent drift and jitter near the dead zone.
        var magnitude = Mathf.Max(Mathf.Abs(stick.x), Mathf.Abs(stick.y));
        if (magnitude < 0.4f)
        {
            m_RankingStickDirection = 0;
        }
        else if (magnitude >= 0.6f)
        {
            m_RankingStickDirection = Mathf.Abs(stick.x) > Mathf.Abs(stick.y)
                ? (stick.x > 0f ? 1 : -1)
                : (stick.y > 0f ? 2 : -2);
        }
        var stickChanged = m_RankingStickDirection != previousStickDirection;

        var left = keyboard != null && keyboard.leftArrowKey.wasPressedThisFrame;
        var right = keyboard != null && keyboard.rightArrowKey.wasPressedThisFrame;
        left |= gamepad != null && gamepad.dpad.left.wasPressedThisFrame;
        right |= gamepad != null && gamepad.dpad.right.wasPressedThisFrame;
        left |= stickChanged && m_RankingStickDirection == -1;
        right |= stickChanged && m_RankingStickDirection == 1;
        if (left != right)
        {
            var count = SoccerLeaderboard.DifficultyNames.Length;
            SelectRankingDifficulty((m_SelectedDifficulty + (right ? 1 : -1) + count) % count);
            return;
        }

        var up = keyboard != null && keyboard.upArrowKey.isPressed;
        var down = keyboard != null && keyboard.downArrowKey.isPressed;
        up |= gamepad != null && gamepad.dpad.up.isPressed;
        down |= gamepad != null && gamepad.dpad.down.isPressed;
        up |= m_RankingStickDirection == 2;
        down |= m_RankingStickDirection == -2;
        var direction = up == down ? 0 : (down ? 1 : -1);
        if (direction == 0)
        {
            m_RankingScrollDirection = 0;
            return;
        }

        var initialPress = direction != m_RankingScrollDirection;
        if (initialPress || Time.unscaledTime >= m_NextRankingScrollTime)
        {
            var rowHeight = Mathf.Ceil(44f * SoccerLeaderboard.UiScale);
            m_RankingScroll.y = Mathf.Clamp(m_RankingScroll.y + direction * rowHeight,
                0f, SoccerLeaderboard.Capacity * rowHeight);
            m_NextRankingScrollTime = Time.unscaledTime + (initialPress ? 0.35f : 0.09f);
        }
        m_RankingScrollDirection = direction;
    }

    void OpenRanking()
    {
        m_ShowRanking = true;
        m_RankingStickDirection = 0;
        SelectRankingDifficulty(m_SelectedDifficulty);
    }

    void StartGame()
    {
        if (!SelectDifficulty(m_SelectedDifficulty)) return;
        s_GameModel = models[s_SelectedModel];
        SceneManager.sceneLoaded -= ApplyModel;
        SceneManager.sceneLoaded += ApplyModel;
        SoccerLeaderboard.SelectedDifficulty = m_SelectedDifficulty;
        SceneManager.LoadScene(k_GameSceneName);
    }
}
