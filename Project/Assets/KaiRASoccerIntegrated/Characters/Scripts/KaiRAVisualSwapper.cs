using UnityEngine;

/// <summary>
/// サッカーの各エージェントの「見た目だけ」を KaiRA君 に差し替える。
/// 当たり判定（Collider / Rigidbody）には一切触れない:
///   - 元の四角いキャラ（AgentCube_*）は Renderer を消すだけ
///   - KaiRA君の見た目からは Collider / Rigidbody を取り除いてから付ける
/// シーン内の空のオブジェクトに付けておけば、再生時に全員に適用される。
/// </summary>
public class KaiRAVisualSwapper : MonoBehaviour
{
    [Header("KaiRA君の見た目（チームごと）")]
    public GameObject blueTeamVisual;
    public GameObject purpleTeamVisual;

    [Header("見た目の位置・大きさの微調整")]
    public Vector3 localOffset = Vector3.zero;
    public float scale = 1f;

    static readonly int s_ColorId = Shader.PropertyToID("_Color");
    static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly Color s_PlayerColor = new Color(1f, 0.82f, 0.05f, 1f);

    void Awake()
    {
        foreach (var agent in FindObjectsByType<AgentSoccer>(FindObjectsSortMode.None))
        {
            var bp = agent.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
            bool isBlue = bp == null || bp.TeamId == (int)Team.Blue;
            var prefab = isBlue ? blueTeamVisual : purpleTeamVisual;
            if (prefab == null) prefab = blueTeamVisual != null ? blueTeamVisual : purpleTeamVisual;
            if (prefab == null) return;

            // 元の見た目を隠す（Renderer だけ無効化。Collider はそのまま）
            foreach (Transform child in agent.transform)
            {
                if (child.name.StartsWith("AgentCube"))
                {
                    foreach (var r in child.GetComponentsInChildren<Renderer>(true))
                    {
                        r.enabled = false;
                    }
                }
            }

            // KaiRA君の見た目を付ける
            var visual = Instantiate(prefab, agent.transform);
            visual.name = "KaiRAVisual";
            visual.transform.localPosition = localOffset;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * scale;

            if (agent.humanControlled) HighlightPlayer(visual);

            // 見た目側の当たり判定は全部消す（元の当たり判定を変えないため）
            foreach (var c in visual.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (var rb in visual.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
        }
    }

    static void HighlightPlayer(GameObject visual)
    {
        // Override only this player's pale body materials; keep eyes and team markers intact.
        var properties = new MaterialPropertyBlock();
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null || (material.name != "KaiRA_Brain" &&
                    material.name != "KaiRA_Cerebellum" && material.name != "KaiRA_Strand")) continue;

                renderer.GetPropertyBlock(properties, i);
                if (material.HasProperty(s_ColorId)) properties.SetColor(s_ColorId, s_PlayerColor);
                if (material.HasProperty(s_BaseColorId)) properties.SetColor(s_BaseColorId, s_PlayerColor);
                renderer.SetPropertyBlock(properties, i);
            }
        }
    }
}
