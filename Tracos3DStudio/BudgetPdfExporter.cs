using System.Globalization;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Tracos3DStudio;

public static class BudgetPdfExporter
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static void Export(BudgetSummary summary, string filePath, byte[]? viewportImagePng = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var generatedAt = DateTime.Now;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => ComposeHeader(c, summary, generatedAt));
                    col.Item().PaddingTop(10).Element(c => ComposeClientBox(c, summary));
                    col.Item().PaddingTop(12).Text(summary.EnvironmentTitle).FontSize(11).SemiBold();

                    foreach (var section in summary.Sections)
                        col.Item().PaddingTop(8).Element(c => ComposeSection(c, section));

                    col.Item().PaddingTop(14).AlignRight().Element(c => ComposeTotals(c, summary));

                    if (!string.IsNullOrWhiteSpace(summary.BudgetPaymentTerms))
                    {
                        col.Item().PaddingTop(10).Element(c => ComposePaymentTerms(c, summary.BudgetPaymentTerms));
                    }

                    if (!string.IsNullOrWhiteSpace(summary.BudgetCommercialNotes))
                    {
                        col.Item().PaddingTop(10).Element(c => ComposeCommercialNotes(c, summary.BudgetCommercialNotes));
                    }

                    if (summary.HasUnpricedItems)
                    {
                        col.Item().PaddingTop(10).Text("ATENÇÃO: O orçamento contém itens sem preço!")
                            .FontSize(9).SemiBold().FontColor(Colors.Red.Medium);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Gerado em ");
                    text.Span(generatedAt.ToString("dd/MM/yyyy HH:mm", PtBr));
                    text.Span(" — Traços 3D Studio");
                });
            });

            if (viewportImagePng is { Length: > 0 })
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content().Column(col =>
                    {
                        col.Item().Text("Visualização 3D").FontSize(14).SemiBold();
                        col.Item().PaddingTop(12).AlignCenter().Image(viewportImagePng).FitArea();
                    });
                });
            }
        }).GeneratePdf(filePath);
    }

    private static void ComposeHeader(IContainer container, BudgetSummary summary, DateTime generatedAt)
    {
        container.Column(col =>
        {
            if (TryLoadLogo(summary.LogoPath, out byte[]? logoBytes) && logoBytes != null)
            {
                col.Item().AlignCenter().Height(52).Image(logoBytes).FitHeight();
            }
            else if (!string.IsNullOrWhiteSpace(summary.CompanyDisplayName))
            {
                col.Item().AlignCenter().Text(summary.CompanyDisplayName).FontSize(16).SemiBold();
            }
            else
            {
                col.Item().AlignCenter().Text("Traços 3D Studio").FontSize(16).SemiBold();
            }

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Data: ").SemiBold();
                    text.Span(generatedAt.ToString("dd/MM/yyyy", PtBr));
                    text.Span("     Hora: ").SemiBold();
                    text.Span(generatedAt.ToString("HH:mm:ss", PtBr));
                    if (!string.IsNullOrWhiteSpace(summary.WorkName))
                    {
                        text.Span("     Obra: ").SemiBold();
                        text.Span(summary.WorkName);
                    }

                    text.Span("     Validade: ").SemiBold();
                    text.Span(summary.GetBudgetValidUntil(generatedAt).ToString("dd/MM/yyyy", PtBr));
                    text.Span($" ({summary.BudgetValidityDays} dias)");
                    if (!string.IsNullOrWhiteSpace(summary.BudgetSalesPerson))
                    {
                        text.Span("     Vendedor: ").SemiBold();
                        text.Span(summary.BudgetSalesPerson);
                    }
                });

                row.ConstantItem(180).AlignRight().Text("Orçamento").FontSize(14).SemiBold();
            });

            if (!string.IsNullOrWhiteSpace(summary.ProjectName) &&
                !string.Equals(summary.ProjectName, summary.WorkName, StringComparison.OrdinalIgnoreCase))
            {
                col.Item().PaddingTop(2).Text($"Projeto: {summary.ProjectName}").FontSize(8);
            }
        });
    }

    private static void ComposeClientBox(IContainer container, BudgetSummary summary)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
        {
            col.Item().Text("Dados do cliente").FontSize(9).SemiBold();
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Element(c => ClientField(c, "Código", summary.ClientCode));
                    left.Item().Element(c => ClientField(c, "Nome", summary.ClientName));
                    left.Item().Element(c => ClientField(c, "Endereço", FormatStreet(summary)));
                    left.Item().Element(c => ClientField(c, "Bairro", summary.ClientNeighborhood));
                    left.Item().Element(c => ClientField(c, "End. Entrega", summary.ClientDeliveryAddress));
                    left.Item().Element(c => ClientField(c, "Telefone", summary.ClientPhone));
                    left.Item().Element(c => ClientField(c, "E-mail", summary.ClientEmail));
                });

                row.RelativeItem().Column(right =>
                {
                    string taxLabel = summary.ClientCustomerType == ClientCustomerType.LegalEntity ? "CNPJ" : "CPF";
                    right.Item().Element(c => ClientField(c, taxLabel, summary.ClientTaxId));
                    right.Item().Element(c => ClientField(c, "CEP", summary.ClientZip));
                    right.Item().Element(c => ClientField(c, "UF", summary.ClientState));
                    right.Item().Element(c => ClientField(c, "Cidade", summary.ClientCity));
                    right.Item().Element(c => ClientField(c, "Celular", summary.ClientMobile));
                });
            });

            if (!string.IsNullOrWhiteSpace(summary.ClientNotes))
            {
                col.Item().PaddingTop(4).Element(c => ClientField(c, "Anotações", summary.ClientNotes));
            }
        });
    }

    private static void ClientField(IContainer container, string label, string? value)
    {
        container.PaddingBottom(2).Text(text =>
        {
            text.Span($"{label}: ").SemiBold();
            text.Span(string.IsNullOrWhiteSpace(value) ? " " : value);
        });
    }

    private static string? FormatStreet(BudgetSummary summary)
    {
        if (string.IsNullOrWhiteSpace(summary.ClientAddress))
            return null;

        var parts = new List<string> { summary.ClientAddress };

        if (!string.IsNullOrWhiteSpace(summary.ClientAddressNumber))
            parts.Add(summary.ClientAddressNumber);

        if (!string.IsNullOrWhiteSpace(summary.ClientAddressComplement))
            parts.Add(summary.ClientAddressComplement);

        return string.Join(", ", parts);
    }

    private static void ComposeSection(IContainer container, BudgetSection section)
    {
        container.Column(col =>
        {
            col.Item().Text(section.Name).FontSize(9).SemiBold();

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(28);
                    columns.ConstantColumn(24);
                    columns.ConstantColumn(52);
                    columns.RelativeColumn(1.4f);
                    columns.ConstantColumn(52);
                    columns.RelativeColumn(2.2f);
                    columns.RelativeColumn(1.4f);
                    columns.ConstantColumn(58);
                    columns.ConstantColumn(58);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Item");
                    header.Cell().Element(HeaderCell).Text("Rep");
                    header.Cell().Element(HeaderCell).Text("Qtd");
                    header.Cell().Element(HeaderCell).Text("Referência");
                    header.Cell().Element(HeaderCell).Text("Modelo");
                    header.Cell().Element(HeaderCell).Text("Descrição");
                    header.Cell().Element(HeaderCell).Text("Dimensões");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Preço Tabela");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Preço Final");

                    static IContainer HeaderCell(IContainer c) =>
                        c.DefaultTextStyle(x => x.SemiBold())
                            .Background(Colors.Grey.Lighten3)
                            .Border(1).BorderColor(Colors.Grey.Lighten2)
                            .PaddingVertical(3).PaddingHorizontal(2);
                });

                foreach (var item in section.Items)
                {
                    table.Cell().Element(DataCell).Text(item.ItemNumber.ToString(PtBr));
                    table.Cell().Element(DataCell).Text(item.RepeatCount.ToString(PtBr));
                    table.Cell().Element(DataCell).Text(item.QuantityText);
                    table.Cell().Element(DataCell).Text(item.Reference ?? "-");
                    table.Cell().Element(DataCell).Text(item.ExternalModel ?? "-");
                    table.Cell().Element(DataCell).Text(item.Description);
                    table.Cell().Element(DataCell).Text(item.DimensionsText);
                    table.Cell().Element(DataCell).AlignRight().Text(FormatTablePrice(item));
                    table.Cell().Element(DataCell).AlignRight().Text(FormatCurrency(item.Total));

                    static IContainer DataCell(IContainer c) =>
                        c.Border(1).BorderColor(Colors.Grey.Lighten3)
                            .PaddingVertical(2).PaddingHorizontal(2);
                }

                table.Cell().ColumnSpan(8).Element(SubtotalLabelCell).AlignRight().Text("Subtotal");
                table.Cell().Element(SubtotalValueCell).AlignRight().Text(FormatCurrency(section.Subtotal));

                static IContainer SubtotalLabelCell(IContainer c) =>
                    c.DefaultTextStyle(x => x.SemiBold())
                        .Border(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2);

                static IContainer SubtotalValueCell(IContainer c) =>
                    c.DefaultTextStyle(x => x.SemiBold())
                        .Border(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2);
            });
        });
    }

    private static void ComposeTotals(IContainer container, BudgetSummary summary)
    {
        container.Column(col =>
        {
            if (summary.DiscountAmount > 0m)
            {
                col.Item().AlignRight()
                    .Text($"Subtotal: {FormatCurrency(summary.Subtotal)}")
                    .FontSize(10);

                col.Item().PaddingTop(2).AlignRight()
                    .Text($"Desconto ({summary.BudgetDiscountPercent:0.#}%): −{FormatCurrency(summary.DiscountAmount)}")
                    .FontSize(10);
            }

            col.Item().PaddingTop(summary.DiscountAmount > 0m ? 4 : 0).AlignRight()
                .Text($"Total geral: {FormatCurrency(summary.FinalTotal)}")
                .FontSize(12).SemiBold();
        });
    }

    private static void ComposePaymentTerms(IContainer container, string paymentTerms)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
        {
            col.Item().Text("Condições de pagamento").FontSize(9).SemiBold();
            col.Item().PaddingTop(4).Text(paymentTerms).FontSize(9);
        });
    }

    private static void ComposeCommercialNotes(IContainer container, string commercialNotes)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
        {
            col.Item().Text("Observações comerciais").FontSize(9).SemiBold();
            col.Item().PaddingTop(4).Text(commercialNotes).FontSize(9);
        });
    }

    private static string FormatTablePrice(BudgetLineItem item) =>
        item.HasPrice ? FormatCurrency(item.TablePrice) : "-";

    private static string FormatCurrency(decimal value) =>
        value.ToString("N2", PtBr);

    private static bool TryLoadLogo(string? path, out byte[]? bytes)
    {
        bytes = null;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        try
        {
            bytes = File.ReadAllBytes(path);
            return bytes.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}
