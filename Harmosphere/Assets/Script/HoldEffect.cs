using UnityEngine;

public class HoldEffect : MonoBehaviour
{
    public GameObject holdParticlePrefab;
    public GameObject shortParticlePrefab;
    public string hexColor = "FF8800";
    public float fadeOutTime = 2f;
    private GameObject currentEffect;
    public Vector3 tem_pos = new Vector3(0, 0, 0);

    void OnMouseDown()
    {
        if (holdParticlePrefab != null && currentEffect == null)
        {
            // 使用物件的實際位置而不是 tem_pos
            Vector3 spawnPosition = transform.position;
            currentEffect = Instantiate(holdParticlePrefab, spawnPosition, Quaternion.identity);
            
            // 確保粒子系統存在並設置顏色
            ParticleSystem particleSystem = currentEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                string colorString = hexColor.StartsWith("#") ? hexColor : "#" + hexColor;
                if (ColorUtility.TryParseHtmlString(colorString, out Color parsedColor))
                {
                    var main = particleSystem.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(parsedColor);
                }
                
                // 確保粒子系統開始播放
                if (!particleSystem.isPlaying)
                {
                    particleSystem.Play();
                }
            }
            
            Debug.Log($"粒子效果已生成在位置: {spawnPosition}");
        }
    }

    void OnMouseUp()
    {
        if (currentEffect != null)
        {
            ParticleSystem particleSystem = currentEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Stop(false);
            }
            Destroy(currentEffect, fadeOutTime);
            currentEffect = null;
            Debug.Log("粒子效果已停止");
        }
    }

    void OnDestroy()
    {
        // 物件被刪除時，立即清理粒子效果
        if (currentEffect != null)
        {
            ParticleSystem particleSystem = currentEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Stop(false);
            }
            Destroy(currentEffect, fadeOutTime);
        }
    }
}