using System.Collections;
using System.Collections.Generic;
using NavMeshPlus.Components;
using NavMeshPlus.Extensions;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NavMeshSurface))]
public class BuildNavMeshSurface : MonoBehaviour
{

    [SerializeField]
    private LayerMask includedLayers;
    [SerializeField]
    private NavMeshSurface[] navMeshSurfaces;
    
    public bool UpdateMeshes;
    private readonly Bounds bounds = new(new(-80, 0, 0), new(80, 550, 100));
    private int timer = 45;
    
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

        if (SceneManager.GetActiveScene().name.ToLower().Contains("singleplayer")) {
            timer = 20;
        }
        
        StartCoroutine(UpdateNavMeshCoroutine());
    }

    private IEnumerator UpdateNavMeshCoroutine() {
        while (true) {
            yield return new WaitForSeconds(5);
            
            /*int index = 1;
            foreach (var surface in navMeshSurfaces)
            {   
                Debug.Log("Updating: " + surface.name);

                // From reading the files directly I found they did it like this, but CollectSources is private and NavMeshBuilder.CollectSources is not the same
                using var builderState = new NavMeshBuilderState() { };
                
                var sources = surface.CollectSources(builderState);

                var sourcesBounds = new Bounds(m_Center, Abs(m_Size));
                if (m_CollectObjects == CollectObjects.All || m_CollectObjects == CollectObjects.Children)
                {
                    sourcesBounds = CalculateWorldBounds(sources);
                }
                
                var async = NavMeshBuilder.UpdateNavMeshDataAsync(surface.navMeshData, surface.GetBuildSettings(), sources, sourcesBounds);

                yield return async;

                index++;

                yield return new WaitForSeconds(10);
            }*/

            foreach (var surface in navMeshSurfaces)
            {   

                surface.UpdateNavMesh(surface.navMeshData);

                yield return new WaitForSeconds(timer);
            }

        }

    }
}
