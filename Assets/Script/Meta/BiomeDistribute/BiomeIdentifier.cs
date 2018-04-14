public interface IBiomeIdentifier
{
    Biome IdentifyBiome(
        float height,
        float temperature,
        float humidity
    );
}

public class BasicBiomeIdentifier : IBiomeIdentifier
{ 
    Biome IBiomeIdentifier.IdentifyBiome(
        float height,
        float temperature,
        float humidity
        )
    {
        return Biome.TROPICAL_DESERT;
    }
}
