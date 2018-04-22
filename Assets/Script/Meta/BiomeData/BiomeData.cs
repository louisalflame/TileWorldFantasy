using UnityEngine;

[CreateAssetMenu(menuName = "World/Biome", order = 1)]
public class BiomeData : ScriptableObject {
    public Biome Biome;
    public Color Color;
}
