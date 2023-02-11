using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingBehavior : MonoBehaviour, IPointerDownHandler
{
    public BuildingSO building;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("TADA");
        var truc = new GameObject("POUET");
    }

    public void OnMouseDown()
    {
        Debug.Log($"Je suis le batiment: {building.Name}");
    }
}
