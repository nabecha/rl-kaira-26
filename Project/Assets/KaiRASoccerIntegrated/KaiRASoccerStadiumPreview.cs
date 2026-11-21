using System.Collections.Generic;
using Unity.InferenceEngine;
using Unity.MLAgents.Policies;
using UnityEngine;

/// <summary>Eight live matches rendered with the gameplay characters, lighting and camera angle.</summary>
public class KaiRASoccerStadiumPreview : MonoBehaviour
{
    const int StadiumCount = 8;
    const float Margin = 16f;
    const float HeaderHeight = 32f;
    const int Columns = 2;
    const float FieldGap = 10f;

    [SerializeField] GameObject stadiumPrefab;
    [SerializeField] GameObject blueTeamVisual;
    [SerializeField] GameObject purpleTeamVisual;
    [SerializeField] GameObject gameplayLightPrefab;

    readonly List<AgentSoccer> m_Agents = new List<AgentSoccer>();
    readonly Bounds[] m_StadiumBounds = new Bounds[StadiumCount];
    static readonly string[] ViewNames = { "8面全体", "1", "2", "3", "4", "5", "6", "7", "8" };
    Camera m_Camera;
    Bounds m_FieldBounds;
    ModelAsset m_Model;
    int m_FocusedStadium = -1;
    float m_OriginalShadowDistance;
    bool m_RestoreShadowDistance;
    int m_Width;
    int m_Height;
    static bool s_DemoPaused;
    bool m_IsPaused;
    float m_TimeScaleBeforePause;
    int m_FrameRateBeforePause;
    int m_VSyncBeforePause;

    public bool IsPaused => m_IsPaused;

    public void TogglePause()
    {
        if (m_Camera == null) return;
        s_DemoPaused = !m_IsPaused;
        if (s_DemoPaused) PauseDemo();
        else ResumeDemo();
    }

    void PauseDemo()
    {
        if (m_IsPaused) return;
        m_TimeScaleBeforePause = Time.timeScale;
        m_FrameRateBeforePause = Application.targetFrameRate;
        m_VSyncBeforePause = QualitySettings.vSyncCount;
        m_IsPaused = true;
        // Academy inference and physics both run in FixedUpdate; stop both together.
        Time.timeScale = 0f;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
    }

    void ResumeDemo()
    {
        if (!m_IsPaused) return;
        Time.timeScale = m_TimeScaleBeforePause;
        QualitySettings.vSyncCount = m_VSyncBeforePause;
        Application.targetFrameRate = m_FrameRateBeforePause;
        m_IsPaused = false;
    }

    void OnEnable()
    {
        if (m_Camera != null && s_DemoPaused) PauseDemo();
    }

    void OnDisable()
    {
        // Gameplay must run normally even when entered from a stopped demo.
        ResumeDemo();
    }

    public static float MenuScale => Mathf.Max(0.01f,
        Mathf.Min(Screen.height / 760f, Screen.width * 0.32f / 420f));

    public void SetModel(ModelAsset model)
    {
        if (model == null || m_Model == model) return;
        m_Model = model;
        foreach (var agent in m_Agents)
        {
            var behavior = agent.GetComponent<BehaviorParameters>();
            agent.SetModel(behavior.BehaviorName, model, behavior.InferenceDevice);
        }
    }

    void Start()
    {
        if (stadiumPrefab == null || blueTeamVisual == null ||
            purpleTeamVisual == null || gameplayLightPrefab == null)
        {
            Debug.LogError("Assign the gameplay field, both KaiRA characters and light to the stadium preview.", this);
            enabled = false;
            return;
        }

        // Configure every agent before OnEnable initializes its policy and position.
        var root = new GameObject("Soccer 3D Matches - AI Demo");
        root.transform.SetParent(transform, false);
        root.SetActive(false);
        var spacing = Vector2.zero;
        for (var i = 0; i < StadiumCount; i++)
        {
            var position = new Vector3(i % Columns * spacing.x, 0f, i / Columns * spacing.y);
            var field = Instantiate(stadiumPrefab, position, Quaternion.identity, root.transform);
            field.name = "Stadium " + (i + 1).ToString("00");
            var controller = field.GetComponent<SoccerEnvController>();
            controller.isPlayerMatch = false;

            foreach (var agent in field.GetComponentsInChildren<AgentSoccer>(true))
            {
                agent.humanControlled = false;
                var behavior = agent.GetComponent<BehaviorParameters>();
                if (m_Model != null) behavior.Model = m_Model;
                behavior.BehaviorType = BehaviorType.InferenceOnly;
                m_Agents.Add(agent);
            }
            // The sample's command-line model overrides must not replace the menu selection.
            foreach (var overrider in field.GetComponentsInChildren<Unity.MLAgentsExamples.ModelOverrider>(true))
                overrider.enabled = false;

            var renderers = field.GetComponentsInChildren<Renderer>(true);
            var bounds = new Bounds(position, Vector3.zero);
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            m_StadiumBounds[i] = bounds;
            if (i == 0)
            {
                spacing = new Vector2(bounds.size.x + FieldGap, bounds.size.z + FieldGap);
                m_FieldBounds = bounds;
            }
            else
            {
                m_FieldBounds.Encapsulate(bounds);
            }
        }

        // Use the same character prefabs and visual replacement as the playable match.
        var swapper = root.AddComponent<KaiRAVisualSwapper>();
        swapper.blueTeamVisual = blueTeamVisual;
        swapper.purpleTeamVisual = purpleTeamVisual;

        var cameraObject = new GameObject("Soccer 3D Spectator Camera", typeof(Camera));
        cameraObject.transform.SetParent(root.transform, false);
        m_Camera = cameraObject.GetComponent<Camera>();
        // Match SoccerHumanVsAI's PlayerCam projection and viewing angle.
        m_Camera.transform.rotation = new Quaternion(-0.3894076f, 0.0016253968f, 0.0021645217f, -0.9210616f);
        m_Camera.orthographic = false;
        m_Camera.fieldOfView = 27.2f;
        m_Camera.nearClipPlane = 0.3f;
        m_Camera.clearFlags = CameraClearFlags.SolidColor;
        m_Camera.backgroundColor = new Color(0.46666667f, 0.5647059f, 0.60784316f);
        m_Camera.depth = 10f;
        m_Camera.allowHDR = true;
        m_Camera.allowMSAA = true;
        m_Camera.useOcclusionCulling = false;

        // Preserve the playable scene's light color, direction, intensity and soft shadows.
        Instantiate(gameplayLightPrefab, root.transform);
        m_OriginalShadowDistance = QualitySettings.shadowDistance;
        m_RestoreShadowDistance = true;

        LayoutCamera();
        root.SetActive(true);
        if (s_DemoPaused) PauseDemo();
    }

    void Update()
    {
        if (m_Width != Screen.width || m_Height != Screen.height) LayoutCamera();
    }

    void LayoutCamera()
    {
        m_Width = Screen.width;
        m_Height = Screen.height;
        if (m_Camera == null) return;
        var left = 420f * MenuScale + Margin * 2f;
        var area = new Rect(left, Margin + HeaderHeight,
            Mathf.Max(1f, Screen.width - left - Margin),
            Mathf.Max(1f, Screen.height - Margin * 2f - HeaderHeight));
        m_Camera.rect = new Rect(area.x / Screen.width,
            1f - area.yMax / Screen.height, area.width / Screen.width, area.height / Screen.height);
        var bounds = m_FocusedStadium < 0 ? m_FieldBounds : m_StadiumBounds[m_FocusedStadium];
        var aspect = area.width / area.height;
        var verticalTangent = Mathf.Tan(m_Camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
        var horizontalTangent = verticalTangent * aspect;
        var inverseRotation = Quaternion.Inverse(m_Camera.transform.rotation);
        var distance = 1f;
        // Fit the actual 3D bounds in perspective, including the walls and goal frames.
        for (var corner = 0; corner < 8; corner++)
        {
            var offset = Vector3.Scale(bounds.extents,
                new Vector3((corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
            var local = inverseRotation * offset;
            distance = Mathf.Max(distance,
                Mathf.Abs(local.x) * 1.08f / horizontalTangent - local.z,
                Mathf.Abs(local.y) * 1.08f / verticalTangent - local.z,
                m_Camera.nearClipPlane - local.z);
        }
        m_Camera.transform.position = bounds.center - m_Camera.transform.forward * distance;
        m_Camera.farClipPlane = distance + bounds.extents.magnitude + 30f;
        // The overview camera is farther away; keep shadows visible on the whole array.
        QualitySettings.shadowDistance = Mathf.Max(m_OriginalShadowDistance, m_Camera.farClipPlane);
    }

    void OnDestroy()
    {
        if (m_RestoreShadowDistance) QualitySettings.shadowDistance = m_OriginalShadowDistance;
    }

    void OnGUI()
    {
        if (m_Camera == null) return;
        var previousDepth = GUI.depth;
        GUI.depth = 10; // Draw labels before the model/ranking panels.
        var left = 420f * MenuScale + Margin * 2f;
        var header = new Rect(left, Margin, Screen.width - left - Margin, HeaderHeight);
        var selected = GUI.Toolbar(header, m_FocusedStadium + 1, ViewNames);
        if (selected != m_FocusedStadium + 1)
        {
            m_FocusedStadium = selected - 1;
            LayoutCamera();
        }
        GUI.depth = previousDepth;
    }
}
