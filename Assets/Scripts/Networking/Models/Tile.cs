using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class Tile
{
    public int position { get; set; }
    public TileType tileType { get; set; }
    public bool isNew { get; set; }
}

public enum TileType
{
    EMPTY = 0,
    SNAKE = 1,
    PELLET = 2,
    WALL = 3,
    SPECIAL = 4,
}