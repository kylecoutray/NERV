using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ClusterCollider : MonoBehaviour
{
    [Tooltip("The exact GameObjects in this cluster")]
    public List<GameObject> clusterObjects;

    [Tooltip("Is clicking this cluster the correct answer?")]
    public bool isCorrect;

    /// <summary>
    /// A representative index for callback (only used if isCorrect==true)
    /// </summary>
    public int representativeIdx
        => (clusterObjects != null && clusterObjects.Count > 0)
            ? clusterObjects[0].GetComponent<StimulusID>().Index
            : -1;

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = isCorrect ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position + box.center, box.size);
    }
}
