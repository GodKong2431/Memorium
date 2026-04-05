using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

//[ExecuteInEditMode]
public class EquipEffectCanvas : MonoBehaviour
{
    public RectMask2D targetMask;
    private List<Material> myMaterials = new List<Material>();
    private Renderer[] allRenderers;

    void OnEnable()
    {
        RefreshReference();
    }

    void RefreshReference()
    {
        myMaterials.Clear();
        allRenderers = GetComponentsInChildren<Renderer>(true);

        foreach (var rend in allRenderers)
        {
            if (Application.isPlaying)
            {
                myMaterials.Add(rend.material);
            }
            else
            {
                myMaterials.Add(rend.sharedMaterial);
            }
        }

        if (targetMask == null)
        {
            targetMask = GetComponentInParent<RectMask2D>(true);
        }
    }

    void LateUpdate()
    {
        if (targetMask == null || allRenderers == null || allRenderers.Length == 0)
        {
            RefreshReference();
        }

        if (targetMask != null && myMaterials.Count > 0)
        {
            Vector3[] corners = new Vector3[4];
            targetMask.rectTransform.GetWorldCorners(corners);
            Vector4 clipRect = new Vector4(corners[0].x, corners[0].y, corners[2].x, corners[2].y);

            foreach (var mat in myMaterials)
            {
                if (mat == null) continue;

                mat.SetVector("_ClipRect", clipRect);
                mat.EnableKeyword("UNITY_UI_CLIP_RECT");
            }
        }
    }
}