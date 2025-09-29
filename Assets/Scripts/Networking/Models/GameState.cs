using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class GameState
{
    public string userId { get; set; }
    public List<Tile> tiles { get; set; }
    public int pellet { get; set; }
    public int special { get; set; }
    public Directions direction { get; set; }
    public Directions nextDirection { get; set; }
    public bool isGameRunning { get; set; }
    public float speed { get; set; }
    public float elapsedTime { get; set; }
    public bool InSpecial { get; set; }
    public bool InZoom { get; set; }
    public bool InSlow { get; set; }
    public bool InInvincible { get; set; }
    public bool CanSpawnSpecial { get; set; }

    override public string ToString()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(this);
    }
}
