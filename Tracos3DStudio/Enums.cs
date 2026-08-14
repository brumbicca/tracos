namespace Tracos3DStudio;

public enum WallSide
{
    Center = 0,
    Left = 1,
    Right = 2
}

public enum WallDraftState
{
    Idle,
    Drawing,
    WaitingLength,
    Closed,
    Cancelled
}

/// <summary>Lado da parede que recebe o Comprimento digitado (equivalente à Orientação no Promob).</summary>
public enum WallMeasureSide
{
    Interior,
    Exterior
}

public enum OpeningType
{
    Door,
    Window,
    Passage
}

public enum ModuleCategory
{
    Cozinha,
    Dormitorio,
    Paineis,
    Generico
}

public enum FaceKind
{
    None,
    WallInner,
    WallOuter,
    WallTop,
    WallStartCap,
    WallEndCap,
    Floor,
    Ceiling,
    ModuleFront,
    ModuleBack,
    ModuleLeft,
    ModuleRight,
    ModuleTop,
    ModuleBottom
}

public enum CameraViewMode
{
    Perspective,
    Top,
    Front,
    Left,
    Right
}

public enum WallManualDimensionKind
{
    Linear,
    Angular
}

public enum WallEditorDimensionTool
{
    None,
    Linear,
    Angular
}