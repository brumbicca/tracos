using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Engenharia dos balcões especiais para coluna. O vazio da coluna é retirado da
/// traseira da caixaria; não é um sólido decorativo acrescentado ao módulo.
/// </summary>
public static class SpecialColumnModuleBuilder
{
    public static bool Build(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        instance.SpecialColumn ??= SpecialColumnParams.FromDefinition(definition);
        instance.SpecialColumn.ClampToModule(instance.Width, instance.Depth);
        var column = instance.SpecialColumn;

        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        var effective = DimensionConfiguratorService.CreateEffectiveRules(definition, settings);
        var structure = effective?.Structure
            ?? ModulationRulesPresets.CreateStandardBox(definition.DoorCount, definition.DrawerCount).Structure;

        float t = Math.Clamp(structure.PanelThicknessMm, 1f, MathF.Max(1f, (w - 2f) * .45f));
        float bt = Math.Clamp(structure.BackThicknessMm, 1f, MathF.Max(1f, d - 2f));
        float ft = Math.Clamp(structure.FrontThicknessMm > 0f
            ? structure.FrontThicknessMm : definition.FrontThickness, 1f, 50f);
        float baseY = MathF.Abs(structure.LateralBaseOverlapMm) >= MathF.Abs(structure.LateralBottomRecessMm)
            ? structure.LateralBaseOverlapMm : structure.LateralBottomRecessMm;
        float backPlane = GetBackPlaneZ(structure, bt);
        var (columnStart, columnEnd) = column.GetHorizontalRange(w);

        Vector3 ToWorld(Vector3 p) => ModulePlacementService.TransformLocalPoint(
            p, instance.Position, instance.RotationYDegrees);

        float lateralGap = Math.Clamp(structure.LateralDepthGapMm, -d * .5f, MathF.Max(0f, d - 10f));
        float lateralZ0 = structure.LateralDepthAlignment switch
        {
            LateralDepthAlignment.Back => lateralGap,
            LateralDepthAlignment.Center => lateralGap * .5f,
            _ => 0f
        };
        float lateralZ1 = structure.LateralDepthAlignment switch
        {
            LateralDepthAlignment.Front => d - lateralGap,
            LateralDepthAlignment.Center => d - lateralGap * .5f,
            _ => d
        };

        float leftZ0 = column.Position == SpecialColumnPosition.Left
            ? MathF.Max(lateralZ0, column.DepthMm) : lateralZ0;
        float rightZ0 = column.Position == SpecialColumnPosition.Right
            ? MathF.Max(lateralZ0, column.DepthMm) : lateralZ0;
        AddBox(instance, ToWorld, new(0f, baseY, leftZ0), new(t, h, lateralZ1),
            FaceKind.ModuleLeft, "Lateral esq.");
        AddBox(instance, ToWorld, new(w - t, baseY, rightZ0), new(w, h, lateralZ1),
            FaceKind.ModuleRight, "Lateral dir.");

        BuildBack(instance, ToWorld, structure, column.Position,
            w, h, d, t, bt, baseY, columnStart, columnEnd);
        BuildColumnBackContour(instance, ToWorld, column.Position, w, h, baseY + t, t,
            columnStart, columnEnd, column.DepthMm);

        float baseOverSide = Math.Clamp(structure.BaseAdvanceOverLateralMm, -t, t);
        AddNotchedPanel(instance, ToWorld,
            t - baseOverSide, w - t + baseOverSide,
            0f, d,
            baseY, baseY + t, columnStart, columnEnd, column.DepthMm,
            "Base inferior");

        BuildSarrafos(instance, ToWorld, structure, column.Position, w, h, d, t, backPlane,
            columnStart, columnEnd, column.DepthMm);
        BuildShelves(instance, ToWorld, structure, column.Position, w, h, d, t, bt,
            columnStart, columnEnd, column.DepthMm, column.ShelfNotched);

        ModuleMeshBuilder.AddFronts(instance.Mesh, instance.Id, ToWorld,
            w, h, d, ft, structure, instance.PartOverrides);
        return true;
    }

    /// <summary>
    /// Travessas verticais que fecham o contorno do pilar, seguindo a montagem do
    /// Canto L. Nas extremidades o recorte tem duas faces; no centro forma um U com
    /// três faces (uma frontal e duas laterais).
    /// </summary>
    private static void BuildColumnBackContour(
        ModuleInstance instance,
        Func<Vector3, Vector3> toWorld,
        SpecialColumnPosition position,
        float moduleWidth,
        float moduleHeight,
        float baseTop,
        float thickness,
        float columnStart,
        float columnEnd,
        float columnDepth)
    {
        // O contorno do recorte não recebe fundo: são travessas estruturais de 18 mm,
        // iguais às do Canto L. O fundo fino existe apenas no plano junto à parede.
        float frontZ0 = Math.Clamp(columnDepth, thickness, instance.Depth - thickness);
        float frontZ1 = MathF.Min(instance.Depth, frontZ0 + thickness);
        float frontX0 = position switch
        {
            SpecialColumnPosition.Left => thickness,
            _ => columnStart - thickness
        };
        float frontX1 = position switch
        {
            SpecialColumnPosition.Right => moduleWidth - thickness,
            _ => columnEnd + thickness
        };
        AddBox(instance, toWorld,
            new(frontX0, baseTop, frontZ0),
            new(frontX1, moduleHeight, frontZ1),
            FaceKind.ModuleBack,
            "Travessa traseira frontal da coluna");

        if (position is SpecialColumnPosition.Center or SpecialColumnPosition.Right)
        {
            float x1 = Math.Clamp(columnStart, thickness, moduleWidth - thickness);
            AddBox(instance, toWorld,
                new(x1 - thickness, baseTop, 0f),
                new(x1, moduleHeight, frontZ0),
                FaceKind.ModuleBack,
                "Travessa traseira lateral esquerda da coluna");
        }

        if (position is SpecialColumnPosition.Center or SpecialColumnPosition.Left)
        {
            float x0 = Math.Clamp(columnEnd, thickness, moduleWidth - thickness);
            AddBox(instance, toWorld,
                new(x0, baseTop, 0f),
                new(x0 + thickness, moduleHeight, frontZ0),
                FaceKind.ModuleBack,
                "Travessa traseira lateral direita da coluna");
        }
    }

    private static void BuildBack(
        ModuleInstance instance, Func<Vector3, Vector3> toWorld, ModulationStructure s,
        SpecialColumnPosition position,
        float w, float h, float d, float t, float bt, float baseY, float col0, float col1)
    {
        if (s.BackPanelLayout == BoxBackPanelLayout.SemFundo)
            return;

        float z0 = s.BackPanelType == BoxBackPanelType.Pregado ? 0f : s.BackRecessMm;
        float afl = Math.Clamp(s.BackAdvanceOverLateralMm, -t, t);
        float alf = Math.Clamp(s.LateralAdvanceOverBackMm, -t, t);
        float afb = Math.Clamp(s.BackAdvanceOverBaseMm, -t, t);
        float x0 = Math.Clamp(t - afl + alf, -t, w * .45f);
        float x1 = Math.Clamp(w - t + afl - alf, w * .55f, w + t);
        float y0 = t + baseY - afb;
        float rail = Math.Clamp(s.CrossRailWidthMm > 0f ? s.CrossRailWidthMm : MathF.Max(40f, t * 3f),
            10f, MathF.Max(10f, MathF.Min(w, h) * .45f));

        if (s.BackPanelLayout == BoxBackPanelLayout.TravessaHorizontal)
        {
            float lower = Math.Clamp(s.BackLowerRailOffsetMm, -h * .5f, h - 10f);
            float upperTop = h - Math.Clamp(s.BackUpperRailOffsetMm, -h * .5f, h - 10f);
            foreach (var range in SplitBackRange(x0, x1,
                         InternalCutStart(position, col0, t), InternalCutEnd(position, col1, t),
                         s.BackAdvanceOverLateralMm))
            {
                AddBox(instance, toWorld, new(range.Min, lower, z0), new(range.Max, lower + rail, z0 + t),
                    FaceKind.ModuleBack, "Travessa traseira inferior");
                AddBox(instance, toWorld, new(range.Min, upperTop - rail, z0), new(range.Max, upperTop, z0 + t),
                    FaceKind.ModuleBack, "Travessa traseira superior");
            }
            return;
        }

        if (s.BackPanelLayout == BoxBackPanelLayout.TravessaVertical)
        {
            foreach (var range in SplitBackRange(x0, x1,
                         InternalCutStart(position, col0, t), InternalCutEnd(position, col1, t),
                         s.BackAdvanceOverLateralMm))
            {
                float rw = MathF.Min(rail, range.Max - range.Min);
                AddBox(instance, toWorld, new(range.Min, y0, z0), new(range.Min + rw, h, z0 + t),
                    FaceKind.ModuleBack, "Travessa traseira");
            }
            return;
        }

        float top = s.BackPanelLayout == BoxBackPanelLayout.Rebaixado
            ? h - Math.Clamp(s.BackHeightRecessMm, -h * .5f, MathF.Max(0f, h - y0 - 10f))
            : h;
        int index = 0;
        foreach (var range in SplitBackRange(x0, x1,
                     InternalCutStart(position, col0, t), InternalCutEnd(position, col1, t),
                     s.BackAdvanceOverLateralMm))
        {
            string label = col0 <= x0 || col1 >= x1 ? "Fundo" : $"Fundo {++index}";
            AddBox(instance, toWorld, new(range.Min, y0, z0), new(range.Max, top, z0 + bt),
                FaceKind.ModuleBack, label);
        }
    }

    private static void BuildSarrafos(
        ModuleInstance instance, Func<Vector3, Vector3> toWorld, ModulationStructure s,
        SpecialColumnPosition position,
        float w, float h, float d, float t, float backPlane, float col0, float col1, float colDepth)
    {
        if (s.SarrafoWhole && s.SarrafoVisible)
        {
            float z0 = MathF.Max(colDepth, backPlane - s.SarrafoAdvanceOverBackMm + s.BackAdvanceOverSarrafoMm);
            AddBox(instance, toWorld, new(t, h - s.SarrafoThicknessMm, z0),
                new(w - t, h, d - s.SarrafoDianteiroRecessMm), FaceKind.ModuleTop, "Sarrafo inteiro");
            return;
        }

        float thickness = Math.Clamp(s.SarrafoThicknessMm, 6f, 50f);
        float advanceX = Math.Clamp(s.SarrafoAdvanceOverLateralMm, -t, t);
        float x0 = t - advanceX;
        float x1 = w - t + advanceX;
        if (s.BackSarrafoVisible)
        {
            float railDepth = Math.Clamp(s.SarrafoTraseiroHeightMm, 10f, h * .5f);
            bool recessedBack = s.BackPanelLayout is BoxBackPanelLayout.Rebaixado
                or BoxBackPanelLayout.TravessaHorizontal
                or BoxBackPanelLayout.TravessaVertical
                or BoxBackPanelLayout.SemFundo;
            float z0 = recessedBack
                ? s.BackSarrafoRecessMm
                : backPlane - s.SarrafoAdvanceOverBackMm + s.BackAdvanceOverSarrafoMm;
            float top = h - s.BackSarrafoLowerRecessMm;
            // Alinha na face interna das travessas laterais do recorte.
            float internalCol0 = InternalCutStart(position, col0, t);
            float internalCol1 = InternalCutEnd(position, col1, t);
            foreach (var range in SplitRange(x0, x1, internalCol0, internalCol1))
            {
                Vector3 min = s.BackSarrafoIsVertical
                    ? new(range.Min, top - railDepth, z0)
                    : new(range.Min, top - thickness, z0);
                Vector3 max = s.BackSarrafoIsVertical
                    ? new(range.Max, top, z0 + thickness)
                    : new(range.Max, top, z0 + railDepth);
                AddBox(instance, toWorld, min, max, FaceKind.ModuleTop, "Sarrafo traseiro");
            }
        }

        if (s.FrontSarrafoVisible)
        {
            float railDepth = Math.Clamp(s.SarrafoHeightMm, 10f, h * .5f);
            float z1 = d - s.SarrafoDianteiroRecessMm;
            // Mesma regra dos módulos retos: avanço/recuo vem de Fixação Sarrafo-Lateral.
            AddBox(instance, toWorld, new(x0, h - thickness, z1 - railDepth),
                new(x1, h, z1), FaceKind.ModuleTop, "Sarrafo dianteiro");
        }
    }

    private static void BuildShelves(
        ModuleInstance instance, Func<Vector3, Vector3> toWorld, ModulationStructure s,
        SpecialColumnPosition position,
        float w, float h, float d, float t, float bt, float col0, float col1,
        float colDepth, bool notched)
    {
        float backPlane = GetBackPlaneZ(s, bt);
        int index = 0;
        foreach (var shelf in s.Shelves)
        {
            index++;
            float y = Math.Clamp(t + (h - 2f * t) * Math.Clamp(shelf.HeightFraction, 0f, 1f), t, h - 2f * t);
            float inset = Math.Clamp(shelf.WidthInsetMm, -t, w * .45f);
            float x0 = t + inset;
            float x1 = w - t - inset;
            float z0 = backPlane + shelf.BackInsetMm;
            float z1 = MathF.Max(z0 + 1f, d - shelf.DepthInsetMm);
            string label = s.Shelves.Count > 1 ? $"Prateleira {index}" : "Prateleira";
            if (notched)
            {
                float internalCut0 = position is SpecialColumnPosition.Center or SpecialColumnPosition.Right
                    ? col0 - t : col0;
                float internalCut1 = position is SpecialColumnPosition.Center or SpecialColumnPosition.Left
                    ? col1 + t : col1;
                AddNotchedPanel(instance, toWorld, x0, x1, z0, z1, y, y + t,
                    internalCut0, internalCut1, colDepth + t, label);
            }
            else
                AddBox(instance, toWorld, new(x0, y, MathF.Max(z0, colDepth)), new(x1, y + t, z1),
                    FaceKind.ModuleTop, label);
        }
    }

    private static float GetBackPlaneZ(ModulationStructure s, float bt) =>
        s.BackPanelLayout is BoxBackPanelLayout.SemFundo or BoxBackPanelLayout.TravessaHorizontal or BoxBackPanelLayout.TravessaVertical
            ? 0f
            : s.BackPanelType == BoxBackPanelType.Pregado ? bt : s.BackRecessMm + bt;

    private static IEnumerable<(float Min, float Max)> SplitRange(float min, float max, float cutMin, float cutMax)
    {
        if (cutMin > min + 1f)
            yield return (min, MathF.Min(max, cutMin));
        if (cutMax < max - 1f)
            yield return (MathF.Max(min, cutMax), max);
    }

    private static IEnumerable<(float Min, float Max)> SplitBackRange(
        float min, float max, float cutMin, float cutMax, float advance)
    {
        float overlap = Math.Clamp(advance, -50f, 50f);
        if (cutMin > min + 1f)
            yield return (min, MathF.Min(max, cutMin + overlap));
        if (cutMax < max - 1f)
            yield return (MathF.Max(min, cutMax - overlap), max);
    }

    private static float InternalCutStart(SpecialColumnPosition position, float columnStart, float thickness) =>
        position is SpecialColumnPosition.Center or SpecialColumnPosition.Right
            ? columnStart - thickness
            : columnStart;

    private static float InternalCutEnd(SpecialColumnPosition position, float columnEnd, float thickness) =>
        position is SpecialColumnPosition.Center or SpecialColumnPosition.Left
            ? columnEnd + thickness
            : columnEnd;

    private static void AddBox(ModuleInstance instance, Func<Vector3, Vector3> toWorld,
        Vector3 min, Vector3 max, FaceKind kind, string label)
    {
        if (max.X <= min.X + .5f || max.Y <= min.Y + .5f || max.Z <= min.Z + .5f)
            return;
        ModuleMeshBuilder.AddPanelBox(instance.Mesh, instance.Id, toWorld, min, max,
            kind, kind, label, instance.PartOverrides);
    }

    private static void AddNotchedPanel(
        ModuleInstance instance, Func<Vector3, Vector3> toWorld,
        float x0, float x1, float z0, float z1, float y0, float y1,
        float cut0, float cut1, float cutDepth, string label)
    {
        float c0 = Math.Clamp(cut0, x0, x1);
        float c1 = Math.Clamp(cut1, x0, x1);
        float cz = Math.Clamp(cutDepth, z0, z1);
        if (c1 <= c0 + .5f || cz <= z0 + .5f)
        {
            AddBox(instance, toWorld, new(x0, y0, z0), new(x1, y1, z1), FaceKind.ModuleTop, label);
            return;
        }

        var plan = new List<Vector2>();
        if (c0 <= x0 + .5f)
            plan.AddRange([new(c1, z0), new(x1, z0), new(x1, z1), new(x0, z1), new(x0, cz), new(c1, cz)]);
        else if (c1 >= x1 - .5f)
            plan.AddRange([new(x0, z0), new(c0, z0), new(c0, cz), new(x1, cz), new(x1, z1), new(x0, z1)]);
        else
            plan.AddRange([new(x0, z0), new(c0, z0), new(c0, cz), new(c1, cz), new(c1, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

        float[] xs = plan.Select(p => p.X).ToArray();
        float[] zs = plan.Select(p => p.Y).ToArray();
        var triangles2d = WallRegionGeometry.TriangulatePolygon(xs, zs);
        Vector3 P(Vector2 p, float y) => toWorld(new(p.X, y, p.Y));
        var bottom = new List<(Vector3 A, Vector3 B, Vector3 C)>();
        var top = new List<(Vector3 A, Vector3 B, Vector3 C)>();
        for (int i = 0; i + 2 < triangles2d.Count; i += 3)
        {
            Vector2 a = new(triangles2d[i].along, triangles2d[i].height);
            Vector2 b = new(triangles2d[i + 1].along, triangles2d[i + 1].height);
            Vector2 c = new(triangles2d[i + 2].along, triangles2d[i + 2].height);
            bottom.Add((P(a, y0), P(c, y0), P(b, y0)));
            top.Add((P(a, y1), P(b, y1), P(c, y1)));
        }

        instance.Mesh.AddPolygonalFace(plan.Select(p => P(p, y0)).ToArray(), bottom,
            FaceKind.ModuleBottom, instance.Id, label);
        instance.Mesh.AddPolygonalFace(plan.Select(p => P(p, y1)).ToArray(), top,
            FaceKind.ModuleTop, instance.Id, label);
        for (int i = 0; i < plan.Count; i++)
        {
            int next = (i + 1) % plan.Count;
            instance.Mesh.AddQuad(P(plan[i], y0), P(plan[next], y0), P(plan[next], y1), P(plan[i], y1),
                FaceKind.ModuleTop, instance.Id, label);
        }
    }
}
