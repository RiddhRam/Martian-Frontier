using System.Collections;
using NavMeshPlus.Components;
using UnityEngine;

[RequireComponent(typeof(NavMeshSurface))]
public class BuildNavMeshSurface : MonoBehaviour
{
    public NavMeshSurface[] navMeshSurfaces;
    public bool UpdateMeshes;
    
    // Start is called before the first frame update
    public void InitializeMesh()
    {
        if (!UpdateMeshes || !Debug.isDebugBuild) {
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

            foreach (var surface in navMeshSurfaces)
            {

                Debug.Log("Updating " + surface.name);
                surface.UpdateNavMesh(surface.navMeshData);
                Debug.Log("Updated " + surface.name);

                yield return new WaitForSeconds(10);
            }

            yield return new WaitForSeconds(7);
        }

    }
}
