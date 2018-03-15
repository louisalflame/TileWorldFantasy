using System.Collections.Generic;

public interface IWorldMap
{
}

public class WorldMap : IWorldMap
{
    private List<TileUnit> _map = new List<TileUnit>();

    private const int MAP_WIDTH = 1000;
    private const int MAP_HEIGHT = 1000;

    public void Initialize()
    {

    }
}