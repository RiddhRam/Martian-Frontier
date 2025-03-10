using System.Collections;
using System.Threading.Tasks;
using NavMeshPlus.Components;
using UnityEngine;

[RequireComponent(typeof(NavMeshSurface))]
public class BuildNavMeshSurface : MonoBehaviour
{
    NavMeshSurface navMeshSurface;
    
    // Start is called before the first frame update
    public void InitializeMesh()
    {
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
