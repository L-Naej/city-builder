using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Generator", menuName = "CityBuilder/Generator", order = 1)]
public class GeneratorSO : BuildingSO
{
    [Tooltip("The resource to generate")]
    public ResourceSO resourceType;
    [Header("Generation")]
    [Tooltip("Number of seconds between each resource generation")]
    public int generationRate;
    [Tooltip("Amount of resources generated each generationRate seconds")]
    public int generationAmount;
}
