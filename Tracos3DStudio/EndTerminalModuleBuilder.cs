using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Caixaria paramétrica para os terminais Diagonal e Chanfrado.
/// O desenho nasce de um único contorno em planta: por isso laterais, base,
/// prateleira e travessas usam exatamente os mesmos encontros angulares.
/// </summary>
public static class EndTerminalModuleBuilder
{
    private const float FrontGapMm = 2f;

    public static bool Build(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        bool isChamfer = definition.ShapeKind == ModuleShapeKind.EndChamfer;
        instance.EndTerminal ??= EndTerminalParams.FromDefinition(definition);
        instance.EndTerminal.ClampToModule(instance.Width, instance.Depth, isChamfer);
        var terminal = instance.EndTerminal;

        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        var effective = DimensionConfiguratorService.CreateEffectiveRules(definition, settings);
        var structure = effective?.Structure
            ?? ModulationRulesPresets.CreateStandardBox(terminal.DoorCount, 0).Structure;
        float t = Math.Clamp(structure.PanelThicknessMm > 0f ? structure.PanelThicknessMm : 18f,
            1f, MathF.Max(1f, MathF.Min(w, d) * .30f));
        float bt = Math.Clamp(structure.BackThicknessMm > 0f ? structure.BackThicknessMm : 6f,
            1f, MathF.Max(1f, d - 2f));
        float ft = Math.Clamp(structure.FrontThicknessMm > 0f ? structure.FrontThicknessMm : definition.FrontThickness,
            1f, 50f);
        float railH = Math.Clamp(structure.SarrafoHeightMm > 0f ? structure.SarrafoHeightMm : 70f,
            25f, h * .30f);
        float railT = Math.Clamp(structure.SarrafoThicknessMm > 0f ? structure.SarrafoThicknessMm : t,
            6f, 50f);
        float lateralBaseY = MathF.Abs(structure.LateralBaseOverlapMm) >= MathF.Abs(structure.LateralBottomRecessMm)
            ? structure.LateralBaseOverlapMm : structure.LateralBottomRecessMm;

        // Contorno externo, sempre com a lateral longa à esquerda. O atalho I
        // reflete a malha e também troca as descrições das peças no fim do rebuild.
        float a = terminal.SmallSideDepthMm;
        float b = isChamfer ? terminal.FrontStraightWidthMm : 0f;
        var outline = new List<Vector2>
        {
            new(0f, 0f),
            new(w, 0f),
            new(w, a)
        };
        if (isChamfer)
            outline.Add(new Vector2(b, d));
        outline.Add(new Vector2(0f, d));
        var centroid = GetCentroid(outline);
        Vector3 World(Vector3 p) => ModulePlacementService.TransformLocalPoint(
            p, instance.Position, instance.RotationYDegrees);

        // Mesmas referências do construtor de balcões inferiores retos.
        float lateralGap = Math.Clamp(structure.LateralDepthGapMm, -d * .5f, Math.Max(0f, d - 10f));
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
        float backPlane = ModuleMeshBuilder.GetBackPlaneZ(structure, bt);
        float baseRecess = Math.Clamp(structure.BaseRecessMm, -d * .5f, Math.Max(0f, d - backPlane - 10f));
        float baseZ0 = structure.BaseFullDepth ? 0f : lateralZ0;
        float baseZ1 = structure.BaseFullDepth ? d : Math.Max(baseZ0 + 10f, lateralZ1 - baseRecess);
        float baseAdvance = Math.Clamp(structure.BaseAdvanceOverLateralMm, -t, t);
        float baseX0 = t - baseAdvance;
        float baseX1 = w - t + baseAdvance;
        var basePlan = CreateInsidePlan(baseX0, baseX1, baseZ0, baseZ1, a, b, isChamfer);

        // Base e prateleira são peças únicas, sem riscos internos. A base usa a
        // profundidade total do contorno, tal como o balcão reto (não acompanha
        // o recuo do fundo encaixado).
        AddPlanPrism(instance, World, basePlan, lateralBaseY, lateralBaseY + t,
            FaceKind.ModuleBottom, "Base inferior");

        // Laterais estruturais: longa (profundidade total) e curta (Medida A).
        int longFront = outline.Count - 1;
        AddSegmentPrism(instance, World, outline[longFront], outline[0], centroid, t, lateralBaseY, h,
            FaceKind.ModuleLeft, "Lateral longa esquerda", inward: true);
        AddSegmentPrism(instance, World, outline[1], outline[2], centroid, t, lateralBaseY, h,
            FaceKind.ModuleRight, "Lateral curta direita", inward: true);
        // A montagem traseira é exatamente a do balcão reto: fundo encaixado,
        // travessas verticais/horizontais e avanços definidos no configurador.
        ModuleMeshBuilder.BuildBackAssembly(instance.Mesh, instance.Id, World, w, h, d, t, bt,
            structure, instance.PartOverrides);
        BuildRearSarrafo(instance, World, structure, w, h, d, t, bt, railT);

        // Prateleiras configuradas: a altura e os recuos vêm do mesmo perfil de
        // caixaria. O contorno interno mantém o chanfro sem criar emendas falsas.
        BuildShelves(instance, World, structure, w, h, d, a, b, isChamfer, t, bt, railH);

        // Frente: no Diagonal há apenas o segmento angular. No Chanfrado, a
        // travessa reta B e a angular encontram-se no mesmo ponto matemático.
        //
        // Não podemos formar essa frente a partir de uma "caixa" retangular:
        // isso fazia a travessa passar da lateral curta e criava uma fresta no
        // joelho do chanfro. O percurso abaixo é recortado pelas FACES INTERNAS
        // das duas laterais e, em seguida, recebe um deslocamento único. Assim,
        // os dois sarrafos se encontram em esquadria, qualquer que seja A/B.
        float sarrafoAdvance = Math.Clamp(structure.SarrafoAdvanceOverLateralMm, -t, t);
        float frontRecess = Math.Clamp(structure.SarrafoDianteiroRecessMm, -d * .5f, d - 10f);
        // Igual ao Promob: a travessa fica na frente do módulo, mas suas duas
        // pontas são cortadas pelas FACES INTERNAS das laterais. Não se desloca
        // toda a frente para dentro; somente se limita o comprimento útil.
        float sideInset = Math.Clamp(t - sarrafoAdvance, 0f, w * .45f);
        var frontPath = BuildFrontPath(outline, centroid, sideInset, frontRecess, isChamfer);
        float frontRailDepth = structure.FrontSarrafoIsVertical ? railT : railH;
        var frontRailInnerPath = OffsetPolylineInside(frontPath, centroid, frontRailDepth);
        // Fecha as extremidades também no plano interno das laterais. Sem esse
        // corte, a face de trás da travessa diagonal avançava sobre a lateral e
        // aparecia uma ponta triangular nos dois encontros.
        frontRailInnerPath[0] = PointOnLineAtX(frontRailInnerPath[0], frontRailInnerPath[1], sideInset);
        frontRailInnerPath[^1] = PointOnLineAtX(frontRailInnerPath[^2], frontRailInnerPath[^1], w - sideInset);
        if (isChamfer && frontPath.Count == 3)
        {
            AddFrontSarrafo(instance, World, frontPath[0], frontPath[1],
                frontRailInnerPath[0], frontRailInnerPath[1], structure,
                railH, railT, h, "Travessa frontal reta");
        }
        AddFrontSarrafo(instance, World, frontPath[^2], frontPath[^1],
            frontRailInnerPath[^2], frontRailInnerPath[^1], structure,
            railH, railT, h, "Travessa frontal diagonal");

        // A porta do Diagonal já seguia corretamente o vão angular. O desconto
        // lateral adicional é necessário apenas no Chanfrado, cuja frente reta
        // B tem duas bordas laterais explícitas no configurador.
        // Diferentemente da travessa, a porta fica posterior à face frontal da
        // caixaria. A referência é a mesma frente inclinada, apenas recuada pela
        // espessura da própria porta, tal como no módulo inferior reto.
        var doorPath = BuildFrontPath(outline, centroid, t, ft, isChamfer);
        BuildDoors(instance, World, doorPath, centroid, ft, h, terminal.DoorCount,
            applySideGap: isChamfer, structure, posteriorToCarcass: true);
        return true;
    }

    private static void BuildDoors(
        ModuleInstance instance,
        Func<Vector3, Vector3> world,
        IReadOnlyList<Vector2> outline,
        Vector2 centroid,
        float frontThickness,
        float height,
        int count,
        bool applySideGap,
        ModulationStructure structure,
        bool posteriorToCarcass)
    {
        // Percurso da frente, da lateral longa até a curta. Uma porta cobre o
        // percurso completo; duas portas dividem o desenvolvimento e preservam
        // a seleção por porta, mesmo no encontro reta/diagonal do chanfro.
        var segments = new List<(Vector2 Start, Vector2 End)>();
        for (int i = 0; i + 1 < outline.Count; i++)
            segments.Add((outline[i], outline[i + 1]));

        float total = segments.Sum(s => (s.End - s.Start).Length);
        if (total <= .01f)
            return;
        // As mesmas folgas externas/entre portas do módulo reto. A largura do
        // retângulo convencional vira comprimento desenvolvido na frente angular.
        float y0 = MathF.Max(0f, structure.FrontBottomGapMm);
        float y1 = MathF.Max(y0 + 40f, height - MathF.Max(0f, structure.FrontTopGapMm));
        float gap = MathF.Max(0f, structure.FrontGapMm);
        // Desconto lateral das frentes: a referência é o vão interno entre as
        // laterais, exatamente como nos balcões retos. Ele vale nos dois lados
        // mesmo quando uma porta percorre a dobra do chanfro.
        float sideGap = applySideGap ? MathF.Max(0f, structure.FrontSideGapMm) : 0f;
        float usable = total - 2f * sideGap - gap * (count - 1);
        if (usable <= .01f)
            return;
        for (int door = 0; door < count; door++)
        {
            float each = MathF.Max(1f, usable / count);
            float from = sideGap + door * (each + gap);
            float to = from + each;
            if (to <= from)
                continue;
            string label = count == 1 ? "Porta frontal" : $"Porta frontal {door + 1}";
            AddPathRange(instance, world, segments, centroid, frontThickness, y0, y1, from, to, label,
                inward: posteriorToCarcass);
        }
    }

    private static void BuildRearSarrafo(
        ModuleInstance instance,
        Func<Vector3, Vector3> world,
        ModulationStructure structure,
        float width,
        float height,
        float depth,
        float panelThickness,
        float backThickness,
        float railThickness)
    {
        if (!structure.BackSarrafoVisible)
            return;
        bool recessedBack = structure.BackPanelLayout is BoxBackPanelLayout.Rebaixado
            or BoxBackPanelLayout.TravessaHorizontal
            or BoxBackPanelLayout.TravessaVertical
            or BoxBackPanelLayout.SemFundo;
        float railZ0 = recessedBack
            ? structure.BackSarrafoRecessMm
            : ModuleMeshBuilder.GetBackPlaneZ(structure, backThickness) - structure.SarrafoAdvanceOverBackMm
                + structure.BackAdvanceOverSarrafoMm;
        float railTop = height - structure.BackSarrafoLowerRecessMm;
        float railSize = Math.Clamp(structure.SarrafoTraseiroHeightMm > 0f
            ? structure.SarrafoTraseiroHeightMm : 70f, 10f, height * .5f);
        Vector3 min;
        Vector3 max;
        if (structure.BackSarrafoIsVertical)
        {
            min = new Vector3(panelThickness, railTop - railSize, railZ0);
            max = new Vector3(width - panelThickness, railTop, railZ0 + railThickness);
        }
        else
        {
            min = new Vector3(panelThickness, railTop - railThickness, railZ0);
            max = new Vector3(width - panelThickness, railTop, railZ0 + railSize);
        }
        ModuleMeshBuilder.AddPanelBox(instance.Mesh, instance.Id, world, min, max,
            FaceKind.ModuleTop, FaceKind.ModuleTop, "Sarrafo traseiro", instance.PartOverrides);
    }

    private static void BuildShelves(
        ModuleInstance instance,
        Func<Vector3, Vector3> world,
        ModulationStructure structure,
        float width,
        float height,
        float depth,
        float smallSideDepth,
        float frontStraightWidth,
        bool isChamfer,
        float thickness,
        float backThickness,
        float railHeight)
    {
        if (structure.Shelves.Count == 0)
            return;
        int index = 0;
        foreach (var shelf in structure.Shelves)
        {
            index++;
            float y = Math.Clamp(thickness + (height - 2f * thickness) * Math.Clamp(shelf.HeightFraction, 0f, 1f),
                thickness, height - railHeight - thickness);
            float widthInset = Math.Clamp(shelf.WidthInsetMm, -thickness, width * .45f);
            float shelfX0 = thickness + widthInset;
            float shelfX1 = width - thickness - widthInset;
            float shelfZ0 = ModuleMeshBuilder.GetBackPlaneZ(structure, backThickness) + shelf.BackInsetMm;
            float shelfZ1 = Math.Max(shelfZ0 + 1f, depth - Math.Clamp(shelf.DepthInsetMm, -depth * .5f, depth - 1f));
            var shelfPlan = CreateInsidePlan(shelfX0, shelfX1, shelfZ0, shelfZ1,
                smallSideDepth, frontStraightWidth, isChamfer);
            string label = structure.Shelves.Count == 1 ? "Prateleira" : $"Prateleira {index}";
            AddPlanPrism(instance, world, shelfPlan, y, y + thickness, FaceKind.ModuleTop, label);
        }
    }

    /// <summary>
    /// Contorno usado pelas peças internas do balcão: começa/termina entre as
    /// laterais, mas conserva o recorte angular somente na frente.
    /// </summary>
    private static List<Vector2> CreateInsidePlan(
        float x0,
        float x1,
        float z0,
        float z1,
        float smallSideDepth,
        float frontStraightWidth,
        bool isChamfer)
    {
        float shortZ = Math.Clamp(smallSideDepth, z0, z1);
        float kneeX = Math.Clamp(frontStraightWidth, x0, x1);
        var plan = new List<Vector2>
        {
            new(x0, z0),
            new(x1, z0),
            new(x1, shortZ)
        };
        if (isChamfer)
            plan.Add(new Vector2(kneeX, z1));
        plan.Add(new Vector2(x0, z1));
        return plan;
    }

    /// <summary>
    /// Percurso frontal já limitado pelo lado interno das laterais. Para o
    /// chanfro, o ponto central permanece o mesmo antes e depois do recuo;
    /// <see cref="OffsetPolylineInside"/> calcula a interseção das duas linhas
    /// deslocadas e elimina a ponta aberta que havia entre as travessas.
    /// </summary>
    private static List<Vector2> BuildFrontPath(
        IReadOnlyList<Vector2> externalOutline,
        Vector2 interior,
        float sideInset,
        float frontRecess,
        bool isChamfer)
    {
        int last = externalOutline.Count - 1;
        var path = isChamfer
            ? new List<Vector2> { externalOutline[last], externalOutline[last - 1], externalOutline[2] }
            : new List<Vector2> { externalOutline[last], externalOutline[2] };

        path[0] = PointAtX(path[0], path[1], sideInset);
        path[^1] = PointAtX(path[^2], path[^1], externalOutline[2].X - sideInset);
        return OffsetPolylineInside(path, interior, frontRecess);
    }

    private static Vector2 PointAtX(Vector2 start, Vector2 end, float x)
    {
        float dx = end.X - start.X;
        if (MathF.Abs(dx) <= .0001f)
            return start;
        float ratio = Math.Clamp((x - start.X) / dx, 0f, 1f);
        return start + (end - start) * ratio;
    }

    // Para a face interna da travessa, o ponto deslocado pode ficar além do
    // trecho original. Não se pode limitar o fator a 0..1: a face precisa ser
    // prolongada até encontrar a lateral interna, nos dois lados.
    private static Vector2 PointOnLineAtX(Vector2 start, Vector2 end, float x)
    {
        float dx = end.X - start.X;
        if (MathF.Abs(dx) <= .0001f)
            return start;
        return start + (end - start) * ((x - start.X) / dx);
    }

    private static List<Vector2> OffsetPolylineInside(
        IReadOnlyList<Vector2> source,
        Vector2 interior,
        float amount)
    {
        if (source.Count < 2 || MathF.Abs(amount) <= .01f)
            return source.ToList();

        int segments = source.Count - 1;
        var starts = new Vector2[segments];
        var directions = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            Vector2 start = source[i];
            Vector2 end = source[i + 1];
            Vector2 direction = end - start;
            direction.Normalize();
            Vector2 normal = new(-direction.Y, direction.X);
            if (Vector2.Dot(interior - (start + end) * .5f, normal) < 0f)
                normal = -normal;
            starts[i] = start + normal * amount;
            directions[i] = direction;
        }

        var result = new List<Vector2>(source.Count) { starts[0] };
        for (int i = 1; i < segments; i++)
            result.Add(IntersectLines(starts[i - 1], directions[i - 1], starts[i], directions[i]));
        result.Add(starts[^1] + directions[^1] * (source[^1] - source[^2]).Length);
        return result;
    }

    private static void AddFrontSarrafo(
        ModuleInstance instance,
        Func<Vector3, Vector3> world,
        Vector2 start,
        Vector2 end,
        Vector2 innerStart,
        Vector2 innerEnd,
        ModulationStructure structure,
        float railHeight,
        float railThickness,
        float moduleHeight,
        string label)
    {
        // A travessa frontal do terminal é indispensável para fechar o recorte
        // angular; ela mantém a orientação/espessura do sarrafo configurado,
        // inclusive quando o sarrafo frontal do módulo reto foi ocultado. Os
        // pontos internos foram mitrados na interseção das duas linhas, para
        // que o chanfro não sobreponha as travessas nem mostre uma fresta.
        float y0 = structure.FrontSarrafoIsVertical ? moduleHeight - railHeight : moduleHeight - railThickness;
        AddPlanPrism(instance, world, [start, end, innerEnd, innerStart], y0, moduleHeight,
            FaceKind.ModuleTop, label);
    }

    private static void AddPathRange(
        ModuleInstance instance,
        Func<Vector3, Vector3> world,
        IReadOnlyList<(Vector2 Start, Vector2 End)> segments,
        Vector2 centroid,
        float thickness,
        float y0,
        float y1,
        float from,
        float to,
        string label,
        bool inward)
    {
        float cursor = 0f;
        foreach (var segment in segments)
        {
            float length = (segment.End - segment.Start).Length;
            float partFrom = MathF.Max(0f, from - cursor);
            float partTo = MathF.Min(length, to - cursor);
            if (partTo > partFrom + .01f)
            {
                var direction = (segment.End - segment.Start) / length;
                AddSegmentPrism(instance, world,
                    segment.Start + direction * partFrom,
                    segment.Start + direction * partTo,
                    centroid, thickness, y0, y1, FaceKind.ModuleFront, label, inward);
            }
            cursor += length;
        }
    }

    private static void AddSegmentPrism(
        ModuleInstance instance,
        Func<Vector3, Vector3> world,
        Vector2 start,
        Vector2 end,
        Vector2 interior,
        float thickness,
        float y0,
        float y1,
        FaceKind kind,
        string label,
        bool inward = true,
        float inwardOffsetMm = 0f)
    {
        Vector2 dir = end - start;
        if (dir.LengthSquared <= .0001f)
            return;
        dir.Normalize();
        Vector2 normal = new(-dir.Y, dir.X);
        Vector2 midpoint = (start + end) * .5f;
        if (Vector2.Dot(interior - midpoint, normal) < 0f)
            normal = -normal;
        if (!inward)
            normal = -normal;
        if (inward && MathF.Abs(inwardOffsetMm) > .01f)
        {
            start += normal * inwardOffsetMm;
            end += normal * inwardOffsetMm;
        }
        var plan = new[] { start, end, end + normal * thickness, start + normal * thickness };
        AddPlanPrism(instance, world, plan, y0, y1, kind, label);
    }

    private static void AddPlanPrism(
        ModuleInstance instance,
        Func<Vector3, Vector3> world,
        IReadOnlyList<Vector2> plan,
        float y0,
        float y1,
        FaceKind kind,
        string label)
    {
        if (plan.Count < 3 || y1 <= y0)
            return;
        var mesh = instance.Mesh;
        var bottom = plan.Select(p => world(new Vector3(p.X, y0, p.Y))).ToArray();
        var top = plan.Select(p => world(new Vector3(p.X, y1, p.Y))).ToArray();
        var topTriangles = new List<(Vector3 A, Vector3 B, Vector3 C)>();
        var bottomTriangles = new List<(Vector3 A, Vector3 B, Vector3 C)>();
        for (int i = 1; i + 1 < plan.Count; i++)
        {
            topTriangles.Add((top[0], top[i], top[i + 1]));
            bottomTriangles.Add((bottom[0], bottom[i + 1], bottom[i]));
        }
        mesh.AddPolygonalFace(top, topTriangles, kind, instance.Id, label);
        mesh.AddPolygonalFace(bottom.Reverse().ToArray(), bottomTriangles, kind, instance.Id, label);
        for (int i = 0; i < plan.Count; i++)
        {
            int next = (i + 1) % plan.Count;
            mesh.AddQuad(bottom[i], bottom[next], top[next], top[i], kind, instance.Id, label);
        }
    }

    private static List<Vector2> InsetConvex(IReadOnlyList<Vector2> polygon, float amount, Vector2 interior)
    {
        var shiftedStarts = new Vector2[polygon.Count];
        var directions = new Vector2[polygon.Count];
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];
            Vector2 direction = b - a;
            direction.Normalize();
            Vector2 normal = new(-direction.Y, direction.X);
            if (Vector2.Dot(interior - (a + b) * .5f, normal) < 0f)
                normal = -normal;
            shiftedStarts[i] = a + normal * amount;
            directions[i] = direction;
        }
        var result = new List<Vector2>(polygon.Count);
        for (int i = 0; i < polygon.Count; i++)
        {
            int previous = (i - 1 + polygon.Count) % polygon.Count;
            result.Add(IntersectLines(shiftedStarts[previous], directions[previous], shiftedStarts[i], directions[i]));
        }
        return result;
    }

    private static Vector2 IntersectLines(Vector2 a, Vector2 da, Vector2 b, Vector2 db)
    {
        float cross = da.X * db.Y - da.Y * db.X;
        if (MathF.Abs(cross) < .0001f)
            return (a + b) * .5f;
        Vector2 delta = b - a;
        float t = (delta.X * db.Y - delta.Y * db.X) / cross;
        return a + da * t;
    }

    private static Vector2 GetCentroid(IReadOnlyList<Vector2> plan) =>
        plan.Aggregate(Vector2.Zero, (sum, point) => sum + point) / plan.Count;
}
