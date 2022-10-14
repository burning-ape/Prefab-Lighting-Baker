using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Text;
using UnityEngine.Rendering;
using System.IO;


public class PrefabsBaker : MonoBehaviour
{
    [Tooltip("Add gameObjects on scene you want to bake")]
    public List<GameObject> PrefabsToBake = new List<GameObject>();
    private List<GameObject> PrefabsCopies = new List<GameObject>();

    [HideInInspector] public StringBuilder PathToCopiesFolder = new StringBuilder();
    [HideInInspector] public StringBuilder PathToMatsFolder = new StringBuilder();

    private List<RendererData> _renderersData = new List<RendererData>();


    #if UNITY_EDITOR
    /// <summary>
    /// Called before baking lights.
    /// </summary>
    public bool PreBaking()
    {
        bool canProceedBaking = true;

        if (PathToCopiesFolder.Length > 0)
        {
            // If folder selected, create prefabs copies
            CreatePrefabsCopies();

            // Get allComponents from prefabs and their children
            var renderers = GetAllRenderersFromPrefabs(PrefabsCopies);
            var originalRenderers = GetAllRenderersFromPrefabs(PrefabsToBake);
            ChangeRenderersSettings(originalRenderers);

            // Fill filters data with filters and materials
            _renderersData = GetRenderersAndMaterials(renderers);         
        }
        else { canProceedBaking = false; }

        return canProceedBaking;
    }



    /// <summary>
    /// Called after baking lights.
    /// </summary>
    public void PostBaking()
    {
        // Set generated lightmaps to renderersData
        _renderersData = GetLightmaps(GetAllRenderersFromPrefabs(PrefabsToBake), _renderersData);

        // Change UV2 on prefabs copies meshes
        var meshes = GetAllMeshesFromPrefabs(PrefabsCopies);
        var LOSs = GetLightmapsOSFromRenderers(GetAllRenderersFromPrefabs(PrefabsToBake));
        ChangeUV2OnMeshes(meshes, LOSs);

        // Create and assign new materials
        CreateAndAssignNewMaterials();
    }



    /// <summary>
    /// Changes lighting setting on filters if its not suitable for light baking
    /// </summary>
    private void ChangeRenderersSettings(List<MeshRenderer> renderers)
    {
        foreach (var renderer in renderers)
        {
            var GO = renderer.gameObject;
            GameObjectUtility.SetStaticEditorFlags(GO, StaticEditorFlags.ContributeGI);

            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.receiveGI = ReceiveGI.Lightmaps;
        }
    }



    /// <summary>
    /// Creates prefab asset copies to write info on them without changing the original prefabs
    /// </summary>
    private void CreatePrefabsCopies()
    {
        PrefabsCopies.Clear();

        foreach (var prefab in PrefabsToBake)
        {
            // Create prefab asset data
            bool success;
            var newPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect
                (prefab, PathToCopiesFolder.ToString() + "/" + prefab.name + "COPY" + ".prefab", InteractionMode.UserAction, out success);

            PrefabUtility.UnpackPrefabInstance(prefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);         

            PrefabsCopies.Add(newPrefab);
        }

        CreateMeshesCopies(GetAllMeshesFromPrefabs(PrefabsCopies), GetAllFiltersFromPrefabs(PrefabsCopies));
    }



    /// <summary>
    /// Creates and return copies of meshes.
    /// </summary>
    /// <param name="meshes">Meshes to copy</param>
    /// <returns>Mesh copies</returns>
    private void CreateMeshesCopies(List<Mesh> meshes, List<MeshFilter> filters)
    {
        var meshesFolder = Directory.CreateDirectory(PathToCopiesFolder.ToString() + "/CopiedMeshes");
        var meshCopies = new List<Mesh>();

        for(int i = 0; i < meshes.Count; i++)
        {
            var copy = new Mesh();

            copy.vertices = meshes[i].vertices;
            copy.triangles = meshes[i].triangles;
            copy.normals = meshes[i].normals;
            copy.tangents = meshes[i].tangents;
            copy.uv = meshes[i].uv;
            copy.uv2 = meshes[i].uv2;

            AssetDatabase.CreateAsset(copy, $"{PathToCopiesFolder}/CopiedMeshes/mesh_{i}.asset");

            filters[i].mesh = copy;
            meshCopies.Add(copy);
        }
    }



    /// <summary>
    /// Changes UV2 channel on meshes.
    /// </summary>
    /// <param pair="lightmapOffsetAndScale">Offset and scale used to get meshes UV on lightmap</param>
    private void ChangeUV2OnMeshes(List<Mesh> meshes, List<Vector4> lightmapOffsetAndScale)
    {
        for (int i = 0; i < meshes.Count; i++)
        {
            var meshToModify = meshes[i];

            Vector2[] modifiedUV2s = meshToModify.uv2;

            for (int j = 0; j < meshToModify.uv2.Length; j++)
            {
                modifiedUV2s[j] = new Vector2
                    (meshToModify.uv2[j].x * lightmapOffsetAndScale[i].x + lightmapOffsetAndScale[i].z,
                     meshToModify.uv2[j].y * lightmapOffsetAndScale[i].y + lightmapOffsetAndScale[i].w);
            }

            meshToModify.uv2 = modifiedUV2s;
        }
    }



    /// <summary>
    /// Generate new materials and asign them to the filters.
    /// </summary>
    private void CreateAndAssignNewMaterials()
    {
        var namesPairs = new List<string>();
        var newMaterials = new List<Material>();

        // Create materials
        for (int i = 0; i< _renderersData.Count; i++)
        {
            var data = _renderersData[i];
            var pair = data.PrebakeMaterial.name + data.Lightmap.name;

            if (!namesPairs.Contains(pair))
            {
                newMaterials.Add(CreateMaterial(data, pair));
                namesPairs.Add(pair);
            }
        }

        // Set material
        foreach(var data in _renderersData)
        {
            var namePair = data.PrebakeMaterial.name + data.Lightmap.name;

            foreach (var mat in newMaterials)
            {
                if (mat.name == namePair)
                {
                    data.Renderer.sharedMaterial = mat;
                }       
            }
        }
    }



    /// <summary>
    /// Creates material and adds it to data of renderer data
    /// </summary>
    /// <param pair="number">Number in the end of the material`s pair</param>
    public Material CreateMaterial(RendererData data, string name)
    {
        // Create new material
        var newMat = new Material(Shader.Find("Custom/LightmapShader"));
        AssetDatabase.CreateAsset(newMat, $"{PathToMatsFolder}/{name}.mat");

        #region Set properties
        // If prebake material has main texture then assign it to the new material
        var prebakeMainTex = data.PrebakeMaterial.HasTexture("_MainTex");
        if (prebakeMainTex)
            newMat.SetTexture("_MainTex", data.PrebakeMaterial.GetTexture("_MainTex"));

        // If preabke material has color then assign it to the new material
        var prebakeColor = data.PrebakeMaterial.HasColor("_Color");
        if (prebakeColor)
            newMat.SetColor("_Color", data.PrebakeMaterial.GetColor("_Color"));

        // Set lightmap texture
        newMat.SetTexture("_Lightmap", data.Lightmap);
        #endregion

        return newMat;
    }




    /// <summary>
    /// Clears all the generated data during baking prefabs
    /// </summary>
    public void ClearData()
    {
        #region Clear prefabs

        // Delete all the generated prefabs
        var allPrefs = Directory.GetFiles(PathToCopiesFolder.ToString());
        foreach(var pref in allPrefs) AssetDatabase.DeleteAsset(pref);

        // Delete copied meshes
        if(Directory.Exists(PathToCopiesFolder + "/CopiedMeshes"))
            Directory.Delete(PathToCopiesFolder + "/CopiedMeshes", true);


        // Unpacks prefabs on scene
        foreach (var prefab in PrefabsToBake)
        {
            if (PrefabUtility.IsPrefabAssetMissing(prefab))
                PrefabUtility.UnpackPrefabInstance(prefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        #endregion


        #region Clear materials
        // Delete all generated materials
        var allMats = Directory.GetFiles(PathToMatsFolder.ToString());
        foreach(var mat in allMats)
        {           
            AssetDatabase.DeleteAsset(mat);
        }
        #endregion

        AssetDatabase.Refresh();
    }

    #endif

    #region Static Extensions

    public static List<RendererData> GetLightmaps(List<MeshRenderer> renderers, List<RendererData> data)
    {
        var lightmaps = LightmapSettings.lightmaps;

        for (int i = 0; i < data.Count; i++)
        {
            var lightmapNumber = renderers[i].lightmapIndex;
            var renderersLightmap = lightmaps[lightmapNumber].lightmapColor;

            data[i].Lightmap = renderersLightmap;
        }

        return data;
    }

    public static List<RendererData> GetRenderersAndMaterials(List<MeshRenderer> renderers)
    {
        var renderersData = new List<RendererData>();

        foreach (var renderer in renderers)
        {
            var data = new RendererData();
         
            data.Renderer = renderer;
            data.PrebakeMaterial = renderer.sharedMaterial;

            renderersData.Add(data);
        }

        return renderersData;
    }

    public static List<Mesh> GetAllMeshesFromPrefabs(List<GameObject> prefabs)
    {
        var meshes = new List<Mesh>();

        foreach (var prefab in prefabs)
        {
            // Iterate through all the children of prefabs
            var filterInPrefab = prefab.transform.GetComponentsInChildren<MeshFilter>();

            // Get meshes from filter and add them to meshes array
            foreach (var meshesInFilter in filterInPrefab)
            {
                meshes.Add(meshesInFilter.sharedMesh);
            }
        }

        return meshes;
    }

    public static List<MeshFilter> GetAllFiltersFromPrefabs(List<GameObject> prefabs)
    {
        var filters = new List<MeshFilter>();

        foreach (var prefab in prefabs)
        {
            var filtersInPrefab = prefab.transform.GetComponentsInChildren<MeshFilter>();

            filters.AddRange(filtersInPrefab);
        }

        return filters;
    }

    public static List<MeshRenderer> GetAllRenderersFromPrefabs(List<GameObject> prefabs)
    {
        var renderers = new List<MeshRenderer>();

        foreach (var prefab in prefabs)
        {
            var renderersInPrefab = prefab.GetComponentsInChildren<MeshRenderer>();

            renderers.AddRange(renderersInPrefab);
        }

        return renderers;
    }

    public static List<Vector4> GetLightmapsOSFromRenderers(List<MeshRenderer> renderers)
    {
        var LOS = new List<Vector4>();

        foreach (var renderer in renderers)
        {
            LOS.Add(renderer.lightmapScaleOffset);
        }

        return LOS;
    }

    #endregion


    #region Structs and Classes
    public class RendererData
    {
        public Material PrebakeMaterial, NewMaterial;
        public MeshRenderer Renderer;
        public Texture2D Lightmap;
    }
    #endregion

}


