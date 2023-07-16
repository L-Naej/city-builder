using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Player", menuName = "CityBuilder/Player", order = 1)]
public class PlayerSO : ScriptableObject
{
    public Dictionary<ResourceSO, int> resourcesAmount;

    //Must be called in the engine initialization sequence (otherwise resourcesAmount won't be initialized)
    public void InitResources(ResourceSO[] resourcesList) 
    {
        resourcesAmount = new Dictionary<ResourceSO, int>(resourcesList.Length);
        foreach (ResourceSO resource in resourcesList) 
        {
            resourcesAmount.Add(resource, resource.InitialAmount);
        }
    }
}
