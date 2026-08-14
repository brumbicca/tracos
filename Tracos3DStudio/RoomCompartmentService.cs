namespace Tracos3DStudio;

public static class RoomCompartmentService
{
    public const string DefaultDisplayName = "Cômodo 1";

    public static void EnsureInitialized(Room room, ProjectMetadata metadata)
    {
        if (room.Compartments.Count == 0)
        {
            string name = string.IsNullOrWhiteSpace(metadata.EnvironmentName)
                ? DefaultDisplayName
                : metadata.EnvironmentName.Trim();

            room.Compartments.Add(new RoomCompartment { DisplayName = name });
        }

        Guid defaultId = room.Compartments[0].Id;

        foreach (var wall in room.Walls)
        {
            if (!wall.CompartmentId.HasValue)
                wall.CompartmentId = defaultId;
        }
    }

    public static void SyncPrimaryCompartmentFromEnvironmentName(Room room, ProjectMetadata metadata)
    {
        EnsureInitialized(room, metadata);

        if (room.Compartments.Count != 1 || string.IsNullOrWhiteSpace(metadata.EnvironmentName))
            return;

        room.Compartments[0].DisplayName = metadata.EnvironmentName.Trim();
    }

    public static void SyncEnvironmentNameFromPrimaryCompartment(Room room, ProjectMetadata metadata)
    {
        if (room.Compartments.Count != 1)
            return;

        metadata.EnvironmentName = room.Compartments[0].DisplayName;
    }

    public static RoomCompartment AddCompartment(Room room)
    {
        int number = room.Compartments.Count + 1;
        var compartment = new RoomCompartment
        {
            DisplayName = $"Cômodo {number}"
        };

        room.Compartments.Add(compartment);
        return compartment;
    }

    public static RoomCompartment? FindCompartment(IReadOnlyList<RoomCompartment> compartments, Guid compartmentId)
    {
        foreach (var compartment in compartments)
        {
            if (compartment.Id == compartmentId)
                return compartment;
        }

        return null;
    }

    public static RoomCompartment GetRequiredCompartment(IReadOnlyList<RoomCompartment> compartments, Guid compartmentId) =>
        FindCompartment(compartments, compartmentId)
        ?? throw new KeyNotFoundException($"Cômodo '{compartmentId}' não encontrado.");

    public static int GetCompartmentNumber(RoomCompartment compartment, IReadOnlyList<RoomCompartment> compartments)
    {
        for (int i = 0; i < compartments.Count; i++)
        {
            if (compartments[i].Id == compartment.Id)
                return i + 1;
        }

        return 0;
    }

    public static string FormatCompartmentGroupTitle(RoomCompartment compartment, IReadOnlyList<RoomCompartment> compartments)
    {
        int number = GetCompartmentNumber(compartment, compartments);
        return number > 0
            ? $"Cômodo {number} — {compartment.DisplayName}"
            : compartment.DisplayName;
    }

    public static Guid ResolveWallCompartmentId(WallSegment wall, IReadOnlyList<RoomCompartment> compartments)
    {
        if (wall.CompartmentId.HasValue &&
            FindCompartment(compartments, wall.CompartmentId.Value) != null)
            return wall.CompartmentId.Value;

        return compartments.Count > 0 ? compartments[0].Id : Guid.Empty;
    }
}
