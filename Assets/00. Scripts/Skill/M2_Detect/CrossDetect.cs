
    using System.Collections.Generic;
    using UnityEngine;

    public class CrossDetect : IDetectShapeStrategy
    {
        private HashSet<Collider> _uniqueSet = new HashSet<Collider>();

        public int Detect(Vector3 center, Vector3 direction, SkillModule2Table data, ISkillDetectable provider, int targetLayer)
        {
            Collider[] buffer = provider.GetBuffer();
            _uniqueSet.Clear();

            //세로
            Vector3 vHalf = new Vector3(data.m2S2 * 0.5f, 10f, data.m2S1 * 0.5f);
            Quaternion rot = Quaternion.LookRotation(direction);
            int count = Physics.OverlapBoxNonAlloc(center, vHalf, buffer, rot, targetLayer);
            for (int i = 0; i < count; i++) _uniqueSet.Add(buffer[i]);

            //가로
            Quaternion hRot = rot * Quaternion.Euler(0, 90, 0);
            Vector3 hHalf = new Vector3(data.m2S1 * 0.5f, 10f, data.m2S2 * 0.5f);
            count = Physics.OverlapBoxNonAlloc(center, hHalf, buffer, hRot, targetLayer);
            for (int i = 0; i < count; i++) _uniqueSet.Add(buffer[i]);

            //결과
            int index = 0;
            foreach (var col in _uniqueSet)
            {
                if (index >= buffer.Length) 
                    break; 
                buffer[index] = col;
                index++;
            }
            return Mathf.Min(_uniqueSet.Count, buffer.Length);
        }

        public void DrawGizmo(Vector3 center, Vector3 direction, SkillModule2Table data)
        {
            Gizmos.color = Color.cyan;
            Matrix4x4 oldMatrix = Gizmos.matrix;

            Vector3 boxSize = new Vector3(data.m2S2, 4f, data.m2S1);

            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.LookRotation(direction), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, boxSize);

            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.LookRotation(direction) * Quaternion.Euler(0, 90, 0), Vector3.one);

            Gizmos.DrawWireCube(Vector3.zero, boxSize);
            Gizmos.matrix = oldMatrix;
        }
    }