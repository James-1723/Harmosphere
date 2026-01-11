using UnityEngine;

public class TrackManager : MonoBehaviour
{
    void Start()
    {
        CreateJudgeLine();
    }

    void CreateJudgeLine()
    {
        GameObject judgeLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        judgeLine.name = "JudgeLine";
        judgeLine.transform.position = new Vector3(0, 0, 0);
        judgeLine.transform.localScale = new Vector3(8f, 0.2f, 0.5f);

        judgeLine.GetComponent<Collider>().isTrigger = true;
        judgeLine.tag = "JudgeLine";
        judgeLine.GetComponent<Renderer>().material.color = Color.red;
    }
}