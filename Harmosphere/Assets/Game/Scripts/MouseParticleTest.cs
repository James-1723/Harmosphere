using UnityEngine;

public class MouseParticleTest: MonoBehaviour
{
    [Header("Test Settings")]
    public GameObject longNoteParticle; // 使用和 Note 相同的預製體

    private GameObject currentParticleEffect;

    void Update()
    {
        // 滑鼠左鍵按下：開始測試
        if (Input.GetMouseButtonDown(0))
        {
            TestCreateLongNoteParticle();
        }

        // 滑鼠左鍵放開：停止測試
        if (Input.GetMouseButtonUp(0))
        {
            TestStopParticle();
        }
    }

    // 完全複製 Note 腳本中的 CreateLongNoteParticle 邏輯
    void TestCreateLongNoteParticle()
    {
        Debug.Log("=== Testing Note Particle Logic ===");
        Debug.Log("Creating long note particle effect - Simple version");

        if (longNoteParticle == null)
        {
            Debug.LogError("longNoteParticle prefab is NULL");
            return;
        }

        if (currentParticleEffect != null)
        {
            Debug.LogWarning("Cleaning existing particle effect");
            Destroy(currentParticleEffect);
            currentParticleEffect = null;
        }

        try
        {
            Debug.Log("About to instantiate particle effect...");
            Debug.Log($"Prefab reference valid: {longNoteParticle != null}");
            Debug.Log($"Spawn position: {transform.position}");

            // Simple instantiation like LongClickArrow
            currentParticleEffect = Instantiate(longNoteParticle, transform.position, Quaternion.identity);

            Debug.Log("Instantiate call completed");
            Debug.Log($"Result is null: {currentParticleEffect == null}");

            if (currentParticleEffect != null)
            {
                Debug.Log($"Particle effect created: {currentParticleEffect.name}");
                Debug.Log($"Position: {currentParticleEffect.transform.position}");
                Debug.Log($"Active: {currentParticleEffect.activeInHierarchy}");

                // Simple particle system access like LongClickArrow
                ParticleSystem ps = currentParticleEffect.GetComponent<ParticleSystem>();
                Debug.Log($"Getting ParticleSystem component...");
                Debug.Log($"ParticleSystem found: {ps != null}");

                if (ps != null)
                {
                    Debug.Log($"Found main ParticleSystem: {ps.gameObject.name}");
                    Debug.Log($"Initial state: isPlaying={ps.isPlaying}, particleCount={ps.particleCount}");

                    // Don't modify settings too much - let the prefab work as designed
                    // Just ensure it's playing
                    if (!ps.isPlaying)
                    {
                        Debug.Log("Starting particle system...");
                        ps.Play();
                        Debug.Log("Play() call completed");
                    }

                    Debug.Log($"After setup: isPlaying={ps.isPlaying}, particleCount={ps.particleCount}");
                }
                else
                {
                    Debug.LogError("No ParticleSystem component found on main object");

                    // Check children like LongClickArrow might need
                    ParticleSystem[] childPS = currentParticleEffect.GetComponentsInChildren<ParticleSystem>();
                    Debug.Log($"Found {childPS.Length} particle systems in children");

                    foreach (ParticleSystem childP in childPS)
                    {
                        Debug.Log($"Child particle system: {childP.gameObject.name}");
                        if (!childP.isPlaying)
                        {
                            childP.Play();
                        }
                    }
                }

                // Set as child to follow note movement
                Debug.Log("Setting as child object...");
                currentParticleEffect.transform.SetParent(transform);
                Debug.Log("SetParent completed");

                Debug.Log("Long note particle effect setup complete");
            }
            else
            {
                Debug.LogError("Instantiate returned null!");
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create particle effect: {e.Message}");
        }
    }

    void TestStopParticle()
    {
        Debug.Log("=== Stopping Test Particle ===");

        if (currentParticleEffect != null)
        {
            Debug.Log($"Destroying particle effect: {currentParticleEffect.name}");

            // 像 Note 一樣停止粒子
            ParticleSystem ps = currentParticleEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop();
            }

            Destroy(currentParticleEffect);
            currentParticleEffect = null;
        }
        else
        {
            Debug.Log("No particle effect to destroy");
        }
    }

    void OnDestroy()
    {
        if (currentParticleEffect != null)
        {
            Destroy(currentParticleEffect);
        }
    }

    // Inspector 測試按鈕
    [ContextMenu("Test Create Particle")]
    void TestCreateButton()
    {
        TestCreateLongNoteParticle();
    }

    [ContextMenu("Test Stop Particle")]
    void TestStopButton()
    {
        TestStopParticle();
    }
}