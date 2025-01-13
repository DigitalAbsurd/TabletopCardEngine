namespace OpenHoldemEngine.Data;

public class Player
{
    public Guid GameId { get; set; }
    public Game Game { get; set; } = new();
    public byte SeatNo { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RoundNoStart { get; set; }
    public int? RoundNoEnd { get; set; }
}