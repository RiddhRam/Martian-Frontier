using System.Collections;
using NavMeshPlus.Components;
using UnityEngine;

[RequireComponent(typeof(NavMeshSurface))]
public class BuildNavMeshSurface : MonoBehaviour
{

    [SerializeField]
    private MineRenderer mineRenderer;
    [SerializeField]
    private LayerMask includedLayers;
    [SerializeField]
    private NavMeshSurface[] navMeshSurfaces;
    
    public bool UpdateMeshes;
    private readonly Bounds bounds = new(new(-80, 0, 0), new(80, 550, 100));
    
    // Start is called before the first frame update
    public void InitializeMesh()
    {
        if (!UpdateMeshes) {
            return;
        } 

        foreach (var surface in navMeshSurfaces)
        {
            surface.hideEditorLogs = true;
            surface.BuildNavMesh();           
        }
        
        StartCoroutine(UpdateNavMeshCoroutine());
    }

    private IEnumerator UpdateNavMeshCoroutine() {
        while (true) {
            yield return new WaitForSeconds(3);

            /*
            int index = 1;
            foreach (var surface in navMeshSurfaces)
            {   
                Debug.Log("Updating: " + surface.name);

                List<NavMeshBuildSource> sources = new();
                
                NavMeshBuilder.CollectSources(surface.navMeshData.sourceBounds, includedLayers, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
                //surface.UpdateNavMesh(surface.navMeshData);
                var async = NavMeshBuilder.UpdateNavMeshDataAsync(surface.navMeshData, NavMesh.GetSettingsByIndex(index), sources, surface.navMeshData.sourceBounds);

                yield return async;

                index++;

                yield return new WaitForSeconds(10);
            }*/

            foreach (var surface in navMeshSurfaces)
            {   
                surface.UpdateNavMesh(surface.navMeshData);

                yield return new WaitForSeconds(60);
            }


        }

    }
}
