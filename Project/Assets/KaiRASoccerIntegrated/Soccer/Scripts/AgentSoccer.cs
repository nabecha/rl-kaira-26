using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public enum Team
{
    Blue = 0,
    Purple = 1
}

public class AgentSoccer : Agent
{
    // 人間操作するAgentだけInspectorでON
    public bool humanControlled = false;

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

    [HideInInspector]
    public Team team;

    float m_KickPower;
    float m_BallTouch;

    public Position position;

    const float k_Power = 2000f;
    const float k_HumanSlowMoveMultiplier = 0.4f;

    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    [HideInInspector]
    public Rigidbody agentRb;

    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;

    public Vector3 initialPos;
    public float rotSign;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        SoccerEnvController envController =
            GetComponentInParent<SoccerEnvController>();

        if (envController != null)
        {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        m_BehaviorParameters =
            gameObject.GetComponent<BehaviorParameters>();

        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;

            initialPos = new Vector3(
                transform.position.x - 5f,
                .5f,
                transform.position.z
            );

            rotSign = 1f;
        }
        else
        {
            team = Team.Purple;

            initialPos = new Vector3(
                transform.position.x + 5f,
                .5f,
                transform.position.z
            );

            rotSign = -1f;
        }

        // AI用の移動速度
        if (position == Position.Goalie)
        {
            m_LateralSpeed = 1.0f;
            m_ForwardSpeed = 1.0f;
        }
        else if (position == Position.Striker)
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.3f;
        }
        else
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.0f;
        }

        m_SoccerSettings =
            FindFirstObjectByType<SoccerSettings>();

        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        m_ResetParams =
            Academy.Instance.EnvironmentParameters;
    }

    // ==================================================
    // AI用の既存移動処理
    // ==================================================

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_KickPower = 0f;

        var forwardAxis = act[0];
        var rightAxis = act[1];
        var rotateAxis = act[2];

        switch (forwardAxis)
        {
            case 1:
                dirToGo =
                    transform.forward * m_ForwardSpeed;

                m_KickPower = 1f;
                break;

            case 2:
                dirToGo =
                    transform.forward * -m_ForwardSpeed;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo =
                    transform.right * m_LateralSpeed;
                break;

            case 2:
                dirToGo =
                    transform.right * -m_LateralSpeed;
                break;
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir =
                    transform.up * -1f;
                break;

            case 2:
                rotateDir =
                    transform.up * 1f;
                break;
        }

        transform.Rotate(
            rotateDir,
            Time.deltaTime * 100f
        );

        agentRb.AddForce(
            dirToGo *
            m_SoccerSettings.agentRunSpeed,
            ForceMode.VelocityChange
        );
    }

    // ==================================================
    // 人間専用の移動処理
    // ==================================================

    void MoveHumanAgent()
    {
        Vector3 dirToGo = Vector3.zero;
        var gamepad = Gamepad.current;

        m_KickPower = 0f;

        /*
         * 十字キーの対応
         *
         * ↑ → 上
         * ↓ → 下
         * ← → 左
         * → → 右
         */

        // ↑キー → 上
        if (Input.GetKey(KeyCode.UpArrow) ||
            (gamepad != null && gamepad.dpad.up.isPressed))
        {
            dirToGo += Vector3.forward;
        }

        // ↓キー → 下
        if (Input.GetKey(KeyCode.DownArrow) ||
            (gamepad != null && gamepad.dpad.down.isPressed))
        {
            dirToGo += Vector3.back;
        }

        // ←キー → 左
        if (Input.GetKey(KeyCode.LeftArrow) ||
            (gamepad != null && gamepad.dpad.left.isPressed))
        {
            dirToGo += Vector3.left;
        }

        // →キー → 右
        if (Input.GetKey(KeyCode.RightArrow) ||
            (gamepad != null && gamepad.dpad.right.isPressed))
        {
            dirToGo += Vector3.right;
        }

        if (gamepad != null)
        {
            var stickInput = gamepad.leftStick.ReadValue();
            dirToGo += new Vector3(
                stickInput.x,
                0f,
                stickInput.y
            );
        }

        // 通常は低速移動、〇またはSpaceを押している間はダッシュ。
        var moveSpeedMultiplier =
            (gamepad != null && gamepad.buttonEast.isPressed) ||
            (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
                ? 1f
                : k_HumanSlowMoveMultiplier;

        if (dirToGo != Vector3.zero)
        {
            // 移動中にボールへ接触すると蹴る
            m_KickPower = 1f;

            // 斜め入力時も最大速度を超えず、スティックの倒し具合を保つ
            dirToGo = Vector3.ClampMagnitude(dirToGo, 1f);
        }

        /*
         * 人間側では
         * m_LateralSpeed / m_ForwardSpeed を使わない。
         *
         * そのため上下左右すべて同じ速度になる。
         */
        agentRb.AddForce(
            dirToGo *
            m_SoccerSettings.agentRunSpeed * moveSpeedMultiplier,
            ForceMode.VelocityChange
        );
    }

    // ==================================================
    // Actionを受け取る
    // ==================================================

    public override void OnActionReceived(
        ActionBuffers actionBuffers)
    {
        if (position == Position.Goalie)
        {
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            AddReward(-m_Existential);
        }

        // 人間
        if (humanControlled)
        {
            MoveHumanAgent();
        }

        // AI
        else
        {
            MoveAgent(
                actionBuffers.DiscreteActions
            );
        }
    }

    // ==================================================
    // Heuristic
    // ==================================================

    public override void Heuristic(
        in ActionBuffers actionsOut)
    {
        /*
         * 人間のキー入力は
         * MoveHumanAgent() で直接取得するため、
         * ここではActionを0にしておく。
         */

        var discreteActionsOut =
            actionsOut.DiscreteActions;

        discreteActionsOut[0] = 0;
        discreteActionsOut[1] = 0;
        discreteActionsOut[2] = 0;
    }

    // ==================================================
    // ボールとの衝突
    // ==================================================

    void OnCollisionEnter(Collision c)
    {
        var force =
            k_Power * m_KickPower;

        if (position == Position.Goalie)
        {
            force = k_Power;
        }

        if (c.gameObject.CompareTag("ball"))
        {
            AddReward(
                .2f * m_BallTouch
            );

            var dir =
                c.contacts[0].point -
                transform.position;

            dir = dir.normalized;

            c.gameObject
                .GetComponent<Rigidbody>()
                .AddForce(dir * force);
        }
    }

    // ==================================================
    // Episode開始
    // ==================================================

    public override void OnEpisodeBegin()
    {
        m_BallTouch =
            m_ResetParams.GetWithDefault(
                "ball_touch",
                0
            );
    }
}
