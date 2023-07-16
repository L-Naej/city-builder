using UnityEngine;

[CreateAssetMenu(fileName = "Resource", menuName = "CityBuilder/Resources", order = 1)]
public class ResourceSO : ScriptableObject
{
    public string Name;
    public string Description;
    public Sprite WorldSprite;
    public Sprite UISprite;

    public int InitialAmount;
}
