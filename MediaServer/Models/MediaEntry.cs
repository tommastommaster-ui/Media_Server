using System.Net;
using System.Text.Json;
using Npgsql;

namespace MediaServer;

public class MediaEntry
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string MediaType { get; set; } = "";
    public int ReleaseYear{ get; set; }
    public int AgeRestriction{ get; set; }
    public List<string> Genres { get; set; }
    public int CreatorId{ get; set; }

    
    
    
}