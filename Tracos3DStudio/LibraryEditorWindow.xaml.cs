using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace Tracos3DStudio;

public partial class LibraryEditorWindow : Window
{
    private readonly Action _onLibraryChanged;
    private readonly ObservableCollection<ModuleRowViewModel> _rows = new();
    private LibraryDocument _document;
    private string _filePath;

    public LibraryEditorWindow(Action onLibraryChanged, string? filePath = null)
    {
        InitializeComponent();
        _onLibraryChanged = onLibraryChanged;
        _filePath = filePath ?? LibraryPersistence.DefaultLibraryPath;
        _document = LibraryPersistence.LoadDefaultOrEmpty();
        ModulesGrid.ItemsSource = _rows;
        ModulesGrid.CellEditEnding += (_, _) => SyncDocumentFromRows();
        Reload();
    }

    private void Reload()
    {
        LibraryPathText.Text = _filePath;
        CompanyNameBox.Text = _document.CompanyDisplayName ?? _document.Name;
        LogoPathBox.Text = _document.BudgetLogoPath ?? "";
        _rows.Clear();

        foreach (var module in _document.Modules)
            _rows.Add(new ModuleRowViewModel(module));
    }

    private void SyncDocumentFromRows()
    {
        _document.Modules = _rows.Select(r => r.ToData()).ToList();
        _document.CompanyDisplayName = string.IsNullOrWhiteSpace(CompanyNameBox.Text)
            ? null
            : CompanyNameBox.Text.Trim();
        _document.BudgetLogoPath = string.IsNullOrWhiteSpace(LogoPathBox.Text)
            ? null
            : LogoPathBox.Text.Trim();
    }

    private void BrowseLogo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Imagens (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            Title = "Selecionar logo para orçamento"
        };

        if (dialog.ShowDialog() != true)
            return;

        LogoPathBox.Text = dialog.FileName;
        SyncDocumentFromRows();
    }

    private void AddModule_Click(object sender, RoutedEventArgs e)
    {
        int index = _rows.Count + 1;
        _rows.Add(new ModuleRowViewModel(new CustomModuleData
        {
            Id = $"modulo-{index}",
            DisplayName = $"Módulo personalizado {index}",
            DefaultWidth = 600f,
            DefaultHeight = 850f,
            DefaultDepth = 550f,
            DoorCount = 2
        }));
        ModulesGrid.SelectedIndex = _rows.Count - 1;
    }

    private void RemoveModule_Click(object sender, RoutedEventArgs e)
    {
        if (ModulesGrid.SelectedItem is ModuleRowViewModel row)
            _rows.Remove(row);
    }

    private void EditModulation_Click(object sender, RoutedEventArgs e)
    {
        if (ModulesGrid.SelectedItem is not ModuleRowViewModel row)
        {
            MessageBox.Show(
                "Selecione um módulo na grade para editar a modulação.",
                "Biblioteca",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var data = row.ToData();
        var window = new ModulationEditorWindow(data)
        {
            Owner = this
        };

        if (window.ShowDialog() != true)
            return;

        row.ApplyFrom(data);
        SyncDocumentFromRows();
    }

    private void ReloadFromDisk_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Recarregar do disco descarta alterações não salvas nesta janela.\n\nContinuar?",
            "Biblioteca",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        _document = File.Exists(_filePath)
            ? LibraryPersistence.LoadFromFile(_filePath)
            : LibraryPersistence.LoadDefaultOrEmpty();

        Reload();
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = $"Biblioteca (*{LibraryPersistence.FileExtension})|*{LibraryPersistence.FileExtension}",
            Title = "Importar biblioteca"
        };

        if (dialog.ShowDialog() != true)
            return;

        _document = LibraryPersistence.LoadFromFile(dialog.FileName);
        _filePath = LibraryPersistence.DefaultLibraryPath;
        Reload();
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        SyncDocumentFromRows();

        var dialog = new SaveFileDialog
        {
            Filter = $"Biblioteca (*{LibraryPersistence.FileExtension})|*{LibraryPersistence.FileExtension}",
            FileName = $"biblioteca{LibraryPersistence.FileExtension}"
        };

        if (dialog.ShowDialog() != true)
            return;

        LibraryPersistence.SaveToFile(_document, dialog.FileName);

        MessageBox.Show(
            "Biblioteca exportada com sucesso.",
            "Biblioteca",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SyncDocumentFromRows();
        LibraryPersistence.SaveToFile(_document, _filePath);
        LibraryPersistence.ApplyToCatalogs(_document);
        _onLibraryChanged();

        MessageBox.Show(
            "Biblioteca salva e aplicada.",
            "Biblioteca",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private sealed class ModuleRowViewModel
    {
        public ModuleRowViewModel(CustomModuleData data)
        {
            Id = data.Id;
            DisplayName = data.DisplayName;
            DefaultWidth = data.DefaultWidth;
            DefaultHeight = data.DefaultHeight;
            DefaultDepth = data.DefaultDepth;
            DoorCount = data.DoorCount;
            DrawerCount = data.DrawerCount;
            IsWallMounted = data.IsWallMounted;
            ModulationRules = data.ModulationRules;
        }

        public string Id { get; set; }

        public string DisplayName { get; set; }

        public float DefaultWidth { get; set; }

        public float DefaultHeight { get; set; }

        public float DefaultDepth { get; set; }

        public int DoorCount { get; set; }

        public int DrawerCount { get; set; }

        public bool IsWallMounted { get; set; }

        public ModulationRules? ModulationRules { get; set; }

        public string DimensionsText =>
            $"{DefaultWidth:0} × {DefaultHeight:0} × {DefaultDepth:0}";

        public CustomModuleData ToData() => new()
        {
            Id = Id.Trim(),
            DisplayName = DisplayName.Trim(),
            DefaultWidth = DefaultWidth,
            DefaultHeight = DefaultHeight,
            DefaultDepth = DefaultDepth,
            DoorCount = DoorCount,
            DrawerCount = DrawerCount,
            IsWallMounted = IsWallMounted,
            ModulationRules = ModulationRules
        };

        public void ApplyFrom(CustomModuleData data)
        {
            Id = data.Id;
            DisplayName = data.DisplayName;
            DefaultWidth = data.DefaultWidth;
            DefaultHeight = data.DefaultHeight;
            DefaultDepth = data.DefaultDepth;
            DoorCount = data.DoorCount;
            DrawerCount = data.DrawerCount;
            IsWallMounted = data.IsWallMounted;
            ModulationRules = data.ModulationRules;
        }
    }
}
