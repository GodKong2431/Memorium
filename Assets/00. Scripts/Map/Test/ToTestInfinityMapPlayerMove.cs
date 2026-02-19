using UnityEngine;

public class ToTestInfinityMapPlayerMove : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    void Update()
    {
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
    }
}
