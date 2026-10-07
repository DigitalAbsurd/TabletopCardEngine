namespace TabletopCardEngine.Data;

internal class Player
{
    public Guid GameId { get; set; }
    public Game Game { get; set; } = new();
    public byte SeatNo { get; set; }
    public string Name { get; set; } = string.Empty;
    public int HandNoStart { get; set; }
    public int? HandNoEnd { get; set; }
}
