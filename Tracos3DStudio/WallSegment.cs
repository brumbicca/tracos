using OpenTK.Mathematics;

namespace Tracos3DStudio;

public enum WallOrientation
{
    Right,
    Left,
    Center
}

public class WallSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Vector2 Start { get; set; }

    public Vector2 End { get; set; }

    // Pé-direito Inicial (altura na extremidade Start)
    public float HeightStart { get; set; } = 2600f;

    // Pé-direito Final (altura na extremidade End)
    public float HeightEnd { get; set; } = 2600f;

    // Backward-compat: get retorna HeightStart; set define ambos
    public float Height
    {
        get => HeightStart;
        set { HeightStart = value; HeightEnd = value; }
    }

    public float Thickness { get; set; } = 150f;

    public WallOrientation Orientation { get; set; } = WallOrientation.Right;

    /// <summary>Lado que recebe o Comprimento (Orientação Promob: interna ou externa ao ambiente).</summary>
    public WallMeasureSide MeasureSide { get; set; } = WallMeasureSide.Interior;

    // Afastamento Piso (distância do piso)
    public float FloorOffset { get; set; } = 0f;

    // Cotas (afastamentos/posição do módulo na parede)
    public float CotaAnterior { get; set; } = 0f;
    public float CotaPosterior { get; set; } = 0f;
    public float CotaInferior { get; set; } = 0f;
    public float CotaSuperior { get; set; } = 0f;

    // Desenho
    public bool DrawBottomFace { get; set; } = false;

    // Outras
    public bool IsMovable { get; set; } = false;
    public bool IsVisible { get; set; } = true;

    public WallConstructionType ConstructionType { get; set; } = WallConstructionType.Normal;

    /// <summary>Camada Promob (parede, divisória, referência…).</summary>
    public string LayerId { get; set; } = WallLayerCatalog.DefaultLayerId;

    /// <summary>Cômodo ao qual a parede pertence (lista Ambiente).</summary>
    public Guid? CompartmentId { get; set; }

    /// <summary>Material na face interna livre (sem faixa/região) — C.2.1.</summary>
    public string? InternalFaceMaterialId { get; set; }

    /// <summary>Material na face externa livre (sem faixa/região) — C.2.1.</summary>
    public string? ExternalFaceMaterialId { get; set; }

    public string? GetFaceMaterialId(FaceType face) =>
        face == FaceType.Internal ? InternalFaceMaterialId : ExternalFaceMaterialId;

    public void SetFaceMaterialId(FaceType face, string? materialId)
    {
        if (face == FaceType.Internal)
            InternalFaceMaterialId = materialId;
        else
            ExternalFaceMaterialId = materialId;
    }

    public List<WallBand> Bands { get; } = new();

    public List<WallRegion> Regions { get; } = new();

    /// <summary>Chanfro manual (Aparar Parede) na extremidade Start, em mm ao longo do eixo.</summary>
    public float ChamferStartMm { get; set; } = 0f;

    /// <summary>Chanfro manual (Aparar Parede) na extremidade End, em mm ao longo do eixo.</summary>
    public float ChamferEndMm { get; set; } = 0f;

    /// <summary>Flecha da curva (sagitta em mm, positivo = esquerda da corda Start→End). Zero = reta.</summary>
    public float FlechaMm { get; set; } = 0f;

    /// <summary>Comprimento desejado (mm) na face de referência (Orientação), definido pelo usuário.</summary>
    public float? InnerLengthTarget { get; set; }

    public List<WallOpening> Openings { get; } = new();

    public MeshData Mesh { get; } = new();

    public float Length
    {
        get
        {
            if (MathF.Abs(FlechaMm) > WallArcGeometry.StraightToleranceMm)
                return WallArcGeometry.FromWall(this).ArcLength;

            return (End - Start).Length;
        }
    }

    public Vector2 Direction
    {
        get
        {
            if (Length <= 0.001f)
                return Vector2.UnitX;

            return Vector2.Normalize(End - Start);
        }
    }

    // Ângulo Absoluto em graus (relativo ao eixo X do mundo)
    public float AngleAbsoluteDegrees
    {
        get
        {
            var d = Direction;
            return MathHelper.RadiansToDegrees(MathF.Atan2(d.Y, d.X));
        }
    }

    // Interpolação linear da altura em um ponto a distância d do Start
    public float HeightAtDistance(float d)
    {
        float len = Length;
        if (len < 0.001f)
            return HeightStart;
        float t = Math.Clamp(d / len, 0f, 1f);
        return HeightStart + (HeightEnd - HeightStart) * t;
    }

    public Vector2 LeftNormal => new(-Direction.Y, Direction.X);

    public Vector2 RightNormal => -LeftNormal;

    public WallSegment(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }

    public WallSegment(Vector2 start, Vector2 end, float thickness, float height, WallOrientation orientation)
    {
        Start = start;
        End = end;
        Thickness = thickness;
        HeightStart = height;
        HeightEnd = height;
        Orientation = orientation;
    }

    public Vector2 GetOrientationOffset()
    {
        return Orientation switch
        {
            WallOrientation.Left => LeftNormal * (Thickness / 2f),
            WallOrientation.Right => RightNormal * (Thickness / 2f),
            WallOrientation.Center => Vector2.Zero,
            _ => Vector2.Zero
        };
    }

    public Vector2 GetPointAtDistance(float distanceMm)
    {
        if (MathF.Abs(FlechaMm) > WallArcGeometry.StraightToleranceMm)
        {
            var arc = WallArcGeometry.FromWall(this);
            return arc.GetPointAtArcLength(Math.Clamp(distanceMm, 0f, arc.ArcLength));
        }

        var distance = Math.Clamp(distanceMm, 0, Length);
        return Start + Direction * distance;
    }

    public float GetDistanceAlongWall(Vector2 point)
    {
        if (MathF.Abs(FlechaMm) > WallArcGeometry.StraightToleranceMm)
            return WallArcGeometry.FromWall(this).ProjectToArcLength(point);

        var delta = point - Start;
        return Vector2.Dot(delta, Direction);
    }

    public bool ContainsDistance(float distanceMm)
    {
        return distanceMm >= 0 && distanceMm <= Length;
    }

    public void AddDoor(float distanceFromStart, float width = 800f, float height = 2100f)
    {
        Openings.Add(WallOpening.Door(distanceFromStart, width, height));
    }

    public void AddWindow(float distanceFromStart, float width = 1200f, float height = 1000f, float sillHeight = 1100f)
    {
        Openings.Add(WallOpening.Window(distanceFromStart, width, height, sillHeight));
    }

    public WallOpening? FindOpeningById(Guid id)
    {
        foreach (var opening in Openings)
        {
            if (opening.Id == id)
                return opening;
        }

        return null;
    }

    public bool RemoveOpening(Guid id)
    {
        for (var i = 0; i < Openings.Count; i++)
        {
            if (Openings[i].Id != id)
                continue;

            Openings.RemoveAt(i);
            return true;
        }

        return false;
    }
}