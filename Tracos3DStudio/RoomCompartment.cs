namespace Tracos3DStudio;

public sealed class RoomCompartment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string DisplayName { get; set; } = RoomCompartmentService.DefaultDisplayName;
}
