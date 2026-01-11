using UnityEngine;

public class HitEffectTest : MonoBehaviour
{
    public GameObject hitParticlePrefab;
    public string hexColor = "7EFFEC";

    void OnMouseDown()
    {
        if (hitParticlePrefab != null)
        {
            GameObject effect = Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);

            string colorString = hexColor.StartsWith("#") ? hexColor : "#" + hexColor;
            if (ColorUtility.TryParseHtmlString(colorString, out Color parsedColor))
            {
                ParticleSystem ps = effect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(parsedColor);

                    // 設定粒子系統播放完畢後自動刪除
                    main.stopAction = ParticleSystemStopAction.Destroy;

                    Debug.Log("Applied color: " + parsedColor + " from input: " + hexColor);
                }
            }
        }
    }
}