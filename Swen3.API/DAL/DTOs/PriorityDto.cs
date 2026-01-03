namespace Swen3.API.DAL.DTOs
{
    public record PriorityDto(int Id, string Name, int Level);

    public record CreatePriorityDto(string Name, int Level);

    public record UpdatePriorityDto(string Name, int Level);
}

