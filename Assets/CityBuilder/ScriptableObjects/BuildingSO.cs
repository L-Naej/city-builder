using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Building", menuName = "Buildings/Default Building", order = 1)]
public class BuildingSO : ScriptableObject
{
    public string Name;
    public string Description;
    public Sprite WorldSprite;
    public Sprite UISprite;
    public int Cost;
}
