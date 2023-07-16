using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Building", menuName = "CityBuilder/Building", order = 1)]
public class BuildingSO : ScriptableObject
{
    public string Name;
    public string Description;
    public Sprite WorldSprite;
    public Sprite UISprite;
    public Cost Cost;
}

[Serializable]
public struct Cost 
{
    public ResourceSO Resource;
    public int Amount;
}