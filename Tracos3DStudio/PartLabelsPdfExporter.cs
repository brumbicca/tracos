using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Tracos3DStudio;

public static class PartLabelsPdfExporter
{
    private const float LabelWidthMm = 92f;
    private const float LabelHeightMm = 48f;
    private const float HorizontalGapMm = 4f;
    private const float VerticalGapMm = 2f;
    private const int ColumnsPerPage = 2;
    private const int RowsPerPage = 5;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static void Export(PartLabelsSummary summary, string filePath)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var labels = summary.Labels;
        int labelsPerPage = ColumnsPerPage * RowsPerPage;

        Document.Create(container =>
        {
            for (int pageStart = 0; pageStart < labels.Count; pageStart += labelsPerPage)
            {
                int pageEnd = Math.Min(pageStart + labelsPerPage, labels.Count);
                int pageStartCopy = pageStart;
                int pageEndCopy = pageEnd;

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(5, Unit.Millimetre);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(LabelWidthMm, Unit.Millimetre);
                            columns.ConstantColumn(HorizontalGapMm, Unit.Millimetre);
                            columns.ConstantColumn(LabelWidthMm, Unit.Millimetre);
                        });

                        for (int row = 0; row < RowsPerPage; row++)
                        {
                            int leftIndex = pageStartCopy + row * ColumnsPerPage;
                            int rightIndex = leftIndex + 1;

                            table.Cell()
                                .Height(LabelHeightMm, Unit.Millimetre)
                                .PaddingBottom(VerticalGapMm, Unit.Millimetre)
                                .Element(cell => RenderLabelCell(cell, labels, leftIndex, pageEndCopy));

                            table.Cell()
                                .Height(LabelHeightMm, Unit.Millimetre)
                                .PaddingBottom(VerticalGapMm, Unit.Millimetre);

                            table.Cell()
                                .Height(LabelHeightMm, Unit.Millimetre)
                                .PaddingBottom(VerticalGapMm, Unit.Millimetre)
                                .Element(cell => RenderLabelCell(cell, labels, rightIndex, pageEndCopy));
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span(summary.ProjectName).SemiBold();
                        text.Span(" — Etiquetas ");
                        text.Span($"{pageStartCopy + 1}-{pageEndCopy}");
                        text.Span(" de ");
                        text.Span(labels.Count.ToString(PtBr));
                    });
                });
            }
        }).GeneratePdf(filePath);
    }

    private static void RenderLabelCell(
        IContainer cell,
        IReadOnlyList<PartLabel> labels,
        int labelIndex,
        int pageEnd)
    {
        if (labelIndex < pageEnd)
            ComposeLabel(cell, labels[labelIndex]);
    }

    private static void ComposeLabel(IContainer container, PartLabel label)
    {
        container.Border(1).BorderColor(Colors.Grey.Medium).Padding(6).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(label.ProjectName).FontSize(7).SemiBold();
                row.ConstantItem(52).AlignRight()
                    .Text($"{label.Index}/{label.Total}").FontSize(7).SemiBold();
            });

            col.Item().PaddingTop(2).Text(label.ModuleName).FontSize(9).SemiBold();

            col.Item().PaddingTop(1).Text(label.PieceName).FontSize(10).SemiBold();

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(label.DimensionsText).FontSize(9);
                row.ConstantItem(70).AlignRight().Text($"{label.ThicknessMm:0} mm").FontSize(8);
            });

            col.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem().Text(label.MaterialName).FontSize(8);
                row.ConstantItem(72).AlignRight().Text(label.ShortCode).FontSize(8).SemiBold();
            });

            if (!string.IsNullOrWhiteSpace(label.DrillingText))
            {
                col.Item().PaddingTop(2).Text($"Furos: {label.DrillingText}").FontSize(7);
            }
        });
    }
}
