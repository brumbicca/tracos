using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Tracos3DStudio;

public static class TechnicalPdfExporter
{
  public static void Export(
    Project project,
    PartsListSummary parts,
    TechnicalDrawingSet drawing,
    string filePath)
  {
    QuestPDF.Settings.License = LicenseType.Community;

    Document.Create(container =>
    {
      container.Page(page =>
      {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(30);
        page.DefaultTextStyle(x => x.FontSize(9));

        page.Header().Column(col =>
        {
          col.Item().Text("Traços 3D Studio — Detalhamento técnico").FontSize(16).SemiBold();
          col.Item().Text($"{project.Metadata.Name} — Espessura painel: {parts.PanelThicknessMm:0} mm").FontSize(11);
        });

        page.Content().PaddingVertical(8).Column(col =>
        {
          col.Item().Text("Planta baixa (cotas em mm)").SemiBold();
          col.Item().PaddingBottom(8).Svg(TechnicalSvgGenerator.FloorPlan(drawing)).FitWidth();

          foreach (var elevation in drawing.Elevations)
          {
            col.Item().PaddingTop(8).Text(elevation.Title).SemiBold();
            col.Item().PaddingBottom(8).Svg(TechnicalSvgGenerator.Elevation(elevation)).FitWidth();
          }

          col.Item().PaddingTop(12).Text("Lista de peças").SemiBold().FontSize(11);

          col.Item().Table(table =>
          {
            table.ColumnsDefinition(columns =>
            {
              columns.RelativeColumn(1.8f);
              columns.RelativeColumn(1.8f);
              columns.RelativeColumn(1.8f);
              columns.RelativeColumn(0.8f);
              columns.RelativeColumn(1.2f);
              columns.RelativeColumn(2f);
            });

            table.Header(header =>
            {
              header.Cell().Element(HeaderCell).Text("Módulo");
              header.Cell().Element(HeaderCell).Text("Peça");
              header.Cell().Element(HeaderCell).Text("L × A × E (mm)");
              header.Cell().Element(HeaderCell).Text("Qtd");
              header.Cell().Element(HeaderCell).Text("Material");
              header.Cell().Element(HeaderCell).Text("Furos");

              static IContainer HeaderCell(IContainer c) =>
                c.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
            });

            foreach (var piece in parts.Items)
            {
              table.Cell().Element(DataCell).Text(piece.ModuleName);
              table.Cell().Element(DataCell).Text(piece.Name);
              table.Cell().Element(DataCell).Text(piece.DimensionsText);
              table.Cell().Element(DataCell).Text(piece.Quantity.ToString(CultureInfo.InvariantCulture));
              table.Cell().Element(DataCell).Text(piece.MaterialName);
              table.Cell().Element(DataCell).Text(piece.DrillingText);

              static IContainer DataCell(IContainer c) =>
                c.PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
            }
          });

          col.Item().PaddingTop(8).Text($"Total de peças: {parts.TotalPieceCount}").SemiBold();
        });

        page.Footer().AlignCenter().Text(text =>
        {
          text.Span("Gerado em ");
          text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR")));
          text.Span(" — Traços 3D Studio");
        });
      });
    }).GeneratePdf(filePath);
  }
}
