namespace MediaServer;

public class Rating
{
    public int Id { get; set; }
    public int MediaId { get; set; }
    public int UserId { get; set; }
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime Timestamp { get; set; }
    public int LikeCount { get; set; }

}