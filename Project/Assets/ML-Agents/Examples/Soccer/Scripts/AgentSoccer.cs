using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

public class AgentSoccer : Agent
{
    // Note that that the detectable tags are different for the blue and purple teams. The order is
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player
    public enum Team
    {
        Blue = 0,
        Purple = 1
    }

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

    [HideInInspector]
    public Team team;
    float m_KickPower;
    int m_PlayerIndex;
    public SoccerFieldArea area;

    // The coefficient for the reward for colliding with a ball. Set using curriculum.
    float m_BallTouch;
    public Position position;

    const float k_Power = 2000f;
    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    [HideInInspector]
    public float timePenalty;

    [HideInInspector]
    public Rigidbody agentRb;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    Vector3 m_Transform;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_Existential = 1f / MaxStep;
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;
            m_Transform = new Vector3(transform.position.x - 3f, .5f, transform.position.z);
        }
        else
        {
            team = Team.Purple;
            m_Transform = new Vector3(transform.position.x + 3f, .5f, transform.position.z);
        }
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
        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        var playerState = new PlayerState
        {
            agentRb = agentRb,
            startingPos = transform.position,
            agentScript = this,
        };
        area.playerStates.Add(playerState);
        m_PlayerIndex = area.playerStates.IndexOf(playerState);
        playerState.playerIndex = m_PlayerIndex;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }

    public void MoveAgent(ActionSegment<float> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_KickPower = 0f;


        var forward = Mathf.Clamp(act[0], -1f, 1f);
        var right = Mathf.Clamp(act[1], -1f, 1f);
        var rotate = Mathf.Clamp(act[2], -1f, 1f);

        dirToGo = transform.forward * forward * m_ForwardSpeed;
        dirToGo += transform.right * right * m_LateralSpeed;
        rotateDir = -transform.up * rotate;
        if (forward > 0)
        {
            m_KickPower = forward;
        }

        //m_KickPower = 0f;
        //var forwardAxis = act[0];
        //var rightAxis = act[1];
        //var rotateAxis = act[2];

        //switch (forwardAxis)
        //{
        //    case 1:
        //        dirToGo = transform.forward * m_ForwardSpeed;
        //        m_KickPower = 1f;
        //        break;
        //    case 2:
        //        dirToGo = transform.forward * -m_ForwardSpeed;
        //        break;
        //}

        //switch (rightAxis)
        //{
        //    case 1:
        //        dirToGo = transform.right * m_LateralSpeed;
        //        break;
        //    case 2:
        //        dirToGo = transform.right * -m_LateralSpeed;
        //        break;
        //}

        //switch (rotateAxis)
        //{
        //    case 1:
        //        rotateDir = transform.up * -1f;
        //        break;
        //    case 2:
        //        rotateDir = transform.up * 1f;
        //        break;
        //}

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {

        if (position == Position.Goalie)
        {
            // Existential bonus for Goalies.
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            // Existential penalty for Strikers
            AddReward(-m_Existential);
        }
        else
        {
            // Existential penalty cumulant for Generic
            timePenalty -= m_Existential;
        }
        //MoveAgent(actionBuffers.DiscreteActions);
        MoveAgent(actionBuffers.ContinuousActions);
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var contOut = actionsOut.ContinuousActions;
        contOut.Clear();
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            contOut[0] = 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            contOut[0] = -1f;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            contOut[1] = -1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            contOut[1] = 1f;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            contOut[2] = 1f;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            contOut[2] = -1f;
        }

        //var discreteActionsOut = actionsOut.DiscreteActions;
        //discreteActionsOut.Clear();
        ////forward
        //if (Input.GetKey(KeyCode.W))
        //{
        //    discreteActionsOut[0] = 1;
        //}
        //if (Input.GetKey(KeyCode.S))
        //{
        //    discreteActionsOut[0] = 2;
        //}
        ////rotate
        //if (Input.GetKey(KeyCode.A))
        //{
        //    discreteActionsOut[2] = 1;
        //}
        //if (Input.GetKey(KeyCode.D))
        //{
        //    discreteActionsOut[2] = 2;
        //}
        ////right
        //if (Input.GetKey(KeyCode.E))
        //{
        //    discreteActionsOut[1] = 1;
        //}
        //if (Input.GetKey(KeyCode.Q))
        //{
        //    discreteActionsOut[1] = 2;
        //}
    }
    /// <summary>
    /// Used to provide a "kick" to the ball.
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        var force = k_Power * m_KickPower;
        if (position == Position.Goalie)
        {
            force = k_Power;
        }
        if (c.gameObject.CompareTag("ball"))
        {
            AddReward(.2f * m_BallTouch);
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
    }

    public override void OnEpisodeBegin()
    {

        timePenalty = 0;
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
        if (team == Team.Purple)
        {
            transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }
        var randomX = Random.Range(-3.0f, 3.0f);
        var randomZ = Random.Range(-0.5f, 0.5f);
        transform.position = m_Transform + new Vector3(randomX, 0f, randomZ);
        agentRb.velocity = Vector3.zero;
        agentRb.angularVelocity = Vector3.zero;
        SetResetParameters();
    }

    public void SetResetParameters()
    {
        area.ResetBall();
    }
}
