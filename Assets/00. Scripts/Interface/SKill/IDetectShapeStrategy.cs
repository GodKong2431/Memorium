using UnityEngine;
public interface IDetectShapeStrategy
{
    int Detect(Vector3 center, Vector3 direction, skillModule2 data, Collider[] buffer, int targetLayer);
}