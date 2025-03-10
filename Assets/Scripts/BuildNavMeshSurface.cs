using System.Collections;
using NavMeshPlus.Components;
using UnityEngine;

[RequireComponent(typeof(NavMeshSurface))]
public class BuildNavMeshSurface : MonoBehaviour
{
    NavMeshSurface navMeshSurface;
    public bool UpdateMeshes;
    
    // Start is called before the first frame update
    public void InitializeMesh()
    {
        if (!UpdateMeshes || !Debug.isDebugBuild) {
            return;
        } 
        navMeshSurface = GetComponent<NavMeshSurface>();
        navMeshSurface.hideEditorLogs = true;
        navMeshSurface.BuildNavMesh();
        
        StartCoroutine(UpdateNavMeshCoroutine());
    }

    private IEnumerator UpdateNavMeshCoroutine() {
        while (true) {
            yield return new WaitForSeconds(3);
            navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
            yield return new WaitForSeconds(27);
        }

    }
}
