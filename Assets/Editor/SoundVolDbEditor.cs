using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundVolDb))]
public sealed class SoundVolDbEditor : Editor
{
    private const string CsvPath = "Assets/04. CSV/Sound/SoundTable.csv";
    private const string DbPath = "Assets/Resources/SoundVolDb.asset";
    private const float ListH = 640f;
    private const float IdW = 72f;
    private const float TypeW = 74f;
    private const float BaseW = 54f;
    private const float ValW = 40f;

    private SerializedProperty itemsProp;
    private Vector2 scroll;
    private string find = string.Empty;

    private void OnEnable()
    {
        itemsProp = serializedObject.FindProperty("items");
    }

    public override void OnInspectorGUI()
    {
        SoundVolDb db = (SoundVolDb)target;

        EditorGUILayout.HelpBox("Run Sync, then edit only Vol.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Sync"))
            Sync(db);
        if (GUILayout.Button("CSV"))
            PingCsv();
        EditorGUILayout.EndHorizontal();

        find = EditorGUILayout.TextField("Find", find);
        EditorGUILayout.Space();

        serializedObject.Update();

        if (itemsProp == null || itemsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No synced sounds.", MessageType.None);
        }
        else
        {
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(ListH));

            for (int i = 0; i < itemsProp.arraySize; i++)
            {
                SerializedProperty itemProp = itemsProp.GetArrayElementAtIndex(i);
                SerializedProperty idProp = itemProp.FindPropertyRelative("id");
                SerializedProperty typeProp = itemProp.FindPropertyRelative("type");
                SerializedProperty descProp = itemProp.FindPropertyRelative("desc");
                SerializedProperty pathProp = itemProp.FindPropertyRelative("path");
                SerializedProperty baseVolProp = itemProp.FindPropertyRelative("baseVol");
                SerializedProperty volProp = itemProp.FindPropertyRelative("vol");

                if (!IsMatch(idProp.intValue, typeProp.stringValue, descProp.stringValue, pathProp.stringValue))
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(idProp.intValue.ToString(), EditorStyles.boldLabel, GUILayout.Width(IdW));
                EditorGUILayout.LabelField(typeProp.stringValue, GUILayout.Width(TypeW));

                string desc = descProp.stringValue;
                if (string.IsNullOrWhiteSpace(desc))
                    desc = "(no desc)";

                string path = pathProp.stringValue;
                GUIContent descContent = string.IsNullOrWhiteSpace(path)
                    ? new GUIContent(desc)
                    : new GUIContent(desc, path);
                EditorGUILayout.LabelField(descContent);

                EditorGUILayout.LabelField(baseVolProp.floatValue.ToString("0.00"), GUILayout.Width(BaseW));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Vol", GUILayout.Width(IdW));
                volProp.floatValue = EditorGUILayout.Slider(volProp.floatValue, 0f, 1f);
                EditorGUILayout.LabelField(volProp.floatValue.ToString("0.00"), GUILayout.Width(ValW));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(db);
            ApplyLive();
        }
    }

    [MenuItem("Tools/Sound/Sync Vol DB")]
    private static void SyncMenu()
    {
        Sync(null);
    }

    [MenuItem("Tools/Sound/Open Vol DB")]
    private static void OpenDb()
    {
        Selection.activeObject = LoadDb();
    }

    private static void Sync(SoundVolDb db)
    {
        if (!File.Exists(CsvPath))
        {
            Debug.LogError("[SoundVolDb] SoundTable.csv not found.");
            return;
        }

        if (db == null)
            db = LoadDb();

        if (db == null)
            return;

        string csv = ReadCsv(CsvPath);
        List<SoundTable> list = CSVHelper.ParseCSVData<SoundTable>(csv);

        db.Sync(list);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = db;
        ApplyLive();

        Debug.Log($"[SoundVolDb] Sync complete. Count: {list.Count}");
    }

    private static void PingCsv()
    {
        Object csv = AssetDatabase.LoadAssetAtPath<Object>(CsvPath);
        if (csv != null)
            Selection.activeObject = csv;
    }

    private static void ApplyLive()
    {
        if (!Application.isPlaying)
            return;

        SoundManager mgr = Object.FindObjectOfType<SoundManager>();
        if (mgr != null)
            mgr.RebuildLibrary();
    }

    private bool IsMatch(int id, string type, string desc, string path)
    {
        if (string.IsNullOrWhiteSpace(find))
            return true;

        string key = find.Trim().ToLowerInvariant();
        if (id.ToString().Contains(key))
            return true;
        if (!string.IsNullOrWhiteSpace(type) && type.ToLowerInvariant().Contains(key))
            return true;
        if (!string.IsNullOrWhiteSpace(desc) && desc.ToLowerInvariant().Contains(key))
            return true;
        if (!string.IsNullOrWhiteSpace(path) && path.ToLowerInvariant().Contains(key))
            return true;

        return false;
    }

    private static SoundVolDb LoadDb()
    {
        SoundVolDb db = AssetDatabase.LoadAssetAtPath<SoundVolDb>(DbPath);
        if (db != null)
            return db;

        EnsureFolder("Assets/Resources");

        db = ScriptableObject.CreateInstance<SoundVolDb>();
        AssetDatabase.CreateAsset(db, DbPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return db;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string[] parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);

            cur = next;
        }
    }

    private static string ReadCsv(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);

        try
        {
            return new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF');
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(949).GetString(bytes).TrimStart('\uFEFF');
        }
    }
}
