# Prefab Lighting Baker
## Tool Overview 
  Bakes lightmaps right into materials on the selected objects with them being untouched. You can also use this tool to bake lighting on dynamic and animated objects. Just copy object, replace SkinnedMeshRenderer with MeshRenderer, bake the object and put new generated materials on the original object with SkinnedMeshRenderer.
  
## Tested on
Unity version: 2021.3.4f1
Renderer pipeline: Built-in

## Guide
- Download the package.
- Create Prefab Baker game objects via menu or put it on any object you want.
- In "Prefabs to bake", place all the objects you need to bake. If its hierarchy, you dont need to assign every object by yourself, just place the root.
- Select folders where to save new generated materials and copies of your objects.
- Press "Bake lights on prefabs" to begin baking. 
- If you need to clear all the new generated objects, baked lighting, press "Clear all data".

## Process of baking
1. Creates copies of assigned Game Objects and copies all the meshes in this objects, including child objects.
2. Changes lighting settings of original Mesh Renderers on suitable for baking.
3. Starts Unity`s lightmap baking process.
4. Changes UV2 on copies of meshes.
5. Creates new materials with lightmaps and assigns them to game objects copies.

## Shader overview
- Copies albedo texture (_MainTex) and color(_Color) from original materials.
- Supports emissive map and you can change it`s internsity.
- Supports lightmap textures.
- As shader doesnt support lighting for obvious reasons, the brightness setting applies to main color, increasing its visibility on scene.


## Limitations
- Doesnt work on mesh renderers with multiple materials.
- Shader doesnt support spec and bump maps.
