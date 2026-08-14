using System.Linq;
using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class ViewportRenderer
{
    public static void PrepareFrame(int width, int height) => RenderEngine.PrepareFrame(width, height);

    public static void DrawFloor(float gridLimit)
    {
        DrawFloorRect(new Vector2(-gridLimit, -gridLimit), new Vector2(gridLimit, gridLimit));
    }

    public static void DrawFloorRect(Vector2 min, Vector2 max)
    {
        RenderEngine.Color3(0.78f, 0.78f, 0.78f);
        RenderEngine.BeginTriangleBatch();
        RenderEngine.Quad(
            new Vector3(min.X, 0, min.Y),
            new Vector3(max.X, 0, min.Y),
            new Vector3(max.X, 0, max.Y),
            new Vector3(min.X, 0, max.Y));
        RenderEngine.EndTriangleBatch();
    }

    public static void DrawAutomaticRoomFloor(Room room, bool baseSelected = false, Guid? selectedZoneId = null)
    {
        var floor = room.Floor;

        if (floor == null || !floor.Visible || floor.Points.Count < 3)
            return;

        const float yBase = 8f;
        const float yZone = 8.6f;

        var defaultMaterial = FloorMaterialCatalog.TryGet(floor.DefaultMaterialId, out var baseMat) && baseMat != null
            ? baseMat
            : FloorMaterialCatalog.GetDefault();

        bool highlightBase = baseSelected && !selectedZoneId.HasValue;
        FloorPatternRenderer.DrawPolygon(floor.Points, defaultMaterial, yBase, highlightBase);

        foreach (var zone in floor.Zones)
        {
            if (!zone.IsValid)
                continue;

            var zoneMaterial = FloorMaterialCatalog.TryGet(zone.MaterialId, out var zm) && zm != null
                ? zm
                : defaultMaterial;

            bool zoneSelected = selectedZoneId.HasValue && selectedZoneId.Value == zone.Id;
            FloorPatternRenderer.DrawPolygon(zone.ToPoints(), zoneMaterial, yZone, zoneSelected);
        }
    }

    public static void DrawFloorZonePreview(Vector2 a, Vector2 b, float y = 9f)
    {
        var zone = FloorZone.FromCorners(a, b);

        if (!zone.IsValid)
            return;

        var color = new Vector4(0.2f, 0.55f, 0.95f, 0.35f);
        var outline = new Vector4(0.05f, 0.35f, 0.95f, 0.9f);

        RenderEngine.BeginTriangleBatch();
        RenderEngine.Quad(
            new Vector3(zone.MinX, y, zone.MinY),
            new Vector3(zone.MaxX, y, zone.MinY),
            new Vector3(zone.MaxX, y, zone.MaxY),
            new Vector3(zone.MinX, y, zone.MaxY),
            color);
        RenderEngine.EndTriangleBatch(blend: true);

        var loop = new[]
        {
            new Vector3(zone.MinX, y + 0.5f, zone.MinY),
            new Vector3(zone.MaxX, y + 0.5f, zone.MinY),
            new Vector3(zone.MaxX, y + 0.5f, zone.MaxY),
            new Vector3(zone.MinX, y + 0.5f, zone.MaxY)
        };

        RenderEngine.BeginLineBatch();
        RenderEngine.LineLoop(loop, outline);
        RenderEngine.EndLineBatch(2f, depthTest: false);
    }

    public static void DrawFloorPerimeter(IReadOnlyList<Vector2> points, bool selected, bool depthTest = true, float y = 11f)
    {
        if (points.Count < 2)
            return;

        var color = selected
            ? new Vector4(0.05f, 0.35f, 1f, 1f)
            : new Vector4(0.35f, 0.35f, 0.38f, 1f);

        float width = selected ? 2.5f : 1.2f;

        var loop = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++)
            loop[i] = new Vector3(points[i].X, y, points[i].Y);

        RenderEngine.BeginLineBatch();
        RenderEngine.LineLoop(loop, color);
        RenderEngine.EndLineBatch(width, depthTest: depthTest);
    }

    public static void DrawInnerCornerEdges(IReadOnlyList<Vector2> cornerPoints, float yFloor, float yTop)
    {
        if (cornerPoints.Count == 0)
            return;

        var color = new Vector4(0.22f, 0.22f, 0.25f, 1f);

        RenderEngine.BeginLineBatch();

        foreach (var p in cornerPoints)
        {
            RenderEngine.Line(
                new Vector3(p.X, yFloor, p.Y),
                new Vector3(p.X, yTop, p.Y),
                color);
        }

        RenderEngine.EndLineBatch(1.2f, depthTest: false);
    }

    public static void DrawAutomaticRoomCeiling(Room room)
    {
        if (!room.IsClosed || !room.ShowAutomaticCeiling)
            return;

        var ceiling = room.Ceiling;

        if (ceiling == null || !ceiling.Visible)
            return;

        var mesh = ceiling.Mesh;

        if (mesh.Vertices.Count == 0 || mesh.Indices.Count == 0)
            return;

        RenderEngine.Color4(0.96f, 0.96f, 0.98f, 0.55f);
        RenderEngine.BeginTriangleBatch();

        for (int i = 0; i + 2 < mesh.Indices.Count; i += 3)
        {
            var v0 = mesh.Vertices[mesh.Indices[i]];
            var v1 = mesh.Vertices[mesh.Indices[i + 1]];
            var v2 = mesh.Vertices[mesh.Indices[i + 2]];

            RenderEngine.Triangle(
                new Vector3(v0.X, v0.Y, v0.Z),
                new Vector3(v1.X, v1.Y, v1.Z),
                new Vector3(v2.X, v2.Y, v2.Z));
        }

        RenderEngine.EndTriangleBatch(blend: true);
    }

    public static void DrawGrid3D(float gridLimit, float gridStep)
    {
        DrawGridInBounds(new Vector2(-gridLimit, -gridLimit), new Vector2(gridLimit, gridLimit), gridStep);
    }

    public static void DrawUniformGridInBounds(Vector2 min, Vector2 max, float preferredStep, float y = 10f)
    {
        var (cols, rows, stepX, stepY) = GridLayoutService.ComputeUniformDivisions(min, max, preferredStep);
        var gridColor = new Vector4(0.82f, 0.82f, 0.85f, 1f);

        RenderEngine.BeginLineBatch();

        for (int i = 0; i <= cols; i++)
        {
            float x = min.X + i * stepX;
            RenderEngine.Line(new Vector3(x, y, min.Y), new Vector3(x, y, max.Y), gridColor);
        }

        for (int j = 0; j <= rows; j++)
        {
            float z = min.Y + j * stepY;
            RenderEngine.Line(new Vector3(min.X, y, z), new Vector3(max.X, y, z), gridColor);
        }

        RenderEngine.EndLineBatch(1f, depthTest: false);
    }

    public static void DrawGridInBounds(Vector2 min, Vector2 max, float gridStep, float y = 10f)
    {
        DrawUniformGridInBounds(min, max, gridStep, y);
    }

    public static void DrawModules(
        IReadOnlyList<ModuleInstance> modules,
        IReadOnlySet<Guid>? highlightedModuleIds,
        ProjectMetadata metadata,
        IReadOnlySet<Guid>? collidingModuleIds = null,
        Guid? openGroupModuleId = null,
        string? selectedPartLabel = null,
        bool xRay = false)
    {
        foreach (var module in modules)
        {
            bool selected = highlightedModuleIds != null && highlightedModuleIds.Contains(module.Id);
            bool colliding = collidingModuleIds != null && collidingModuleIds.Contains(module.Id);
            bool groupOpen = openGroupModuleId.HasValue && openGroupModuleId.Value == module.Id;
            string? partLabel = groupOpen ? selectedPartLabel : null;
            var fillMode = WallLayerCatalog.GetLayerFillMode(metadata, module.LayerId);

            if (LayerFillModeCatalog.ShouldDrawSolid(fillMode))
            {
                DrawModuleMesh(module, selected, colliding, fillMode, groupOpen, partLabel, xRay);
                // Estilo Promob: arestas pretas só nas faces visíveis (hidden-line / depth-test).
                DrawModuleEdges(module, selected, colliding, groupOpen, partLabel);
            }
            else if (fillMode == LayerFillMode.OutlineOnly)
            {
                DrawModuleEdges(module, selected, colliding, groupOpen, partLabel);
            }

            if (fillMode == LayerFillMode.OutlineOnly || (selected && !groupOpen) || colliding)
                DrawModuleOutline(module, selected, colliding, fillMode);
        }
    }

    public static void DrawModulePreview(
        Vector3 position,
        float width,
        float height,
        float depth,
        float rotationYDegrees,
        bool snappedToWall,
        bool wouldCollide = false)
    {
        var (min, max) = ModulePlacementService.ComputeBounds(position, width, height, depth, rotationYDegrees);

        float alpha = wouldCollide ? 0.5f : snappedToWall ? 0.45f : 0.35f;
        float r = wouldCollide ? 0.95f : snappedToWall ? 0.35f : 0.55f;
        float g = wouldCollide ? 0.25f : snappedToWall ? 0.75f : 0.55f;
        float b = wouldCollide ? 0.2f : snappedToWall ? 0.45f : 0.55f;

        var fillColor = new Vector4(r, g, b, alpha);
        RenderEngine.BeginTriangleBatch();
        AddPreviewBoxFaces(min, max, fillColor);
        RenderEngine.EndTriangleBatch(blend: true);

        var outlineColor = wouldCollide
            ? new Vector4(0.95f, 0.15f, 0.1f, 0.95f)
            : new Vector4(0.1f, 0.85f, 0.35f, 0.95f);

        float y = min.Y;
        RenderEngine.BeginLineBatch();
        RenderEngine.LineLoop(
        [
            new Vector3(min.X, y, min.Z),
            new Vector3(max.X, y, min.Z),
            new Vector3(max.X, y, max.Z),
            new Vector3(min.X, y, max.Z)
        ], outlineColor);
        RenderEngine.EndLineBatch(2f);
    }

    private static void AddPreviewBoxFaces(Vector3 min, Vector3 max, Vector4 color)
    {
        float y0 = min.Y;
        float y1 = max.Y;

        RenderEngine.Quad(
            new Vector3(min.X, y0, min.Z),
            new Vector3(max.X, y0, min.Z),
            new Vector3(max.X, y0, max.Z),
            new Vector3(min.X, y0, max.Z),
            color);

        RenderEngine.Quad(
            new Vector3(min.X, y1, min.Z),
            new Vector3(max.X, y1, min.Z),
            new Vector3(max.X, y1, max.Z),
            new Vector3(min.X, y1, max.Z),
            color);

        RenderEngine.Quad(
            new Vector3(min.X, y0, min.Z),
            new Vector3(min.X, y1, min.Z),
            new Vector3(min.X, y1, max.Z),
            new Vector3(min.X, y0, max.Z),
            color);

        RenderEngine.Quad(
            new Vector3(max.X, y0, min.Z),
            new Vector3(max.X, y1, min.Z),
            new Vector3(max.X, y1, max.Z),
            new Vector3(max.X, y0, max.Z),
            color);

        RenderEngine.Quad(
            new Vector3(min.X, y0, min.Z),
            new Vector3(max.X, y0, min.Z),
            new Vector3(max.X, y1, min.Z),
            new Vector3(min.X, y1, min.Z),
            color);

        RenderEngine.Quad(
            new Vector3(min.X, y0, max.Z),
            new Vector3(max.X, y0, max.Z),
            new Vector3(max.X, y1, max.Z),
            new Vector3(min.X, y1, max.Z),
            color);
    }

    private static void DrawModuleMesh(
        ModuleInstance module,
        bool selected,
        bool colliding,
        LayerFillMode fillMode,
        bool groupOpen = false,
        string? selectedPartLabel = null,
        bool xRay = false)
    {
        var mesh = module.Mesh;

        if (mesh.Indices.Count == 0)
            return;

        var material = MaterialCatalog.TryGet(module.MaterialId, out var mat) && mat != null
            ? mat
            : MaterialCatalog.GetDefault();
        var (frontR, frontG, frontB) = ColorParsing.ParseHexRgb(material.ColorHex);
        float alpha = LayerFillModeCatalog.ResolveSolidAlpha(fillMode, 1f);
        bool blend = fillMode == LayerFillMode.Ghost;

        RenderEngine.BeginTriangleBatch();

        for (int i = 0; i < mesh.Indices.Count; i += 3)
        {
            var face = FindFaceForTriangle(mesh, i);

            // Raio X: omite o preenchimento das frentes (portas/gavetas) para revelar
            // o interior da caixa (fundo, sarrafo, prateleiras). O contorno permanece.
            if (xRay && face?.Kind == FaceKind.ModuleFront)
                continue;

            // Grupo aberto: destacar a peça selecionada por seu label.
            bool partSelected = groupOpen &&
                !string.IsNullOrEmpty(selectedPartLabel) &&
                face != null &&
                face.Label == selectedPartLabel;

            var color = partSelected
                ? new Vector4(1f, 0.62f, 0.18f, 1f)
                : ResolveModuleFaceColor(face?.Kind, selected && !groupOpen, colliding, frontR, frontG, frontB);
            color.W = alpha;

            int i0 = mesh.Indices[i];
            int i1 = mesh.Indices[i + 1];
            int i2 = mesh.Indices[i + 2];
            var v0 = mesh.Vertices[i0];
            var v1 = mesh.Vertices[i1];
            var v2 = mesh.Vertices[i2];

            RenderEngine.Triangle(
                new Vector3(v0.X, v0.Y, v0.Z),
                new Vector3(v1.X, v1.Y, v1.Z),
                new Vector3(v2.X, v2.Y, v2.Z),
                color);
        }

        RenderEngine.EndTriangleBatch(
            blend: blend,
            // Empurra o preenchimento para trás — arestas pretas ficam limpas na face (Promob).
            polygonOffsetFill: true,
            polygonOffsetFactor: 1f,
            polygonOffsetUnits: 1f);
    }

    private static void DrawModuleEdges(
        ModuleInstance module,
        bool selected,
        bool colliding,
        bool groupOpen = false,
        string? selectedPartLabel = null)
    {
        var mesh = module.Mesh;

        if (mesh.Faces.Count == 0)
            return;

        // Contorno preto estilo Promob: só arestas visíveis (depth-test ON).
        // Peças internas aparecem quando a câmera enxerga o vão — sem efeito “raio X”.
        var edgeColor = colliding
            ? new Vector4(0.55f, 0.05f, 0.05f, 1f)
            : (selected && !groupOpen)
                ? new Vector4(0.02f, 0.22f, 0.75f, 1f)
                : new Vector4(0f, 0f, 0f, 1f);

        RenderEngine.BeginLineBatch();

        foreach (var face in mesh.Faces)
        {
            if (face.Vertices is not { Length: >= 2 })
                continue;

            bool partSelected = groupOpen &&
                !string.IsNullOrEmpty(selectedPartLabel) &&
                face.Label == selectedPartLabel;

            RenderEngine.LineLoop(
                face.Vertices,
                partSelected ? new Vector4(0.90f, 0.40f, 0.02f, 1f) : edgeColor);
        }

        RenderEngine.EndLineBatch(
            1.6f,
            depthTest: true,
            polygonOffsetLine: true,
            polygonOffsetFactor: -1.25f,
            polygonOffsetUnits: -1.25f);

        if (groupOpen && !string.IsNullOrEmpty(selectedPartLabel))
        {
            RenderEngine.BeginLineBatch();

            foreach (var face in mesh.Faces)
            {
                if (face.Label == selectedPartLabel && face.Vertices is { Length: >= 2 })
                    RenderEngine.LineLoop(face.Vertices, new Vector4(0.95f, 0.55f, 0.08f, 1f));
            }

            RenderEngine.EndLineBatch(
                2.4f,
                depthTest: true,
                polygonOffsetLine: true,
                polygonOffsetFactor: -1.5f,
                polygonOffsetUnits: -1.5f);
        }
    }

    private static void DrawModuleOutline(
        ModuleInstance module,
        bool selected,
        bool colliding,
        LayerFillMode fillMode)
    {
        var (min, max) = module.GetBounds();
        var color = colliding
            ? new Vector4(0.95f, 0.15f, 0.1f, 1f)
            : selected
                ? new Vector4(0.05f, 0.35f, 1f, 1f)
                : fillMode == LayerFillMode.OutlineOnly
                    ? WallLayerCatalog.GetLayerOutlineColor(module.LayerId)
                    : new Vector4(0.05f, 0.35f, 1f, 1f);

        var bottomLoop = new[]
        {
            new Vector3(min.X, min.Y, min.Z),
            new Vector3(max.X, min.Y, min.Z),
            new Vector3(max.X, min.Y, max.Z),
            new Vector3(min.X, min.Y, max.Z)
        };

        var topLoop = new[]
        {
            new Vector3(min.X, max.Y, min.Z),
            new Vector3(max.X, max.Y, min.Z),
            new Vector3(max.X, max.Y, max.Z),
            new Vector3(min.X, max.Y, max.Z)
        };

        RenderEngine.BeginLineBatch();
        RenderEngine.LineLoop(bottomLoop, color);
        RenderEngine.LineLoop(topLoop, color);
        RenderEngine.EndLineBatch(2.5f, depthTest: false);
    }

    private static SelectableFace? FindFaceForTriangle(MeshData mesh, int triangleStartIndex)
    {
        foreach (var face in mesh.Faces)
        {
            if (face.TriangleStartIndex <= triangleStartIndex &&
                triangleStartIndex < face.TriangleStartIndex + face.TriangleCount * 3)
                return face;
        }

        return null;
    }

    private static Vector4 ResolveModuleFaceColor(
        FaceKind? kind,
        bool selected,
        bool colliding,
        float frontR,
        float frontG,
        float frontB)
    {
        if (colliding)
            return new Vector4(0.92f, 0.35f, 0.32f, 1f);

        if (selected)
            return new Vector4(0.45f, 0.62f, 0.95f, 1f);

        return kind switch
        {
            FaceKind.ModuleFront => new Vector4(frontR, frontG, frontB, 1f),
            FaceKind.ModuleTop => new Vector4(
                Math.Min(frontR + 0.06f, 1f),
                Math.Min(frontG + 0.06f, 1f),
                Math.Min(frontB + 0.06f, 1f),
                1f),
            _ => new Vector4(frontR * 0.9f, frontG * 0.9f, frontB * 0.9f, 1f)
        };
    }

    public static void DrawModuleInsertionCotas(
        WallSegment wall,
        IReadOnlyList<WallSegment> walls,
        Vector3 modulePosition,
        float moduleWidth,
        float moduleHeight,
        float moduleDepth,
        float distanceAlongInner)
    {
        var innerFace = WallInnerFaceService.GetInnerFace(wall, walls);
        float halfWidth = moduleWidth * 0.5f;
        float leftAlong = distanceAlongInner - halfWidth;
        float rightAlong = distanceAlongInner + halfWidth;
        float wallTop = wall.FloorOffset + MathF.Max(wall.HeightStart, wall.HeightEnd);
        float moduleBottom = modulePosition.Y;
        float moduleTop = moduleBottom + moduleHeight;

        Vector3 FacePoint(float alongInner, float y)
        {
            Vector2 floor = innerFace.PointAtDistance(alongInner);
            return new Vector3(floor.X, y, floor.Y);
        }

        var lineColor = new Vector4(0.05f, 0.35f, 0.95f, 1f);
        var tickColor = new Vector4(0.85f, 0.15f, 0.1f, 1f);

        float dimY = moduleBottom + moduleHeight * 0.5f;
        float rulerOffset = -40f;

        RenderEngine.BeginLineBatch();
        RenderEngine.Color4(lineColor.X, lineColor.Y, lineColor.Z, lineColor.W);

        Vector3 wallStart = FacePoint(0f, dimY);
        Vector3 wallEnd = FacePoint(innerFace.Length, dimY);
        Vector3 modLeft = FacePoint(leftAlong, dimY);
        Vector3 modRight = FacePoint(rightAlong, dimY);

        RenderEngine.Line(wallStart, modLeft);
        RenderEngine.Line(modRight, wallEnd);

        RenderEngine.Line(FacePoint(leftAlong, moduleBottom), FacePoint(leftAlong, dimY));
        RenderEngine.Line(FacePoint(rightAlong, moduleBottom), FacePoint(rightAlong, dimY));
        RenderEngine.Line(FacePoint(0f, dimY), FacePoint(0f, moduleBottom));
        RenderEngine.Line(FacePoint(innerFace.Length, dimY), FacePoint(innerFace.Length, moduleBottom));

        Vector3 rulerBase = FacePoint(rulerOffset, wall.FloorOffset);
        Vector3 rulerTop = FacePoint(rulerOffset, wallTop);
        RenderEngine.Color4(tickColor.X, tickColor.Y, tickColor.Z, tickColor.W);
        RenderEngine.Line(rulerBase, rulerTop);

        foreach (float markY in new[] { wall.FloorOffset, moduleBottom, moduleTop, wallTop })
        {
            Vector3 a = FacePoint(rulerOffset - 25f, markY);
            Vector3 b = FacePoint(rulerOffset + 25f, markY);
            RenderEngine.Line(a, b);
        }

        RenderEngine.Color4(lineColor.X, lineColor.Y, lineColor.Z, lineColor.W);
        Vector3 vLeftTop = FacePoint(leftAlong, moduleTop);
        Vector3 floorLeft = FacePoint(leftAlong, wall.FloorOffset);
        Vector3 topLeft = FacePoint(leftAlong, wallTop);

        RenderEngine.Line(floorLeft, FacePoint(leftAlong, moduleBottom));
        RenderEngine.Line(vLeftTop, topLeft);

        RenderEngine.EndLineBatch(2f, depthTest: false);
    }

    public static void DrawAutomaticWallDimensions(
        IReadOnlyList<WallAutomaticDimension> dimensions,
        float yFloor = 120f,
        float tickHalfLength = 35f)
    {
        if (dimensions.Count == 0)
            return;

        var lineColor = new Vector4(0.25f, 0.45f, 0.82f, 1f);
        var tickColor = new Vector4(0.85f, 0.15f, 0.1f, 1f);

        RenderEngine.BeginLineBatch();
        RenderEngine.Color4(lineColor.X, lineColor.Y, lineColor.Z, lineColor.W);

        foreach (var dim in dimensions)
        {
            Vector3 faceA = FloorPoint(dim.FaceStart, yFloor);
            Vector3 faceB = FloorPoint(dim.FaceEnd, yFloor);
            Vector3 dimA = FloorPoint(dim.DimStart, yFloor);
            Vector3 dimB = FloorPoint(dim.DimEnd, yFloor);

            RenderEngine.Line(faceA, dimA);
            RenderEngine.Line(faceB, dimB);
            RenderEngine.Line(dimA, dimB);

            RenderEngine.Color4(tickColor.X, tickColor.Y, tickColor.Z, tickColor.W);
            DrawTick(dimA, dim.DimStart, dim.DimEnd, yFloor, tickHalfLength);
            DrawTick(dimB, dim.DimStart, dim.DimEnd, yFloor, tickHalfLength);
            RenderEngine.Color4(lineColor.X, lineColor.Y, lineColor.Z, lineColor.W);
        }

        RenderEngine.EndLineBatch(2f, depthTest: false);
    }

    private static Vector3 FloorPoint(Vector2 p, float y) => new(p.X, y, p.Y);

    private static void DrawTick(Vector3 onDimLine, Vector2 dimStart, Vector2 dimEnd, float y, float half)
    {
        Vector2 dir = dimEnd - dimStart;
        if (dir.LengthSquared < 0.01f)
            return;

        Vector2 normal = new(-dir.Y, dir.X);
        normal = Vector2.Normalize(normal) * half;

        Vector3 a = new(onDimLine.X + normal.X, y, onDimLine.Z + normal.Y);
        Vector3 b = new(onDimLine.X - normal.X, y, onDimLine.Z - normal.Y);
        RenderEngine.Line(a, b);
    }

    public static void DrawManualWallDimensions(
        IReadOnlyList<WallManualDimension> dimensions,
        Guid? selectedId = null,
        float yFloor = 120f,
        float tickHalfLength = 35f)
    {
        if (dimensions.Count == 0)
            return;

        var lineColor = new Vector4(0.12f, 0.62f, 0.28f, 1f);
        var selectedColor = new Vector4(0.95f, 0.55f, 0.05f, 1f);
        var tickColor = new Vector4(0.85f, 0.15f, 0.1f, 1f);

        RenderEngine.BeginLineBatch();

        foreach (var dim in dimensions)
        {
            bool selected = selectedId.HasValue && dim.Id == selectedId.Value;
            var color = selected ? selectedColor : lineColor;
            RenderEngine.Color4(color.X, color.Y, color.Z, color.W);

            if (dim.Kind == WallManualDimensionKind.Linear)
            {
                Vector3 faceA = FloorPoint(dim.PointA, yFloor);
                Vector3 faceB = FloorPoint(dim.PointB, yFloor);
                Vector3 dimA = FloorPoint(dim.DimStart, yFloor);
                Vector3 dimB = FloorPoint(dim.DimEnd, yFloor);

                RenderEngine.Line(faceA, dimA);
                RenderEngine.Line(faceB, dimB);
                RenderEngine.Line(dimA, dimB);

                RenderEngine.Color4(tickColor.X, tickColor.Y, tickColor.Z, tickColor.W);
                DrawTick(dimA, dim.DimStart, dim.DimEnd, yFloor, tickHalfLength);
                DrawTick(dimB, dim.DimStart, dim.DimEnd, yFloor, tickHalfLength);
            }
            else
            {
                Vector3 vertex = FloorPoint(dim.PointB, yFloor);
                Vector3 legA = FloorPoint(dim.PointA, yFloor);
                Vector3 legC = FloorPoint(dim.PointC, yFloor);
                RenderEngine.Line(vertex, legA);
                RenderEngine.Line(vertex, legC);
                DrawArcTicks(dim, yFloor, tickHalfLength);
            }
        }

        RenderEngine.EndLineBatch(2.2f, depthTest: false);
    }

    public static void DrawManualDimensionPreview(
        WallEditorDimensionTool tool,
        int step,
        Vector2 pointA,
        Vector2 pointB,
        Vector2 preview,
        float yFloor = 120f)
    {
        var previewColor = new Vector4(0.35f, 0.85f, 0.45f, 0.95f);

        RenderEngine.BeginLineBatch();
        RenderEngine.Color4(previewColor.X, previewColor.Y, previewColor.Z, previewColor.W);

        if (tool == WallEditorDimensionTool.Linear)
        {
            if (step >= 1)
            {
                var previewDim = WallManualDimensionService.TryCreateLinear(pointA, preview, preview);

                if (previewDim != null)
                {
                    RenderEngine.Line(FloorPoint(pointA, yFloor), FloorPoint(previewDim.DimStart, yFloor));
                    RenderEngine.Line(FloorPoint(preview, yFloor), FloorPoint(previewDim.DimEnd, yFloor));
                    RenderEngine.Line(
                        FloorPoint(previewDim.DimStart, yFloor),
                        FloorPoint(previewDim.DimEnd, yFloor));
                }
            }
            else
            {
                RenderEngine.Line(FloorPoint(pointA, yFloor), FloorPoint(preview, yFloor));
            }
        }
        else if (tool == WallEditorDimensionTool.Angular)
        {
            if (step >= 2)
            {
                var previewDim = WallManualDimensionService.TryCreateAngular(pointA, pointB, preview, preview);

                if (previewDim != null)
                {
                    Vector3 vertex = FloorPoint(pointB, yFloor);
                    RenderEngine.Line(vertex, FloorPoint(pointA, yFloor));
                    RenderEngine.Line(vertex, FloorPoint(preview, yFloor));
                    DrawArcTicks(previewDim, yFloor, 30f);
                }
            }
            else if (step >= 1)
            {
                RenderEngine.Line(FloorPoint(pointB, yFloor), FloorPoint(preview, yFloor));
            }
            else
            {
                RenderEngine.Line(FloorPoint(pointA, yFloor), FloorPoint(preview, yFloor));
            }
        }

        RenderEngine.EndLineBatch(2f, depthTest: false);
    }

    private static void DrawArcTicks(WallManualDimension dim, float yFloor, float tickHalfLength)
    {
        Vector2 ba = dim.PointA - dim.PointB;
        Vector2 bc = dim.PointC - dim.PointB;

        if (ba.LengthSquared < 0.01f || bc.LengthSquared < 0.01f)
            return;

        float startAngle = MathF.Atan2(ba.Y, ba.X);
        float endAngle = MathF.Atan2(bc.Y, bc.X);
        float sweep = endAngle - startAngle;

        while (sweep <= -MathF.PI)
            sweep += MathF.Tau;

        while (sweep > MathF.PI)
            sweep -= MathF.Tau;

        int segments = Math.Max(4, (int)(MathF.Abs(sweep) / MathF.PI * 12f));
        Vector3 prev = FloorPoint(dim.DimStart, yFloor);

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = startAngle + sweep * t;
            Vector2 arcPoint = dim.PointB + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dim.ArcRadius;
            Vector3 next = FloorPoint(arcPoint, yFloor);
            RenderEngine.Line(prev, next);
            prev = next;
        }

        var tickColor = new Vector4(0.85f, 0.15f, 0.1f, 1f);
        RenderEngine.Color4(tickColor.X, tickColor.Y, tickColor.Z, tickColor.W);
        DrawTick(FloorPoint(dim.DimStart, yFloor), dim.DimStart, dim.DimEnd, yFloor, tickHalfLength);
        DrawTick(FloorPoint(dim.DimEnd, yFloor), dim.DimStart, dim.DimEnd, yFloor, tickHalfLength);
    }

    /// <summary>
    /// Setas de dimensão da peça selecionada (estilo Promob). A seta apontada por
    /// <paramref name="selectedHandle"/> é destacada em verde; as demais em vermelho.
    /// </summary>
    public static void DrawPartHandles(
        ModuleInstance module,
        string partLabel,
        PartHandle? selectedHandle)
    {
        if (string.IsNullOrEmpty(partLabel))
            return;

        var red = new Vector4(0.90f, 0.12f, 0.10f, 1f);
        var green = new Vector4(0.12f, 0.75f, 0.20f, 1f);

        RenderEngine.BeginLineBatch();

        foreach (var handle in ModulePartHandleService.AllHandles())
        {
            if (!ModulePartHandleService.TryGetHandleSegment(module, partLabel, handle, out var a, out var b))
                continue;

            bool sel = selectedHandle.HasValue && selectedHandle.Value == handle;
            var color = sel ? green : red;

            RenderEngine.Line(a, b, color);
            DrawArrowHead(a, a - b, color);
            DrawArrowHead(b, b - a, color);
        }

        RenderEngine.EndLineBatch(2.6f, depthTest: false);
    }

    private static void DrawArrowHead(Vector3 tip, Vector3 outward, Vector4 color)
    {
        if (outward.LengthSquared < 1e-6f)
            return;

        Vector3 dir = Vector3.Normalize(outward);

        // Perpendicular estável (evita cross nulo quando dir ≈ up).
        Vector3 reference = MathF.Abs(dir.Y) > 0.9f ? Vector3.UnitX : Vector3.UnitY;
        Vector3 perp1 = Vector3.Normalize(Vector3.Cross(dir, reference));
        Vector3 perp2 = Vector3.Normalize(Vector3.Cross(dir, perp1));

        const float headLen = 26f;
        const float headWidth = 16f;
        Vector3 baseCenter = tip - dir * headLen;

        RenderEngine.Line(tip, baseCenter + perp1 * headWidth, color);
        RenderEngine.Line(tip, baseCenter - perp1 * headWidth, color);
        RenderEngine.Line(tip, baseCenter + perp2 * headWidth, color);
        RenderEngine.Line(tip, baseCenter - perp2 * headWidth, color);
    }

    public static void DrawWallReferenceGuide(
        WallReferencePick pick,
        float signedOffsetMm,
        float yFloor = 120f,
        float tickHalfLength = 35f)
    {
        Vector2 offsetEnd = pick.AnchorOnInnerFace + pick.InteriorNormal * signedOffsetMm;
        var lineColor = new Vector4(0.05f, 0.35f, 0.95f, 1f);
        var tickColor = new Vector4(0.85f, 0.15f, 0.1f, 1f);

        Vector3 anchor = FloorPoint(pick.AnchorOnInnerFace, yFloor);
        Vector3 target = FloorPoint(offsetEnd, yFloor);

        RenderEngine.BeginLineBatch();
        RenderEngine.Color4(lineColor.X, lineColor.Y, lineColor.Z, lineColor.W);
        RenderEngine.Line(anchor, target);

        Vector2 wallDir = pick.WallDirection;
        if (wallDir.LengthSquared > 0.01f)
        {
            wallDir = Vector2.Normalize(wallDir) * tickHalfLength;
            Vector3 tickA = FloorPoint(pick.AnchorOnInnerFace + wallDir, yFloor);
            Vector3 tickB = FloorPoint(pick.AnchorOnInnerFace - wallDir, yFloor);
            RenderEngine.Line(tickA, tickB);
        }

        RenderEngine.Color4(tickColor.X, tickColor.Y, tickColor.Z, tickColor.W);
        DrawTick(target, pick.AnchorOnInnerFace, offsetEnd, yFloor, tickHalfLength);
        DrawTick(anchor, pick.AnchorOnInnerFace, offsetEnd, yFloor, tickHalfLength);

        RenderEngine.EndLineBatch(2.5f, depthTest: false);
    }

    public static void DrawWallSegmentSplitPreview(
        WallSegment wall,
        float distanceAlong,
        float yFloor = 120f,
        float tickHalfLength = 45f)
    {
        if (!WallSegmentationService.CanSplit(wall, distanceAlong))
            return;

        Vector2 splitPoint = wall.GetPointAtDistance(distanceAlong);
        Vector2 dir = wall.Direction;
        Vector2 normal = Vector2.Normalize(new Vector2(-dir.Y, dir.X)) * tickHalfLength;

        var color = new Vector4(0.9f, 0.35f, 0.05f, 1f);
        Vector3 center = FloorPoint(splitPoint, yFloor);
        Vector3 a = FloorPoint(splitPoint + normal, yFloor);
        Vector3 b = FloorPoint(splitPoint - normal, yFloor);

        RenderEngine.BeginLineBatch();
        RenderEngine.Color4(color.X, color.Y, color.Z, color.W);
        RenderEngine.Line(a, b);
        RenderEngine.EndLineBatch(2.5f, depthTest: false);
    }

    public static void DrawWallChamferHotpoint(
        Vector2 vertex,
        float yFloor = 120f,
        float radius = 55f)
    {
        var color = new Vector4(1f, 0.55f, 0.1f, 1f);
        Vector3 center = FloorPoint(vertex, yFloor);

        RenderEngine.BeginLineBatch();
        RenderEngine.Color4(color.X, color.Y, color.Z, color.W);

        const int segments = 14;
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * MathF.Tau / segments;
            float a1 = (i + 1) * MathF.Tau / segments;
            Vector3 p0 = center + new Vector3(MathF.Cos(a0) * radius, 0f, MathF.Sin(a0) * radius);
            Vector3 p1 = center + new Vector3(MathF.Cos(a1) * radius, 0f, MathF.Sin(a1) * radius);
            RenderEngine.Line(p0, p1);
        }

        RenderEngine.Line(center + new Vector3(-radius, 0f, 0f), center + new Vector3(radius, 0f, 0f));
        RenderEngine.Line(center + new Vector3(0f, 0f, -radius), center + new Vector3(0f, 0f, radius));
        RenderEngine.EndLineBatch(2.5f, depthTest: false);
    }

    public static void DrawWallFlechaHotpoint(
        Vector2 vertex,
        float yFloor = 120f,
        float radius = 55f)
    {
        var color = new Vector4(0.15f, 0.82f, 0.28f, 1f);
        Vector3 center = FloorPoint(vertex, yFloor);

        RenderEngine.BeginLineBatch();
        RenderEngine.Color4(color.X, color.Y, color.Z, color.W);

        const int segments = 14;
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * MathF.Tau / segments;
            float a1 = (i + 1) * MathF.Tau / segments;
            Vector3 p0 = center + new Vector3(MathF.Cos(a0) * radius, 0f, MathF.Sin(a0) * radius);
            Vector3 p1 = center + new Vector3(MathF.Cos(a1) * radius, 0f, MathF.Sin(a1) * radius);
            RenderEngine.Line(p0, p1);
        }

        RenderEngine.EndLineBatch(2.5f, depthTest: false);

        RenderEngine.BeginTriangleBatch();
        RenderEngine.Color4(color.X, color.Y, color.Z, 0.35f);
        RenderEngine.PointMarker(center, radius * 0.55f);
        RenderEngine.EndTriangleBatch(depthTest: false);
    }

    public static void DrawWallMoveGuide(
        Vector2 originalStart,
        Vector2 originalEnd,
        Vector2 previewDelta,
        IReadOnlyList<WallSegment> walls,
        Guid wallId,
        float yFloor = 120f,
        float tickHalfLength = 35f)
    {
        if (previewDelta.LengthSquared < 1f)
            return;

        var originalWall = walls.FirstOrDefault(w => w.Id == wallId);

        if (originalWall == null)
            return;

        var previewWall = new WallSegment(originalStart + previewDelta, originalEnd + previewDelta)
        {
            Thickness = originalWall.Thickness,
            MeasureSide = originalWall.MeasureSide,
            Orientation = originalWall.Orientation
        };

        var innerOriginal = WallInnerFaceService.GetInnerFace(
            new WallSegment(originalStart, originalEnd)
            {
                Thickness = originalWall.Thickness,
                MeasureSide = originalWall.MeasureSide,
                Orientation = originalWall.Orientation,
                Id = wallId
            },
            walls);

        var innerPreview = WallInnerFaceService.GetInnerFace(previewWall, walls);
        Vector2 midOriginal = (innerOriginal.InnerStart + innerOriginal.InnerEnd) * 0.5f;
        Vector2 midPreview = (innerPreview.InnerStart + innerPreview.InnerEnd) * 0.5f;

        var lineColor = new Vector4(0.05f, 0.35f, 0.95f, 1f);
        var tickColor = new Vector4(0.85f, 0.15f, 0.1f, 1f);
        var ghostColor = new Vector4(0.55f, 0.55f, 0.55f, 0.65f);

        RenderEngine.BeginLineBatch();
        RenderEngine.Color4(ghostColor.X, ghostColor.Y, ghostColor.Z, ghostColor.W);
        RenderEngine.Line(FloorPoint(innerOriginal.InnerStart, yFloor), FloorPoint(innerOriginal.InnerEnd, yFloor));

        RenderEngine.Color4(lineColor.X, lineColor.Y, lineColor.Z, lineColor.W);
        RenderEngine.Line(FloorPoint(midOriginal, yFloor), FloorPoint(midPreview, yFloor));

        RenderEngine.Color4(tickColor.X, tickColor.Y, tickColor.Z, tickColor.W);
        DrawTick(FloorPoint(midOriginal, yFloor), midOriginal, midPreview, yFloor, tickHalfLength);
        DrawTick(FloorPoint(midPreview, yFloor), midOriginal, midPreview, yFloor, tickHalfLength);

        RenderEngine.EndLineBatch(2.5f, depthTest: false);
    }
}
