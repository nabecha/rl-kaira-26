using UnityEngine;

/// <summary>
/// KaiRA君のニューラルネットのひれを、おたまじゃくしのように左右にゆらす（見た目だけ）。
/// </summary>
public class KaiRATailWiggle : MonoBehaviour
{
    public float angle = 10f;
    public float speed = 6f;

    Quaternion m_BaseRotation;
    float m_Phase;

    void Awake()
    {
        m_BaseRotation = transform.localRotation;
        m_Phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        float yaw = Mathf.Sin(Time.time * speed + m_Phase) * angle;
        transform.localRotation = m_BaseRotation * Quaternion.Euler(0f, yaw, 0f);
    }
}
