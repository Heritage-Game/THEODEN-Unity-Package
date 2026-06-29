public class Position
{
    public Position()
    {
    }

    public Position(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public float x { get; set; }
    public float y { get; set; }
}

public class Codex
{
    public Codex()
    {
    }

    public Codex(int id, string path, string name, string printableName, int stopType, string linkMaps,
        Position position)
    {
        this.id = id;
        this.path = path;
        this.name = name;
        printable_name = printableName;
        stop_type = stopType;
        link_maps = linkMaps;
        this.position = position;
    }

    public int id { get; set; }
    public string path { get; set; }
    public string name { get; set; }
    public string printable_name { get; set; }
    public int stop_type { get; set; }
    public string link_maps { get; set; }
    public Position position { get; set; }
}