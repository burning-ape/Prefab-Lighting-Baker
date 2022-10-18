using System.Text;
using UnityEditor;
using UnityEngine;
using System.IO;


[CustomEditor(typeof(PrefabsBaker))]
public class PrefabsBakerEditor : Editor
{
    private Texture2D _closeButtonTexture;
    private PrefabsBaker _prefabsBaker;


#if UNITY_EDITOR
    public void OnEnable()
    {
        // Get prefabs baker monoBeh
        _prefabsBaker = (PrefabsBaker)target;

        // Get path of the package
        var packagePath = GetPackagePath(this);


        // Load assets
        _closeButtonTexture = (Texture2D)AssetDatabase
                .LoadAssetAtPath($"{packagePath}/Sprites/crossTexture.png", typeof(Texture2D));


        // Load paths
        var pathToCopiesFolder = PlayerPrefs.GetString("CopiesFolder");
        _prefabsBaker.PathToCopiesFolder.Clear();
        _prefabsBaker.PathToCopiesFolder.Append(pathToCopiesFolder);

        var pathToMatsFolder = PlayerPrefs.GetString("MatFolder");
        _prefabsBaker.PathToMatsFolder.Clear();
        _prefabsBaker.PathToMatsFolder.Append(pathToMatsFolder);
    }
#endif

    public override void OnInspectorGUI()
    {
        // Draw inspector
        base.DrawDefaultInspector();


        #region GUIStyles

        // Bold white text
        var boldWhiteText = new GUIStyle();
        boldWhiteText.fontStyle = FontStyle.Bold;
        boldWhiteText.normal.textColor = Color.white;


        // Cross button texture
        var buttonTexture = new GUIStyle();
        buttonTexture.normal.background = _closeButtonTexture;

        #endregion


        #region Select copies folder

        GUILayout.Space(25);
        GUILayout.BeginHorizontal();

        var contentPref = new SFEContent();
        contentPref.Style = boldWhiteText;
        contentPref.DeletePathTexture = buttonTexture;
        contentPref.Name = "CopiesFolder";
        contentPref.LabelText = "Folder to save prefab copies to:";
        contentPref.TooltipText = "Folder in which all the newly generated prefabs will be saved to. Recommended to create a new one.";
        contentPref.PathToFolder = _prefabsBaker.PathToCopiesFolder;

        SelectFolderElement.SelectFolder(contentPref);

        GUILayout.EndHorizontal();

        #endregion


        #region Select material folder

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();

        var contentMat = new SFEContent();
        contentMat.Style = boldWhiteText;
        contentMat.DeletePathTexture = buttonTexture;
        contentMat.Name = "MatFolder";
        contentMat.LabelText = "Folder to save materials to:";
        contentMat.TooltipText = "Folder in which all the newly generated materials will be saved to. Recommended to create a new one.";
        contentMat.PathToFolder = _prefabsBaker.PathToMatsFolder;

        SelectFolderElement.SelectFolder(contentMat);

        GUILayout.EndHorizontal();


        #endregion


        #region Bake lighting elements

        GUILayout.Space(25);
        GUILayout.BeginHorizontal();

        // Bake prefabs
        if (GUILayout.Button("Bake Lights On Prefabs"))
        {
            // Check if there is no errors to proceed baking lightmaps
            var canProceedBaking = _prefabsBaker.PreBaking();

            if (canProceedBaking)
            {
                Lightmapping.Bake();
                _prefabsBaker.PostBaking();
            }
        }

        GUILayout.EndHorizontal();

        #endregion


        #region Clear lighting elements

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();

        var clearButton = new GUIContent("Clear all data", "Warning! Clears all the lightmap data on the scene " +
            "and deletes all assets in selected folders.");

        // Delete all the generated data
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button(clearButton))
        {
            Lightmapping.Clear();
            Lightmapping.ClearLightingDataAsset();
            Lightmapping.ClearDiskCache();

            _prefabsBaker.ClearData();
        }
        GUILayout.EndHorizontal();
        #endregion

    }



    #region Menu Item

    [MenuItem("GameObject/Tools/Prefabs Baker", priority = 0)]
    public static void CreateEmpty()
    {
        GameObject prefabsBakerGO = new GameObject("Prefabs Baker");
        Undo.RegisterCreatedObjectUndo(prefabsBakerGO, "Create prefabs baker");

        prefabsBakerGO.AddComponent<PrefabsBaker>();
    }
    #endregion


    #region Static Extensions

    public static string GetPackagePath(ScriptableObject script)
    {
        MonoScript thisAsset = MonoScript.FromScriptableObject(script);
        var filePath = AssetDatabase.GetAssetPath(thisAsset);

        return filePath.Replace($"/Editor/{Path.GetFileName(filePath)}", "");
    }
    #endregion


    #region Structs and Classes

    public static class SelectFolderElement
    {
        public static void SelectFolder(SFEContent content)
        {
            bool pathSelected = false;

            if (content.PathToFolder.Length > 0) pathSelected = true;
            else pathSelected = false;


            // Main label
            var selectMatFolderContent = new GUIContent(content.LabelText, content.TooltipText);
            GUILayout.Label(selectMatFolderContent, content.Style, GUILayout.Width(190));

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();


            if (!pathSelected)
            {
                if (GUILayout.Button("Select folder"))
                {
                    // Select folder and save its path
                    // Folder was pathSelected this time
                    var path = content.PathToFolder.Append(EditorUtility.OpenFolderPanel("Select folder", "", ""));
                    path.Replace(Application.dataPath.Replace("Assets", ""), "");

                    PlayerPrefs.SetString(content.Name, content.PathToFolder.ToString());
                    GUIUtility.ExitGUI();
                }
            }
            else
            {
                // Show path to the folder
                GUILayout.Label(content.PathToFolder.ToString(), GUILayout.Width(200));

                // Show clear button
                // Pressing it will delete path to the folder, clean saved path
                // Folder was not pathSelected this time
                if (GUILayout.Button("", content.DeletePathTexture, GUILayout.MaxHeight(16), GUILayout.MaxWidth(16)))
                {
                    content.PathToFolder.Clear();
                    PlayerPrefs.DeleteKey(content.Name);
                }
            }

            GUILayout.EndHorizontal();
        }
    }


    public class SFEContent
    {
        public GUIStyle Style, DeletePathTexture;
        public string LabelText, TooltipText, Name;
        public bool PathSelected;
        public StringBuilder PathToFolder;
    }
    #endregion
}
