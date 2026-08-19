using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using OpenTK.Mathematics;
using OpenTK.Wpf;

namespace Tracos3DStudio;

public enum OpeningInsertMode
{
    None,
    Door,
    Window
}

public partial class MainWindow : Window
{
    private readonly WallDraft _wallDraft = new();
    private readonly ProjectTabWorkspace _projectTabs = new();
    private Project _project => _projectTabs.Active.Project;

    private bool _wallMode;
    private bool _hasLastPoint;
    private bool _hasPreview;
    private bool _isDefaultRoom;

    private Vector2 _lastPoint;
    private Vector2 _previewPoint;
    private float _lastTypedInnerLength;

    private bool _wallAppendMode;
    private bool _wallReferencePending;
    private bool _hasWallReferencePick;
    private WallReferencePick _wallReferencePick;
    private float _wallReferenceOffsetPreview;

    private bool _wallMoveDragging;
    private Guid _wallMoveWallId;
    private Vector2 _wallMoveDragStartFloor;
    private Vector2 _wallMoveOriginalStart;
    private Vector2 _wallMoveOriginalEnd;
    private Vector2 _wallMovePreviewDelta;

    private bool _moduleWallDragPending;
    private bool _moduleWallDragging;
    private Guid _moduleWallDragModuleId;
    private Guid _moduleWallDragWallId;
    private Point _moduleWallDragStartScreen;
    private Vector3 _moduleWallDragOriginalPosition;
    private float _moduleWallDragOriginalRotationY;
    private float _moduleWallDragOriginalDistanceAlong;
    private Guid? _moduleWallDragOriginalWallId;
    private float _moduleWallDragLastMountY;
    private double _moduleWallDragLastMouseX;
    private double _moduleWallDragLastMouseY;
    private ModulePlacementService.ModuleWallCotas? _moduleWallDragCotas;
    private const double ModuleWallDragThresholdPx = 4d;

    private bool _wallEditorActive;
    private CameraViewMode _viewModeBeforeWallEditor = CameraViewMode.Perspective;
    private WallEditorDimensionTool _wallEditorDimensionTool = WallEditorDimensionTool.None;
    private int _manualDimStep;
    private Vector2 _manualDimPointA;
    private Vector2 _manualDimPointB;
    private Vector2 _manualDimPreview;
    private Guid? _selectedManualDimId;

    private bool _wallSegmentPickMode;
    private Guid _wallSegmentTargetId;
    private float _wallSegmentPreviewDistance;

    private bool _wallHorizontalBandPickMode;
    private int _wallHorizontalBandPickStep;
    private float _wallHorizontalBandPickHeight1;
    private float _wallHorizontalBandPreviewHeight2;

    private bool _wallVerticalBandPickMode;
    private int _wallVerticalBandPickStep;
    private float _wallVerticalBandPickAlong1;
    private float _wallVerticalBandPreviewAlong;

    private bool _wallBandDragging;
    private Guid _wallBandDragWallId;
    private Guid _wallBandDragBandId;
    private WallBandEdgeKind _wallBandDragEdge;
    private float _wallBandDragPreviewValue;

    private WallBandsWindow? _wallBandsWindow;

    private bool _wallRegionDragging;
    private Guid _wallRegionDragWallId;
    private Guid _wallRegionDragRegionId;
    private WallRegionEdgeKind _wallRegionDragEdge;
    private float _wallRegionDragPreviewValue;
    private bool _wallRegionBodyDragging;
    private Guid _wallRegionBodyDragWallId;
    private Guid _wallRegionBodyDragRegionId;
    private float _wallRegionBodyDragStartAlong;
    private float _wallRegionBodyDragStartHeight;
    private float _wallRegionBodyDragPreviewDeltaAlong;
    private float _wallRegionBodyDragPreviewDeltaHeight;
    private WallRegionMoveSnapshot? _wallRegionBodyDragSnapshot;
    private bool _wallRegionRotating;
    private Guid _wallRegionRotateWallId;
    private Guid _wallRegionRotateRegionId;
    private float _wallRegionRotateStartAngleDegrees;
    private float _wallRegionRotatePreviewDeltaDegrees;
    private WallRegionMoveSnapshot? _wallRegionRotateSnapshot;

    private bool _wallRegionVerticalCutMode;
    private Guid _wallRegionVerticalCutRegionId;
    private float _wallRegionVerticalCutAlongMm;
    private bool _wallRegionVerticalCutHasLine;

    private bool _wallRegionPickMode;
    private int _wallRegionPickStep;
    private float _wallRegionPickAlong1;
    private float _wallRegionPickHeight1;
    private float _wallRegionPickAlong2;
    private float _wallRegionPickHeight2;
    private FaceType _wallRegionPickFace;

    private bool _wallCircleRegionPickMode;
    private float _wallCircleRegionPreviewAlong;
    private float _wallCircleRegionPreviewHeight;

    private bool _wallPolygonRegionPickMode;
    private bool _wallPolygonVertexInsertMode;
    private Guid _wallPolygonVertexRegionId;
    private readonly List<float> _wallPolygonPickAlong = new();
    private readonly List<float> _wallPolygonPickHeight = new();
    private float _wallPolygonPreviewAlong;
    private float _wallPolygonPreviewHeight;

    private Guid? _wall304050MovingWallId;
    private bool _wall304050PickMovingMode;

    private bool _wallChamferMode;
    private Guid _wallChamferPreviewWallId;
    private bool _wallChamferPreviewAtStart;

    private bool _wallFlechaHotpointMode;
    private bool _wallFlechaDragging;
    private Guid _wallFlechaDragWallId;

    private bool _wallJunctionMode;
    private WallJunctionKind _wallJunctionKind;
    private int _wallJunctionStep;
    private Guid _wallJunctionFirstWallId;

    private readonly CameraController _camera = new();

    private bool _isMiddleDown;
    private bool _isRightDown;
    private Point _lastMousePosition;
    private Point _wallContextMenuPressPoint;
    private bool _wallContextMenuCandidate;

    private bool _wallGroupSelected;
    private bool _floorSelected;
    private Guid? _selectedFloorZoneId;
    private bool _floorZoneDrawMode;
    private bool _hasFloorZoneStart;
    private Vector2 _floorZoneStart;
    private Vector2 _floorZonePreview;
    private bool _floorCircleRegionPickMode;
    private Vector2 _floorCirclePickCenter;
    private float _floorCirclePickRadius;
    private bool _floorPolygonRegionPickMode;
    private readonly List<float> _floorPolygonPickX = new();
    private readonly List<float> _floorPolygonPickY = new();
    private float _floorPolygonPreviewX;
    private float _floorPolygonPreviewY;
    private bool _floorZoneDragging;
    private Guid _floorZoneDragId;
    private WallRegionEdgeKind _floorZoneDragEdge;
    private float _floorZoneDragPreviewValue;
    private bool _syncingFloorZoneMaterial;
    private Guid? _selectedWallId;
    private Guid? _selectedModuleId;
    private readonly HashSet<Guid> _selectedModuleIds = new();
    private DimensionConfiguratorWindow? _dimensionConfiguratorWindow;

    // Edição de peças: módulo cujo grupo está "aberto" (duplo-clique) e peça selecionada dentro dele.
    private Guid? _openModuleGroupId;
    private string? _selectedPartLabel;

    // Seta de dimensão selecionada na peça (eixo + sentido / ponto de referência).
    private PartHandle? _selectedPartHandle;

    private Guid? _selectedOpeningId;

    private OpeningInsertMode _openingInsertMode = OpeningInsertMode.None;
    private Guid? _previewOpeningWallId;
    private float _previewOpeningDistance;
    private bool _hasOpeningPreview;

    private string? _moduleInsertDefinitionId;
    private bool _hasModulePreview;
    private Vector3 _previewModulePosition;
    private float _previewModuleRotationY;
    private bool _previewModuleSnappedToWall;
    private Guid? _previewModuleWallId;
    private float _previewModuleDistanceAlong;
    private bool _syncingPropertyPanel;
    private bool _syncingSceneModuleList;
    private bool _syncingSceneModuleVisibilityChecks;
    private bool _syncingWallSurfaceMaterial;
    private bool _syncingMeasureSideCombo;
    private bool _collisionEnabled = true;
    private HashSet<Guid> _collidingModuleIds = new();
    private string? _projectFilePath;
    private bool _isProjectDirty;
    private bool _syncingProjectTabList;
    private bool _captureViewportOnNextRender;
    private IReadOnlyList<WallAutomaticDimension> _activeWallDimensions = Array.Empty<WallAutomaticDimension>();
    private ViewportCaptureRequest? _pendingViewportCapture;
    private byte[]? _lastCapturedViewportPng;
    private bool _materialCopyMode;
    private bool _materialCopyHasSource;
    private string? _materialCopyMaterialId;
    private string? _statusBarContextOverride;
    private string? _statusBarHint;
    private Point _moduleLibraryDragStart;
    private bool _moduleLibraryDragStarted;
    private bool _moduleLibraryDragPending;
    private string? _moduleLibraryPendingDefinitionId;
    private bool _moduleLibraryCustomDragging;
    private ModulePlacementService.ModuleWallCotas? _previewModuleCotas;
    private const double ModuleLibraryDragThresholdPx = 4d;

    // Seleção múltipla Promob: Ctrl+clique (alternada) e Ctrl+arraste (caixa).
    private bool _moduleMarqueePending;
    private bool _moduleMarqueeActive;
    private Point _moduleMarqueeStart;
    private Point _moduleMarqueeEnd;
    private Guid? _moduleMarqueeClickCandidateId;
    private System.Windows.Shapes.Rectangle? _moduleMarqueeRect;
    private const double ModuleMarqueeThresholdPx = 6d;

    private const float DefaultWallThickness = 150f;
    private const float DefaultWallHeight = 2600f;
    private const float GridLimit = 3000f;
    private const float GridStep = 500f;

    public MainWindow()
    {
        InitializeComponent();

        Focusable = true;
        AllowDrop = true;
        PreviewDragOver += MainWindow_MaterialPreviewDragOver;
        Drop += MainWindow_MaterialDrop;
        PreviewMouseMove += MainWindow_ModuleLibraryPreviewMouseMove;
        PreviewMouseLeftButtonUp += MainWindow_ModuleLibraryPreviewMouseLeftButtonUp;
        KeyDown += MainWindow_KeyDown;

        var settings = new GLWpfControlSettings
        {
            MajorVersion = 3,
            MinorVersion = 3,
            Profile = OpenTK.Windowing.Common.ContextProfile.Core
        };

        Viewport.Start(settings);
        Viewport.Render += OnRender;

        Viewport.MouseLeftButtonDown += Viewport_MouseLeftButtonDown;
        Viewport.MouseDown += Viewport_MouseDown;
        Viewport.MouseUp += Viewport_MouseUp;
        Viewport.MouseRightButtonUp += Viewport_MouseRightButtonUp;
        Viewport.MouseMove += Viewport_MouseMove;
        Viewport.MouseWheel += Viewport_MouseWheel;

        _wallDraft.Thickness = DefaultWallThickness;
        _wallDraft.Height = DefaultWallHeight;
        _wallDraft.Orientation = WallOrientation.Right;
        _wallDraft.MeasureSide = WallMeasureSide.Interior;
        ResetPropertyPanelLabels();
        UpdateCollisionToggleButton();
        UpdateXRayButton();
        PopulateMaterialCombo();
        PopulateWallSurfaceMaterialCombos();
        PopulateFloorMaterialCombo();
        FloorZoneMaterialCombo.ItemsSource = FloorMaterialCatalog.All;
        PopulateWallLayerCombo();
        PopulateWallCompartmentCombo();
        PopulateModuleLayerCombo();
        LoadUserLibrary();
        RoomCompartmentService.EnsureInitialized(_project.Room, _project.Metadata);
        RefreshSceneModuleList();
        RefreshCozinhasLibraryButtons();
        ApplyBuiltinModuleLibraryIcons();
        RefreshPanelModuleButtons();
        ApplyLibraryCatalogFilter();
        MaterialsPanel.Bind(
            _project,
            BuildMaterialApplicationContext,
            OnMaterialSelectedFromWindow,
            BeginMaterialCopyMode);
        Closing += MainWindow_Closing;
        Loaded += (_, _) =>
        {
            TryLoadStartupProject();
            if (_project.Room.Walls.Count < 2)
                SeedDefaultRoom();
        };
        UpdateProjectWindowTitle();
        ApplyStatusBar();
        RefreshProjectTabBar();
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        if (!_wallEditorActive &&
            !_wallMode &&
            !_wallMoveDragging &&
            !_moduleWallDragging &&
            !_moduleWallDragPending &&
            !_floorZoneDrawMode &&
            !_floorCircleRegionPickMode &&
            !_floorPolygonRegionPickMode &&
            !_floorZoneDragging &&
            !_wallSegmentPickMode &&
            !_wallHorizontalBandPickMode &&
            !_wallVerticalBandPickMode &&
            !_wallRegionPickMode &&
            !_wallCircleRegionPickMode &&
            !_wallPolygonRegionPickMode &&
            !_wallPolygonVertexInsertMode &&
            !_wallBandDragging &&
            !_wallRegionDragging &&
            !_wallRegionBodyDragging &&
            !_wallRegionRotating &&
            !_wallRegionVerticalCutMode &&
            !_wall304050PickMovingMode &&
            !_wallChamferMode &&
            !_wallFlechaHotpointMode &&
            !_wallFlechaDragging &&
            !_wallJunctionMode &&
            !_materialCopyMode &&
            _wallEditorDimensionTool == WallEditorDimensionTool.None &&
            _openingInsertMode == OpeningInsertMode.None &&
            _moduleInsertDefinitionId == null)
            return;

        MainWindow_KeyDown(sender, e);
        e.Handled = true;
    }

    private void TryLoadStartupProject()
    {
        string[] args = Environment.GetCommandLineArgs();

        if (args.Length < 2)
            return;

        string path = Path.GetFullPath(args[1]);

        if (!path.EndsWith(ProjectPersistence.FileExtension, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(path))
            return;

        LoadProjectFromFile(path);
    }

    private string ProjectDisplayName =>
        _projectFilePath != null
            ? Path.GetFileNameWithoutExtension(_projectFilePath)
            : _project.Metadata.Name;

    private void MarkProjectDirty()
    {
        if (_isProjectDirty)
            return;

        _isProjectDirty = true;
        UpdateProjectWindowTitle();
        RefreshProjectTabBar();
        RefreshStatusBarAfterViewChange();
    }

    private void ClearProjectDirty()
    {
        _isProjectDirty = false;
        UpdateProjectWindowTitle();
        RefreshProjectTabBar();
    }

    private void PersistActiveTabState() =>
        _projectTabs.SyncActive(_projectFilePath, _isProjectDirty);

    private void RefreshProjectTabBar()
    {
        PersistActiveTabState();

        var items = new List<ProjectTabBarItem>(_projectTabs.Tabs.Count);
        for (int i = 0; i < _projectTabs.Tabs.Count; i++)
        {
            var session = _projectTabs.Tabs[i];
            string dirtyMark = session.IsDirty ? " *" : string.Empty;
            items.Add(new ProjectTabBarItem
            {
                Index = i,
                DisplayName = session.GetDisplayName() + dirtyMark
            });
        }

        _syncingProjectTabList = true;
        ProjectTabList.ItemsSource = items;
        ProjectTabList.SelectedIndex = _projectTabs.ActiveIndex;
        _syncingProjectTabList = false;
    }

    private void SwitchToProjectTab(int index)
    {
        if (index < 0 || index >= _projectTabs.Tabs.Count || index == _projectTabs.ActiveIndex)
            return;

        PersistActiveTabState();
        _projectTabs.SetActive(index);
        ApplyActiveTabProjectToUi();
        RefreshProjectTabBar();
    }

    private void ApplyActiveTabProjectToUi()
    {
        var session = _projectTabs.Active;
        _projectFilePath = session.FilePath;
        _isProjectDirty = session.IsDirty;

        ExitWallEditorMode();
        CancelWallMode();
        CancelOpeningInsertMode();
        CancelModuleInsertMode();
        ClearSelection();

        _wallDraft.Reset();
        _wallDraft.Thickness = DefaultWallThickness;
        _wallDraft.Height = DefaultWallHeight;
        _isDefaultRoom = false;
        RebuildWallDraftFromRoom();

        RoomCompartmentService.EnsureInitialized(_project.Room, _project.Metadata);
        PopulateWallLayerCombo();
        PopulateWallCompartmentCombo();
        PopulateModuleLayerCombo();
        FrameCameraOnRoom();
        RefreshSceneModuleList();
        MaterialsPanel.Bind(
            _project,
            BuildMaterialApplicationContext,
            OnMaterialSelectedFromWindow,
            BeginMaterialCopyMode);
        UpdateProjectWindowTitle();
        RefreshStatusBarAfterViewChange();
        Viewport.InvalidateVisual();
        Keyboard.Focus(this);
    }

    private void CreateNewProjectTab()
    {
        PersistActiveTabState();
        _projectTabs.AddTab();
        _projectTabs.SetActive(_projectTabs.Tabs.Count - 1);
        ResetToNewProjectCore();
        RefreshProjectTabBar();
    }

    private void ProjectTabNew_Click(object sender, RoutedEventArgs e) =>
        CreateNewProjectTab();

    private void ProjectTabList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingProjectTabList)
            return;

        if (ProjectTabList.SelectedItem is not ProjectTabBarItem item)
            return;

        SwitchToProjectTab(item.Index);
    }

    private void ProjectTabClose_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ProjectTabBarItem item })
            return;

        TryCloseProjectTab(item.Index);
    }

    private void CloseProjectTab_Click(object sender, RoutedEventArgs e) =>
        TryCloseProjectTab(_projectTabs.ActiveIndex);

    private void TryCloseProjectTab(int index)
    {
        if (index < 0 || index >= _projectTabs.Tabs.Count)
            return;

        if (index != _projectTabs.ActiveIndex)
            SwitchToProjectTab(index);

        if (!ConfirmDiscardUnsavedChanges())
            return;

        PersistActiveTabState();
        _projectTabs.RemoveAt(index);
        ApplyActiveTabProjectToUi();
        RefreshProjectTabBar();
    }

    private bool ConfirmCloseAllDirtyTabs()
    {
        PersistActiveTabState();

        for (int i = 0; i < _projectTabs.Tabs.Count; i++)
        {
            if (!_projectTabs.Tabs[i].IsDirty)
                continue;

            if (i != _projectTabs.ActiveIndex)
                SwitchToProjectTab(i);

            if (!ConfirmDiscardUnsavedChanges())
                return false;
        }

        return true;
    }

    private void UpdateProjectWindowTitle()
    {
        string dirtyMark = _isProjectDirty ? " *" : "";
        Title = $"Tra?os 3D Studio ? {ProjectDisplayName}{dirtyMark}";
        MaterialsPanel.RefreshFromProject();
    }

    private void SetStatusTitle(string detail)
    {
        string dirtyMark = _isProjectDirty ? " *" : "";
        Title = $"Tra?os 3D Studio ? {ProjectDisplayName}{dirtyMark} ? {detail}";
    }

    private bool ConfirmDiscardUnsavedChanges()
    {
        if (!_isProjectDirty)
            return true;

        var result = MessageBox.Show(
            "O projeto tem altera??es n?o salvas. Deseja salvar antes de continuar?",
            "Tra?os 3D Studio",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        return result switch
        {
            MessageBoxResult.Yes => SaveProjectInternal(false),
            MessageBoxResult.No => true,
            _ => false
        };
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!ConfirmCloseAllDirtyTabs())
            e.Cancel = true;
    }

    private void NewProject_Click(object sender, RoutedEventArgs e) =>
        CreateNewProjectTab();

    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = $"Projeto Tra?os (*{ProjectPersistence.FileExtension})|*{ProjectPersistence.FileExtension}",
            Title = "Abrir projeto"
        };

        if (dialog.ShowDialog() != true)
            return;

        LoadProjectFromFile(dialog.FileName);
    }

    private void SaveProject_Click(object sender, RoutedEventArgs e) =>
        SaveProjectInternal(false);

    private void SaveProjectAs_Click(object sender, RoutedEventArgs e) =>
        SaveProjectInternal(true);

    private void OpenWallLayers_Click(object sender, RoutedEventArgs e)
    {
        var window = new WallLayersWindow(_project, () =>
        {
            ClearSelectionIfLockedOrHidden();
            PopulateWallLayerCombo();
            PopulateModuleLayerCombo();
            Viewport.InvalidateVisual();
            MarkProjectDirty();
        });

        window.Owner = this;
        window.ShowDialog();
        PopulateWallLayerCombo();
        PopulateModuleLayerCombo();
    }

    private void OpenWallBandsEditor_Click(object sender, RoutedEventArgs e)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
        {
            MessageBox.Show(
                "Selecione uma parede (face individual, não o grupo) para editar faixas.",
                "Traços 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        if (_wallBandsWindow != null)
        {
            _wallBandsWindow.Close();
            _wallBandsWindow = null;
        }

        var materials = WallSurfaceMaterialCatalog.All.ToList();

        _wallBandsWindow = new WallBandsWindow(
            wall,
            ProjectDisplayName,
            materials,
            onBandsChanged: () =>
            {
                UpdateWallPropertyPanel(wall);
                Viewport.InvalidateVisual();
                MarkProjectDirty();
            },
            beginHorizontalBandPick: BeginHorizontalBandPick,
            beginVerticalBandPick: BeginVerticalBandPick,
            openRegionsEditor: OpenWallRegionsEditorFromBandsEditor,
            onBandSelectionChanged: bandId =>
            {
                if (bandId.HasValue)
                    SelectWallSurfaceSelector(WallBandSelectorCombo, bandId.Value);
            });

        _wallBandsWindow.Owner = this;
        _wallBandsWindow.Closed += (_, _) => _wallBandsWindow = null;
        _wallBandsWindow.Show();

        WallBandsExpander.IsExpanded = true;
    }

    private void OpenWallRegionsEditorFromBandsEditor()
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        CancelWallHorizontalBandPickMode();
        CancelWallVerticalBandPickMode();
        CancelWallRegionPickMode();
        CancelWallCircleRegionPickMode();
        CancelWallPolygonRegionPickMode();
        CancelWallPolygonVertexInsertMode();
        CancelWallRegionVerticalCutMode();

        WallRegionsExpander.IsExpanded = true;
        PopulateWallRegionSelector(wall);
        UpdateWallRegionsSummary(wall);
        UpdateWallPropertyPanel(wall);

        Activate();
        Focus();
        Keyboard.Focus(this);

        SetStatusBarOverrides(hint: "Regiões — painel à direita ou clique direito na parede → Editar Regiões.");
        Title = $"Traços 3D - Editar regiões | {ProjectDisplayName}";
    }

    private bool CanShowWallContextMenu() =>
        !_wallMode &&
        _openingInsertMode == OpeningInsertMode.None &&
        _moduleInsertDefinitionId == null &&
        !_moduleWallDragging &&
        !_moduleWallDragPending &&
        !_wallHorizontalBandPickMode &&
        !_wallVerticalBandPickMode &&
        !_wallRegionPickMode &&
        !_wallCircleRegionPickMode &&
        !_wallPolygonRegionPickMode &&
        !_wallPolygonVertexInsertMode &&
        !_wallRegionVerticalCutMode &&
        !_materialCopyMode;

    private void TryShowWallContextMenu(Point position)
    {
        if (!TryPickWallAtScreen(position.X, position.Y, out var wall, out _, out bool hitTop) ||
            hitTop ||
            !IsWallPickable(wall))
            return;

        SelectWall(wall, groupSelection: false);

        var menu = new ContextMenu();

        var editBandsItem = new MenuItem { Header = "Editar Faixas..." };
        editBandsItem.SetValue(
            AutomationProperties.AutomationIdProperty,
            "WallContextEditBandsMenuItem");
        editBandsItem.Click += (_, _) => OpenWallBandsEditor_Click(this, new RoutedEventArgs());
        menu.Items.Add(editBandsItem);

        var editRegionsItem = new MenuItem { Header = "Editar Regiões..." };
        editRegionsItem.SetValue(
            AutomationProperties.AutomationIdProperty,
            "WallContextEditRegionsMenuItem");
        editRegionsItem.Click += (_, _) => OpenWallRegionsEditorFromBandsEditor();
        menu.Items.Add(editRegionsItem);

        menu.PlacementTarget = Viewport;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private void RefreshWallBandsEditor()
    {
        _wallBandsWindow?.RefreshFromWall();
    }

    private void BeginHorizontalBandPick()
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        WallBandsExpander.IsExpanded = true;
        CancelWallVerticalBandPickMode();
        CancelWallRegionPickMode();
        CancelWallCircleRegionPickMode();
        CancelWallPolygonRegionPickMode();
        CancelWallPolygonVertexInsertMode();
        _wallHorizontalBandPickMode = true;
        _wallHorizontalBandPickStep = 0;
        _wallHorizontalBandPickHeight1 = 0f;
        _wallHorizontalBandPreviewHeight2 = MathF.Max(wall.HeightStart, wall.HeightEnd) * 0.5f;
        Title = "Traços 3D - Faixa horizontal: clique a primeira altura na face | Esc cancela";
        Keyboard.Focus(this);
    }

    private void BeginVerticalBandPick()
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        WallBandsExpander.IsExpanded = true;
        CancelWallHorizontalBandPickMode();
        CancelWallRegionPickMode();
        CancelWallCircleRegionPickMode();
        CancelWallPolygonRegionPickMode();
        CancelWallPolygonVertexInsertMode();
        _wallVerticalBandPickMode = true;
        _wallVerticalBandPickStep = 0;
        _wallVerticalBandPickAlong1 = 0f;
        _wallVerticalBandPreviewAlong = wall.Length * 0.5f;
        Title = "Traços 3D - Faixa vertical: clique a primeira posição na face | Esc cancela";
        Keyboard.Focus(this);
    }

    private MaterialApplicationContext BuildMaterialApplicationContext()
    {
        Guid? wallBandId = null;
        Guid? wallRegionId = null;
        FaceType? wallFace = null;

        if (_selectedWallId.HasValue && !_wallGroupSelected)
        {
            if (WallRegionSelectorCombo.SelectedItem is WallSurfaceSelectorItem regionItem &&
                WallRegionSelectorCombo.IsEnabled)
                wallRegionId = regionItem.Id;

            if (WallBandSelectorCombo.SelectedItem is WallSurfaceSelectorItem bandItem &&
                WallBandSelectorCombo.IsEnabled)
                wallBandId = bandItem.Id;

            if (!wallRegionId.HasValue && !wallBandId.HasValue)
            {
                wallFace = WallRegionFaceCombo.SelectedIndex == 1
                    ? FaceType.External
                    : FaceType.Internal;
            }
        }

        return new MaterialApplicationContext
        {
            ModuleId = _selectedModuleId,
            WallId = _wallGroupSelected ? null : _selectedWallId,
            WallBandId = wallBandId,
            WallRegionId = wallRegionId,
            WallFace = wallFace,
            FloorSelected = _floorSelected,
            FloorZoneId = _selectedFloorZoneId
        };
    }

    private void OpenMaterials_Click(object sender, RoutedEventArgs e)
    {
        LibraryTabControl.SelectedItem = LibraryTabMaterials;
        MaterialsPanel.RefreshFromProject();
    }

    private void OnMaterialSelectedFromWindow(
        string materialId,
        MaterialApplicationTarget target,
        string? error)
    {
        _ = error;

        if (target != MaterialApplicationTarget.None)
            MarkProjectDirty();

        RefreshAfterMaterialApply(target, materialId);
        Viewport.InvalidateVisual();
    }

    private void RefreshAfterMaterialApply(MaterialApplicationTarget target, string materialId)
    {
        switch (target)
        {
            case MaterialApplicationTarget.Module:
                if (_selectedModuleId.HasValue)
                {
                    var module = _project.FindModule(_selectedModuleId.Value);

                    if (module != null)
                    {
                        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
                        UpdateModulePropertyPanel(module, definition);
                    }
                }

                break;

            case MaterialApplicationTarget.WallBand:
            case MaterialApplicationTarget.WallRegion:
            case MaterialApplicationTarget.WallFace:
                if (_selectedWallId.HasValue)
                {
                    var wall = FindWallById(_selectedWallId.Value);

                    if (wall != null)
                    {
                        _syncingWallSurfaceMaterial = true;
                        SyncWallBandMaterialCombo(wall);
                        SyncWallRegionMaterialCombo(wall);
                        SyncWallFaceMaterialCombo(wall);
                        UpdateWallBandsSummary(wall);
                        UpdateWallRegionsSummary(wall);
                        _syncingWallSurfaceMaterial = false;
                    }
                }

                break;

            case MaterialApplicationTarget.FloorZone:
            case MaterialApplicationTarget.FloorBase:
                UpdateFloorPropertyPanel();
                UpdateFloorRegionsSummary();
                break;
        }

        if (target != MaterialApplicationTarget.None)
        {
            string name = WallSurfaceMaterialCatalog.GetDisplayName(materialId);
            SetStatusBarOverrides(context: $"Material: {name}");
        }
    }

    private void MainWindow_MaterialPreviewDragOver(object sender, DragEventArgs e) =>
        HandleMaterialDragOver(e);

    private void MainWindow_MaterialDrop(object sender, DragEventArgs e) =>
        HandleMaterialDrop(e);

    private void ViewportHost_PreviewDragOver(object sender, DragEventArgs e) =>
        HandleViewportDragOver(e);

    private void ViewportHost_DragLeave(object sender, DragEventArgs e) =>
        HandleViewportDragLeave(e);

    private void ViewportHost_Drop(object sender, DragEventArgs e) =>
        HandleViewportDrop(e);

    private void HandleViewportDragOver(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(ModuleDragFormats.ModuleDefinitionId))
        {
            Point pos = e.GetPosition(Viewport);

            if (pos.X < 0 || pos.Y < 0 || pos.X > Viewport.ActualWidth || pos.Y > Viewport.ActualHeight ||
                _project.Room.Walls.Count == 0)
            {
                e.Effects = DragDropEffects.None;
                ClearModuleInsertPreview();
            }
            else
            {
                e.Effects = DragDropEffects.Copy;
                UpdateModulePreview(pos.X, pos.Y);
                Viewport.InvalidateVisual();
            }

            e.Handled = true;
            return;
        }

        HandleMaterialDragOver(e);
    }

    private void HandleViewportDragLeave(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(ModuleDragFormats.ModuleDefinitionId))
            return;

        ClearModuleInsertPreview();
        e.Handled = true;
    }

    private void ClearModuleInsertPreview()
    {
        if (_moduleInsertDefinitionId == null)
            return;

        _hasModulePreview = false;
        _previewModuleWallId = null;
        Viewport.InvalidateVisual();
    }

    private void HandleViewportDrop(DragEventArgs e)
    {
        Point pos = e.GetPosition(Viewport);

        if (pos.X < 0 || pos.Y < 0 || pos.X > Viewport.ActualWidth || pos.Y > Viewport.ActualHeight)
            return;

        if (e.Data.GetDataPresent(ModuleDragFormats.ModuleDefinitionId))
        {
            string? definitionId = e.Data.GetData(ModuleDragFormats.ModuleDefinitionId) as string;

            if (TryInsertModuleFromDrop(definitionId, pos.X, pos.Y, out string? error))
                e.Handled = true;
            else
            {
                CancelModuleInsertMode();
                if (!string.IsNullOrWhiteSpace(error))
                    SetStatusBarOverrides(hint: error);
            }

            return;
        }

        HandleMaterialDrop(e);
    }

    private bool TryInsertModuleFromDrop(string? definitionId, double mouseX, double mouseY, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(definitionId))
            return false;

        EnsureCameraMatricesForPicking();

        if (!ModuleInsertDropService.TryInsertFromScreen(
                _project,
                definitionId,
                mouseX,
                mouseY,
                Viewport.ActualWidth,
                Viewport.ActualHeight,
                _camera.View,
                _camera.Projection,
                BuildWallPickTargets(),
                _collisionEnabled,
                IsModuleCollisionBypassActive(),
                GetEffectiveDimensionSettings(),
                out ModuleInstance? instance,
                out error) ||
            instance == null)
            return false;

        CancelModuleInsertMode();
        SelectModule(instance);
        MarkProjectDirty();
        RefreshCollisionState();
        RefreshSceneModuleList();
        Viewport.InvalidateVisual();
        Keyboard.Focus(this);

        var definition = ModuleCatalog.GetRequired(definitionId);
        SetStatusBarOverrides(hint: $"Módulo inserido: {definition.DisplayName}.");

        if (_collisionEnabled && _collidingModuleIds.Contains(instance.Id))
            Title = $"Tra?os 3D - {definition.DisplayName} | Colis?o detectada após inserir";
        else
            Title = $"Tra?os 3D - {definition.DisplayName} inserido";

        return true;
    }

    private void HandleMaterialDragOver(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(MaterialDragFormats.MaterialId))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        Point pos = e.GetPosition(Viewport);

        if (pos.X < 0 || pos.Y < 0 || pos.X > Viewport.ActualWidth || pos.Y > Viewport.ActualHeight)
            e.Effects = DragDropEffects.None;
        else
            e.Effects = DragDropEffects.Copy;

        e.Handled = true;
    }

    private void HandleMaterialDrop(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(MaterialDragFormats.MaterialId))
            return;

        string? materialId = e.Data.GetData(MaterialDragFormats.MaterialId) as string;

        if (string.IsNullOrWhiteSpace(materialId))
            return;

        Point pos = e.GetPosition(Viewport);

        if (pos.X < 0 || pos.Y < 0 || pos.X > Viewport.ActualWidth || pos.Y > Viewport.ActualHeight)
            return;

        if (!TryApplyMaterialDropAtScreen(pos.X, pos.Y, materialId, out string? error))
        {
            if (!string.IsNullOrWhiteSpace(error))
                SetStatusBarOverrides(hint: error);

            return;
        }

        e.Handled = true;
    }

    private bool TryApplyMaterialDropAtScreen(
        double mouseX,
        double mouseY,
        string materialId,
        out string? error)
    {
        error = null;

        if (!TryBuildMaterialDropRayHit(mouseX, mouseY, out MaterialDropRayHit rayHit))
        {
            error = "Solte sobre módulo, face, faixa, região ou piso.";
            return false;
        }

        if (!MaterialDropService.TryResolveTarget(_project, rayHit, out MaterialApplicationContext context, out _))
        {
            error = "Nenhum alvo de material neste ponto.";
            return false;
        }

        if (!MaterialApplicationService.TryApplyMaterial(
                _project,
                context,
                materialId,
                out MaterialApplicationTarget applied,
                out error))
            return false;

        SelectMaterialDropTarget(context, applied);
        MarkProjectDirty();
        RefreshAfterMaterialApply(applied, materialId);
        Viewport.InvalidateVisual();
        return true;
    }

    private void MaterialCopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_materialCopyMode)
            CancelMaterialCopyMode();
        else
            BeginMaterialCopyMode();
    }

    public void BeginMaterialCopyMode()
    {
        CancelWallMode();
        CancelModuleInsertMode();
        _materialCopyMode = true;
        _materialCopyHasSource = false;
        _materialCopyMaterialId = null;
        UpdateMaterialCopyButtonState();
        Title = "Traços 3D — Copiar material: clique no item de origem | Esc cancela";
        SetStatusBarOverrides(hint: "Copiar material: clique na origem");
        Viewport.Focus();
    }

    private void CancelMaterialCopyMode()
    {
        if (!_materialCopyMode)
            return;

        _materialCopyMode = false;
        _materialCopyHasSource = false;
        _materialCopyMaterialId = null;
        UpdateMaterialCopyButtonState();
        UpdateViewTitle();
        RefreshStatusBarAfterViewChange();
    }

    private void UpdateMaterialCopyButtonState()
    {
        MaterialCopyButton.Background = _materialCopyMode
            ? new SolidColorBrush(Color.FromRgb(0xD4, 0xE8, 0xFF))
            : Brushes.Transparent;
    }

    private bool TryHandleMaterialCopyViewportClick(double mouseX, double mouseY)
    {
        if (!_materialCopyMode)
            return false;

        if (!TryBuildMaterialDropRayHit(mouseX, mouseY, out MaterialDropRayHit rayHit))
        {
            SetStatusBarOverrides(hint: "Nenhum item neste ponto");
            return true;
        }

        if (!_materialCopyHasSource)
        {
            if (!MaterialCopyService.TryReadMaterialFromRayHit(
                    _project,
                    rayHit,
                    out string? materialId,
                    out _,
                    out _,
                    out string? error))
            {
                SetStatusBarOverrides(hint: error ?? "Não foi possível copiar.");
                return true;
            }

            _materialCopyMaterialId = materialId;
            _materialCopyHasSource = true;
            MaterialApplicationService.ActiveMaterialId = materialId!;

            string name = WallSurfaceMaterialCatalog.GetDisplayName(materialId);
            Title = $"Traços 3D — Material copiado: {name}. Clique nos destinos | Esc cancela";
            SetStatusBarOverrides(hint: $"Material copiado: {name} — clique nos destinos");
            return true;
        }

        if (!MaterialDropService.TryResolveTarget(
                _project,
                rayHit,
                MaterialApplicationMode.Auto,
                out MaterialApplicationContext context,
                out _))
        {
            SetStatusBarOverrides(hint: "Nenhum alvo neste ponto");
            return true;
        }

        if (!MaterialApplicationService.TryApplyMaterial(
                _project,
                context,
                _materialCopyMaterialId!,
                out MaterialApplicationTarget applied,
                out string? applyError))
        {
            SetStatusBarOverrides(hint: applyError ?? "Não foi possível aplicar.");
            return true;
        }

        SelectMaterialDropTarget(context, applied);
        MarkProjectDirty();
        RefreshAfterMaterialApply(applied, _materialCopyMaterialId!);
        Viewport.InvalidateVisual();

        string appliedName = WallSurfaceMaterialCatalog.GetDisplayName(_materialCopyMaterialId);
        SetStatusBarOverrides(hint: $"Material aplicado: {appliedName}");
        return true;
    }

    private bool TryBuildMaterialDropRayHit(double mouseX, double mouseY, out MaterialDropRayHit hit)
    {
        hit = new MaterialDropRayHit();

        EnsureCameraMatricesForPicking();

        if (!Geometry3D.TryCreateWorldRay(
                mouseX,
                mouseY,
                Viewport.ActualWidth,
                Viewport.ActualHeight,
                _camera.View,
                _camera.Projection,
                out Vector3 origin,
                out Vector3 direction))
            return false;

        ModuleInstance? module = null;
        float moduleDistance = float.MaxValue;

        if (ModulePickService.TryPickRay(
                origin,
                direction,
                GetPickableModules(),
                out var pickedModule,
                out float modT) &&
            pickedModule != null)
        {
            module = pickedModule;
            moduleDistance = modT;
        }

        WallSegment? wall = null;
        float wallDistance = float.MaxValue;
        float along = 0f;
        float height = 0f;
        FaceType face = FaceType.Internal;
        bool wallHitTop = false;

        var pickTargets = BuildWallPickTargets();

        if (WallPickService.TryPickRayDetailed(
                origin,
                direction,
                pickTargets,
                out Guid wallId,
                out along,
                out height,
                out WallPickService.WallPickFaceKind faceKind,
                out _))
        {
            wall = FindWallById(wallId);

            if (!IsWallPickable(wall))
                wall = null;
            else
            {
                wallHitTop = faceKind == WallPickService.WallPickFaceKind.Top;

                if (wallHitTop && _camera.ViewMode == CameraViewMode.Top)
                    wallHitTop = false;

                face = faceKind == WallPickService.WallPickFaceKind.LateralB
                    ? FaceType.External
                    : FaceType.Internal;

                if (WallPickService.TryPickRay(
                        origin,
                        direction,
                        pickTargets,
                        out _,
                        out _,
                        out float wallT,
                        out _))
                    wallDistance = wallT;
            }
        }

        bool hasFloorHit = false;
        float floorDistance = float.MaxValue;
        Vector2 floorPoint = Vector2.Zero;

        if (_project.Room.Floor != null &&
            _project.Room.Floor.Points.Count >= 3 &&
            FloorPickService.TryPickRay(origin, direction, _project.Room.Floor.Points, out float floorT))
        {
            if (wallDistance >= floorT)
            {
                hasFloorHit = true;
                floorDistance = floorT;
                floorPoint = Geometry3D.HitPointToFloor(origin + direction * floorT);
            }
        }

        hit = new MaterialDropRayHit
        {
            Module = module,
            ModuleDistance = moduleDistance,
            Wall = wall,
            WallDistance = wallDistance,
            Along = along,
            Height = height,
            Face = face,
            WallHitTop = wallHitTop,
            HasFloorHit = hasFloorHit,
            FloorDistance = floorDistance,
            FloorPoint = floorPoint
        };

        return module != null || wall != null || hasFloorHit;
    }

    private void SelectMaterialDropTarget(MaterialApplicationContext context, MaterialApplicationTarget target)
    {
        switch (target)
        {
            case MaterialApplicationTarget.Module:
                if (context.ModuleId is Guid moduleId)
                {
                    var module = _project.FindModule(moduleId);

                    if (module != null)
                        SelectModule(module);
                }

                break;

            case MaterialApplicationTarget.WallRegion:
            {
                if (context.WallId is Guid wallId && context.WallRegionId is Guid regionId)
                {
                    var wall = FindWallById(wallId);

                    if (wall != null)
                    {
                        SelectWall(wall);
                        SelectWallSurfaceSelector(WallRegionSelectorCombo, regionId);
                        WallRegionsExpander.IsExpanded = true;
                    }
                }

                break;
            }

            case MaterialApplicationTarget.WallBand:
            {
                if (context.WallId is Guid bandWallId && context.WallBandId is Guid bandId)
                {
                    var wall = FindWallById(bandWallId);

                    if (wall != null)
                    {
                        SelectWall(wall);
                        SelectWallSurfaceSelector(WallBandSelectorCombo, bandId);
                        WallBandsExpander.IsExpanded = true;
                    }
                }

                break;
            }

            case MaterialApplicationTarget.WallFace:
            {
                if (context.WallId is Guid faceWallId && context.WallFace is FaceType face)
                {
                    var wall = FindWallById(faceWallId);

                    if (wall != null)
                    {
                        SelectWall(wall);
                        WallRegionFaceCombo.SelectedIndex = face == FaceType.External ? 1 : 0;
                        WallRegionsExpander.IsExpanded = true;
                    }
                }

                break;
            }

            case MaterialApplicationTarget.FloorZone:
                if (context.FloorZoneId is Guid zoneId && _project.Room.Floor != null)
                {
                    var zone = _project.Room.Floor.Zones.FirstOrDefault(z => z.Id == zoneId);

                    if (zone != null)
                        SelectFloorZone(zone);
                }

                break;

            case MaterialApplicationTarget.FloorBase:
                SelectFloor();
                break;
        }
    }

    private static void SelectWallSurfaceSelector(ComboBox combo, Guid itemId)
    {
        if (combo.ItemsSource is not IEnumerable<WallSurfaceSelectorItem> items)
            return;

        foreach (var item in items)
        {
            if (item.Id != itemId)
                continue;

            combo.SelectedItem = item;
            return;
        }
    }

    private void AuditBudget_Click(object sender, RoutedEventArgs e)
    {
        var audit = BudgetAuditService.Run(_project);
        var window = new BudgetAuditWindow(audit, _project.Metadata.GetWorkDisplayName(), allowContinue: false);
        window.Owner = this;
        window.ShowDialog();
    }

    private void OpenProjectClientData_Click(object sender, RoutedEventArgs e)
    {
        var window = new ProjectClientDataWindow(_project, () =>
        {
            MarkProjectDirty();
            UpdateViewTitle();
            RefreshSceneModuleList();
        });
        window.Owner = this;
        window.ShowDialog();
    }

    private void AddCompartment_Click(object sender, RoutedEventArgs e)
    {
        RoomCompartmentService.EnsureInitialized(_project.Room, _project.Metadata);
        var compartment = RoomCompartmentService.AddCompartment(_project.Room);

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
            {
                wall.CompartmentId = compartment.Id;
                UpdateWallPropertyPanel(wall);
            }
        }

        MarkProjectDirty();
        RefreshSceneModuleList();
        SetStatusBarOverrides(hint: $"{RoomCompartmentService.FormatCompartmentGroupTitle(compartment, _project.Room.Compartments)} criado. Atribua paredes no painel Outras → Cômodo.");
    }

    private void OpenBudget_Click(object sender, RoutedEventArgs e)
    {
        var audit = BudgetAuditService.Run(_project);

        if (!audit.IsClean)
        {
            var auditWindow = new BudgetAuditWindow(audit, _project.Metadata.GetWorkDisplayName(), allowContinue: true);
            auditWindow.Owner = this;

            if (auditWindow.ShowDialog() != true)
                return;
        }

        var window = new BudgetWindow(
            _project,
            MarkProjectDirty,
            CaptureViewportForExport,
            audit);
        window.Owner = this;
        window.ShowDialog();
    }

    private void ExportViewportPng_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG (*.png)|*.png",
            FileName = $"{ProjectDisplayName}-viewport.png"
        };

        if (dialog.ShowDialog() != true)
            return;

        var png = CaptureViewportForExport();

        if (png == null || png.Length == 0)
        {
            MessageBox.Show(
                "N?o foi poss?vel capturar o viewport.",
                "Exportar PNG",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        File.WriteAllBytes(dialog.FileName, png);
        MessageBox.Show(
            "Imagem exportada com sucesso.",
            "Exportar PNG",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportPresentationPng_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG (*.png)|*.png",
            FileName = $"{ProjectDisplayName}-apresentacao.png"
        };

        if (dialog.ShowDialog() != true)
            return;

        var png = CaptureViewportForPresentationExport();

        if (png == null || png.Length == 0)
        {
            MessageBox.Show(
                "N?o foi poss?vel capturar o viewport em alta resolu??o.",
                "Exportar PNG apresenta??o",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        File.WriteAllBytes(dialog.FileName, png);
        MessageBox.Show(
            "Imagem de apresenta??o exportada com sucesso.",
            "Exportar PNG apresenta??o",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private byte[]? CaptureViewportForPresentationExport()
    {
        _pendingViewportCapture = new ViewportCaptureRequest
        {
            Scale = 2f,
            PresentationOnly = true,
            TargetMinWidthPx = 1920
        };
        _lastCapturedViewportPng = null;
        Viewport.InvalidateVisual();
        Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        return _lastCapturedViewportPng;
    }

    private byte[]? CaptureViewportForExport()
    {
        _captureViewportOnNextRender = true;
        _lastCapturedViewportPng = null;
        Viewport.InvalidateVisual();
        Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        return _lastCapturedViewportPng;
    }

    private void PopulateWallSurfaceMaterialCombos()
    {
        var materials = WallSurfaceMaterialCatalog.All;
        WallBandMaterialCombo.ItemsSource = materials;
        WallRegionMaterialCombo.ItemsSource = materials;
        WallFaceMaterialCombo.ItemsSource = materials;
    }

    private void PopulateMaterialCombo()
    {
        PropertyMaterialCombo.ItemsSource = MaterialCatalog.All;
    }

    private void PopulateFloorMaterialCombo()
    {
        FloorMaterialCombo.ItemsSource = FloorMaterialCatalog.All;
    }

    private void FloorMaterialCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPropertyPanel)
            return;

        if (FloorMaterialCombo.SelectedItem is not FloorMaterialDefinition material)
            return;

        var floor = _project.Room.Floor;

        if (floor == null)
            return;

        if (_selectedFloorZoneId.HasValue)
        {
            var zone = floor.Zones.FirstOrDefault(z => z.Id == _selectedFloorZoneId.Value);

            if (zone == null)
                return;

            zone.MaterialId = material.Id;
        }
        else
        {
            floor.DefaultMaterialId = material.Id;
        }

        MarkProjectDirty();
        UpdateFloorPropertyPanel();
    }

    private void FloorShowGridCheck_Click(object sender, RoutedEventArgs e)
    {
        _project.Room.ShowFloorGrid = FloorShowGridCheck.IsChecked == true;
        MarkProjectDirty();
    }

    private void FloorDefineZoneButton_Click(object sender, RoutedEventArgs e)
    {
        if (_project.Room.Floor == null)
            return;

        CancelOpeningInsertMode();
        CancelModuleInsertMode();

        if (_wallMode)
            CancelWallMode();

        FloorRegionsExpander.IsExpanded = true;
        CancelFloorCircleRegionPickMode();
        CancelFloorPolygonRegionPickMode();
        _floorZoneDrawMode = true;
        _hasFloorZoneStart = false;
        _floorSelected = true;
        _selectedFloorZoneId = null;

        UpdateFloorPropertyPanel();
        Title = "Tra?os 3D - Delimitar ?rea de piso | 1? canto | Esc cancela";
        Viewport.Focus();
    }

    private void FloorDeleteZoneButton_Click(object sender, RoutedEventArgs e) =>
        DeleteSelectedFloorZone();

    private void CancelFloorZoneDrawMode()
    {
        _floorZoneDrawMode = false;
        _hasFloorZoneStart = false;

        if (_floorSelected)
            UpdateFloorStatus();
        else
            UpdateViewTitle();
    }

    private void DeleteSelectedFloorZone()
    {
        if (!_selectedFloorZoneId.HasValue || _project.Room.Floor == null)
            return;

        var zoneId = _selectedFloorZoneId.Value;
        var floor = _project.Room.Floor;

        for (int i = floor.Zones.Count - 1; i >= 0; i--)
        {
            if (floor.Zones[i].Id != zoneId)
                continue;

            floor.Zones.RemoveAt(i);
            break;
        }

        _selectedFloorZoneId = null;
        MarkProjectDirty();
        UpdateFloorPropertyPanel();
        UpdateFloorStatus();
    }

    private void FloorAddRegionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_project.Room.Floor == null)
            return;

        CancelFloorZoneDrawMode();
        CancelFloorCircleRegionPickMode();
        CancelFloorPolygonRegionPickMode();
        FloorRegionsExpander.IsExpanded = true;

        if (FloorZoneService.TryAddDefaultRectZone(_project.Room.Floor, out var zone, out string? error) && zone != null)
        {
            MarkProjectDirty();
            SelectFloorZone(zone);
        }
        else if (error != null)
            FloorRegionsSummaryText.Text = error;
    }

    private void FloorAddCircleRegionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_project.Room.Floor == null)
            return;

        CancelFloorZoneDrawMode();
        CancelFloorPolygonRegionPickMode();
        FloorRegionsExpander.IsExpanded = true;
        _floorCircleRegionPickMode = true;
        _floorCirclePickRadius = FloorZoneService.DefaultCircleRadiusMm;
        Title = "Traços 3D - Região circular no piso: clique o centro | Esc cancela";
        Keyboard.Focus(this);
    }

    private void FloorAddPolygonRegionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_project.Room.Floor == null)
            return;

        CancelFloorZoneDrawMode();
        CancelFloorCircleRegionPickMode();
        FloorRegionsExpander.IsExpanded = true;
        _floorPolygonRegionPickMode = true;
        _floorPolygonPickX.Clear();
        _floorPolygonPickY.Clear();
        _floorPolygonPreviewX = 0f;
        _floorPolygonPreviewY = 0f;
        Title = GetFloorPolygonRegionPickTitle();
        ShowMeasureBox();
        Keyboard.Focus(this);
    }

    private string GetFloorPolygonRegionPickTitle()
    {
        if (_floorPolygonPickX.Count == 0)
            return "Traços 3D - Polígono no piso: clique o 1º ponto | Esc cancela";

        return $"Traços 3D - Polígono no piso ({_floorPolygonPickX.Count} pts): comprimento + Enter ou clique | fechar no 1º ponto | Esc cancela";
    }

    private void CancelFloorCircleRegionPickMode()
    {
        if (!_floorCircleRegionPickMode)
            return;

        _floorCircleRegionPickMode = false;
        _floorCirclePickRadius = 0f;

        if (_floorSelected)
            UpdateFloorStatus();
        else
            UpdateViewTitle();
    }

    private void CancelFloorPolygonRegionPickMode()
    {
        if (!_floorPolygonRegionPickMode)
            return;

        _floorPolygonRegionPickMode = false;
        _floorPolygonPickX.Clear();
        _floorPolygonPickY.Clear();
        _floorPolygonPreviewX = 0f;
        _floorPolygonPreviewY = 0f;

        if (_floorSelected)
            UpdateFloorStatus();
        else
            UpdateViewTitle();
    }

    private void CancelFloorZoneDrag()
    {
        if (!_floorZoneDragging)
            return;

        _floorZoneDragging = false;
        _floorZoneDragId = Guid.Empty;
        _floorZoneDragPreviewValue = 0f;

        if (_floorSelected)
            UpdateFloorStatus();
        else
            UpdateViewTitle();
    }

    private FloorZone? GetSelectedFloorZone()
    {
        if (!_selectedFloorZoneId.HasValue || _project.Room.Floor == null)
            return null;

        return _project.Room.Floor.Zones.FirstOrDefault(z => z.Id == _selectedFloorZoneId.Value);
    }

    private void PopulateFloorZoneSelector()
    {
        var floor = _project.Room.Floor;
        if (floor == null)
            return;

        var items = floor.Zones
            .Select(z => new WallSurfaceSelectorItem(z.Id, FormatFloorZoneSelectorLabel(z)))
            .ToList();

        _syncingFloorZoneMaterial = true;
        FloorZoneSelectorCombo.ItemsSource = items;
        int selectedIndex = 0;
        if (_selectedFloorZoneId.HasValue)
        {
            int idx = items.FindIndex(i => i.Id == _selectedFloorZoneId.Value);
            if (idx >= 0)
                selectedIndex = idx;
        }

        FloorZoneSelectorCombo.SelectedIndex = items.Count > 0 ? selectedIndex : -1;
        FloorZoneSelectorCombo.IsEnabled = items.Count > 0;
        SyncFloorZoneMaterialCombo();
        SyncFloorRegionOffsetFields();
        _syncingFloorZoneMaterial = false;
    }

    private static string FormatFloorZoneSelectorLabel(FloorZone zone) =>
        $"{zone.Name} ({FloorZoneGeometry.FormatSummary(zone)})";

    private void SyncFloorZoneMaterialCombo()
    {
        FloorZoneMaterialCombo.IsEnabled = _project.Room.Floor?.Zones.Count > 0;

        if (FloorZoneSelectorCombo.SelectedItem is not WallSurfaceSelectorItem selected)
        {
            FloorZoneMaterialCombo.SelectedItem = null;
            return;
        }

        var zone = GetSelectedFloorZone();
        if (zone == null)
        {
            FloorZoneMaterialCombo.SelectedItem = null;
            return;
        }

        FloorZoneMaterialCombo.SelectedItem = FloorMaterialCatalog.TryGet(zone.MaterialId, out var mat) && mat != null
            ? mat
            : FloorMaterialCatalog.GetDefault();
    }

    private void SyncFloorRegionOffsetFields()
    {
        var zone = GetSelectedFloorZone();

        if (zone == null)
        {
            FloorRegionOffsetBox.Text = string.Empty;
            FloorRegionOffsetStartAlongBox.Text = string.Empty;
            FloorRegionOffsetEndAlongBox.Text = string.Empty;
            FloorRegionOffsetBottomBox.Text = string.Empty;
            FloorRegionOffsetTopBox.Text = string.Empty;
            return;
        }

        FloorRegionOffsetBox.Text = zone.OffsetMm.ToString("0", CultureInfo.InvariantCulture);

        bool rectangular = zone.Shape == WallRegionShape.Rectangular;
        FloorRegionOffsetStartAlongBox.Text = rectangular
            ? zone.OffsetEdgeStartAlongMm.ToString("0", CultureInfo.InvariantCulture)
            : string.Empty;
        FloorRegionOffsetEndAlongBox.Text = rectangular
            ? zone.OffsetEdgeEndAlongMm.ToString("0", CultureInfo.InvariantCulture)
            : string.Empty;
        FloorRegionOffsetBottomBox.Text = rectangular
            ? zone.OffsetEdgeBottomMm.ToString("0", CultureInfo.InvariantCulture)
            : string.Empty;
        FloorRegionOffsetTopBox.Text = rectangular
            ? zone.OffsetEdgeTopMm.ToString("0", CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private void FloorZoneSelectorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingFloorZoneMaterial || _project.Room.Floor == null)
            return;

        _syncingFloorZoneMaterial = true;
        SyncFloorZoneMaterialCombo();
        SyncFloorRegionOffsetFields();
        _syncingFloorZoneMaterial = false;

        if (FloorZoneSelectorCombo.SelectedItem is WallSurfaceSelectorItem selected)
            _selectedFloorZoneId = selected.Id;

        bool edgeOffset = GetSelectedFloorZone()?.Shape == WallRegionShape.Rectangular;
        FloorRegionOffsetStartAlongBox.IsEnabled = edgeOffset && FloorZoneSelectorCombo.IsEnabled;
        FloorRegionOffsetEndAlongBox.IsEnabled = edgeOffset && FloorZoneSelectorCombo.IsEnabled;
        FloorRegionOffsetBottomBox.IsEnabled = edgeOffset && FloorZoneSelectorCombo.IsEnabled;
        FloorRegionOffsetTopBox.IsEnabled = edgeOffset && FloorZoneSelectorCombo.IsEnabled;
        Viewport.InvalidateVisual();
    }

    private void FloorZoneMaterialCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingFloorZoneMaterial || _project.Room.Floor == null)
            return;

        if (FloorZoneSelectorCombo.SelectedItem is not WallSurfaceSelectorItem selected)
            return;

        if (FloorZoneMaterialCombo.SelectedItem is not FloorMaterialDefinition material)
            return;

        var zone = GetSelectedFloorZone();
        if (zone == null)
            return;

        zone.MaterialId = material.Id;
        MarkProjectDirty();
        UpdateFloorRegionsSummary();
        Viewport.InvalidateVisual();
    }

    private void ApplyFloorRegionOffsetFromPanel()
    {
        if (_syncingPropertyPanel || _project.Room.Floor == null)
            return;

        if (FloorZoneSelectorCombo.SelectedItem is not WallSurfaceSelectorItem selected)
            return;

        if (!PropertyPanelInput.TryParseMm(FloorRegionOffsetBox.Text, out float offset))
            return;

        if (FloorZoneService.TrySetZoneOffset(_project.Room.Floor, selected.Id, offset, out string? error))
        {
            MarkProjectDirty();
            UpdateFloorRegionsSummary();
            Viewport.InvalidateVisual();
        }
        else if (error != null)
            FloorRegionsSummaryText.Text = error;
    }

    private void ApplyFloorRegionEdgeOffsetFromPanel()
    {
        if (_syncingPropertyPanel || _project.Room.Floor == null)
            return;

        if (FloorZoneSelectorCombo.SelectedItem is not WallSurfaceSelectorItem selected)
            return;

        var zone = GetSelectedFloorZone();
        if (zone == null || zone.Shape != WallRegionShape.Rectangular)
            return;

        if (!PropertyPanelInput.TryParseMm(FloorRegionOffsetStartAlongBox.Text, out float startAlong) ||
            !PropertyPanelInput.TryParseMm(FloorRegionOffsetEndAlongBox.Text, out float endAlong) ||
            !PropertyPanelInput.TryParseMm(FloorRegionOffsetBottomBox.Text, out float bottom) ||
            !PropertyPanelInput.TryParseMm(FloorRegionOffsetTopBox.Text, out float top))
            return;

        var floor = _project.Room.Floor;
        bool ok =
            FloorZoneService.TrySetZoneEdgeOffset(floor, selected.Id, WallRegionEdgeKind.StartAlong, startAlong, out string? error) &&
            FloorZoneService.TrySetZoneEdgeOffset(floor, selected.Id, WallRegionEdgeKind.EndAlong, endAlong, out error) &&
            FloorZoneService.TrySetZoneEdgeOffset(floor, selected.Id, WallRegionEdgeKind.Bottom, bottom, out error) &&
            FloorZoneService.TrySetZoneEdgeOffset(floor, selected.Id, WallRegionEdgeKind.Top, top, out error);

        if (ok)
        {
            MarkProjectDirty();
            UpdateFloorRegionsSummary();
            Viewport.InvalidateVisual();
        }
        else if (error != null)
            FloorRegionsSummaryText.Text = error;
    }

    private void FloorRegionOffsetBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        ApplyFloorRegionOffsetFromPanel();
        e.Handled = true;
    }

    private void FloorRegionOffsetEdgeBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        ApplyFloorRegionEdgeOffsetFromPanel();
        e.Handled = true;
    }

    private void UpdateFloorRegionsSummary()
    {
        var floor = _project.Room.Floor;
        if (floor == null || floor.Zones.Count == 0)
        {
            FloorRegionsSummaryText.Text = "Nenhuma região.";
            return;
        }

        FloorRegionsSummaryText.Text = string.Join("\n",
            floor.Zones.Select(z => $"• {FloorZoneGeometry.FormatSummary(z)}"));
    }

    private bool TryCommitFloorPolygonRegion(out string? error)
    {
        error = null;
        var floor = _project.Room.Floor;

        if (floor == null)
        {
            error = "Piso não encontrado.";
            return false;
        }

        if (_floorPolygonPickX.Count < FloorZoneService.MinPolygonVertices)
        {
            error = $"Polígono precisa de pelo menos {FloorZoneService.MinPolygonVertices} vértices.";
            return false;
        }

        if (!FloorZoneService.TryAddPolygonZone(
                floor,
                _floorPolygonPickX,
                _floorPolygonPickY,
                out var zone,
                out error))
            return false;

        _floorPolygonRegionPickMode = false;
        _floorPolygonPickX.Clear();
        _floorPolygonPickY.Clear();
        MarkProjectDirty();
        SelectFloorZone(zone!);
        return true;
    }

    private bool TryExtendFloorPolygonPickByLength(float lengthMm, out string? error)
    {
        error = null;

        if (_floorPolygonPickX.Count == 0 || lengthMm <= 0f)
        {
            error = "Defina o primeiro ponto antes de digitar o comprimento.";
            return false;
        }

        if (_project.Room.Floor == null)
        {
            error = "Piso não encontrado.";
            return false;
        }

        float lastX = _floorPolygonPickX[^1];
        float lastY = _floorPolygonPickY[^1];
        float dx = _floorPolygonPreviewX - lastX;
        float dy = _floorPolygonPreviewY - lastY;
        float len = MathF.Sqrt(dx * dx + dy * dy);

        if (len < 1f)
        {
            dx = 1f;
            dy = 0f;
            len = 1f;
        }

        float scale = lengthMm / len;
        _floorPolygonPickX.Add(lastX + dx * scale);
        _floorPolygonPickY.Add(lastY + dy * scale);
        _floorPolygonPreviewX = _floorPolygonPickX[^1];
        _floorPolygonPreviewY = _floorPolygonPickY[^1];
        return true;
    }

    private void PropertyMaterialCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPropertyPanel || !_selectedModuleId.HasValue)
            return;

        if (PropertyMaterialCombo.SelectedItem is not MaterialDefinition material)
            return;

        var module = _project.FindModule(_selectedModuleId.Value);

        if (module == null || !SceneModuleVisibilityService.IsEditable(module) || module.MaterialId == material.Id)
            return;

        module.MaterialId = material.Id;
        MarkProjectDirty();
        Viewport.InvalidateVisual();
        PropertyMaterialHintText.Text = $"Acabamento: {material.DisplayName}.";
    }

    private void OpenPartsList_Click(object sender, RoutedEventArgs e)
    {
        var window = new PartsListWindow(_project, MarkProjectDirty);
        window.Owner = this;
        window.ShowDialog();
    }

    private void ExportTechnicalPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"{ProjectDisplayName}-tecnico.pdf"
        };

        if (dialog.ShowDialog() != true)
            return;

        var parts = PartsListService.Build(_project);
        var drawing = TechnicalDrawingService.Build(_project);
        TechnicalPdfExporter.Export(_project, parts, drawing, dialog.FileName);

        MessageBox.Show(
            "PDF t?cnico exportado com sucesso.",
            "Projeto",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportDxf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "DXF (*.dxf)|*.dxf",
            FileName = $"{ProjectDisplayName}-planta.dxf"
        };

        if (dialog.ShowDialog() != true)
            return;

        var drawing = TechnicalDrawingService.Build(_project);
        DxfExporter.ExportFloorPlan(drawing, dialog.FileName);

        MessageBox.Show(
            "DXF da planta exportado com sucesso.",
            "Projeto",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportDxfPieces_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "DXF (*.dxf)|*.dxf",
            FileName = $"{ProjectDisplayName}-pecas.dxf"
        };

        if (dialog.ShowDialog() != true)
            return;

        var parts = PartsListService.Build(_project);
        DxfExporter.ExportPieces(parts, dialog.FileName);

        MessageBox.Show(
            "DXF das peças exportado com sucesso.",
            "Projeto",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ImportDxf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "DXF (*.dxf)|*.dxf",
            Title = "Importar planta DXF"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var result = DxfImporter.ImportFloorPlan(dialog.FileName);

            if (result.Walls.Count == 0)
            {
                MessageBox.Show(
                    "Nenhuma linha DXF foi encontrada no arquivo.",
                    "Importar DXF",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _project.Room.SetWalls(result.Walls);
            _project.Metadata.ShowAutomaticCeiling = _project.Room.ShowAutomaticCeiling;
            MarkProjectDirty();
            RefreshStatusBarAfterViewChange();

            MessageBox.Show(
                $"{result.Walls.Count} parede(s) importada(s) do DXF.",
                "Importar DXF",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"N?o foi poss?vel importar o DXF.\n\n{ex.Message}",
                "Importar DXF",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ExportFloorPlanPng_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG (*.png)|*.png",
            FileName = $"{ProjectDisplayName}-planta-cotas.png"
        };

        if (dialog.ShowDialog() != true)
            return;

        var drawing = TechnicalDrawingService.Build(_project);
        TechnicalFloorPlanPngExporter.Export(drawing, dialog.FileName, ProjectDisplayName);

        MessageBox.Show(
            "PNG da planta com cotas exportado com sucesso.",
            "Exibir",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ToggleAutomaticCeiling_Click(object sender, RoutedEventArgs e)
    {
        _project.Room.ShowAutomaticCeiling = !_project.Room.ShowAutomaticCeiling;
        _project.Metadata.ShowAutomaticCeiling = _project.Room.ShowAutomaticCeiling;
        _project.Room.RebuildAutomaticCeiling();
        MarkProjectDirty();
        RefreshStatusBarAfterViewChange();
    }

    private void OpenCutPlan_Click(object sender, RoutedEventArgs e)
    {
        var window = new CutPlanWindow(_project, MarkProjectDirty);
        window.Owner = this;
        window.ShowDialog();
    }

    private void ExportCutPlanCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"{ProjectDisplayName}-plano-corte.csv"
        };

        if (dialog.ShowDialog() != true)
            return;

        var plan = CutPlanService.Build(_project);
        CutPlanCsvExporter.Export(plan, dialog.FileName);

        MessageBox.Show(
            $"CSV exportado ? {plan.TotalSheets} chapa(s), aproveitamento m?dio {plan.OverallUtilizationPercent:0.0}%.",
            "Produ??o",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportMachineCutPlanJson_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON plano de corte (*.json)|*.json",
            FileName = $"{ProjectDisplayName}-plano-corte-maquina.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        var document = MachineCutPlanExportService.Build(_project);
        MachineCutPlanExportService.ExportToFile(_project, dialog.FileName);

        MessageBox.Show(
            $"JSON exportado — {document.Summary.TotalSheets} chapa(s), {document.Summary.TotalPlacedPieces} peça(s), aproveitamento {document.Summary.OverallUtilizationPercent:0.0}%.",
            "Produção",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportCncDrillCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV furos CNC (*.csv)|*.csv",
            FileName = $"{ProjectDisplayName}-furos-cnc.csv"
        };

        if (dialog.ShowDialog() != true)
            return;

        int drillRows = MachineCutPlanExportService.CountDrillRows(_project);
        MachineCutPlanExportService.ExportDrillCsv(_project, dialog.FileName);

        MessageBox.Show(
            $"CSV exportado — {drillRows} furo(s) com coordenadas na chapa.",
            "Produção",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportCncJobJson_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON CNC (*.json)|*.json",
            FileName = $"{ProjectDisplayName}-cnc-job.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        var (cutOps, drillOps) = MachineCutPlanExportService.CountCncJobOperations(_project);
        MachineCutPlanExportService.ExportCncJob(_project, dialog.FileName);

        MessageBox.Show(
            $"JSON CNC exportado — {cutOps} corte(s), {drillOps} furo(s) em coordenadas de chapa.",
            "Produção",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportJaraguaTap_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "G-code Mach4 (*.tap)|*.tap",
            FileName = $"{ProjectDisplayName}.tap"
        };

        if (dialog.ShowDialog() != true)
            return;

        var (cutOps, drillOps, sheets) = MachineCutPlanExportService.CountJaraguaTapOperations(_project);
        MachineCutPlanExportService.ExportJaraguaTap(_project, dialog.FileName);

        string sheetNote = sheets > 1
            ? $" ({sheets} arquivos *-chapa-NN.tap)"
            : string.Empty;

        MessageBox.Show(
            $"G-code Jaraguá exportado — {cutOps} corte(s), {drillOps} furo(s){sheetNote}.",
            "Produção",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportPartLabelsPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"{ProjectDisplayName}-etiquetas.pdf"
        };

        if (dialog.ShowDialog() != true)
            return;

        var labels = PartLabelsService.Build(_project);
        PartLabelsPdfExporter.Export(labels, dialog.FileName);

        MessageBox.Show(
            $"{labels.TotalCount} etiquetas exportadas com sucesso.",
            "Produ??o",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void LoadUserLibrary()
    {
        ReloadLibraryCatalog();
        RefreshCustomModuleButtons();
        PopulateMaterialCombo();
        PopulateWallSurfaceMaterialCombos();
    }

    private void ReloadLibraryCatalog() =>
        LibraryReloadService.ReloadFromDefaultPath();

    private void RefreshLibraryUiAfterCatalogChange()
    {
        RefreshCustomModuleButtons();
        RefreshCozinhasLibraryButtons();
        ApplyBuiltinModuleLibraryIcons();
        RefreshPanelModuleButtons();
        ApplyLibraryCatalogFilter();
        RefreshSceneModuleList();
        PopulateMaterialCombo();
        PopulateWallSurfaceMaterialCombos();
    }

    private void ReloadLibrary_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ReloadLibraryCatalog();
            RefreshLibraryUiAfterCatalogChange();
            SetStatusBarOverrides(hint: "Biblioteca recarregada do arquivo local.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível recarregar a biblioteca.\n\n{ex.Message}",
                "Traços 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void RefreshCustomModuleButtons()
    {
        CustomModulesPanel.Children.Clear();
        var customModules = ModuleCatalog.Custom;

        CustomLibraryExpander.Visibility = customModules.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        foreach (var module in customModules)
        {
            var button = new Button
            {
                Content = ModuleCatalogThumbnail.BuildInsertButtonContent(module),
                Height = 32,
                Margin = new Thickness(0, 4, 0, 4),
                Padding = new Thickness(4, 2, 4, 2),
                Tag = module.Id
            };

            button.Click += CustomModuleButton_Click;
            CustomModulesPanel.Children.Add(button);
            EnableModuleInsertDrag(button);
        }

        ApplyLibraryCatalogFilter();
    }

    private void RefreshCozinhasLibraryButtons()
    {
        CozinhasLibraryHost.Children.Clear();

        var byGroup = ModuleCatalog.GetCozinhaCatalog()
            .Select(definition => ModuleCatalog.GetRequired(definition.Id))
            .GroupBy(definition => string.IsNullOrWhiteSpace(definition.LibraryGroup)
                ? ModuleLibraryHierarchy.GroupInferiores
                : definition.LibraryGroup)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (string groupName in ModuleLibraryHierarchy.CozinhaGroupOrder)
        {
            if (!byGroup.TryGetValue(groupName, out List<ModuleDefinition>? groupModules) ||
                groupModules.Count == 0)
                continue;

                var groupExpander = new Expander
            {
                Header = groupName,
                IsExpanded = groupName == ModuleLibraryHierarchy.GroupInferiores,
                Margin = new Thickness(0, 2, 0, 2),
                Tag = $"group:{groupName}"
            };
            AutomationProperties.SetAutomationId(groupExpander, $"CozinhaGroup_{SanitizeAutomationId(groupName)}");

            var groupHost = new StackPanel();
            string[] subOrder = ModuleLibraryHierarchy.CozinhaSubGroupOrder.TryGetValue(groupName, out string[]? ordered)
                ? ordered
                : [];

            var bySub = groupModules
                .GroupBy(definition => string.IsNullOrWhiteSpace(definition.LibrarySubGroup)
                    ? "Diversos"
                    : definition.LibrarySubGroup)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (string subName in subOrder.Concat(bySub.Keys.Except(subOrder, StringComparer.OrdinalIgnoreCase)))
            {
                if (!bySub.TryGetValue(subName, out List<ModuleDefinition>? subModules) ||
                    subModules.Count == 0)
                    continue;

                var subExpander = new Expander
                {
                    Header = subName,
                    IsExpanded = groupName == ModuleLibraryHierarchy.GroupInferiores &&
                                 subName == ModuleLibraryHierarchy.SubCantos,
                    Margin = new Thickness(8, 2, 0, 2),
                    Tag = $"sub:{groupName}/{subName}"
                };
                AutomationProperties.SetAutomationId(
                    subExpander,
                    $"CozinhaSub_{SanitizeAutomationId(groupName)}_{SanitizeAutomationId(subName)}");

                var subHost = new StackPanel();

                foreach (var module in subModules.OrderBy(definition => definition.CatalogOrder)
                             .ThenBy(definition => definition.DisplayName))
                {
                    var button = new Button
                    {
                        Content = ModuleCatalogThumbnail.BuildInsertButtonContent(module),
                        Height = 48,
                        Margin = new Thickness(0, 4, 0, 4),
                        Padding = new Thickness(4, 2, 4, 2),
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Tag = module.Id
                    };

                    AutomationProperties.SetAutomationId(
                        button,
                        ModuleLibraryHierarchy.GetBuiltinInsertAutomationId(module.Id));
                    button.Click += ModuleLibraryInsertButton_Click;
                    subHost.Children.Add(button);
                    EnableModuleInsertDrag(button);
                }

                subExpander.Content = subHost;
                groupHost.Children.Add(subExpander);
            }

            groupExpander.Content = groupHost;
            CozinhasLibraryHost.Children.Add(groupExpander);
        }
    }

    private static string SanitizeAutomationId(string value) =>
        string.Concat(value.Where(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-'))
            .Replace(' ', '_');

    private void LibrarySearchBox_TextChanged(object sender, TextChangedEventArgs e) =>
        ApplyLibraryCatalogFilter();

    private void ApplyLibraryCatalogFilter()
    {
        string query = LibrarySearchBox.Text ?? string.Empty;
        bool filtering = !string.IsNullOrWhiteSpace(query);

        int visibleCount = 0;
        visibleCount += FilterNestedLibraryButtons(CozinhasLibraryHost, query);
        visibleCount += SetModuleInsertButtonVisibility(ModuleWardrobeButton, "guarda-roupa-2p", query);
        visibleCount += SetModuleInsertButtonVisibility(ModuleNightstandButton, "criado-mudo", query);
        visibleCount += SetModuleInsertButtonVisibility(ModuleChestButton, "comoda-4g", query);

        int panelVisible = 0;

        foreach (var child in PanelModulesPanel.Children)
        {
            if (child is not Button button || button.Tag is not string definitionId)
                continue;

            if (!ModuleCatalog.TryGet(definitionId, out ModuleDefinition? definition) || definition == null)
                continue;

            bool visible = ModuleCatalogFilterService.Matches(definition, query);
            button.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            if (visible)
                panelVisible++;
        }

        visibleCount += panelVisible;

        int customVisible = 0;

        foreach (var child in CustomModulesPanel.Children)
        {
            if (child is not Button button || button.Tag is not string definitionId)
                continue;

            if (!ModuleCatalog.TryGet(definitionId, out ModuleDefinition? definition) || definition == null)
                continue;

            bool visible = ModuleCatalogFilterService.Matches(definition, query);
            button.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            if (visible)
                customVisible++;
        }

        visibleCount += customVisible;

        LibrarySearchEmptyText.Visibility = filtering && visibleCount == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        UpdateNestedLibraryExpanderVisibility(CozinhasLibraryExpander, CozinhasLibraryHost, filtering);

        UpdateLibraryExpanderVisibility(
            DormitoriosLibraryExpander,
            filtering,
            ModuleWardrobeButton,
            ModuleNightstandButton,
            ModuleChestButton);

        UpdatePanelLibraryExpanderVisibility(filtering, panelVisible);

        if (filtering)
        {
            CustomLibraryExpander.Visibility = customVisible > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (customVisible > 0)
                CustomLibraryExpander.IsExpanded = true;

            return;
        }

        CustomLibraryExpander.Visibility = ModuleCatalog.Custom.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        PanelsLibraryExpander.Visibility = Visibility.Visible;
    }

    private static int FilterNestedLibraryButtons(Panel host, string query)
    {
        int visible = 0;

        foreach (var child in host.Children)
        {
            if (child is Expander expander && expander.Content is Panel nested)
            {
                int nestedVisible = FilterNestedLibraryButtons(nested, query);
                bool show = nestedVisible > 0 || string.IsNullOrWhiteSpace(query);
                expander.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

                if (nestedVisible > 0 && !string.IsNullOrWhiteSpace(query))
                    expander.IsExpanded = true;

                visible += nestedVisible;
                continue;
            }

            if (child is not Button button || button.Tag is not string definitionId)
                continue;

            if (!ModuleCatalog.TryGet(definitionId, out ModuleDefinition? definition) || definition == null)
                continue;

            bool match = ModuleCatalogFilterService.Matches(definition, query);
            button.Visibility = match ? Visibility.Visible : Visibility.Collapsed;

            if (match)
                visible++;
        }

        return visible;
    }

    private static void UpdateNestedLibraryExpanderVisibility(
        Expander root,
        Panel host,
        bool filtering)
    {
        if (!filtering)
        {
            root.Visibility = Visibility.Visible;
            return;
        }

        bool anyVisible = host.Children
            .OfType<Expander>()
            .Any(expander => expander.Visibility == Visibility.Visible);

        root.Visibility = anyVisible ? Visibility.Visible : Visibility.Collapsed;

        if (anyVisible)
            root.IsExpanded = true;
    }

    private void UpdatePanelLibraryExpanderVisibility(bool filtering, int panelVisible)
    {
        if (!filtering)
        {
            PanelsLibraryExpander.Visibility = Visibility.Visible;
            return;
        }

        PanelsLibraryExpander.Visibility = panelVisible > 0 ? Visibility.Visible : Visibility.Collapsed;

        if (panelVisible > 0)
            PanelsLibraryExpander.IsExpanded = true;
    }

    private void RefreshPanelModuleButtons()
    {
        PanelModulesPanel.Children.Clear();

        foreach (var module in ModuleCatalog.BuiltIn
                     .Where(definition => definition.Category == ModuleCategory.Paineis)
                     .OrderBy(definition => definition.DisplayName))
        {
            var button = new Button
            {
                Content = ModuleCatalogThumbnail.BuildInsertButtonContent(module),
                Height = 32,
                Margin = new Thickness(0, 4, 0, 4),
                Padding = new Thickness(4, 2, 4, 2),
                Tag = module.Id
            };

            AutomationProperties.SetAutomationId(button, GetPanelModuleAutomationId(module.Id));
            button.Click += PanelModuleButton_Click;
            PanelModulesPanel.Children.Add(button);
            EnableModuleInsertDrag(button);
        }
    }

    private static string GetPanelModuleAutomationId(string definitionId) =>
        definitionId.ToLowerInvariant() switch
        {
            "painel-liso" => "ModulePanelPlainButton",
            "painel-canaletado" => "ModulePanelGroovedButton",
            "painel-ripado" => "ModulePanelSlattedButton",
            _ => $"ModulePanel_{definitionId.Replace('-', '_')}"
        };

    private void PanelModuleButton_Click(object sender, RoutedEventArgs e)
    {
        ConsumeModuleLibraryClick();
    }

    private static int SetModuleInsertButtonVisibility(Button button, string definitionId, string query)
    {
        bool visible = ModuleCatalogFilterService.Matches(ModuleCatalog.GetRequired(definitionId), query);
        button.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        return visible ? 1 : 0;
    }

    private static void UpdateLibraryExpanderVisibility(
        Expander expander,
        bool filtering,
        params Button[] buttons)
    {
        if (!filtering)
        {
            expander.Visibility = Visibility.Visible;
            return;
        }

        bool anyVisible = buttons.Any(button => button.Visibility == Visibility.Visible);
        expander.Visibility = anyVisible ? Visibility.Visible : Visibility.Collapsed;

        if (anyVisible)
            expander.IsExpanded = true;
    }

    private void CustomModuleButton_Click(object sender, RoutedEventArgs e)
    {
        ConsumeModuleLibraryClick();
    }

    private void ModuleLibraryInsertButton_Click(object sender, RoutedEventArgs e) =>
        ConsumeModuleLibraryClick();

    private void ApplyBuiltinModuleLibraryIcons()
    {
        ApplyInsertButtonIcon(ModuleWardrobeButton, "guarda-roupa-2p");
        ApplyInsertButtonIcon(ModuleNightstandButton, "criado-mudo");
        ApplyInsertButtonIcon(ModuleChestButton, "comoda-4g");
    }

    private void ApplyInsertButtonIcon(Button button, string definitionId)
    {
        var definition = ModuleCatalog.GetRequired(definitionId);
        button.Tag = definitionId;
        button.Content = ModuleCatalogThumbnail.BuildInsertButtonContent(definition);
        button.Padding = new Thickness(4, 2, 4, 2);
        EnableModuleInsertDrag(button);
    }

    private void EnableModuleInsertDrag(Button button)
    {
        button.PreviewMouseLeftButtonDown -= ModuleInsertButton_PreviewMouseLeftButtonDown;
        button.PreviewMouseLeftButtonDown += ModuleInsertButton_PreviewMouseLeftButtonDown;
    }

    private void ModuleInsertButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Button { Tag: string definitionId })
            return;

        _moduleLibraryDragStart = e.GetPosition(null);
        _moduleLibraryDragPending = true;
        _moduleLibraryPendingDefinitionId = definitionId;
        _moduleLibraryDragStarted = false;
    }

    private void MainWindow_ModuleLibraryPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_moduleLibraryDragPending && e.LeftButton == MouseButtonState.Pressed)
        {
            Vector delta = e.GetPosition(null) - _moduleLibraryDragStart;

            if (Math.Abs(delta.X) < ModuleLibraryDragThresholdPx &&
                Math.Abs(delta.Y) < ModuleLibraryDragThresholdPx)
                return;

            if (string.IsNullOrWhiteSpace(_moduleLibraryPendingDefinitionId))
                return;

            _moduleLibraryDragPending = false;
            _moduleLibraryCustomDragging = true;
            _moduleLibraryDragStarted = true;
            BeginModuleInsertMode(_moduleLibraryPendingDefinitionId);
            Mouse.Capture(this);
        }

        if (!_moduleLibraryCustomDragging || _moduleInsertDefinitionId == null)
            return;

        Point viewportPoint = Mouse.GetPosition(Viewport);
        UpdateModulePreview(viewportPoint.X, viewportPoint.Y);
        Viewport.InvalidateVisual();
    }

    private void MainWindow_ModuleLibraryPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_moduleLibraryCustomDragging)
        {
            FinishModuleLibraryDrag();
            e.Handled = true;
            return;
        }

        if (_moduleLibraryDragPending)
        {
            _moduleLibraryDragPending = false;
            _moduleLibraryPendingDefinitionId = null;
        }
    }

    private void FinishModuleLibraryDrag()
    {
        if (!_moduleLibraryCustomDragging)
            return;

        _moduleLibraryCustomDragging = false;
        _moduleLibraryPendingDefinitionId = null;

        if (Mouse.Captured == this)
            Mouse.Capture(null);

        Point viewportPoint = Mouse.GetPosition(Viewport);
        bool overViewport = viewportPoint.X >= 0 && viewportPoint.Y >= 0 &&
                            viewportPoint.X <= Viewport.ActualWidth &&
                            viewportPoint.Y <= Viewport.ActualHeight;

        if (overViewport &&
            _hasModulePreview &&
            _previewModuleSnappedToWall &&
            !string.IsNullOrWhiteSpace(_moduleInsertDefinitionId))
        {
            if (!TryInsertModuleFromDrop(_moduleInsertDefinitionId, viewportPoint.X, viewportPoint.Y, out string? error))
            {
                CancelModuleInsertMode();

                if (!string.IsNullOrWhiteSpace(error))
                    SetStatusBarOverrides(hint: error);
            }
        }
        else
        {
            CancelModuleInsertMode();
        }

        Viewport.InvalidateVisual();
    }

    private bool ConsumeModuleLibraryClick()
    {
        if (!_moduleLibraryDragStarted)
            return false;

        _moduleLibraryDragStarted = false;
        return true;
    }

    private void OpenLibraryEditor_Click(object sender, RoutedEventArgs e)
    {
        var window = new LibraryEditorWindow(OnLibraryChanged);
        window.Owner = this;
        window.ShowDialog();
    }

    private void OnLibraryChanged()
    {
        ReloadLibraryCatalog();
        RefreshLibraryUiAfterCatalogChange();
        MarkProjectDirty();
        SetStatusBarOverrides(hint: "Biblioteca salva e aplicada.");
    }

    private void ExportErp_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            FileName = $"{ProjectDisplayName}-erp.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        ErpExportService.ExportToFile(_project, dialog.FileName, LibraryState.LibraryName);

        MessageBox.Show(
            "Pacote ERP exportado com sucesso.",
            "Ferramentas",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportBackup_Click(object sender, RoutedEventArgs e)
    {
        if (_projectFilePath == null || !File.Exists(_projectFilePath))
        {
            MessageBox.Show(
                "Salve o projeto antes de gerar o backup.",
                "Backup",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "ZIP (*.zip)|*.zip",
            FileName = $"{ProjectDisplayName}-backup.zip"
        };

        if (dialog.ShowDialog() != true)
            return;

        ProjectBackupService.ExportZip(_projectFilePath, dialog.FileName);

        MessageBox.Show(
            "Backup criado com projeto e biblioteca.",
            "Ferramentas",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OpenDimensionConfigurator_Click(object sender, RoutedEventArgs e)
    {
        // Se já está aberta, apenas traz para frente (instância única, comportamento Promob).
        if (_dimensionConfiguratorWindow != null)
        {
            _dimensionConfiguratorWindow.Activate();
            return;
        }

        var settings = DimensionConfiguratorService.GetSettings(_project);
        bool hasSelection = _selectedModuleId.HasValue;

        _dimensionConfiguratorWindow = new DimensionConfiguratorWindow(settings, hasSelection)
        {
            Owner = this
        };

        _dimensionConfiguratorWindow.OnAutoSave = settings =>
        {
            DimensionConfiguratorService.SaveSettings(_project, settings);
            MarkProjectDirty();
        };

        // Callback compartilhado por Aplicar (sem fechar) e OK (fecha depois).
        _dimensionConfiguratorWindow.OnApply = (resultSettings, scope) =>
        {
            switch (scope)
            {
                case DimensionConfiguratorApplyScope.NextInsertionsOnly:
                    DimensionConfiguratorService.SaveSettings(_project, resultSettings);
                    SetStatusBarOverrides(hint: "Padrão salvo para próximas inserções (módulos existentes não alterados).");
                    break;

                case DimensionConfiguratorApplyScope.SelectedAndNext:
                    DimensionConfiguratorService.SaveSettings(_project, resultSettings);
                    DimensionConfiguratorService.ApplyToModules(
                        _project,
                        resultSettings,
                        scope,
                        _selectedModuleId,
                        _selectedModuleIds.Count > 0 ? _selectedModuleIds : null);
                    SetStatusBarOverrides(hint: "Configuração aplicada nos módulos selecionados.");
                    break;

                case DimensionConfiguratorApplyScope.AllExistingAndNext:
                    DimensionConfiguratorService.SaveSettings(_project, resultSettings);
                    DimensionConfiguratorService.ApplyToModules(
                        _project,
                        resultSettings,
                        scope,
                        _selectedModuleId,
                        _selectedModuleIds.Count > 0 ? _selectedModuleIds : null);
                    SetStatusBarOverrides(hint: "Configuração aplicada em todos os módulos existentes.");
                    break;
            }

            MarkProjectDirty();
            Viewport.InvalidateVisual();
            RefreshStatusBarAfterViewChange();

            if (_selectedModuleId.HasValue)
            {
                var module = _project.FindModule(_selectedModuleId.Value);
                if (module != null)
                {
                    var definition = ModuleCatalog.GetRequired(module.DefinitionId);
                    UpdateModulePropertyPanel(module, definition);
                }
            }
        };

        _dimensionConfiguratorWindow.Closed += (_, _) => _dimensionConfiguratorWindow = null;

        // Não-modal: o usuário continua manipulando o 3D com a janela aberta (comportamento Promob).
        _dimensionConfiguratorWindow.Show();
    }

    private void SyncDimensionConfiguratorSelectionState()
    {
        if (_dimensionConfiguratorWindow == null)
            return;

        bool hasSelection = _selectedModuleId.HasValue || _selectedModuleIds.Count > 0;
        _dimensionConfiguratorWindow.UpdateHasSelectedModule(hasSelection);
    }

    private void ConstructionProfilePadrao_Click(object sender, RoutedEventArgs e) =>
        ApplyConstructionProfile(ConstructionProfiles.Padrao);

    private void ConstructionProfileReforcado_Click(object sender, RoutedEventArgs e) =>
        ApplyConstructionProfile(ConstructionProfiles.Reforcado);

    private void ConstructionProfileEconomico_Click(object sender, RoutedEventArgs e) =>
        ApplyConstructionProfile(ConstructionProfiles.Economico);

    private void ApplyConstructionProfile(string profileId)
    {
        ConstructionProfiles.Apply(_project, profileId);
        MarkProjectDirty();
        RefreshStatusBarAfterViewChange();
    }

    private void ResetToNewProject() =>
        ResetToNewProjectCore();

    private void ResetToNewProjectCore()
    {
        ExitWallEditorMode();
        CancelWallMode();
        CancelOpeningInsertMode();
        CancelModuleInsertMode();
        ClearSelection();

        _wallDraft.Reset();
        _wallDraft.Thickness = DefaultWallThickness;
        _wallDraft.Height = DefaultWallHeight;
        _projectTabs.Active.ResetToEmpty();

        _projectFilePath = null;
        ClearProjectDirty();

        SeedDefaultRoom();

        RefreshSceneModuleList();
        UpdateProjectWindowTitle();
        RefreshStatusBarAfterViewChange();
        Keyboard.Focus(this);
    }

    private void SeedDefaultRoom()
    {
        // 2 paredes em L (esquerda + fundo) — paridade Promob.
        // Câmera olha da diagonal (frente-direita) para a esquina interna.
        var draft = new WallDraft
        {
            Thickness = DefaultWallThickness,
            Height = DefaultWallHeight
        };

        draft.Start(new OpenTK.Mathematics.Vector2(0f, 5000f));   // frente-esquerda
        draft.ConfirmPoint(new OpenTK.Mathematics.Vector2(0f, 0f));     // esquina (fundo-esq)
        draft.ConfirmPoint(new OpenTK.Mathematics.Vector2(5000f, 0f));  // fundo-direita
        // 2 paredes: esquerda (0,5000→0,0) + fundo (0,0→5000,0)

        _project.Room.SetWalls(draft.BuildWalls());
        _project.Room.SeedFloorFromBounds();

        RoomCompartmentService.EnsureInitialized(_project.Room, _project.Metadata);
        RebuildWallDraftFromRoom();

        MeasureBox.Visibility = Visibility.Collapsed;

        // Câmera da diagonal frente-direita olhando para a esquina — paridade Promob
        _camera.ViewMode = CameraViewMode.Perspective;
        _camera.Yaw = 45f;
        _camera.Pitch = 30f;
        FrameCameraOnRoom();

        Viewport.InvalidateVisual();
        _isDefaultRoom = true;
    }

    private void LoadProjectFromFile(string filePath)
    {
        string fullPath = Path.GetFullPath(filePath);

        if (_projectTabs.TryFindByFilePath(fullPath, out int existingIndex))
        {
            SwitchToProjectTab(existingIndex);
            return;
        }

        bool useCurrentTab = _projectTabs.Tabs.Count == 1
            && string.IsNullOrEmpty(_projectTabs.Active.FilePath)
            && !_projectTabs.Active.IsDirty
            && !_isProjectDirty;

        try
        {
            var document = ProjectPersistence.LoadFromFile(fullPath);
            var loaded = ProjectPersistence.LoadProject(document);

            if (!useCurrentTab)
            {
                PersistActiveTabState();
                _projectTabs.AddTab();
                _projectTabs.SetActive(_projectTabs.Tabs.Count - 1);
            }

            CancelWallMode();
            CancelOpeningInsertMode();
            CancelModuleInsertMode();
            ExitWallEditorMode();
            ClearSelection();

            _wallDraft.Reset();
            _project.ImportFrom(loaded);
            RebuildWallDraftFromRoom();

            _projectFilePath = fullPath;
            _project.Metadata = document.Metadata;
            PopulateWallLayerCombo();
            PopulateWallCompartmentCombo();
            PopulateModuleLayerCombo();
            ClearProjectDirty();
            FrameCameraOnRoom();
            RefreshSceneModuleList();
            MaterialsPanel.Bind(
                _project,
                BuildMaterialApplicationContext,
                OnMaterialSelectedFromWindow,
                BeginMaterialCopyMode);
            UpdateProjectWindowTitle();
            RefreshStatusBarAfterViewChange();
            RefreshProjectTabBar();
            Keyboard.Focus(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"N?o foi poss?vel abrir o projeto:\n{ex.Message}",
                "Tra?os 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private bool SaveProjectInternal(bool saveAs)
    {
        try
        {
            if (saveAs || string.IsNullOrEmpty(_projectFilePath))
            {
                var dialog = new SaveFileDialog
                {
                    Filter = $"Projeto Tra?os (*{ProjectPersistence.FileExtension})|*{ProjectPersistence.FileExtension}",
                    Title = "Salvar projeto como",
                    DefaultExt = ProjectPersistence.FileExtension.TrimStart('.'),
                    FileName = ProjectDisplayName + ProjectPersistence.FileExtension
                };

                if (dialog.ShowDialog() != true)
                    return false;

                _projectFilePath = dialog.FileName;
                _project.Metadata.Name = Path.GetFileNameWithoutExtension(_projectFilePath);
            }

            var document = ProjectPersistence.CreateFromProject(_project, _project.Metadata);
            ProjectPersistence.SaveToFile(document, _projectFilePath);
            _project.Metadata = document.Metadata;
            ClearProjectDirty();
            PersistActiveTabState();
            UpdateProjectWindowTitle();
            RefreshProjectTabBar();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"N?o foi poss?vel salvar o projeto:\n{ex.Message}",
                "Tra?os 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    private void RebuildWallDraftFromRoom()
    {
        _wallDraft.Reset();
        _wallDraft.Thickness = DefaultWallThickness;
        _wallDraft.Height = DefaultWallHeight;

        if (_project.Room.Walls.Count == 0)
            return;

        var first = _project.Room.Walls[0];
        _wallDraft.Thickness = first.Thickness;
        _wallDraft.Height = first.Height;
        _wallDraft.Orientation = first.Orientation;
        _wallDraft.MeasureSide = first.MeasureSide;

        // Cantos da face de referência (não o eixo) — BuildWalls offseta o eixo a partir daqui.
        // Precisa de N+1 pontos para N paredes (Start da 1ª + End de cada uma).
        var referenceCorners = new List<OpenTK.Mathematics.Vector2>(_project.Room.Walls.Count + 1);
        for (var i = 0; i < _project.Room.Walls.Count; i++)
        {
            var face = WallInnerFaceService.GetReferenceFace(_project.Room.Walls[i], _project.Room.Walls);
            if (i == 0)
                referenceCorners.Add(face.InnerStart);
            referenceCorners.Add(face.InnerEnd);
        }

        _wallDraft.Start(referenceCorners[0]);
        for (var i = 1; i < referenceCorners.Count; i++)
            _wallDraft.ConfirmPoint(referenceCorners[i]);

        if (_project.Room.IsClosed)
            _wallDraft.CloseSmart();
    }

    private void SyncRoomFromDraft() =>
        _project.Room.SetWalls(_wallDraft.BuildRoom().Walls);

    private void ResetPropertyPanelLabels()
    {
        PropertyLengthLabel.Text = "Comprimento (mm)";
        PropertyHeightLabel.Text = "Altura (mm)";
        PropertyDepthLabel.Text = "Espessura (mm)";
        PropertyHintText.Text = "Selecione uma parede, abertura ou m?dulo. Enter confirma.";
    }

    private void PropertyPanelBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        ApplyPropertyPanelFromInputs();
        e.Handled = true;
    }

    private void PropertyObliqueDoorCountCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPropertyPanel || !_selectedModuleId.HasValue)
            return;

        var module = _project.FindModule(_selectedModuleId.Value);
        if (module == null || !SceneModuleVisibilityService.IsEditable(module))
            return;

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        bool isOblique = definition.ShapeKind == ModuleShapeKind.Oblique;
        bool isEndTerminal = definition.ShapeKind is ModuleShapeKind.EndDiagonal or ModuleShapeKind.EndChamfer;
        if (!isOblique && !isEndTerminal)
            return;

        if (isOblique)
            module.ObliqueDoorCount = Math.Clamp(PropertyObliqueDoorCountCombo.SelectedIndex + 1, 1, 2);
        else
        {
            module.EndTerminal ??= EndTerminalParams.FromDefinition(definition);
            module.EndTerminal.DoorCount = Math.Clamp(PropertyObliqueDoorCountCombo.SelectedIndex + 1, 1, 2);
            module.EndTerminal.ClampToModule(module.Width, module.Depth,
                definition.ShapeKind == ModuleShapeKind.EndChamfer);
        }
        module.RebuildMesh(definition, DimensionConfiguratorService.GetSettings(_project));
        MarkProjectDirty();
        RefreshCollisionState();
        UpdateModulePropertyPanel(module, definition);
        RefreshSceneModuleList();
        Viewport.InvalidateVisual();
    }

    private void PropertyObliqueHingeSideCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPropertyPanel || !_selectedModuleId.HasValue)
            return;

        var module = _project.FindModule(_selectedModuleId.Value);
        if (module == null || !SceneModuleVisibilityService.IsEditable(module))
            return;

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        if (definition.ShapeKind != ModuleShapeKind.Oblique)
            return;

        module.ObliqueHingesOnLeft = PropertyObliqueHingeSideCombo.SelectedIndex <= 0;
        module.RebuildMesh(definition, DimensionConfiguratorService.GetSettings(_project));
        MarkProjectDirty();
        UpdateModulePropertyPanel(module, definition);
        Viewport.InvalidateVisual();
    }

    private void PropertyDrawerSlideTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPropertyPanel || !_selectedModuleId.HasValue)
            return;

        var module = _project.FindModule(_selectedModuleId.Value);
        if (module == null || !SceneModuleVisibilityService.IsEditable(module))
            return;

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        if (definition.DrawerCount <= 0 ||
            !string.Equals(definition.LibrarySubGroup, ModuleLibraryHierarchy.SubGaveteiros,
                StringComparison.OrdinalIgnoreCase))
            return;

        module.DrawerSlideType = PropertyDrawerSlideTypeCombo.SelectedIndex == 1
            ? DrawerSlideType.Concealed
            : DrawerSlideType.Telescopic;
        module.RebuildMesh(definition, DimensionConfiguratorService.GetSettings(_project));
        MarkProjectDirty();
        RefreshCollisionState();
        UpdateModulePropertyPanel(module, definition);
        Viewport.InvalidateVisual();
    }

    private void PropertySpecialColumnShelfCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPropertyPanel || !_selectedModuleId.HasValue)
            return;

        var module = _project.FindModule(_selectedModuleId.Value);
        if (module?.SpecialColumn == null || !SceneModuleVisibilityService.IsEditable(module))
            return;

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        module.SpecialColumn.ShelfNotched = PropertySpecialColumnShelfCombo.SelectedIndex <= 0;
        module.RebuildMesh(definition, DimensionConfiguratorService.GetSettings(_project));
        MarkProjectDirty();
        RefreshCollisionState();
        UpdateModulePropertyPanel(module, definition);
        Viewport.InvalidateVisual();
    }

    private void PartDeltaBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        ApplyPartDeltasFromPanel();
        e.Handled = true;
    }

    private void PartWidthDeltaBox_GotFocus(object sender, RoutedEventArgs e) =>
        EnsurePartHandleForPanelRow(PartHandleAxis.Width);

    private void PartDepthDeltaBox_GotFocus(object sender, RoutedEventArgs e) =>
        EnsurePartHandleForPanelRow(PartHandleAxis.Depth);

    private void PartHeightDeltaBox_GotFocus(object sender, RoutedEventArgs e) =>
        EnsurePartHandleForPanelRow(PartHandleAxis.Height);

    /// <summary>
    /// Ao focar o campo "+" do painel, seleciona a seta correspondente
    /// (respeitando o remap de Porta esq.: Largura↔vão em Z).
    /// </summary>
    private void EnsurePartHandleForPanelRow(PartHandleAxis panelRow)
    {
        if (_syncingPropertyPanel || _openModuleGroupId == null || string.IsNullOrEmpty(_selectedPartLabel))
            return;

        var module = _project.FindModule(_openModuleGroupId.Value);
        if (module == null ||
            !ModulePartDimensionService.TryComputeLocalDimensions(module, _selectedPartLabel, out var dims))
            return;

        bool swap = ModulePartAxisDisplay.FaceWidthIsDepth(_selectedPartLabel, dims);
        PartHandleAxis axis = panelRow switch
        {
            PartHandleAxis.Width => ModulePartAxisDisplay.PanelWidthAxis(swap),
            PartHandleAxis.Depth => ModulePartAxisDisplay.PanelDepthAxis(swap),
            _ => PartHandleAxis.Height
        };

        bool positive = true;
        if (_selectedPartHandle is { } current && current.Axis == axis)
            positive = current.Positive;

        _selectedPartHandle = new PartHandle(axis, positive);
        HighlightActivePartDeltaBox(axis);
        Viewport.InvalidateVisual();
    }

    private void PartApplyButton_Click(object sender, RoutedEventArgs e) => ApplyPartDeltasFromPanel();

    private void WallPropertyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        ApplyWallNewPropertiesFromPanel();
        e.Handled = true;
    }

    private void ApplyWallNewPropertiesFromPanel()
    {
        if (_syncingPropertyPanel || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);
        if (wall == null) return;

        bool changed = false;

        bool applyToGroup = _wallGroupSelected;

        // Comprimento: apenas face individual (Promob — grupo edita pé-direito/espessura/afastamento)
        if (!applyToGroup &&
            PropertyPanelInput.TryParseMm(WallLengthBox.Text, out float length) &&
            length > 0)
        {
            WallInnerFaceService.ApplyReferenceLengthToWall(wall, _project.Room.Walls, length);
            changed = true;
        }

        // Espessura
        if (PropertyPanelInput.TryParseMm(WallThicknessBox.Text, out float thickness) && thickness > 0)
        {
            if (applyToGroup)
                foreach (var w in _project.Room.Walls) w.Thickness = thickness;
            else
                wall.Thickness = thickness;
            changed = true;
        }

        // P?-direito Inicial/Final
        bool hasHs = PropertyPanelInput.TryParseMm(WallHeightStartBox.Text, out float hs) && hs > 0;
        bool hasHe = PropertyPanelInput.TryParseMm(WallHeightEndBox.Text, out float he) && he > 0;

        // Grupo: p?-direito inicial define ambos (altura uniforme)
        if (applyToGroup && hasHs)
        {
            he = hs;
            hasHe = true;
            WallHeightEndBox.Text = hs.ToString("0", CultureInfo.InvariantCulture);
        }

        if (hasHs || hasHe)
        {
            var targets = applyToGroup ? _project.Room.Walls : (IEnumerable<WallSegment>)[wall];
            foreach (var w in targets)
            {
                if (hasHs) w.HeightStart = hs;
                if (hasHe) w.HeightEnd   = he;
            }
            changed = true;
        }

        if (!applyToGroup && PropertyPanelInput.TryParseMm(WallFlechaBox.Text, out float flecha))
        {
            wall.FlechaMm = flecha;
            changed = true;
        }

        // Afastamento Piso
        if (PropertyPanelInput.TryParseMm(WallFloorOffsetBox.Text, out float fo))
        {
            if (applyToGroup)
                foreach (var w in _project.Room.Walls) w.FloorOffset = fo;
            else
                wall.FloorOffset = fo;
            changed = true;
        }

        // Cotas: face individual
        if (!applyToGroup)
        {
            if (PropertyPanelInput.TryParseMm(WallCotaAnteriorBox.Text, out float ca))
            { wall.CotaAnterior = ca; changed = true; }
            if (PropertyPanelInput.TryParseMm(WallCotaPosteriorBox.Text, out float cp))
            { wall.CotaPosterior = cp; changed = true; }
            if (PropertyPanelInput.TryParseMm(WallCotaInferiorBox.Text, out float ci))
            { wall.CotaInferior = ci; changed = true; }
            if (PropertyPanelInput.TryParseMm(WallCotaSuperiorBox.Text, out float cs))
            { wall.CotaSuperior = cs; changed = true; }
        }

        if (changed)
        {
            _project.Room.RebuildAutomaticFloor();
            MarkProjectDirty();
            string msg = applyToGroup
                ? $"Aplicado a todas as {_project.Room.Walls.Count} paredes."
                : "Aplicado ? parede selecionada.";
            UpdateSelectedWallStatus(wall, msg);
        }
    }

    private void WallDrawBottomFaceCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_syncingPropertyPanel || !_selectedWallId.HasValue) return;
        var wall = FindWallById(_selectedWallId.Value);
        if (wall == null) return;
        wall.DrawBottomFace = WallDrawBottomFaceCheck.IsChecked == true;
        MarkProjectDirty();
    }

    private void WallIsMovableCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_syncingPropertyPanel || !_selectedWallId.HasValue) return;
        var wall = FindWallById(_selectedWallId.Value);
        if (wall == null) return;
        wall.IsMovable = WallIsMovableCheck.IsChecked == true;
        UpdateWallPropertyPanel(wall);
        MarkProjectDirty();
    }

    private void WallIsVisibleCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_syncingPropertyPanel || !_selectedWallId.HasValue) return;
        var wall = FindWallById(_selectedWallId.Value);
        if (wall == null) return;
        wall.IsVisible = WallIsVisibleCheck.IsChecked == true;
        MarkProjectDirty();
    }

    private void WallConstructionTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPropertyPanel || _wallGroupSelected || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        var type = WallConstructionTypeCombo.SelectedIndex == 1
            ? WallConstructionType.DryWall
            : WallConstructionType.Normal;

        if (wall.ConstructionType == type)
            return;

        wall.ConstructionType = type;
        wall.Thickness = type == WallConstructionType.DryWall
            ? WallTypeDefaults.DryWallThicknessMm
            : WallTypeDefaults.NormalThicknessMm;
        MarkProjectDirty();
        UpdateWallPropertyPanel(wall);
    }

    private void PopulateWallLayerCombo()
    {
        WallLayerCombo.ItemsSource = WallLayerCatalog.GetDefinitions(_project.Metadata);
    }

    private void PopulateWallCompartmentCombo()
    {
        RoomCompartmentService.EnsureInitialized(_project.Room, _project.Metadata);
        WallCompartmentCombo.ItemsSource = _project.Room.Compartments.ToList();
    }

    private void PopulateModuleLayerCombo()
    {
        ModuleLayerCombo.ItemsSource = WallLayerCatalog.GetDefinitions(_project.Metadata);
    }

    private void ModuleLayerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPropertyPanel || !_selectedModuleId.HasValue)
            return;

        if (ModuleLayerCombo.SelectedItem is not WallLayerDefinition layer)
            return;

        var module = _project.FindModule(_selectedModuleId.Value);

        if (module == null || !SceneModuleVisibilityService.IsEditable(module))
            return;

        string layerId = WallLayerCatalog.NormalizeLayerId(layer.Id);

        if (WallLayerCatalog.NormalizeModuleLayerId(module.LayerId) == layerId)
            return;

        module.LayerId = layerId;
        MarkProjectDirty();

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        Title =
            $"Traços 3D - {definition.DisplayName} | Camada: {layer.DisplayName} | R gira 90° | Delete remove";
    }

    private void WallLayerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPropertyPanel || _wallGroupSelected || !_selectedWallId.HasValue)
            return;

        if (WallLayerCombo.SelectedItem is not WallLayerDefinition layer)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        string layerId = WallLayerCatalog.NormalizeLayerId(layer.Id);

        if (wall.LayerId == layerId)
            return;

        wall.LayerId = layerId;
        MarkProjectDirty();
        UpdateWallBandsSummary(wall);
        UpdateWallRegionsSummary(wall);
        PopulateWallBandSelector(wall);
        PopulateWallRegionSelector(wall);
    }

    private void WallCompartmentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPropertyPanel || _wallGroupSelected || !_selectedWallId.HasValue)
            return;

        if (WallCompartmentCombo.SelectedItem is not RoomCompartment compartment)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null || wall.CompartmentId == compartment.Id)
            return;

        wall.CompartmentId = compartment.Id;
        MarkProjectDirty();
        RefreshSceneModuleList();
        SetStatusBarOverrides(hint: $"Parede atribuída ao {RoomCompartmentService.FormatCompartmentGroupTitle(compartment, _project.Room.Compartments)}.");
    }

    private void WallAddBandButton_Click(object sender, RoutedEventArgs e) =>
        BeginHorizontalBandPick();

    private void WallAddVerticalBandButton_Click(object sender, RoutedEventArgs e) =>
        BeginVerticalBandPick();

    private void WallAddRegionByClickButton_Click(object sender, RoutedEventArgs e)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        WallRegionsExpander.IsExpanded = true;
        CancelWallHorizontalBandPickMode();
        CancelWallVerticalBandPickMode();
        CancelWallCircleRegionPickMode();
        CancelWallPolygonRegionPickMode();
        CancelWallPolygonVertexInsertMode();
        _wallRegionPickMode = true;
        _wallRegionPickStep = 0;
        _wallRegionPickFace = WallRegionFaceCombo.SelectedIndex == 1 ? FaceType.External : FaceType.Internal;
        Title = "Traços 3D - Região: clique o primeiro canto na face | Esc cancela";
        Keyboard.Focus(this);
    }

    private void WallAddPolygonRegionByClickButton_Click(object sender, RoutedEventArgs e)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        WallRegionsExpander.IsExpanded = true;
        CancelWallHorizontalBandPickMode();
        CancelWallVerticalBandPickMode();
        CancelWallRegionPickMode();
        CancelWallCircleRegionPickMode();
        _wallPolygonRegionPickMode = true;
        _wallPolygonPickAlong.Clear();
        _wallPolygonPickHeight.Clear();
        _wallPolygonPreviewAlong = wall.Length * 0.5f;
        _wallPolygonPreviewHeight = MathF.Max(wall.HeightStart, wall.HeightEnd) * 0.5f;
        _wallRegionPickFace = WallRegionFaceCombo.SelectedIndex == 1 ? FaceType.External : FaceType.Internal;
        Title = GetWallPolygonRegionPickTitle();
        ShowMeasureBox();
        Keyboard.Focus(this);
    }

    private string GetWallPolygonRegionPickTitle()
    {
        string faceLabel = _wallRegionPickFace == FaceType.Internal ? "interna" : "externa";

        if (_wallPolygonPickAlong.Count == 0)
            return $"Traços 3D - Polígono: clique o 1º ponto na face {faceLabel} | Esc cancela";

        return $"Traços 3D - Polígono ({_wallPolygonPickAlong.Count} pts): comprimento + Enter ou clique | fechar no 1º ponto | Esc cancela";
    }

    private void CancelWallPolygonRegionPickMode()
    {
        if (!_wallPolygonRegionPickMode)
            return;

        _wallPolygonRegionPickMode = false;
        _wallPolygonPickAlong.Clear();
        _wallPolygonPickHeight.Clear();
        _wallPolygonPreviewAlong = 0f;
        _wallPolygonPreviewHeight = 0f;

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }
        else
            UpdateViewTitle();
    }

    private void CancelWallPolygonVertexInsertMode()
    {
        if (!_wallPolygonVertexInsertMode)
            return;

        _wallPolygonVertexInsertMode = false;
        _wallPolygonVertexRegionId = Guid.Empty;

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }
        else
            UpdateViewTitle();
    }

    private void WallAddPolygonVertexButton_Click(object sender, RoutedEventArgs e)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        var region = GetSelectedWallRegion(wall);

        if (region == null || region.Shape != WallRegionShape.Polygon)
        {
            MessageBox.Show(
                "Selecione uma região poligonal na lista Região.",
                "Traços 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        WallRegionsExpander.IsExpanded = true;
        CancelWallHorizontalBandPickMode();
        CancelWallVerticalBandPickMode();
        CancelWallRegionPickMode();
        CancelWallCircleRegionPickMode();
        CancelWallPolygonRegionPickMode();

        _wallPolygonVertexInsertMode = true;
        _wallPolygonVertexRegionId = region.Id;
        Keyboard.Focus(this);
        Title = $"Traços 3D - {region.Name ?? "Polígono"}: clique na aresta para novo vértice | Esc cancela";
    }

    private void WallRotateRegion90Button_Click(object sender, RoutedEventArgs e)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        var region = GetSelectedWallRegion(wall);

        if (region == null)
        {
            MessageBox.Show(
                "Selecione uma região na lista Região.",
                "Traços 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (region.Shape == WallRegionShape.Circular)
        {
            MessageBox.Show(
                "Região circular não pode ser rotacionada.",
                "Traços 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (WallRegionService.TryRotateRegionByDelta(wall, region.Id, 90f, out string? error))
        {
            MarkProjectDirty();
            UpdateWallPropertyPanel(wall);
            Viewport.InvalidateVisual();
            Title = "Traços 3D - Região rotacionada 90°";
        }
        else if (error != null)
            WallRegionsSummaryText.Text = error;
    }

    private void CancelWallRegionVerticalCutMode()
    {
        if (!_wallRegionVerticalCutMode)
            return;

        _wallRegionVerticalCutMode = false;
        _wallRegionVerticalCutRegionId = Guid.Empty;
        _wallRegionVerticalCutAlongMm = 0f;
        _wallRegionVerticalCutHasLine = false;
        WallApplyVerticalCutButton.IsEnabled = false;

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }
        else
            UpdateViewTitle();
    }

    private void WallVerticalCutRegionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        var region = GetSelectedWallRegion(wall);

        if (region == null)
        {
            MessageBox.Show(
                "Selecione uma região na lista Região.",
                "Traços 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (region.Shape == WallRegionShape.Circular)
        {
            MessageBox.Show(
                "Região circular não pode ser cortada.",
                "Traços 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        WallRegionsExpander.IsExpanded = true;
        CancelWallHorizontalBandPickMode();
        CancelWallVerticalBandPickMode();
        CancelWallRegionPickMode();
        CancelWallCircleRegionPickMode();
        CancelWallPolygonRegionPickMode();
        CancelWallPolygonVertexInsertMode();

        _wallRegionVerticalCutMode = true;
        _wallRegionVerticalCutRegionId = region.Id;
        _wallRegionVerticalCutAlongMm = 0f;
        _wallRegionVerticalCutHasLine = false;
        WallApplyVerticalCutButton.IsEnabled = false;
        Keyboard.Focus(this);
        Title = $"Traços 3D - {region.Name ?? "Região"}: clique na região para posicionar corte vertical | Enter aplica | Esc cancela";
    }

    private void WallApplyVerticalCutButton_Click(object sender, RoutedEventArgs e) =>
        CommitWallRegionVerticalCut();

    private bool TryUpdateWallRegionVerticalCutAtScreen(double mouseX, double mouseY)
    {
        if (!_wallRegionVerticalCutMode || !_selectedWallId.HasValue)
            return false;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return false;

        var region = wall.Regions.FirstOrDefault(r => r.Id == _wallRegionVerticalCutRegionId);

        if (region == null || region.Shape == WallRegionShape.Circular)
        {
            CancelWallRegionVerticalCutMode();
            return false;
        }

        if (!TryPickWallFaceAtScreen(
                mouseX,
                mouseY,
                out WallSegment pickWall,
                out float along,
                out float height,
                out FaceType face,
                out bool hitTop) ||
            hitTop ||
            pickWall.Id != wall.Id ||
            face != region.Face ||
            !WallRegionGeometry.ContainsPoint(region, along, height))
            return false;

        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        _wallRegionVerticalCutAlongMm = WallRegionGeometry.ClampVerticalCutAlong(region, wall.Length, wallTop, along);
        _wallRegionVerticalCutHasLine = true;
        WallApplyVerticalCutButton.IsEnabled = true;
        Title = $"Traços 3D - Corte vertical em {_wallRegionVerticalCutAlongMm:0} mm | Enter ou Aplicar confirma | Esc cancela";
        Viewport.InvalidateVisual();
        return true;
    }

    private void UpdateWallRegionVerticalCutPreview(double mouseX, double mouseY)
    {
        if (!_wallRegionVerticalCutMode || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        var region = wall.Regions.FirstOrDefault(r => r.Id == _wallRegionVerticalCutRegionId);

        if (region == null)
            return;

        if (!TryPickWallFaceAtScreen(
                mouseX,
                mouseY,
                out var pickWall,
                out float along,
                out float height,
                out FaceType face,
                out bool hitTop) ||
            hitTop ||
            pickWall.Id != wall.Id ||
            face != region.Face ||
            !WallRegionGeometry.ContainsPoint(region, along, height))
            return;

        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        _wallRegionVerticalCutAlongMm = WallRegionGeometry.ClampVerticalCutAlong(region, wall.Length, wallTop, along);
        _wallRegionVerticalCutHasLine = true;
        WallApplyVerticalCutButton.IsEnabled = true;
    }

    private void CommitWallRegionVerticalCut()
    {
        if (!_wallRegionVerticalCutMode || !_wallRegionVerticalCutHasLine || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        if (WallRegionService.TryVerticalCutRegion(
                wall,
                _wallRegionVerticalCutRegionId,
                _wallRegionVerticalCutAlongMm,
                out Guid leftId,
                out Guid rightId,
                out string? error))
        {
            MarkProjectDirty();
            CancelWallRegionVerticalCutMode();
            UpdateWallPropertyPanel(wall);
            SelectWallSurfaceSelector(WallRegionSelectorCombo, leftId);
            Viewport.InvalidateVisual();
            Title = "Traços 3D - Região dividida pelo corte vertical";
        }
        else if (error != null)
        {
            WallRegionsSummaryText.Text = error;
            Title = $"Traços 3D - {error}";
        }
    }

    private bool TryInsertPolygonVertexAtScreen(double mouseX, double mouseY)
    {
        if (!_wallPolygonVertexInsertMode || !_selectedWallId.HasValue)
            return false;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return false;

        var region = wall.Regions.FirstOrDefault(r => r.Id == _wallPolygonVertexRegionId);

        if (region == null || region.Shape != WallRegionShape.Polygon)
        {
            CancelWallPolygonVertexInsertMode();
            return false;
        }

        if (!TryPickWallFaceAtScreen(
                mouseX,
                mouseY,
                out WallSegment pickWall,
                out float along,
                out float height,
                out FaceType face,
                out bool hitTop) ||
            hitTop ||
            pickWall.Id != wall.Id ||
            face != region.Face)
        {
            Title = $"Traços 3D - {region.Name ?? "Polígono"}: clique na aresta do polígono | Esc cancela";
            return true;
        }

        if (!WallRegionService.TryInsertPolygonVertexAtPoint(wall, region.Id, along, height, out string? error))
        {
            Title = $"Traços 3D - {error} | Esc cancela";
            return true;
        }

        MarkProjectDirty();
        PopulateWallRegionSelector(wall);
        SelectWallSurfaceSelector(WallRegionSelectorCombo, region.Id);
        UpdateWallPropertyPanel(wall);
        Title = $"Traços 3D - Vértice adicionado ({region.PolygonAlongMm.Count} pts) | clique outra aresta ou Esc";
        return true;
    }

    private bool TryCommitWallPolygonRegion(WallSegment wall, out string? error)
    {
        error = null;

        if (_wallPolygonPickAlong.Count < WallRegionService.MinPolygonVertices)
        {
            error = $"Polígono precisa de pelo menos {WallRegionService.MinPolygonVertices} vértices.";
            return false;
        }

        if (!WallRegionService.TryAddPolygonRegion(
                wall,
                _wallRegionPickFace,
                _wallPolygonPickAlong,
                _wallPolygonPickHeight,
                out _,
                out error))
            return false;

        _wallPolygonRegionPickMode = false;
        _wallPolygonPickAlong.Clear();
        _wallPolygonPickHeight.Clear();
        MarkProjectDirty();
        UpdateWallPropertyPanel(wall);
        return true;
    }

    private bool TryExtendWallPolygonPickByLength(float lengthMm, out string? error)
    {
        error = null;

        if (_wallPolygonPickAlong.Count == 0 || lengthMm <= 0f)
        {
            error = "Defina o primeiro ponto antes de digitar o comprimento.";
            return false;
        }

        var wall = FindWallById(_selectedWallId ?? Guid.Empty);

        if (wall == null)
        {
            error = "Parede não encontrada.";
            return false;
        }

        float lastAlong = _wallPolygonPickAlong[^1];
        float lastHeight = _wallPolygonPickHeight[^1];
        float dx = _wallPolygonPreviewAlong - lastAlong;
        float dy = _wallPolygonPreviewHeight - lastHeight;
        float len = MathF.Sqrt(dx * dx + dy * dy);

        if (len < 1f)
        {
            dx = 1f;
            dy = 0f;
            len = 1f;
        }

        float scale = lengthMm / len;
        float along = Math.Clamp(lastAlong + dx * scale, 0f, wall.Length);
        float height = Math.Clamp(lastHeight + dy * scale, 0f, MathF.Max(wall.HeightStart, wall.HeightEnd));

        _wallPolygonPickAlong.Add(along);
        _wallPolygonPickHeight.Add(height);
        _wallPolygonPreviewAlong = along;
        _wallPolygonPreviewHeight = height;
        return true;
    }

    private void WallAddCircleRegionByClickButton_Click(object sender, RoutedEventArgs e)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        CancelWallHorizontalBandPickMode();
        CancelWallVerticalBandPickMode();
        CancelWallRegionPickMode();
        CancelWallPolygonRegionPickMode();
        CancelWallPolygonVertexInsertMode();
        _wallCircleRegionPickMode = true;
        _wallCircleRegionPreviewAlong = wall.Length * 0.5f;
        _wallCircleRegionPreviewHeight = MathF.Max(wall.HeightStart, wall.HeightEnd) * 0.5f;
        _wallRegionPickFace = WallRegionFaceCombo.SelectedIndex == 1 ? FaceType.External : FaceType.Internal;
        Title = "Traços 3D - Região circular: clique o centro na face | Esc cancela";
        Keyboard.Focus(this);
    }

    private void CancelWallHorizontalBandPickMode()
    {
        if (!_wallHorizontalBandPickMode)
            return;

        _wallHorizontalBandPickMode = false;
        _wallHorizontalBandPickStep = 0;
        _wallHorizontalBandPickHeight1 = 0f;
        _wallHorizontalBandPreviewHeight2 = 0f;

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }
        else
            UpdateViewTitle();
    }

    private void CancelWallVerticalBandPickMode()
    {
        if (!_wallVerticalBandPickMode)
            return;

        _wallVerticalBandPickMode = false;
        _wallVerticalBandPickStep = 0;
        _wallVerticalBandPickAlong1 = 0f;
        _wallVerticalBandPreviewAlong = 0f;

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }
        else
            UpdateViewTitle();
    }

    private void CancelWallRegionPickMode()
    {
        if (!_wallRegionPickMode)
            return;

        _wallRegionPickMode = false;
        _wallRegionPickStep = 0;

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }
        else
            UpdateViewTitle();
    }

    private void CancelWallCircleRegionPickMode()
    {
        if (!_wallCircleRegionPickMode)
            return;

        _wallCircleRegionPickMode = false;
        _wallCircleRegionPreviewAlong = 0f;
        _wallCircleRegionPreviewHeight = 0f;

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }
        else
            UpdateViewTitle();
    }

    private void CancelWallBandDrag()
    {
        if (!_wallBandDragging)
            return;

        _wallBandDragging = false;
        _wallBandDragWallId = Guid.Empty;
        _wallBandDragBandId = Guid.Empty;
        _wallBandDragPreviewValue = 0f;

        if (Viewport.IsMouseCaptured)
            Viewport.ReleaseMouseCapture();

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }
        else
            UpdateViewTitle();
    }

    private bool TryBeginWallBandDragAtScreen(double mouseX, double mouseY)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return false;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null || wall.Bands.Count == 0)
            return false;

        if (!TryPickWallBandEdgeAtScreen(
                mouseX,
                mouseY,
                wall,
                out WallBand band,
                out WallBandEdgeKind edge,
                out float edgeValue))
            return false;

        _wallBandDragging = true;
        _wallBandDragWallId = wall.Id;
        _wallBandDragBandId = band.Id;
        _wallBandDragEdge = edge;
        _wallBandDragPreviewValue = edgeValue;
        Viewport.CaptureMouse();
        Title = "Traços 3D - Arraste a linha da faixa | Esc cancela";
        return true;
    }

    private void UpdateWallBandDragPreview(double mouseX, double mouseY)
    {
        if (!_wallBandDragging)
            return;

        var wall = FindWallById(_wallBandDragWallId);

        if (wall == null)
            return;

        var band = wall.Bands.FirstOrDefault(b => b.Id == _wallBandDragBandId);

        if (band == null)
            return;

        if (!TryPickWallFaceAtScreen(
                mouseX,
                mouseY,
                out var pickWall,
                out float along,
                out float height,
                out _,
                out bool hitTop) ||
            hitTop ||
            pickWall.Id != wall.Id)
            return;

        float raw = band.IsHorizontal ? height : along;
        _wallBandDragPreviewValue = MathF.Round(raw / 10f) * 10f;
    }

    private void CommitWallBandDrag()
    {
        if (!_wallBandDragging)
            return;

        string? error = null;
        var wall = FindWallById(_wallBandDragWallId);

        if (wall != null &&
            WallBandService.TrySetBandEdge(
                wall,
                _wallBandDragBandId,
                _wallBandDragEdge,
                _wallBandDragPreviewValue,
                out error))
        {
            MarkProjectDirty();
            UpdateWallPropertyPanel(wall);
            RefreshWallBandsEditor();
        }
        else if (error != null)
            Title = $"Traços 3D - {error}";

        CancelWallBandDrag();
    }

    private void CancelWallRegionDrag()
    {
        if (!_wallRegionDragging)
            return;

        _wallRegionDragging = false;
        _wallRegionDragWallId = Guid.Empty;
        _wallRegionDragRegionId = Guid.Empty;
        _wallRegionDragPreviewValue = 0f;

        if (Viewport.IsMouseCaptured)
            Viewport.ReleaseMouseCapture();

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }
        else
            UpdateViewTitle();
    }

    private void CancelWallRegionBodyDrag()
    {
        if (!_wallRegionBodyDragging)
            return;

        if (_wallRegionBodyDragSnapshot != null)
        {
            var wall = FindWallById(_wallRegionBodyDragWallId);

            if (wall != null)
            {
                var region = wall.Regions.FirstOrDefault(r => r.Id == _wallRegionBodyDragRegionId);

                if (region != null)
                    _wallRegionBodyDragSnapshot.RestoreTo(region);
            }
        }

        _wallRegionBodyDragging = false;
        _wallRegionBodyDragWallId = Guid.Empty;
        _wallRegionBodyDragRegionId = Guid.Empty;
        _wallRegionBodyDragPreviewDeltaAlong = 0f;
        _wallRegionBodyDragPreviewDeltaHeight = 0f;
        _wallRegionBodyDragSnapshot = null;

        if (Viewport.IsMouseCaptured)
            Viewport.ReleaseMouseCapture();

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }
        else
            UpdateViewTitle();
    }

    private bool TryBeginWallRegionBodyDragAtScreen(double mouseX, double mouseY)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return false;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null || wall.Regions.Count == 0)
            return false;

        if (!TryPickWallFaceAtScreen(
                mouseX,
                mouseY,
                out WallSegment pickWall,
                out float along,
                out float height,
                out FaceType face,
                out bool hitTop) ||
            hitTop ||
            pickWall.Id != wall.Id)
            return false;

        WallRegion? region = GetSelectedWallRegion(wall);

        if (region == null || region.Face != face)
        {
            region = wall.Regions.FirstOrDefault(r =>
                r.Face == face &&
                WallRegionGeometry.ContainsPoint(r, along, height));
        }

        if (region == null)
            return false;

        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);

        if (!WallRegionGeometry.ContainsPoint(region, along, height))
            return false;

        float distToEdge = WallRegionGeometry.DistanceToBoundary(region, along, height, wall.Length, wallTop);

        if (region.Shape != WallRegionShape.Polygon &&
            distToEdge < WallRegionService.RegionBodyDragEdgeToleranceMm)
            return false;

        SelectWallSurfaceSelector(WallRegionSelectorCombo, region.Id);
        WallRegionsExpander.IsExpanded = true;

        _wallRegionBodyDragging = true;
        _wallRegionBodyDragWallId = wall.Id;
        _wallRegionBodyDragRegionId = region.Id;
        _wallRegionBodyDragStartAlong = along;
        _wallRegionBodyDragStartHeight = height;
        _wallRegionBodyDragPreviewDeltaAlong = 0f;
        _wallRegionBodyDragPreviewDeltaHeight = 0f;
        _wallRegionBodyDragSnapshot = WallRegionMoveSnapshot.From(region);
        Viewport.CaptureMouse();
        Title = "Traços 3D - Arraste para mover a região | Esc cancela";
        return true;
    }

    private void UpdateWallRegionBodyDragPreview(double mouseX, double mouseY)
    {
        if (!_wallRegionBodyDragging || _wallRegionBodyDragSnapshot == null)
            return;

        var wall = FindWallById(_wallRegionBodyDragWallId);

        if (wall == null)
            return;

        var region = wall.Regions.FirstOrDefault(r => r.Id == _wallRegionBodyDragRegionId);

        if (region == null)
            return;

        if (!TryPickWallFaceAtScreen(
                mouseX,
                mouseY,
                out var pickWall,
                out float along,
                out float height,
                out FaceType face,
                out bool hitTop) ||
            hitTop ||
            pickWall.Id != wall.Id ||
            face != region.Face)
            return;

        float rawAlong = MathF.Round((along - _wallRegionBodyDragStartAlong) / 10f) * 10f;
        float rawHeight = MathF.Round((height - _wallRegionBodyDragStartHeight) / 10f) * 10f;
        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        (rawAlong, rawHeight) = WallRegionGeometry.ClampMoveDelta(region, wall.Length, wallTop, rawAlong, rawHeight);

        _wallRegionBodyDragPreviewDeltaAlong = rawAlong;
        _wallRegionBodyDragPreviewDeltaHeight = rawHeight;

        _wallRegionBodyDragSnapshot.RestoreTo(region);
        WallRegionGeometry.ApplyMoveDelta(region, rawAlong, rawHeight);
    }

    private void CommitWallRegionBodyDrag()
    {
        if (!_wallRegionBodyDragging)
            return;

        string? error = null;
        var wall = FindWallById(_wallRegionBodyDragWallId);

        if (wall != null &&
            WallRegionService.TryMoveRegion(
                wall,
                _wallRegionBodyDragRegionId,
                _wallRegionBodyDragPreviewDeltaAlong,
                _wallRegionBodyDragPreviewDeltaHeight,
                out error))
        {
            MarkProjectDirty();
            UpdateWallPropertyPanel(wall);
        }
        else if (error != null)
        {
            if (_wallRegionBodyDragSnapshot != null)
            {
                var region = wall?.Regions.FirstOrDefault(r => r.Id == _wallRegionBodyDragRegionId);

                if (region != null)
                    _wallRegionBodyDragSnapshot.RestoreTo(region);
            }

            Title = $"Traços 3D - {error}";
        }

        _wallRegionBodyDragging = false;
        _wallRegionBodyDragWallId = Guid.Empty;
        _wallRegionBodyDragRegionId = Guid.Empty;
        _wallRegionBodyDragPreviewDeltaAlong = 0f;
        _wallRegionBodyDragPreviewDeltaHeight = 0f;
        _wallRegionBodyDragSnapshot = null;

        if (Viewport.IsMouseCaptured)
            Viewport.ReleaseMouseCapture();

        if (_selectedWallId.HasValue)
        {
            var selectedWall = FindWallById(_selectedWallId.Value);

            if (selectedWall != null)
                UpdateSelectedWallStatus(selectedWall);
        }
        else
            UpdateViewTitle();
    }

    private bool TryBeginWallRegionRotationAtScreen(double mouseX, double mouseY)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return false;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null || wall.Regions.Count == 0)
            return false;

        if (!TryPickWallFaceAtScreen(
                mouseX,
                mouseY,
                out WallSegment pickWall,
                out float along,
                out float height,
                out FaceType face,
                out bool hitTop) ||
            hitTop ||
            pickWall.Id != wall.Id)
            return false;

        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        const float toleranceMm = 150f;

        WallRegion? region = GetSelectedWallRegion(wall);

        if (region == null ||
            region.Face != face ||
            region.Shape == WallRegionShape.Circular ||
            !WallRegionGeometry.TryPickRotationHandle(region, along, height, wall.Length, wallTop, toleranceMm))
        {
            region = wall.Regions.FirstOrDefault(r =>
                r.Face == face &&
                r.Shape != WallRegionShape.Circular &&
                WallRegionGeometry.TryPickRotationHandle(r, along, height, wall.Length, wallTop, toleranceMm));
        }

        if (region == null)
            return false;

        SelectWallSurfaceSelector(WallRegionSelectorCombo, region.Id);
        WallRegionsExpander.IsExpanded = true;

        _wallRegionRotating = true;
        _wallRegionRotateWallId = wall.Id;
        _wallRegionRotateRegionId = region.Id;
        _wallRegionRotateStartAngleDegrees = WallRegionGeometry.GetAngleDegreesFromCenter(region, along, height);
        _wallRegionRotatePreviewDeltaDegrees = 0f;
        _wallRegionRotateSnapshot = WallRegionMoveSnapshot.From(region);
        Viewport.CaptureMouse();
        Title = "Traços 3D - Arraste a alça preta para rotacionar | Esc cancela";
        return true;
    }

    private void UpdateWallRegionRotationPreview(double mouseX, double mouseY)
    {
        if (!_wallRegionRotating || _wallRegionRotateSnapshot == null)
            return;

        var wall = FindWallById(_wallRegionRotateWallId);

        if (wall == null)
            return;

        var region = wall.Regions.FirstOrDefault(r => r.Id == _wallRegionRotateRegionId);

        if (region == null)
            return;

        if (!TryPickWallFaceAtScreen(
                mouseX,
                mouseY,
                out var pickWall,
                out float along,
                out float height,
                out FaceType face,
                out bool hitTop) ||
            hitTop ||
            pickWall.Id != wall.Id ||
            face != region.Face)
            return;

        float rawDelta = WallRegionGeometry.GetAngleDegreesFromCenter(region, along, height) -
                         _wallRegionRotateStartAngleDegrees;

        while (rawDelta > 180f)
            rawDelta -= 360f;

        while (rawDelta < -180f)
            rawDelta += 360f;

        _wallRegionRotatePreviewDeltaDegrees = WallRegionGeometry.SnapRotationDegrees(rawDelta);

        _wallRegionRotateSnapshot.RestoreTo(region);
        WallRegionGeometry.ApplyRotationDelta(region, _wallRegionRotatePreviewDeltaDegrees);
    }

    private void CommitWallRegionRotation()
    {
        if (!_wallRegionRotating)
            return;

        string? error = null;
        var wall = FindWallById(_wallRegionRotateWallId);

        if (wall != null &&
            WallRegionService.TryRotateRegionByDelta(
                wall,
                _wallRegionRotateRegionId,
                _wallRegionRotatePreviewDeltaDegrees,
                out error))
        {
            MarkProjectDirty();
            UpdateWallPropertyPanel(wall);
        }
        else if (error != null)
        {
            if (_wallRegionRotateSnapshot != null)
            {
                var region = wall?.Regions.FirstOrDefault(r => r.Id == _wallRegionRotateRegionId);

                if (region != null)
                    _wallRegionRotateSnapshot.RestoreTo(region);
            }

            Title = $"Traços 3D - {error}";
        }

        _wallRegionRotating = false;
        _wallRegionRotateWallId = Guid.Empty;
        _wallRegionRotateRegionId = Guid.Empty;
        _wallRegionRotateStartAngleDegrees = 0f;
        _wallRegionRotatePreviewDeltaDegrees = 0f;
        _wallRegionRotateSnapshot = null;

        if (Viewport.IsMouseCaptured)
            Viewport.ReleaseMouseCapture();

        if (_selectedWallId.HasValue)
        {
            var selectedWall = FindWallById(_selectedWallId.Value);

            if (selectedWall != null)
                UpdateSelectedWallStatus(selectedWall);
        }
        else
            UpdateViewTitle();
    }

    private void CancelWallRegionRotation()
    {
        if (!_wallRegionRotating)
            return;

        if (_wallRegionRotateSnapshot != null)
        {
            var wall = FindWallById(_wallRegionRotateWallId);

            if (wall != null)
            {
                var region = wall.Regions.FirstOrDefault(r => r.Id == _wallRegionRotateRegionId);

                if (region != null)
                    _wallRegionRotateSnapshot.RestoreTo(region);
            }
        }

        _wallRegionRotating = false;
        _wallRegionRotateWallId = Guid.Empty;
        _wallRegionRotateRegionId = Guid.Empty;
        _wallRegionRotateStartAngleDegrees = 0f;
        _wallRegionRotatePreviewDeltaDegrees = 0f;
        _wallRegionRotateSnapshot = null;

        if (Viewport.IsMouseCaptured)
            Viewport.ReleaseMouseCapture();

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }
        else
            UpdateViewTitle();
    }

    private bool TryBeginWallRegionDragAtScreen(double mouseX, double mouseY)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return false;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null || wall.Regions.Count == 0)
            return false;

        if (!TryPickWallRegionEdgeAtScreen(
                mouseX,
                mouseY,
                wall,
                out WallRegion region,
                out WallRegionEdgeKind edge,
                out float edgeValue))
            return false;

        _wallRegionDragging = true;
        _wallRegionDragWallId = wall.Id;
        _wallRegionDragRegionId = region.Id;
        _wallRegionDragEdge = edge;
        _wallRegionDragPreviewValue = edgeValue;
        Viewport.CaptureMouse();
        Title = "Traços 3D - Arraste a borda da região | Esc cancela";
        return true;
    }

    private void UpdateWallRegionDragPreview(double mouseX, double mouseY)
    {
        if (!_wallRegionDragging)
            return;

        var wall = FindWallById(_wallRegionDragWallId);

        if (wall == null)
            return;

        var region = wall.Regions.FirstOrDefault(r => r.Id == _wallRegionDragRegionId);

        if (region == null)
            return;

        if (!TryPickWallFaceAtScreen(
                mouseX,
                mouseY,
                out var pickWall,
                out float along,
                out float height,
                out FaceType face,
                out bool hitTop) ||
            hitTop ||
            pickWall.Id != wall.Id ||
            face != region.Face)
            return;

        float raw = _wallRegionDragEdge switch
        {
            WallRegionEdgeKind.StartAlong or WallRegionEdgeKind.EndAlong => along,
            WallRegionEdgeKind.Radius => MathF.Sqrt(
                MathF.Pow(along - region.CenterAlongMm, 2f) +
                MathF.Pow(height - region.CenterHeightMm, 2f)),
            _ => height
        };
        _wallRegionDragPreviewValue = MathF.Round(raw / 10f) * 10f;
    }

    private void CommitWallRegionDrag()
    {
        if (!_wallRegionDragging)
            return;

        string? error = null;
        var wall = FindWallById(_wallRegionDragWallId);

        if (wall != null &&
            WallRegionService.TrySetRegionEdge(
                wall,
                _wallRegionDragRegionId,
                _wallRegionDragEdge,
                _wallRegionDragPreviewValue,
                out error))
        {
            MarkProjectDirty();
            UpdateWallPropertyPanel(wall);
        }
        else if (error != null)
            Title = $"Traços 3D - {error}";

        CancelWallRegionDrag();
    }

    private void WallAddRegionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        FaceType face = WallRegionFaceCombo.SelectedIndex == 1 ? FaceType.External : FaceType.Internal;
        string? error;

        bool ok = face == FaceType.Internal
            ? WallRegionService.TryAddDefaultTileRegion(wall, out _, out error)
            : WallRegionService.TryAddRectRegion(
                wall,
                face,
                wall.Length * 0.25f,
                wall.Length * 0.75f,
                1100f,
                2100f,
                out _,
                out error);

        if (!ok)
        {
            WallRegionsSummaryText.Text = error ?? "Não foi possível adicionar região.";
            return;
        }

        MarkProjectDirty();
        UpdateWallPropertyPanel(wall);
    }

    private void UpdateWallBandsSummary(WallSegment wall)
    {
        if (wall.Bands.Count == 0)
        {
            WallBandsSummaryText.Text = "Nenhuma faixa.";
            return;
        }

        WallBandsSummaryText.Text = string.Join("\n",
            wall.Bands.Select(b =>
            {
                string span = b.IsHorizontal
                    ? $"Horizontal {b.StartMm:0}–{b.EndMm:0} mm"
                    : $"Vertical {b.StartMm:0}–{b.EndMm:0} mm";
                string material = WallSurfaceMaterialCatalog.GetDisplayName(b.MaterialId);
                return string.IsNullOrWhiteSpace(b.MaterialId) ? span : $"{span} · {material}";
            }));
    }

    private void UpdateWallRegionsSummary(WallSegment wall)
    {
        if (wall.Regions.Count == 0)
        {
            WallRegionsSummaryText.Text = "Nenhuma região.";
            return;
        }

        WallRegionsSummaryText.Text = string.Join("\n",
            wall.Regions.Select(r =>
            {
                string region = WallRegionGeometry.FormatSummary(r);
                string material = WallSurfaceMaterialCatalog.GetDisplayName(r.MaterialId);
                return string.IsNullOrWhiteSpace(r.MaterialId) ? region : $"{region} · {material}";
            }));
    }

    private void PopulateWallBandSelector(WallSegment wall)
    {
        var items = wall.Bands
            .Select(b => new WallSurfaceSelectorItem(
                b.Id,
                b.IsHorizontal
                    ? $"Horizontal {b.StartMm:0}–{b.EndMm:0} mm"
                    : $"Vertical {b.StartMm:0}–{b.EndMm:0} mm"))
            .ToList();

        _syncingWallSurfaceMaterial = true;
        WallBandSelectorCombo.ItemsSource = items;
        WallBandSelectorCombo.SelectedIndex = items.Count > 0 ? 0 : -1;
        WallBandSelectorCombo.IsEnabled = items.Count > 0;
        SyncWallBandMaterialCombo(wall);
        _syncingWallSurfaceMaterial = false;
    }

    private void PopulateWallRegionSelector(WallSegment wall)
    {
        var items = wall.Regions
            .Select(r => new WallSurfaceSelectorItem(
                r.Id,
                FormatWallRegionSelectorLabel(r)))
            .ToList();

        _syncingWallSurfaceMaterial = true;
        WallRegionSelectorCombo.ItemsSource = items;
        WallRegionSelectorCombo.SelectedIndex = items.Count > 0 ? 0 : -1;
        WallRegionSelectorCombo.IsEnabled = items.Count > 0;
        SyncWallRegionMaterialCombo(wall);
        SyncWallFaceMaterialCombo(wall);
        SyncWallRegionOffsetFields(wall);
        _syncingWallSurfaceMaterial = false;
    }

    private void SyncWallBandMaterialCombo(WallSegment wall)
    {
        WallBandMaterialCombo.IsEnabled = wall.Bands.Count > 0;

        if (WallBandSelectorCombo.SelectedItem is not WallSurfaceSelectorItem selected)
        {
            WallBandMaterialCombo.SelectedItem = null;
            return;
        }

        var band = wall.Bands.FirstOrDefault(b => b.Id == selected.Id);
        WallBandMaterialCombo.SelectedItem = band == null
            ? null
            : WallSurfaceMaterialCatalog.FindOption(band.MaterialId);
    }

    private void SyncWallRegionMaterialCombo(WallSegment wall)
    {
        WallRegionMaterialCombo.IsEnabled = wall.Regions.Count > 0;

        if (WallRegionSelectorCombo.SelectedItem is not WallSurfaceSelectorItem selected)
        {
            WallRegionMaterialCombo.SelectedItem = null;
            return;
        }

        var region = wall.Regions.FirstOrDefault(r => r.Id == selected.Id);
        WallRegionMaterialCombo.SelectedItem = region == null
            ? null
            : WallSurfaceMaterialCatalog.FindOption(region.MaterialId);
    }

    private void SyncWallFaceMaterialCombo(WallSegment wall)
    {
        FaceType face = WallRegionFaceCombo.SelectedIndex == 1 ? FaceType.External : FaceType.Internal;
        string? materialId = wall.GetFaceMaterialId(face);
        WallFaceMaterialCombo.SelectedItem = string.IsNullOrWhiteSpace(materialId)
            ? null
            : WallSurfaceMaterialCatalog.FindOption(materialId);
    }

    private void WallRegionFaceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPropertyPanel || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        _syncingWallSurfaceMaterial = true;
        SyncWallFaceMaterialCombo(wall);
        _syncingWallSurfaceMaterial = false;
    }

    private void WallFaceMaterialCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingWallSurfaceMaterial || !_selectedWallId.HasValue)
            return;

        if (WallFaceMaterialCombo.SelectedItem is not WallSurfaceMaterialOption material)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        FaceType face = WallRegionFaceCombo.SelectedIndex == 1 ? FaceType.External : FaceType.Internal;
        wall.SetFaceMaterialId(face, material.Id);
        MarkProjectDirty();
        Viewport.InvalidateVisual();
    }

    private WallRegion? GetSelectedWallRegion(WallSegment wall)
    {
        if (WallRegionSelectorCombo.SelectedItem is not WallSurfaceSelectorItem selected)
            return null;

        return wall.Regions.FirstOrDefault(r => r.Id == selected.Id);
    }

    private void SyncWallRegionOffsetFields(WallSegment wall)
    {
        var region = GetSelectedWallRegion(wall);

        if (region == null)
        {
            PropertyRegionOffsetBox.Text = string.Empty;
            PropertyRegionOffsetStartAlongBox.Text = string.Empty;
            PropertyRegionOffsetEndAlongBox.Text = string.Empty;
            PropertyRegionOffsetBottomBox.Text = string.Empty;
            PropertyRegionOffsetTopBox.Text = string.Empty;
            return;
        }

        PropertyRegionOffsetBox.Text = region.OffsetMm.ToString("0", CultureInfo.InvariantCulture);

        bool rectangular = region.Shape == WallRegionShape.Rectangular;
        PropertyRegionOffsetStartAlongBox.Text = rectangular
            ? region.OffsetEdgeStartAlongMm.ToString("0", CultureInfo.InvariantCulture)
            : string.Empty;
        PropertyRegionOffsetEndAlongBox.Text = rectangular
            ? region.OffsetEdgeEndAlongMm.ToString("0", CultureInfo.InvariantCulture)
            : string.Empty;
        PropertyRegionOffsetBottomBox.Text = rectangular
            ? region.OffsetEdgeBottomMm.ToString("0", CultureInfo.InvariantCulture)
            : string.Empty;
        PropertyRegionOffsetTopBox.Text = rectangular
            ? region.OffsetEdgeTopMm.ToString("0", CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private void ApplyWallRegionOffsetFromPanel()
    {
        if (_syncingPropertyPanel || !_selectedWallId.HasValue)
            return;

        if (WallRegionSelectorCombo.SelectedItem is not WallSurfaceSelectorItem selected)
            return;

        if (!PropertyPanelInput.TryParseMm(PropertyRegionOffsetBox.Text, out float offset))
            return;

        var wall = FindWallById(_selectedWallId.Value);
        if (wall == null)
            return;

        if (WallRegionService.TrySetRegionOffset(wall, selected.Id, offset, out string? error))
        {
            MarkProjectDirty();
            UpdateWallRegionsSummary(wall);
            Viewport.InvalidateVisual();
        }
        else if (error != null)
            WallRegionsSummaryText.Text = error;
    }

    private void ApplyWallRegionEdgeOffsetFromPanel()
    {
        if (_syncingPropertyPanel || !_selectedWallId.HasValue)
            return;

        if (WallRegionSelectorCombo.SelectedItem is not WallSurfaceSelectorItem selected)
            return;

        var wall = FindWallById(_selectedWallId.Value);
        if (wall == null)
            return;

        var region = wall.Regions.FirstOrDefault(r => r.Id == selected.Id);
        if (region == null || region.Shape != WallRegionShape.Rectangular)
            return;

        if (!PropertyPanelInput.TryParseMm(PropertyRegionOffsetStartAlongBox.Text, out float startAlong) ||
            !PropertyPanelInput.TryParseMm(PropertyRegionOffsetEndAlongBox.Text, out float endAlong) ||
            !PropertyPanelInput.TryParseMm(PropertyRegionOffsetBottomBox.Text, out float bottom) ||
            !PropertyPanelInput.TryParseMm(PropertyRegionOffsetTopBox.Text, out float top))
            return;

        bool ok =
            WallRegionService.TrySetRegionEdgeOffset(wall, selected.Id, WallRegionEdgeKind.StartAlong, startAlong, out string? error) &&
            WallRegionService.TrySetRegionEdgeOffset(wall, selected.Id, WallRegionEdgeKind.EndAlong, endAlong, out error) &&
            WallRegionService.TrySetRegionEdgeOffset(wall, selected.Id, WallRegionEdgeKind.Bottom, bottom, out error) &&
            WallRegionService.TrySetRegionEdgeOffset(wall, selected.Id, WallRegionEdgeKind.Top, top, out error);

        if (ok)
        {
            MarkProjectDirty();
            UpdateWallRegionsSummary(wall);
            Viewport.InvalidateVisual();
        }
        else if (error != null)
            WallRegionsSummaryText.Text = error;
    }

    private void PropertyRegionOffsetEdgeBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        ApplyWallRegionEdgeOffsetFromPanel();
        e.Handled = true;
    }

    private void PropertyRegionOffsetBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        ApplyWallRegionOffsetFromPanel();
        e.Handled = true;
    }

    private void WallBandSelectorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingWallSurfaceMaterial || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);
        if (wall == null)
            return;

        _syncingWallSurfaceMaterial = true;
        SyncWallBandMaterialCombo(wall);
        _syncingWallSurfaceMaterial = false;
    }

    private void WallBandMaterialCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingWallSurfaceMaterial || !_selectedWallId.HasValue)
            return;

        if (WallBandSelectorCombo.SelectedItem is not WallSurfaceSelectorItem selected)
            return;

        if (WallBandMaterialCombo.SelectedItem is not WallSurfaceMaterialOption material)
            return;

        var wall = FindWallById(_selectedWallId.Value);
        var band = wall?.Bands.FirstOrDefault(b => b.Id == selected.Id);
        if (band == null)
            return;

        band.MaterialId = material.Id;
        MarkProjectDirty();
        UpdateWallBandsSummary(wall!);
        Viewport.InvalidateVisual();
    }

    private void WallRegionSelectorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingWallSurfaceMaterial || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);
        if (wall == null)
            return;

        _syncingWallSurfaceMaterial = true;
        SyncWallRegionMaterialCombo(wall);
        SyncWallRegionOffsetFields(wall);
        _syncingWallSurfaceMaterial = false;

        bool regionEdgeOffsetEnabled = GetSelectedWallRegion(wall)?.Shape == WallRegionShape.Rectangular;
        bool polygonSelected = GetSelectedWallRegion(wall)?.Shape == WallRegionShape.Polygon;
        WallAddPolygonVertexButton.IsEnabled = polygonSelected && WallRegionSelectorCombo.IsEnabled;
        PropertyRegionOffsetStartAlongBox.IsEnabled = regionEdgeOffsetEnabled && WallRegionSelectorCombo.IsEnabled;
        PropertyRegionOffsetEndAlongBox.IsEnabled = regionEdgeOffsetEnabled && WallRegionSelectorCombo.IsEnabled;
        PropertyRegionOffsetBottomBox.IsEnabled = regionEdgeOffsetEnabled && WallRegionSelectorCombo.IsEnabled;
        PropertyRegionOffsetTopBox.IsEnabled = regionEdgeOffsetEnabled && WallRegionSelectorCombo.IsEnabled;
        Viewport.InvalidateVisual();
    }

    private void WallRegionMaterialCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingWallSurfaceMaterial || !_selectedWallId.HasValue)
            return;

        if (WallRegionSelectorCombo.SelectedItem is not WallSurfaceSelectorItem selected)
            return;

        if (WallRegionMaterialCombo.SelectedItem is not WallSurfaceMaterialOption material)
            return;

        var wall = FindWallById(_selectedWallId.Value);
        var region = wall?.Regions.FirstOrDefault(r => r.Id == selected.Id);
        if (region == null)
            return;

        region.MaterialId = material.Id;
        MarkProjectDirty();
        UpdateWallRegionsSummary(wall!);
        Viewport.InvalidateVisual();
    }

    private static string FormatWallRegionSelectorLabel(WallRegion region)
    {
        string face = region.Face == FaceType.Internal ? "interna" : "externa";
        string name = region.Name ?? "Região";

        if (region.Shape == WallRegionShape.Polygon)
            return $"{name} polígono ({region.PolygonAlongMm.Count} pts, {face})";

        if (region.Shape == WallRegionShape.Circular)
            return $"{name} círculo ({face})";

        return $"{name} ({face})";
    }

    private sealed record WallSurfaceSelectorItem(Guid Id, string Label);

    private bool ShouldRenderWall(WallSegment wall) =>
        wall.IsVisible && WallLayerCatalog.IsLayerVisible(_project.Metadata, wall.LayerId);

    private bool ShouldRenderModule(ModuleInstance module) =>
        module.IsVisible && WallLayerCatalog.IsLayerVisible(_project.Metadata, module.LayerId);

    private bool IsWallPickable(WallSegment? wall) =>
        wall != null &&
        wall.IsVisible &&
        WallLayerCatalog.CanPickOnLayer(_project.Metadata, wall.LayerId);

    private bool IsModulePickable(ModuleInstance module) =>
        module.IsVisible &&
        !module.IsLocked &&
        WallLayerCatalog.CanPickOnLayer(_project.Metadata, module.LayerId);

    private void ClearSelectionIfLockedOrHidden()
    {
        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);
            if (!IsWallPickable(wall))
                ClearSelection();
        }

        if (_selectedModuleId.HasValue)
        {
            var module = _project.FindModule(_selectedModuleId.Value);
            if (module == null || !IsModulePickable(module) || !ShouldRenderModule(module))
                ClearSelection();
        }
    }

    private void WallSegmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        var confirm = MessageBox.Show(
            "Deseja segmentar a parede neste trecho?\n\nClique no ponto da parede onde deseja dividir.",
            "Tra?os 3D Studio",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        _wallSegmentPickMode = true;
        _wallSegmentTargetId = wall.Id;
        _wallSegmentPreviewDistance = wall.Length * 0.5f;
        Title = "Tra?os 3D - Segmentar parede: clique no ponto de divis?o | Esc cancela";
        Keyboard.Focus(this);
    }

    private void CancelWallSegmentPickMode()
    {
        if (!_wallSegmentPickMode)
            return;

        _wallSegmentPickMode = false;
        _wallSegmentTargetId = Guid.Empty;
        _wallSegmentPreviewDistance = 0f;

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }
        else
            UpdateViewTitle();
    }

    private void ApplyWallSegmentation(WallSegment original, float splitDistance, List<WallSegment> segments)
    {
        if (!_project.Room.TryReplaceWallWithSegments(original.Id, segments))
            return;

        WallSegmentationService.ReassignModulesAfterSplit(
            _project.Modules,
            original.Id,
            segments[0].Id,
            segments[1].Id,
            splitDistance);

        _wallSegmentPickMode = false;
        _wallSegmentTargetId = Guid.Empty;
        _wallSegmentPreviewDistance = 0f;
        MarkProjectDirty();
        SelectWall(segments[1]);
    }

    private void Wall304050PickMovingButton_Click(object sender, RoutedEventArgs e)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return;

        _wall304050PickMovingMode = true;
        Title = "Tra?os 3D - 30-40-50: clique na parede que desloca | Esc cancela";
        Keyboard.Focus(this);
    }

    private void Wall304050ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return;

        var referenceWall = FindWallById(_selectedWallId.Value);

        if (referenceWall == null)
            return;

        if (!PropertyPanelInput.TryParseMm(Wall304050ABox.Text, out float a) ||
            !PropertyPanelInput.TryParseMm(Wall304050BBox.Text, out float b) ||
            !PropertyPanelInput.TryParseMm(Wall304050CBox.Text, out float c))
            return;

        var movingWall = _wall304050MovingWallId.HasValue
            ? FindWallById(_wall304050MovingWallId.Value)
            : WallThirtyFortyFiftyService.TryFindAdjacentWall(referenceWall, _project.Room.Walls);

        if (movingWall == null)
        {
            MessageBox.Show(
                "Selecione a parede que desloca ou escolha uma parede com canto adjacente.",
                "Tra?os 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (!WallThirtyFortyFiftyService.TryApply(referenceWall, movingWall, a, b, c, out float angle))
        {
            MessageBox.Show(
                "N?o foi poss?vel aplicar 30-40-50. Verifique A, B, C e o canto entre as paredes.",
                "Tra?os 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _wall304050PickMovingMode = false;
        _project.Room.RebuildAutomaticFloor();
        MarkProjectDirty();
        SelectWall(movingWall);
        Title = $"Tra?os 3D - 30-40-50 aplicado: {angle:0.0}°";
        SetStatusBarOverrides(context: $"30-40-50: {angle:0.0}°");
    }

    private void CancelWall304050PickMode()
    {
        if (!_wall304050PickMovingMode)
            return;

        _wall304050PickMovingMode = false;

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }
        else
            UpdateViewTitle();
    }

    private void WallChamferButton_Click(object sender, RoutedEventArgs e)
    {
        if (_project.Room.Walls.Count == 0)
        {
            MessageBox.Show(
                "Construa paredes antes de aparar cantos.",
                "Traços 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (_wallChamferMode)
        {
            CancelWallChamferMode();
            return;
        }

        if (!_wallMode && !_wallEditorActive)
            BeginWallDrawing();

        PauseWallDrawingForChamfer();
        _wallChamferMode = true;
        _wallChamferPreviewWallId = Guid.Empty;
        UpdateWallChamferButton();

        if (string.IsNullOrWhiteSpace(WallChamferDistanceBox.Text))
            WallChamferDistanceBox.Text = WallCornerChamferService.DefaultChamferMm.ToString("0", CultureInfo.InvariantCulture);

        Title = "Traços 3D - Aparar Parede: clique no canto da parede | Esc cancela";
        Keyboard.Focus(this);
    }

    private void PauseWallDrawingForChamfer()
    {
        if (!_wallMode)
            return;

        _wallMode = false;
        _wallAppendMode = false;
        _hasLastPoint = false;
        _hasPreview = false;
        ClearWallReferenceState();
        MeasureBox.Visibility = Visibility.Collapsed;
    }

    private void CancelWallChamferMode()
    {
        if (!_wallChamferMode)
            return;

        _wallChamferMode = false;
        _wallChamferPreviewWallId = Guid.Empty;
        UpdateWallChamferButton();

        if (_wallEditorActive)
        {
            BeginWallDrawing();
            UpdateWallEditorStatus();
        }
        else
        {
            HideWallConstructionPanel();
            UpdateViewTitle();
        }
    }

    private void UpdateWallChamferButton()
    {
        WallChamferButton.FontWeight = _wallChamferMode ? FontWeights.Bold : FontWeights.Normal;
    }

    private void UpdateWallChamferPreview(double mouseX, double mouseY)
    {
        _wallChamferPreviewWallId = Guid.Empty;

        if (!TryPickWallAtScreen(mouseX, mouseY, out var wall, out _, out bool hitTop) || hitTop)
            return;

        Vector2 floorPoint = ScreenToFloor(mouseX, mouseY);

        if (!WallCornerChamferService.TryPickEndpoint(wall, floorPoint, out bool atStart))
            return;

        _wallChamferPreviewWallId = wall.Id;
        _wallChamferPreviewAtStart = atStart;
    }

    private void TryApplyWallChamferAtScreen(double mouseX, double mouseY)
    {
        if (!TryPickWallAtScreen(mouseX, mouseY, out var wall, out _, out bool hitTop) || hitTop)
        {
            Title = "Traços 3D - Aparar: clique mais perto do canto da parede | Esc cancela";
            return;
        }

        Vector2 floorPoint = ScreenToFloor(mouseX, mouseY);

        if (!WallCornerChamferService.TryPickEndpoint(wall, floorPoint, out bool atStart))
        {
            Title = "Traços 3D - Aparar: clique mais perto do canto da parede | Esc cancela";
            return;
        }

        if (!PropertyPanelInput.TryParseMm(WallChamferDistanceBox.Text, out float chamferMm))
        {
            Title = "Traços 3D - Aparar: informe o aparo em mm | Esc cancela";
            return;
        }

        if (!WallCornerChamferService.TryApply(wall, atStart, chamferMm))
        {
            Title = "Traços 3D - Aparo inválido (mín. 50 mm, parede muito curta) | Esc cancela";
            return;
        }

        _project.Room.RebuildAutomaticFloor();
        MarkProjectDirty();

        string endLabel = atStart ? "início" : "fim";
        Title = $"Traços 3D - Chanfro {chamferMm:0} mm no {endLabel} da parede";
        SetStatusBarOverrides(context: $"Chanfro {chamferMm:0} mm");
        _wallChamferPreviewWallId = wall.Id;
        _wallChamferPreviewAtStart = atStart;
    }

    private void ApplyPropertyPanelFromInputs()
    {
        if (_syncingPropertyPanel)
            return;

        if (!_selectedWallId.HasValue && !_selectedOpeningId.HasValue && !_selectedModuleId.HasValue)
        {
            PropertyHintText.Text = "Selecione uma parede, abertura ou m?dulo para editar.";
            return;
        }

        if (!PropertyPanelInput.TryReadWallDimensions(
                PropertyLengthBox.Text,
                PropertyHeightBox.Text,
                PropertyDepthBox.Text,
                out float lengthValue,
                out float heightValue,
                out float depthValue))
        {
            PropertyHintText.Text = "Preencha todos os campos com n?meros em mm e pressione Enter.";
            return;
        }

        if (_openModuleGroupId != null && !string.IsNullOrEmpty(_selectedPartLabel))
        {
            ApplyPartPropertiesFromPanel(lengthValue, heightValue, depthValue);
            return;
        }

        if (_selectedModuleId.HasValue)
        {
            ApplyModulePropertiesFromPanel(lengthValue, heightValue, depthValue);
            return;
        }

        if (_selectedOpeningId.HasValue)
        {
            ApplyOpeningPropertiesFromPanel(lengthValue, heightValue, depthValue);
            return;
        }

        if (_selectedWallId.HasValue)
            ApplyWallPropertiesFromPanel(lengthValue, heightValue, depthValue);
    }

    private void ApplyWallPropertiesFromPanel(float length, float height, float thickness)
    {
        if (!_selectedWallId.HasValue || _wallGroupSelected)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        if (length <= 0f || height <= 0f || thickness <= 0f)
        {
            PropertyHintText.Text = "Comprimento, altura e espessura devem ser maiores que zero.";
            return;
        }

        wall.Height = height;
        wall.Thickness = thickness;

        WallInnerFaceService.ApplyReferenceLengthToWall(wall, _project.Room.Walls, length);

        _project.Room.RebuildAutomaticFloor();
        MarkProjectDirty();

        float referenceLength = WallInnerFaceService.GetDisplayReferenceLength(wall, _project.Room.Walls);
        string orientacao = FormatMeasureSideLabel(wall.MeasureSide);
        string hint = referenceLength >= WallOpeningPlacement.MinWallLengthForStandardDoor
            ? $"Dimens?es aplicadas (Orienta??o {orientacao}). OK para porta padr?o (800 mm)."
            : $"Dimens?es aplicadas (Orienta??o {orientacao}). Para porta padr?o use comprimento = {WallOpeningPlacement.MinWallLengthForStandardDoor:0} mm.";

        UpdateSelectedWallStatus(wall, hint);
        UpdateStatusBarSelection("Parede", referenceLength);
    }

    private void ApplyOpeningPropertiesFromPanel(float width, float height, float thirdValue)
    {
        if (!_selectedOpeningId.HasValue)
            return;

        var found = FindOpeningById(_selectedOpeningId.Value);

        if (found == null)
            return;

        var (wall, opening) = found.Value;
        float prevWidth = opening.Width;
        float prevHeight = opening.Height;
        float prevThird = opening.Type == OpeningType.Window ? opening.SillHeight : opening.DistanceFromStart;
        float prevStart = opening.DistanceFromStart;

        opening.Width = width;
        opening.Height = height;

        if (opening.Type == OpeningType.Window)
            opening.SillHeight = thirdValue;
        else
            opening.DistanceFromStart = WallOpeningPlacement.SnapDistance(
                WallOpeningPlacement.ClampStart(thirdValue, opening.Width, wall.Length));

        if (!WallOpeningPlacement.CanPlace(wall, opening))
        {
            opening.Width = prevWidth;
            opening.Height = prevHeight;

            if (opening.Type == OpeningType.Window)
                opening.SillHeight = prevThird;
            else
                opening.DistanceFromStart = prevStart;

            PropertyHintText.Text =
                $"N?o cabe na parede ({wall.Length:0} mm). " +
                $"M?nimo parede: {width + WallOpeningPlacement.MinEdgeMargin * 2f:0} mm.";
            UpdateOpeningPropertyPanel(opening);
            return;
        }

        UpdateSelectedOpeningStatus(wall, opening);
        MarkProjectDirty();
    }

    private void UpdateStatusBarClosedRoom()
    {
        RefreshCollisionState();
        ClearStatusBarOverrides();
        ApplyStatusBar();
    }

    private void UpdateStatusBarSelection(string kind, float primaryMm, string? detailName = null)
    {
        RefreshCollisionState();
        _statusBarContextOverride = StatusBarPresenter.FormatSelection(kind, primaryMm, detailName);
        _statusBarHint = null;
        ApplyStatusBar();
    }

    private void ClearPropertyPanelSelection()
    {
        _syncingPropertyPanel = true;

        // Painel de m?dulos
        PropertyLengthBox.Text = "";
        PropertyHeightBox.Text = "";
        PropertyDepthBox.Text = "";
        PropertyCornerMeasuresPanel.Visibility = Visibility.Collapsed;
        PropertyCornerMeasureABox.Text = "";
        PropertyCornerMeasureBBox.Text = "";
        PropertyMaterialCombo.SelectedItem = null;
        PropertyMaterialHintText.Text = "Dispon?vel ao selecionar um m?dulo.";
        ModuleCotasExpander.Visibility = Visibility.Collapsed;
        ModuleCotaAnteriorBox.Text = "";
        ModuleCotaPosteriorBox.Text = "";
        ModuleCotaInferiorBox.Text = "";
        ModuleCotaSuperiorBox.Text = "";

        // Painel de paredes
        WallLengthBox.Text = "";
        WallThicknessBox.Text = "";
        WallHeightStartBox.Text = "";
        WallHeightEndBox.Text = "";
        WallAngleAbsoluteBox.Text = "";
        WallAngleRelativeBox.Text = "";
        WallFloorOffsetBox.Text = "";
        WallCotaAnteriorBox.Text = "";
        WallCotaPosteriorBox.Text = "";
        WallCotaInferiorBox.Text = "";
        WallCotaSuperiorBox.Text = "";
        WallDrawBottomFaceCheck.IsChecked = false;
        WallIsMovableCheck.IsChecked = false;
        WallIsVisibleCheck.IsChecked = true;

        _syncingPropertyPanel = false;

        _floorSelected = false;
        _selectedFloorZoneId = null;
        HideWallConstructionPanel();
        WallPropertiesPanel.Visibility = Visibility.Collapsed;
        FloorPropertiesPanel.Visibility = Visibility.Collapsed;
        ModulePropertiesPanel.Visibility = Visibility.Visible;

        ResetPropertyPanelLabels();
        ClearStatusBarOverrides();
        ApplyStatusBar();
    }

    private void ModuleCotaBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (sender is not System.Windows.Controls.TextBox box)
            return;

        ApplyModuleCotaFromPanel(box);
        e.Handled = true;
    }

    private void WallButton_Click(object sender, RoutedEventArgs e)
    {
        CancelOpeningInsertMode();
        CancelModuleInsertMode();
        BeginWallDrawing();
    }

    private void BeginWallDrawing()
    {
        _wallMode = true;
        _hasLastPoint = false;
        _hasPreview = false;
        _wallGroupSelected = false;
        _selectedWallId = null;

        _wallDraft.Reset();
        _wallDraft.Thickness = DefaultWallThickness;
        _wallDraft.Height = DefaultWallHeight;

        _wallAppendMode = _project.Room.Walls.Count > 0 && !_isDefaultRoom;
        _isDefaultRoom = false;
        ClearWallReferenceState();
        _wallReferencePending = _wallAppendMode;

        if (!_wallAppendMode && !_wallEditorActive)
        {
            _project.Room.Clear();
            _camera.Target = new Vector3(0, 0, 0);
        }
        else
        {
            FrameCameraOnRoom();
        }

        ClearPropertyPanelSelection();
        MarkProjectDirty();

        MeasureBox.Visibility = Visibility.Collapsed;

        Keyboard.Focus(this);

        ShowWallConstructionPanel();
        UpdateWallModeTitle();
    }

    private void WallEditorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_wallEditorActive)
            ExitWallEditorMode();
        else
            EnterWallEditorMode();
    }

    private void EnterWallEditorMode()
    {
        CancelOpeningInsertMode();
        CancelModuleInsertMode();
        ClearSelection();

        if (!_wallEditorActive)
            _viewModeBeforeWallEditor = _camera.ViewMode;

        _wallEditorActive = true;
        _camera.ViewMode = CameraViewMode.Top;
        _camera.XRayEnabled = false;
        FrameCameraOnRoom();
        UpdateXRayButton();
        UpdateWallEditorButton();
        WallEditorToolsPanel.Visibility = Visibility.Visible;

        BeginWallDrawing();
        UpdateWallEditorStatus();
        Keyboard.Focus(this);
    }

    private void ExitWallEditorMode()
    {
        if (!_wallEditorActive)
            return;

        _wallEditorActive = false;
        ResetWallEditorDimensionTool();

        if (_wallFlechaHotpointMode || _wallFlechaDragging)
            CancelWallFlechaHotpointMode();

        if (_wallJunctionMode)
            CancelWallJunctionMode();

        if (_wallMode)
            CancelWallMode();

        ClearSelection();
        HideWallConstructionPanel();
        UpdateWallEditorButton();
        WallEditorToolsPanel.Visibility = Visibility.Collapsed;

        ApplyViewMode(_viewModeBeforeWallEditor);
        UpdateWallEditorStatus();
    }

    private void UpdateWallEditorButton()
    {
        WallEditorButton.FontWeight = _wallEditorActive ? FontWeights.Bold : FontWeights.Normal;
    }

    private void WallEditorLinearDimButton_Click(object sender, RoutedEventArgs e) =>
        ToggleWallEditorDimensionTool(WallEditorDimensionTool.Linear);

    private void WallEditorAngularDimButton_Click(object sender, RoutedEventArgs e) =>
        ToggleWallEditorDimensionTool(WallEditorDimensionTool.Angular);

    private void WallEditorRemoveDimButton_Click(object sender, RoutedEventArgs e) =>
        RemoveSelectedManualDimension();

    private void ToggleWallEditorDimensionTool(WallEditorDimensionTool tool)
    {
        if (!_wallEditorActive)
            return;

        PauseWallDrawingForDimensionTool();

        if (_wallEditorDimensionTool == tool)
            ResetWallEditorDimensionTool();
        else
        {
            _wallEditorDimensionTool = tool;
            ResetManualDimPlacement();
        }

        UpdateWallEditorDimensionButtons();
        UpdateWallEditorDimensionTitle();
        Keyboard.Focus(this);
    }

    private void ResetWallEditorDimensionTool()
    {
        _wallEditorDimensionTool = WallEditorDimensionTool.None;
        ResetManualDimPlacement();
        UpdateWallEditorDimensionButtons();
    }

    private void ResetManualDimPlacement()
    {
        _manualDimStep = 0;
        _manualDimPointA = Vector2.Zero;
        _manualDimPointB = Vector2.Zero;
        _manualDimPreview = Vector2.Zero;
    }

    private void PauseWallDrawingForDimensionTool()
    {
        if (!_wallMode)
            return;

        _wallMode = false;
        _wallAppendMode = false;
        _hasLastPoint = false;
        _hasPreview = false;
        ClearWallReferenceState();
        MeasureBox.Visibility = Visibility.Collapsed;
        HideWallConstructionPanel();
    }

    private void UpdateWallEditorDimensionButtons()
    {
        WallEditorLinearDimButton.FontWeight =
            _wallEditorDimensionTool == WallEditorDimensionTool.Linear ? FontWeights.Bold : FontWeights.Normal;
        WallEditorAngularDimButton.FontWeight =
            _wallEditorDimensionTool == WallEditorDimensionTool.Angular ? FontWeights.Bold : FontWeights.Normal;
        WallEditorHotpointButton.FontWeight =
            _wallFlechaHotpointMode ? FontWeights.Bold : FontWeights.Normal;
        WallCornerJoinButton.FontWeight =
            _wallJunctionMode && _wallJunctionKind == WallJunctionKind.Corner ? FontWeights.Bold : FontWeights.Normal;
        WallTJoinButton.FontWeight =
            _wallJunctionMode && _wallJunctionKind == WallJunctionKind.T ? FontWeights.Bold : FontWeights.Normal;
    }

    private void WallCornerJoinButton_Click(object sender, RoutedEventArgs e) =>
        ToggleWallJunctionMode(WallJunctionKind.Corner);

    private void WallTJoinButton_Click(object sender, RoutedEventArgs e) =>
        ToggleWallJunctionMode(WallJunctionKind.T);

    private void ToggleWallJunctionMode(WallJunctionKind kind)
    {
        if (!_wallEditorActive)
            return;

        if (_wallJunctionMode && _wallJunctionKind == kind)
        {
            CancelWallJunctionMode();
            return;
        }

        CancelWallFlechaHotpointMode();
        ResetWallEditorDimensionTool();

        if (!_wallMode)
            BeginWallDrawing();

        PauseWallDrawingForJunction();
        _wallJunctionMode = true;
        _wallJunctionKind = kind;
        _wallJunctionStep = 0;
        _wallJunctionFirstWallId = Guid.Empty;
        UpdateWallJunctionButtons();

        string label = kind == WallJunctionKind.Corner ? "Encontro Canto" : "Encontro T";
        Title = $"Traços 3D - {WallEditorService.ModeLabel} | {label}: clique a parede que desloca | Esc cancela";
        Keyboard.Focus(this);
    }

    private void PauseWallDrawingForJunction()
    {
        if (!_wallMode)
            return;

        _wallMode = false;
        _wallAppendMode = false;
        _hasLastPoint = false;
        _hasPreview = false;
        ClearWallReferenceState();
        MeasureBox.Visibility = Visibility.Collapsed;
    }

    private void CancelWallJunctionMode()
    {
        if (!_wallJunctionMode)
            return;

        _wallJunctionMode = false;
        _wallJunctionStep = 0;
        _wallJunctionFirstWallId = Guid.Empty;
        UpdateWallJunctionButtons();

        if (_wallEditorActive)
        {
            BeginWallDrawing();
            UpdateWallEditorStatus();
        }
        else
            UpdateViewTitle();
    }

    private void UpdateWallJunctionButtons()
    {
        WallCornerJoinButton.FontWeight =
            _wallJunctionMode && _wallJunctionKind == WallJunctionKind.Corner ? FontWeights.Bold : FontWeights.Normal;
        WallTJoinButton.FontWeight =
            _wallJunctionMode && _wallJunctionKind == WallJunctionKind.T ? FontWeights.Bold : FontWeights.Normal;
    }

    private void HandleWallJunctionClick(double mouseX, double mouseY)
    {
        if (!TryPickWallAtScreen(mouseX, mouseY, out var pickWall, out _, out bool hitTop) || hitTop)
            return;

        if (_wallJunctionStep == 0)
        {
            _wallJunctionFirstWallId = pickWall.Id;
            _wallJunctionStep = 1;
            string label = _wallJunctionKind == WallJunctionKind.Corner ? "Encontro Canto" : "Encontro T";
            Title = $"Traços 3D - {WallEditorService.ModeLabel} | {label}: clique a segunda parede | Esc cancela";
            return;
        }

        var firstWall = FindWallById(_wallJunctionFirstWallId);

        if (firstWall == null ||
            !WallJunctionService.TryPickSecondWall(firstWall, pickWall, _wallJunctionKind))
        {
            Title = "Traços 3D - Segunda parede inválida para este encontro | Esc cancela";
            return;
        }

        bool ok = _wallJunctionKind == WallJunctionKind.Corner
            ? WallJunctionService.TryApplyCornerJoin(firstWall, pickWall, out _)
            : WallJunctionService.TryApplyTJoin(firstWall, pickWall);

        if (!ok)
        {
            Title = "Traços 3D - Encontro inválido (parede muito curta) | Esc cancela";
            return;
        }

        _project.Room.RebuildAutomaticFloor();
        MarkProjectDirty();
        SelectWall(firstWall);
        CancelWallJunctionMode();
    }

    private void WallEditorHotpointButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_wallEditorActive)
            return;

        if (_wallFlechaHotpointMode)
            CancelWallFlechaHotpointMode();
        else
            EnterWallFlechaHotpointMode();
    }

    private void EnterWallFlechaHotpointMode()
    {
        ResetWallEditorDimensionTool();
        PauseWallDrawingForDimensionTool();
        _wallFlechaHotpointMode = true;
        _wallFlechaDragging = false;
        _wallFlechaDragWallId = Guid.Empty;
        UpdateWallEditorDimensionButtons();
        Title = $"Traços 3D - {WallEditorService.ModeLabel} | Mover HotPoint: arraste o ponto verde | Esc cancela";
        Keyboard.Focus(this);
    }

    private void CancelWallFlechaHotpointMode()
    {
        if (!_wallFlechaHotpointMode && !_wallFlechaDragging)
            return;

        _wallFlechaHotpointMode = false;
        _wallFlechaDragging = false;
        _wallFlechaDragWallId = Guid.Empty;
        UpdateWallEditorDimensionButtons();

        if (_wallEditorActive)
        {
            BeginWallDrawing();
            UpdateWallEditorStatus();
        }
        else
            UpdateViewTitle();
    }

    private void BeginWallFlechaDrag(WallSegment wall)
    {
        _wallFlechaDragging = true;
        _wallFlechaDragWallId = wall.Id;
        _wallFlechaHotpointMode = true;
    }

    private void UpdateWallFlechaFromCursor(Vector2 floorPoint)
    {
        if (!_wallFlechaDragging || _wallFlechaDragWallId == Guid.Empty)
            return;

        var wall = FindWallById(_wallFlechaDragWallId);

        if (wall == null)
            return;

        var arc = WallArcGeometry.FromWall(wall);
        wall.FlechaMm = arc.SignedFlechaFromPoint(floorPoint);

        if (_selectedWallId == wall.Id)
        {
            WallFlechaBox.Text = wall.FlechaMm.ToString("0", CultureInfo.InvariantCulture);
            WallArcAngleBox.Text = WallArcGeometry.FromWall(wall).GetArcAngleDegrees().ToString("0.0", CultureInfo.InvariantCulture);
        }
    }

    private void CommitWallFlechaDrag()
    {
        if (!_wallFlechaDragging)
            return;

        _wallFlechaDragging = false;
        _project.Room.RebuildAutomaticFloor();
        MarkProjectDirty();

        if (_wallFlechaDragWallId != Guid.Empty)
        {
            var wall = FindWallById(_wallFlechaDragWallId);

            if (wall != null && _selectedWallId == wall.Id)
                UpdateSelectedWallStatus(wall, $"Flecha {wall.FlechaMm:0} mm aplicada.");
        }
    }

    private bool TryBeginWallFlechaDragAtScreen(double mouseX, double mouseY)
    {
        if (!TryPickWallAtScreen(mouseX, mouseY, out var wall, out _, out bool hitTop) || hitTop)
            return false;

        SelectWall(wall);

        var arc = WallArcGeometry.FromWall(wall);
        Vector2 floorPoint = ScreenToFloor(mouseX, mouseY);
        Vector2 hotpoint = arc.IsStraight ? arc.Midpoint : arc.BulgePoint;

        if ((floorPoint - hotpoint).Length > 350f)
            return false;

        BeginWallFlechaDrag(wall);
        UpdateWallFlechaFromCursor(floorPoint);
        return true;
    }

    private void UpdateWallEditorDimensionTitle()
    {
        if (!_wallEditorActive || _wallEditorDimensionTool == WallEditorDimensionTool.None)
            return;

        if (_wallEditorDimensionTool == WallEditorDimensionTool.Linear)
        {
            Title = _manualDimStep switch
            {
                0 => $"Tra?os 3D - {WallEditorService.ModeLabel} | Cota reta: 1? ponto | Esc cancela",
                _ => $"Tra?os 3D - {WallEditorService.ModeLabel} | Cota reta: 2? ponto | Esc cancela"
            };
            return;
        }

        Title = _manualDimStep switch
        {
            0 => $"Tra?os 3D - {WallEditorService.ModeLabel} | Cota angular: 1? ponto | Esc cancela",
            1 => $"Tra?os 3D - {WallEditorService.ModeLabel} | Cota angular: v?rtice | Esc cancela",
            _ => $"Tra?os 3D - {WallEditorService.ModeLabel} | Cota angular: 3? ponto | Esc cancela"
        };
    }

    private void HandleManualDimensionClick(Vector2 rawPoint)
    {
        Vector2 point = Snap(rawPoint, 100);
        point = WallManualDimensionService.SnapPoint(point, _project.Room.Walls);

        if (_wallEditorDimensionTool == WallEditorDimensionTool.Linear)
        {
            if (_manualDimStep == 0)
            {
                _manualDimPointA = point;
                _manualDimStep = 1;
                _manualDimPreview = point;
                UpdateWallEditorDimensionTitle();
                return;
            }

            var dim = WallManualDimensionService.TryCreateLinear(_manualDimPointA, point, point);

            if (dim != null)
            {
                _project.ManualWallDimensions.Add(dim);
                MarkProjectDirty();
                SelectManualDimension(dim.Id);
            }

            ResetManualDimPlacement();
            UpdateWallEditorDimensionTitle();
            return;
        }

        if (_wallEditorDimensionTool == WallEditorDimensionTool.Angular)
        {
            if (_manualDimStep == 0)
            {
                _manualDimPointA = point;
                _manualDimStep = 1;
                _manualDimPreview = point;
                UpdateWallEditorDimensionTitle();
                return;
            }

            if (_manualDimStep == 1)
            {
                _manualDimPointB = point;
                _manualDimStep = 2;
                _manualDimPreview = point;
                UpdateWallEditorDimensionTitle();
                return;
            }

            var dim = WallManualDimensionService.TryCreateAngular(
                _manualDimPointA,
                _manualDimPointB,
                point,
                point);

            if (dim != null)
            {
                _project.ManualWallDimensions.Add(dim);
                MarkProjectDirty();
                SelectManualDimension(dim.Id);
            }

            ResetManualDimPlacement();
            UpdateWallEditorDimensionTitle();
        }
    }

    private void SelectManualDimension(Guid dimId)
    {
        _selectedManualDimId = dimId;
        _selectedWallId = null;
        _selectedOpeningId = null;
        _selectedModuleId = null;
        _selectedModuleIds.Clear();
        _wallGroupSelected = false;
        _floorSelected = false;
        _selectedFloorZoneId = null;
        HideWallConstructionPanel();
        WallPropertiesPanel.Visibility = Visibility.Collapsed;
        FloorPropertiesPanel.Visibility = Visibility.Collapsed;
        ModulePropertiesPanel.Visibility = Visibility.Collapsed;

        var dim = _project.ManualWallDimensions.FirstOrDefault(d => d.Id == dimId);
        string label = dim != null ? WallManualDimensionService.FormatLabel(dim) : "";
        Title = $"Tra?os 3D - {WallEditorService.ModeLabel} | Cota manual: {label} | Delete remove";
        SetStatusBarOverrides(context: $"Cota manual: {label}");
        SyncDimensionConfiguratorSelectionState();
    }

    private void RemoveSelectedManualDimension()
    {
        if (!_selectedManualDimId.HasValue)
            return;

        var dim = _project.ManualWallDimensions.FirstOrDefault(d => d.Id == _selectedManualDimId.Value);

        if (dim == null)
            return;

        _project.ManualWallDimensions.Remove(dim);
        _selectedManualDimId = null;
        MarkProjectDirty();
        UpdateWallEditorStatus();
    }

    private void UpdateWallEditorStatus()
    {
        if (_wallEditorActive && !_wallMode)
            Title = $"Tra?os 3D - {WallEditorService.ModeLabel} | Esc fecha o editor";

        RefreshStatusBarAfterViewChange();
    }

    private string WallEditorTitlePrefix() =>
        _wallEditorActive ? $"{WallEditorService.ModeLabel} | " : "";

    private void DoorButton_Click(object sender, RoutedEventArgs e)
    {
        ExitWallEditorMode();
        CancelWallMode();
        CancelModuleInsertMode();
        ClearSelection();
        _openingInsertMode = OpeningInsertMode.Door;
        MeasureBox.Visibility = Visibility.Collapsed;
        Keyboard.Focus(this);
        Title = "Tra?os 3D - Porta | Clique na parede | Esc cancela";
    }

    private void WindowButton_Click(object sender, RoutedEventArgs e)
    {
        ExitWallEditorMode();
        CancelWallMode();
        CancelModuleInsertMode();
        ClearSelection();
        _openingInsertMode = OpeningInsertMode.Window;
        MeasureBox.Visibility = Visibility.Collapsed;
        Keyboard.Focus(this);
        Title = "Tra?os 3D - Janela | Clique na parede | Esc cancela";
    }

    private void PerspectiveButton_Click(object sender, RoutedEventArgs e) =>
        ApplyViewMode(CameraViewMode.Perspective);

    private void TopViewButton_Click(object sender, RoutedEventArgs e) =>
        ApplyViewMode(CameraViewMode.Top);

    private void FrontViewButton_Click(object sender, RoutedEventArgs e) =>
        ApplyViewMode(CameraViewMode.Front);

    private void LeftViewButton_Click(object sender, RoutedEventArgs e) =>
        ApplyViewMode(CameraViewMode.Left);

    private void RightViewButton_Click(object sender, RoutedEventArgs e) =>
        ApplyViewMode(CameraViewMode.Right);

    private void XRayButton_Click(object sender, RoutedEventArgs e)
    {
        if (_camera.ViewMode != CameraViewMode.Perspective)
            return;

        _camera.XRayEnabled = !_camera.XRayEnabled;
        UpdateXRayButton();
        UpdateViewTitle();
        RefreshStatusBarAfterViewChange();
    }

    private void UpdateXRayButton()
    {
        bool active = _camera.XRayEnabled && _camera.ViewMode == CameraViewMode.Perspective;
        XRayButton.Content = active ? "Raio X: ON" : "Raio X";
        XRayButton.FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
    }

    private void ApplyViewMode(CameraViewMode mode)
    {
        if (!WallEditorService.CanSwitchToView(mode, _wallEditorActive))
        {
            SetStatusBarOverrides(hint: "Esc fecha o Editor de Paredes");
            return;
        }

        CancelOpeningInsertMode();
        CancelModuleInsertMode();

        if (_wallMode && mode != CameraViewMode.Top)
            CancelWallMode();

        _camera.ViewMode = mode;
        if (mode != CameraViewMode.Perspective)
            _camera.XRayEnabled = false;

        FrameCameraOnRoom();
        UpdateXRayButton();
        UpdateViewTitle();
        RefreshStatusBarAfterViewChange();
        Keyboard.Focus(this);
    }

    private void RefreshStatusBarAfterViewChange()
    {
        ClearStatusBarOverrides();

        if (_selectedOpeningId.HasValue)
        {
            var found = FindOpeningById(_selectedOpeningId.Value);

            if (found.HasValue)
            {
                UpdateSelectedOpeningStatus(found.Value.Wall, found.Value.Opening);
                return;
            }
        }

        if (_selectedModuleId.HasValue)
        {
            var module = _project.FindModule(_selectedModuleId.Value);

            if (module != null)
            {
                UpdateStatusBarSelection(
                    "Módulo",
                    module.Width,
                    ModuleCatalog.GetRequired(module.DefinitionId).DisplayName);
                return;
            }
        }

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
            {
                UpdateStatusBarSelection("Parede",
                    WallInnerFaceService.GetDisplayReferenceLength(wall, _project.Room.Walls));
                return;
            }
        }

        if (_project.Room.IsClosed)
        {
            UpdateStatusBarClosedRoom();
            return;
        }

        UpdateStatusBarViewContext();
    }

    private void FrameCameraOnRoom() =>
        _camera.FrameOnRoom(_project.Room.Walls);

    private void FrameCameraOnModule(ModuleInstance module)
    {
        var (min, max) = module.GetBounds();
        _camera.FrameOnBounds(min, max);
        Viewport.InvalidateVisual();
    }

    private void UpdateViewTitle()
    {
        if (_wallMode || _openingInsertMode != OpeningInsertMode.None || _moduleInsertDefinitionId != null)
            return;

        if (_selectedWallId.HasValue || _selectedOpeningId.HasValue || _selectedModuleId.HasValue)
            return;

        if (_wallEditorActive)
        {
            Title = $"Tra?os 3D - {WallEditorService.ModeLabel} | Esc fecha o editor";
            return;
        }

        Title = $"Tra?os 3D - Vista: {CameraController.GetViewLabel(_camera.ViewMode, _camera.XRayEnabled)}";
    }

    private void ClearStatusBarOverrides()
    {
        _statusBarContextOverride = null;
        _statusBarHint = null;
    }

    private void SetStatusBarOverrides(string? context = null, string? hint = null)
    {
        if (context != null)
            _statusBarContextOverride = context;

        if (hint != null)
            _statusBarHint = hint;

        ApplyStatusBar();
    }

    private void ApplyStatusBar()
    {
        var input = new StatusBarInput
        {
            ProfileName = ConstructionProfiles.GetDisplayName(_project.Metadata.ConstructionProfileId),
            ProjectName = ProjectDisplayName,
            IsProjectDirty = _isProjectDirty,
            BuildLabel = AppVersion.DisplayBuildLabel,
            ViewLabel = WallEditorService.GetViewLabel(_wallEditorActive, _camera.ViewMode, _camera.XRayEnabled),
            ModuleCount = _project.Modules.Count,
            WallCount = _project.Room.Walls.Count,
            RoomClosed = _project.Room.IsClosed,
            ActiveMaterialName = WallSurfaceMaterialCatalog.GetDisplayName(MaterialApplicationService.ActiveMaterialId),
            ApplicationMode = MaterialApplicationService.ApplicationMode,
            CollisionEnabled = _collisionEnabled,
            CollidingModuleCount = _collidingModuleIds.Count,
            ContextOverride = _statusBarContextOverride,
            HintOverride = _statusBarHint
        };

        var presentation = StatusBarPresenter.Build(input);
        StatusBarProjectText.Text = presentation.ProjectInfo;
        StatusBarViewContextText.Text = presentation.ViewContext;

        bool hasMaterial = !string.IsNullOrWhiteSpace(presentation.MaterialInfo);
        StatusBarMaterialText.Text = presentation.MaterialInfo;
        StatusBarMaterialText.Visibility = hasMaterial ? Visibility.Visible : Visibility.Collapsed;

        bool hasHint = !string.IsNullOrWhiteSpace(presentation.Hint);
        StatusBarHintText.Text = presentation.Hint;
        StatusBarHintText.Visibility = hasHint ? Visibility.Visible : Visibility.Collapsed;
        StatusBarHintSeparator.Visibility = hasHint ? Visibility.Visible : Visibility.Collapsed;

        StatusBarStatusText.Text = presentation.Status;
        StatusBarText.Text = presentation.FullText;
    }

    private void UpdateCollisionToggleButton()
    {
        CollisionToggleButton.Content = _collisionEnabled ? "Colis?o: ON" : "Colis?o: OFF";
    }

    private void RefreshCollisionState()
    {
        _collidingModuleIds = _collisionEnabled
            ? ModuleCollisionService.FindCollidingModuleIds(_project.Modules)
            : new HashSet<Guid>();
    }

    private void CollisionToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _collisionEnabled = !_collisionEnabled;
        UpdateCollisionToggleButton();
        RefreshCollisionState();
        RefreshStatusBarAfterViewChange();
        Keyboard.Focus(this);
    }

    private bool WouldPreviewCollide()
    {
        if (!_collisionEnabled || !_hasModulePreview || _moduleInsertDefinitionId == null || IsModuleCollisionBypassActive())
            return false;

        var (width, height, depth) = GetActiveModuleInsertionDimensions();

        return ModuleCollisionService.WouldCollide(
            _previewModulePosition,
            width,
            height,
            depth,
            _previewModuleRotationY,
            _project.Modules,
            candidateWallId: _previewModuleWallId,
            distanceAlongWall: _previewModuleDistanceAlong,
            candidateDefinition: ModuleCatalog.GetRequired(_moduleInsertDefinitionId!),
            dimensionSettings: DimensionConfiguratorService.GetSettings(_project));
    }

    private void UpdateStatusBarViewContext()
    {
        if (_selectedWallId.HasValue || _selectedOpeningId.HasValue || _project.Room.IsClosed)
            return;

        ClearStatusBarOverrides();
        ApplyStatusBar();
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
        {
            SaveProjectInternal(false);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.W)
        {
            TryCloseProjectTab(_projectTabs.ActiveIndex);
            e.Handled = true;
            return;
        }

        // Promob: Ctrl+O reexibe todos os módulos e peças ocultados no ambiente.
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O)
        {
            RevealAllHiddenSceneItems();
            e.Handled = true;
            return;
        }

        // Promob: O oculta a peça aberta/selecionada; sem peça aberta, oculta o módulo.
        // Não interceptar a letra quando o usuário estiver digitando em um campo.
        if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.O && !IsTextEntryFocused())
        {
            if (HideSelectedSceneItem())
                e.Handled = true;
            return;
        }

        // Promob: I espelha o(s) módulo(s) selecionado(s). A engenharia é a
        // mesma instância paramétrica; não são necessários SKUs Esq./Dir.
        if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.I && !IsTextEntryFocused())
        {
            if (MirrorSelectedModules())
                e.Handled = true;
            return;
        }

        // Promob: Ctrl+T → selecionar todos os módulos.
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.T)
        {
            if (Keyboard.FocusedElement is not System.Windows.Controls.TextBox &&
                Keyboard.FocusedElement is not System.Windows.Controls.ComboBox &&
                Keyboard.FocusedElement is not System.Windows.Controls.ComboBoxItem)
            {
                SelectAllModules();
                e.Handled = true;
                return;
            }
        }

        if (e.Key == Key.Escape)
        {
            if (CancelModuleMarqueeIfActive())
            {
                e.Handled = true;
                return;
            }

            if (CancelModuleWallDragIfActive())
            {
                e.Handled = true;
                return;
            }

            if (CancelWallMoveIfActive())
            {
                e.Handled = true;
                return;
            }

            if (_materialCopyMode)
            {
                CancelMaterialCopyMode();
                e.Handled = true;
                return;
            }

            // Peça com seta selecionada: 1º Esc solta a seta (mantém a peça).
            if (_openModuleGroupId != null && _selectedPartHandle != null)
            {
                _selectedPartHandle = null;
                HighlightActivePartDeltaBox(null);
                Viewport.InvalidateVisual();
                SetStatusBarOverrides(hint: "Seta desmarcada.");
                e.Handled = true;
                return;
            }

            // Grupo de módulo aberto: Esc fecha a edição por peça (volta ao módulo inteiro).
            if (_openModuleGroupId != null)
            {
                if (!string.IsNullOrEmpty(_selectedPartLabel) &&
                    !DrawerPartNaming.IsAssemblySelection(_selectedPartLabel) &&
                    DrawerPartNaming.TryGetAssembly(_selectedPartLabel, out string drawerAssembly))
                {
                    _selectedPartLabel = drawerAssembly;
                    _selectedPartHandle = null;
                    var drawerModule = _project.FindModule(_openModuleGroupId.Value);
                    if (drawerModule != null)
                        UpdatePartSelectionStatus(drawerModule);
                    Viewport.InvalidateVisual();
                    SetStatusBarOverrides(hint: $"{drawerAssembly} selecionada — dois cliques entram nas peças.");
                    e.Handled = true;
                    return;
                }

                _openModuleGroupId = null;
                _selectedPartLabel = null;
                _selectedPartHandle = null;
                SetStatusBarOverrides(hint: "Grupo fechado.");
                e.Handled = true;
                return;
            }

            if (_floorZoneDrawMode)
            {
                CancelFloorZoneDrawMode();
                e.Handled = true;
                return;
            }

            if (_floorCircleRegionPickMode)
            {
                CancelFloorCircleRegionPickMode();
                e.Handled = true;
                return;
            }

            if (_floorPolygonRegionPickMode)
            {
                CancelFloorPolygonRegionPickMode();
                e.Handled = true;
                return;
            }

            if (_floorZoneDragging)
            {
                CancelFloorZoneDrag();
                e.Handled = true;
                return;
            }

            if (_wallEditorDimensionTool != WallEditorDimensionTool.None)
            {
                if (_manualDimStep > 0)
                    ResetManualDimPlacement();
                else
                    ResetWallEditorDimensionTool();

                UpdateWallEditorDimensionTitle();
                UpdateWallEditorStatus();
                e.Handled = true;
                return;
            }

            if (_wallSegmentPickMode)
            {
                CancelWallSegmentPickMode();
                e.Handled = true;
                return;
            }

            if (_wallHorizontalBandPickMode)
            {
                CancelWallHorizontalBandPickMode();
                e.Handled = true;
                return;
            }

            if (_wallVerticalBandPickMode)
            {
                CancelWallVerticalBandPickMode();
                e.Handled = true;
                return;
            }

            if (_wallRegionPickMode)
            {
                CancelWallRegionPickMode();
                e.Handled = true;
                return;
            }

            if (_wallCircleRegionPickMode)
            {
                CancelWallCircleRegionPickMode();
                e.Handled = true;
                return;
            }

            if (_wallPolygonRegionPickMode)
            {
                CancelWallPolygonRegionPickMode();
                e.Handled = true;
                return;
            }

            if (_wallPolygonVertexInsertMode)
            {
                CancelWallPolygonVertexInsertMode();
                e.Handled = true;
                return;
            }

            if (_wallRegionVerticalCutMode)
            {
                CancelWallRegionVerticalCutMode();
                e.Handled = true;
                return;
            }

            if (_wallBandDragging)
            {
                CancelWallBandDrag();
                e.Handled = true;
                return;
            }

            if (_wallRegionDragging)
            {
                CancelWallRegionDrag();
                e.Handled = true;
                return;
            }

            if (_wallRegionBodyDragging)
            {
                CancelWallRegionBodyDrag();
                e.Handled = true;
                return;
            }

            if (_wallRegionRotating)
            {
                CancelWallRegionRotation();
                e.Handled = true;
                return;
            }

            if (_wall304050PickMovingMode)
            {
                CancelWall304050PickMode();
                e.Handled = true;
                return;
            }

            if (_wallChamferMode)
            {
                CancelWallChamferMode();
                e.Handled = true;
                return;
            }

            if (_wallFlechaHotpointMode || _wallFlechaDragging)
            {
                CancelWallFlechaHotpointMode();
                e.Handled = true;
                return;
            }

            if (_wallJunctionMode)
            {
                CancelWallJunctionMode();
                e.Handled = true;
                return;
            }

            if (_wallEditorActive && _wallMode)
            {
                CancelWallMode();
                UpdateWallEditorStatus();
                e.Handled = true;
                return;
            }

            if (_wallEditorActive)
            {
                ExitWallEditorMode();
                e.Handled = true;
                return;
            }

            CancelWallMode();
            CancelOpeningInsertMode();
            CancelModuleInsertMode();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.R)
        {
            if (_selectedModuleId.HasValue && !_wallMode)
            {
                RotateSelectedModule90();
                e.Handled = true;
                return;
            }

            ToggleWallOrientation();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
        {
            UndoLastWall();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete)
        {
            if (_selectedManualDimId.HasValue)
            {
                RemoveSelectedManualDimension();
                e.Handled = true;
                return;
            }

            if (_selectedFloorZoneId.HasValue)
                DeleteSelectedFloorZone();
            else if (_selectedOpeningId.HasValue)
                DeleteSelectedOpening();
            else if (_selectedModuleId.HasValue)
                DeleteSelectedModules();
            else
                DeleteSelectedWall();

            e.Handled = true;
        }
    }

    private static bool IsTextEntryFocused() =>
        Keyboard.FocusedElement is System.Windows.Controls.TextBox or
            System.Windows.Controls.ComboBox or
            System.Windows.Controls.ComboBoxItem;

    private bool MirrorSelectedModules()
    {
        var ids = _selectedModuleIds.Count > 0
            ? _selectedModuleIds.ToList()
            : _selectedModuleId.HasValue
                ? [_selectedModuleId.Value]
                : [];
        var settings = DimensionConfiguratorService.GetSettings(_project);
        int mirrored = 0;

        foreach (Guid id in ids)
        {
            var module = _project.FindModule(id);
            if (module == null || !SceneModuleVisibilityService.IsEditable(module))
                continue;

            module.IsMirrored = !module.IsMirrored;
            module.RebuildMesh(ModuleCatalog.GetRequired(module.DefinitionId), settings);
            mirrored++;
        }

        if (mirrored == 0)
            return false;

        MarkProjectDirty();
        Viewport.InvalidateVisual();
        SetStatusBarOverrides(hint: mirrored == 1
            ? "Módulo espelhado. Pressione I novamente para inverter."
            : $"{mirrored} módulos espelhados.");
        return true;
    }

    private bool HideSelectedSceneItem()
    {
        if (_openModuleGroupId.HasValue && !string.IsNullOrEmpty(_selectedPartLabel))
        {
            var module = _project.FindModule(_openModuleGroupId.Value);
            if (module == null || !SceneOcclusionService.HidePart(module, _selectedPartLabel))
                return false;

            string hiddenLabel = _selectedPartLabel;
            _selectedPartLabel = null;
            _selectedPartHandle = null;
            HighlightActivePartDeltaBox(null);
            MarkProjectDirty();
            UpdateModulePropertyPanel(module, ModuleCatalog.GetRequired(module.DefinitionId));
            Viewport.InvalidateVisual();
            SetStatusBarOverrides(hint: DrawerPartNaming.IsAssemblySelection(hiddenLabel)
                ? $"{hiddenLabel} inteira ocultada. Ctrl+O reexibe todos os ocultos."
                : $"Peça “{hiddenLabel}” ocultada. Ctrl+O reexibe todos os ocultos.");
            return true;
        }

        var ids = _selectedModuleIds.Count > 0
            ? _selectedModuleIds.ToList()
            : _selectedModuleId.HasValue
                ? [_selectedModuleId.Value]
                : [];
        var modules = ids.Select(id => _project.FindModule(id)).OfType<ModuleInstance>().ToList();
        int hidden = SceneOcclusionService.HideModules(modules);
        if (hidden == 0)
            return false;

        MarkProjectDirty();
        ClearSelection();
        RefreshSceneModuleList();
        Viewport.InvalidateVisual();
        SetStatusBarOverrides(hint: hidden == 1
            ? "Módulo ocultado. Ctrl+O reexibe todos os ocultos."
            : $"{hidden} módulos ocultados. Ctrl+O reexibe todos os ocultos.");
        return true;
    }

    private void RevealAllHiddenSceneItems()
    {
        RevealHiddenResult result = SceneOcclusionService.RevealAll(_project);
        if (!result.Changed)
        {
            SetStatusBarOverrides(hint: "Não existem módulos ou peças ocultados.");
            return;
        }

        MarkProjectDirty();
        RefreshSceneModuleList();
        Viewport.InvalidateVisual();
        SetStatusBarOverrides(hint:
            $"Reexibidos: {result.Modules} módulo(s) e {result.Parts} peça(s).");
    }

    private void ToggleWallOrientation()
    {
        if (!_wallMode && _selectedWallId.HasValue)
        {
            if (_wallGroupSelected)
                return;

            var selectedWall = FindWallById(_selectedWallId.Value);

            if (selectedWall != null)
            {
                ApplyMeasureSide(NextMeasureSide(selectedWall.MeasureSide), updateDraft: false, updateSelectedWall: true);
            }

            return;
        }

        if (_wallMode)
            ApplyMeasureSide(NextMeasureSide(_wallDraft.MeasureSide), updateDraft: true, updateSelectedWall: false);
    }

    private void ApplyMeasureSide(WallMeasureSide side, bool updateDraft, bool updateSelectedWall)
    {
        if (updateDraft)
            _wallDraft.MeasureSide = side;

        if (updateSelectedWall && _selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
            {
                wall.MeasureSide = side;
                MarkProjectDirty();
                UpdateSelectedWallStatus(wall);
            }
        }

        SyncWallMeasureSideCombos(side);
        UpdateWallModeTitle();
    }

    private static WallMeasureSide NextMeasureSide(WallMeasureSide side) =>
        side == WallMeasureSide.Interior ? WallMeasureSide.Exterior : WallMeasureSide.Interior;

    private static string FormatMeasureSideLabel(WallMeasureSide side) =>
        side == WallMeasureSide.Interior ? "Interna" : "Externa";

    private void SyncWallMeasureSideCombos(WallMeasureSide side)
    {
        _syncingMeasureSideCombo = true;
        int index = side == WallMeasureSide.Interior ? 0 : 1;

        if (WallConstructionMeasureSideCombo != null)
            WallConstructionMeasureSideCombo.SelectedIndex = index;

        if (WallMeasureSideCombo != null)
            WallMeasureSideCombo.SelectedIndex = index;

        _syncingMeasureSideCombo = false;
    }

    private void WallMeasureSideCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingMeasureSideCombo || !IsLoaded || _wallGroupSelected)
            return;

        if (sender is not System.Windows.Controls.ComboBox combo || combo.SelectedIndex < 0)
            return;

        var side = combo.SelectedIndex == 0 ? WallMeasureSide.Interior : WallMeasureSide.Exterior;
        ApplyMeasureSide(side, updateDraft: _wallMode, updateSelectedWall: !_wallMode && _selectedWallId.HasValue);
    }

    private void WallConstructionBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !_wallMode)
            return;

        if (PropertyPanelInput.TryParseMm(WallConstructionThicknessBox.Text, out float thickness) && thickness > 0)
            _wallDraft.Thickness = thickness;

        if (PropertyPanelInput.TryParseMm(WallConstructionHeightBox.Text, out float height) && height > 0)
            _wallDraft.Height = height;

        MarkProjectDirty();
        e.Handled = true;
    }

    private void ShowWallConstructionPanel()
    {
        WallConstructionPanel.Visibility = Visibility.Visible;
        WallPropertiesPanel.Visibility = Visibility.Collapsed;
        FloorPropertiesPanel.Visibility = Visibility.Collapsed;
        ModulePropertiesPanel.Visibility = Visibility.Collapsed;
        SyncWallConstructionPanelFromDraft();
    }

    private void HideWallConstructionPanel()
    {
        WallConstructionPanel.Visibility = Visibility.Collapsed;
    }

    private void SyncWallConstructionPanelFromDraft()
    {
        if (WallConstructionPanel.Visibility != Visibility.Visible)
            return;

        _syncingMeasureSideCombo = true;

        if (WallConstructionMeasureSideCombo != null)
            WallConstructionMeasureSideCombo.SelectedIndex =
                _wallDraft.MeasureSide == WallMeasureSide.Interior ? 0 : 1;

        _syncingMeasureSideCombo = false;

        WallConstructionThicknessBox.Text = _wallDraft.Thickness.ToString("0", CultureInfo.InvariantCulture);
        WallConstructionHeightBox.Text = _wallDraft.Height.ToString("0", CultureInfo.InvariantCulture);

        float length = TryGetDraftPreviewReferenceLength(out float referenceLength)
            ? referenceLength
            : (_hasPreview ? (_previewPoint - _lastPoint).Length : 0f);

        WallConstructionLengthBox.Text = length > 0.5f
            ? length.ToString("0", CultureInfo.InvariantCulture)
            : "";

        WallConstructionAngleAbsoluteBox.Text = _wallDraft.PreviewAngleDegrees.ToString("0.0", CultureInfo.InvariantCulture);
        WallConstructionAngleRelativeBox.Text = ComputeDraftRelativeAngleDegrees().ToString("0.0", CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(WallChamferDistanceBox.Text))
            WallChamferDistanceBox.Text = WallCornerChamferService.DefaultChamferMm.ToString("0", CultureInfo.InvariantCulture);
    }

    private float ComputeDraftRelativeAngleDegrees()
    {
        if (_wallDraft.Points.Count < 2)
            return _wallDraft.PreviewAngleDegrees;

        Vector2 prevDir = _wallDraft.Points[^1] - _wallDraft.Points[^2];

        if (prevDir.LengthSquared < 1f)
            return 0f;

        float prevAngle = MathHelper.RadiansToDegrees(MathF.Atan2(prevDir.Y, prevDir.X));
        float delta = _wallDraft.PreviewAngleDegrees - prevAngle;

        while (delta > 180f) delta -= 360f;
        while (delta < -180f) delta += 360f;

        return delta;
    }

    private void UpdateWallModeTitle()
    {
        if (!_wallMode)
            return;

        if (_wallReferencePending && !_hasLastPoint)
        {
            if (!_hasWallReferencePick)
            {
                Title = $"Tra?os 3D - {WallEditorTitlePrefix()}Refer?ncia: clique na face interna da parede | Esc cancela";
                return;
            }

            Title = $"Tra?os 3D - {WallEditorTitlePrefix()}Refer?ncia: {Math.Abs(_wallReferenceOffsetPreview):0} mm | Enter confirma | Esc cancela";
            return;
        }

        Title = $"Tra?os 3D - {WallEditorTitlePrefix()}Parede | Orienta??o: {FormatMeasureSideLabel(_wallDraft.MeasureSide)} | R alterna Orienta??o";
    }

    private void CancelOpeningInsertMode()
    {
        _openingInsertMode = OpeningInsertMode.None;
        _hasOpeningPreview = false;
        _previewOpeningWallId = null;

        if (!_wallMode && !_selectedWallId.HasValue && !_selectedOpeningId.HasValue && _moduleInsertDefinitionId == null)
        {
            if (_wallEditorActive)
                Title = $"Tra?os 3D - {WallEditorService.ModeLabel} | Esc fecha o editor";
            else
                Title = "Tra?os 3D";
        }
    }

    private void CancelModuleInsertMode()
    {
        _moduleInsertDefinitionId = null;
        _hasModulePreview = false;
        _moduleLibraryDragPending = false;
        _moduleLibraryPendingDefinitionId = null;
        _moduleLibraryCustomDragging = false;
        _previewModuleCotas = null;

        if (Mouse.Captured == this)
            Mouse.Capture(null);

        if (!_wallMode && !_selectedWallId.HasValue && !_selectedOpeningId.HasValue && _openingInsertMode == OpeningInsertMode.None)
        {
            if (_wallEditorActive)
                Title = $"Tra?os 3D - {WallEditorService.ModeLabel} | Esc fecha o editor";
            else
                Title = "Tra?os 3D";
        }
    }

    private void ClearSelection()
    {
        _wallGroupSelected = false;
        _floorSelected = false;
        _selectedFloorZoneId = null;
        _selectedWallId = null;
        _selectedOpeningId = null;
        _selectedModuleId = null;
        _selectedModuleIds.Clear();
        _selectedManualDimId = null;
        _wall304050MovingWallId = null;
        ClearPropertyPanelSelection();
        ClearSceneModuleListSelection();
        SyncDimensionConfiguratorSelectionState();
    }

    private void RefreshSceneModuleList()
    {
        var idsToRestore = _selectedModuleIds.Count > 0
            ? _selectedModuleIds.ToHashSet()
            : _selectedModuleId.HasValue
                ? new HashSet<Guid> { _selectedModuleId.Value }
                : new HashSet<Guid>();

        var entries = SceneModuleListService.BuildGroupedEntries(
            _project.Modules,
            _project.Room.Walls,
            _project.Room.Compartments);

        _syncingSceneModuleList = true;
        try
        {
            SceneModuleListBox.ItemsSource = entries;
            SceneModuleListBox.SelectedItems.Clear();

            foreach (var item in entries.OfType<SceneModuleListItem>())
            {
                if (idsToRestore.Contains(item.Module.Id))
                    SceneModuleListBox.SelectedItems.Add(item);
            }
        }
        finally
        {
            _syncingSceneModuleList = false;
        }

        UpdateSceneModuleListActionsState();
        SyncSceneModuleRenameBox();
    }

    private int GetSceneModuleListSelectedCount() =>
        SceneModuleListBox.SelectedItems.OfType<SceneModuleListItem>().Count();

    private List<ModuleInstance> GetSelectedSceneModules()
    {
        if (SceneModuleListBox?.SelectedItems == null)
            return [];

        return SceneModuleListBox.SelectedItems.OfType<SceneModuleListItem>()
            .Select(item => item.Module)
            .ToList();
    }

    private void UpdateSceneModuleListActionsState()
    {
        int selectedCount = GetSceneModuleListSelectedCount();
        var modules = GetSelectedSceneModules();
        SceneModuleDeleteButton.IsEnabled =
            SceneModuleSelectionService.CanDelete(selectedCount) &&
            SceneModuleVisibilityService.CanDeleteSelection(modules);
        bool canRename = SceneModuleSelectionService.CanRename(selectedCount);
        SceneModuleRenameBox.IsEnabled = canRename;
        SceneModuleRenameButton.IsEnabled = canRename;
        SyncSceneModuleVisibilityChecks();
    }

    private void SyncSceneModuleVisibilityChecks()
    {
        var modules = GetSelectedSceneModules();
        bool canToggle = SceneModuleVisibilityService.CanToggle(modules);

        _syncingSceneModuleVisibilityChecks = true;
        try
        {
            SceneModuleVisibleCheck.IsEnabled = canToggle;
            SceneModuleLockedCheck.IsEnabled = canToggle;

            if (!canToggle)
            {
                SceneModuleVisibleCheck.IsChecked = true;
                SceneModuleLockedCheck.IsChecked = false;
                return;
            }

            SceneModuleVisibleCheck.IsChecked = SceneModuleVisibilityService.GetVisibleState(modules);
            SceneModuleLockedCheck.IsChecked = SceneModuleVisibilityService.GetLockedState(modules);
        }
        finally
        {
            _syncingSceneModuleVisibilityChecks = false;
        }
    }

    private void SceneModuleVisibleCheck_Changed(object sender, RoutedEventArgs e) =>
        ApplySceneModuleVisibilityFromCheckBox();

    private void SceneModuleLockedCheck_Changed(object sender, RoutedEventArgs e) =>
        ApplySceneModuleLockFromCheckBox();

    private void ApplySceneModuleVisibilityFromCheckBox()
    {
        if (_syncingSceneModuleVisibilityChecks || SceneModuleVisibleCheck.IsChecked is not bool isVisible)
            return;

        var modules = GetSelectedSceneModules();
        if (modules.Count == 0)
            return;

        foreach (var module in modules)
            module.IsVisible = isVisible;

        MarkProjectDirty();
        RefreshSceneModuleList();
        SyncSceneModuleVisibilityChecks();
        RefreshStatusBarAfterViewChange();
        Viewport.InvalidateVisual();
        SetStatusBarOverrides(hint: isVisible ? "Módulo visível no 3D." : "Módulo oculto no 3D (continua na lista).");
    }

    private void ApplySceneModuleLockFromCheckBox()
    {
        if (_syncingSceneModuleVisibilityChecks || SceneModuleLockedCheck.IsChecked is not bool isLocked)
            return;

        var modules = GetSelectedSceneModules();
        if (modules.Count == 0)
            return;

        foreach (var module in modules)
            module.IsLocked = isLocked;

        MarkProjectDirty();

        if (_selectedModuleId.HasValue)
        {
            var module = _project.FindModule(_selectedModuleId.Value);
            if (module != null)
            {
                var definition = ModuleCatalog.GetRequired(module.DefinitionId);
                UpdateModulePropertyPanel(module, definition);
            }
        }

        RefreshSceneModuleList();
        SyncSceneModuleVisibilityChecks();
        RefreshStatusBarAfterViewChange();
        SetStatusBarOverrides(hint: isLocked
            ? "Módulo bloqueado — edição e exclusão desativadas."
            : "Módulo desbloqueado.");
    }

    private void SyncSceneModuleRenameBox()
    {
        int selectedCount = GetSceneModuleListSelectedCount();

        if (selectedCount == 1 &&
            SceneModuleListBox.SelectedItems.OfType<SceneModuleListItem>().FirstOrDefault() is SceneModuleListItem item)
        {
            SceneModuleRenameBox.Text = item.Module.InstanceDisplayName
                ?? ModuleInstanceNamingService.GetCatalogDisplayName(item.Module);
            return;
        }

        SceneModuleRenameBox.Text = selectedCount > 1
            ? $"({selectedCount} selecionados)"
            : string.Empty;
    }

    private void SceneModuleRenameButton_Click(object sender, RoutedEventArgs e) =>
        ApplySceneModuleRename();

    private void SceneModuleRenameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplySceneModuleRename();
            e.Handled = true;
        }
    }

    private void ApplySceneModuleRename()
    {
        if (!SceneModuleSelectionService.CanRename(GetSceneModuleListSelectedCount()) ||
            SceneModuleListBox.SelectedItems.OfType<SceneModuleListItem>().FirstOrDefault() is not SceneModuleListItem item)
            return;

        if (!ModuleInstanceNamingService.TryApplyRename(
                item.Module,
                SceneModuleRenameBox.Text,
                out string? error))
        {
            MessageBox.Show(error, "Ambiente", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MarkProjectDirty();
        RefreshSceneModuleList();
        SyncSceneModuleRenameBox();
        SetStatusBarOverrides(hint: $"Nome no ambiente: {ModuleInstanceNamingService.GetEffectiveDisplayName(item.Module)}.");
    }

    private void UpdateSceneModuleDeleteButtonState()
    {
        UpdateSceneModuleListActionsState();
    }

    private void SceneModuleDeleteButton_Click(object sender, RoutedEventArgs e) =>
        DeleteSelectedModules();

    private void ClearSceneModuleListSelection()
    {
        _syncingSceneModuleList = true;
        try
        {
            SceneModuleListBox.SelectedItems.Clear();
        }
        finally
        {
            _syncingSceneModuleList = false;
        }

        UpdateSceneModuleListActionsState();
        SceneModuleRenameBox.Text = string.Empty;
    }

    private void SyncSceneModuleListMultiSelection()
    {
        if (SceneModuleListBox.ItemsSource is not IEnumerable<SceneModuleListEntry> entries)
            return;

        _syncingSceneModuleList = true;
        try
        {
            SceneModuleListBox.SelectedItems.Clear();

            foreach (var item in entries.OfType<SceneModuleListItem>())
            {
                if (_selectedModuleIds.Contains(item.Module.Id))
                    SceneModuleListBox.SelectedItems.Add(item);
            }
        }
        finally
        {
            _syncingSceneModuleList = false;
        }
    }

    private void SyncSceneModuleListSelection(Guid moduleId)
    {
        _selectedModuleIds.Clear();
        _selectedModuleIds.Add(moduleId);
        SyncSceneModuleListMultiSelection();
    }

    private void SceneModuleListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingSceneModuleList)
            return;

        var selectedItems = SceneModuleListBox.SelectedItems.OfType<SceneModuleListItem>().ToList();
        _selectedModuleIds.Clear();

        foreach (var item in selectedItems)
            _selectedModuleIds.Add(item.Module.Id);

        if (selectedItems.Count == 0)
        {
            _selectedModuleId = null;
            ClearPropertyPanelSelection();
            UpdateSceneModuleListActionsState();
            SyncSceneModuleRenameBox();
            SyncDimensionConfiguratorSelectionState();
            return;
        }

        var primary = e.AddedItems.OfType<SceneModuleListItem>().LastOrDefault()
            ?? selectedItems[^1];

        ApplyModuleSelectionUi(primary.Module, selectedItems.Count, syncList: false);
        UpdateSceneModuleListActionsState();
        SyncSceneModuleRenameBox();
    }

    private void SceneModuleListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SceneModuleListBox.SelectedItems.OfType<SceneModuleListItem>().LastOrDefault() is not SceneModuleListItem item)
            return;

        ApplyModuleSelectionUi(item.Module, GetSceneModuleListSelectedCount(), syncList: false);
        FrameCameraOnModule(item.Module);
        LibraryTabControl.SelectedItem = LibraryTabScene;
        e.Handled = true;
    }

    private void SelectModule(ModuleInstance module)
    {
        _selectedModuleIds.Clear();
        _selectedModuleIds.Add(module.Id);
        ApplyModuleSelectionUi(module, 1, syncList: true);
        UpdateSceneModuleListActionsState();
        SyncSceneModuleRenameBox();
    }

    private void SelectAllModules()
    {
        CancelModuleMarqueeIfActive();
        _selectedModuleIds.Clear();

        foreach (var module in GetPickableModules())
            _selectedModuleIds.Add(module.Id);

        if (_selectedModuleIds.Count == 0)
        {
            _selectedModuleId = null;
            ClearPropertyPanelSelection();
            SyncSceneModuleListMultiSelection();
            UpdateSceneModuleListActionsState();
            SyncSceneModuleRenameBox();
            SyncDimensionConfiguratorSelectionState();
            SetStatusBarOverrides(hint: "Nenhum módulo para selecionar.");
            return;
        }

        var primary = GetPickableModules().LastOrDefault(m => _selectedModuleIds.Contains(m.Id))
            ?? GetPickableModules()[^1];
        ApplyModuleSelectionUi(primary, _selectedModuleIds.Count, syncList: true);
        UpdateSceneModuleListActionsState();
        SyncSceneModuleRenameBox();
        Viewport.InvalidateVisual();
    }

    /// <summary>Ctrl+clique: adiciona/remove módulo (seleção alternada Promob).</summary>
    private void ToggleModuleInSelection(ModuleInstance module)
    {
        bool added = SceneModuleSelectionService.ToggleId(_selectedModuleIds, module.Id);

        if (_selectedModuleIds.Count == 0)
        {
            _selectedModuleId = null;
            ClearPropertyPanelSelection();
            SyncSceneModuleListMultiSelection();
            UpdateSceneModuleListActionsState();
            SyncSceneModuleRenameBox();
            SyncDimensionConfiguratorSelectionState();
            Title = "Traços 3D";
            SetStatusBarOverrides(hint: "Seleção limpa.");
            Viewport.InvalidateVisual();
            return;
        }

        ModuleInstance primary;
        if (added)
            primary = module;
        else if (_selectedModuleId.HasValue &&
                 _selectedModuleIds.Contains(_selectedModuleId.Value) &&
                 _project.FindModule(_selectedModuleId.Value) is { } keep)
            primary = keep;
        else
            primary = _project.FindModule(_selectedModuleIds.First()) ?? module;

        ApplyModuleSelectionUi(primary, _selectedModuleIds.Count, syncList: true);
        UpdateSceneModuleListActionsState();
        SyncSceneModuleRenameBox();
        Viewport.InvalidateVisual();
    }

    private void ApplyMarqueeModuleSelection()
    {
        var (minX, minY, maxX, maxY) = SceneModuleSelectionService.NormalizeScreenRect(
            _moduleMarqueeStart.X,
            _moduleMarqueeStart.Y,
            _moduleMarqueeEnd.X,
            _moduleMarqueeEnd.Y);

        EnsureCameraMatricesForPicking();
        int width = Math.Max(1, (int)Viewport.ActualWidth);
        int height = Math.Max(1, (int)Viewport.ActualHeight);

        var hits = SceneModuleSelectionService.FindModulesIntersectingScreenRect(
            GetPickableModules(),
            minX,
            minY,
            maxX,
            maxY,
            _camera.View,
            _camera.Projection,
            width,
            height);

        // Ctrl+caixa: inclui os módulos do retângulo na seleção (não remove os já selecionados).
        foreach (var module in hits)
            _selectedModuleIds.Add(module.Id);

        if (_selectedModuleIds.Count == 0)
        {
            _selectedModuleId = null;
            ClearPropertyPanelSelection();
            SyncSceneModuleListMultiSelection();
            UpdateSceneModuleListActionsState();
            SyncSceneModuleRenameBox();
            SyncDimensionConfiguratorSelectionState();
            return;
        }

        var primary = hits.Count > 0
            ? hits[^1]
            : GetPickableModules().First(m => _selectedModuleIds.Contains(m.Id));

        ApplyModuleSelectionUi(primary, _selectedModuleIds.Count, syncList: true);
        UpdateSceneModuleListActionsState();
        SyncSceneModuleRenameBox();
        Viewport.InvalidateVisual();
    }

    private bool CancelModuleMarqueeIfActive()
    {
        if (!_moduleMarqueePending && !_moduleMarqueeActive)
            return false;

        ClearModuleMarqueeState();
        Viewport.InvalidateVisual();
        return true;
    }

    private void ClearModuleMarqueeState()
    {
        _moduleMarqueePending = false;
        _moduleMarqueeActive = false;
        _moduleMarqueeClickCandidateId = null;

        if (_moduleMarqueeRect != null)
        {
            ModuleMarqueeCanvas.Children.Remove(_moduleMarqueeRect);
            _moduleMarqueeRect = null;
        }

        if (Viewport.IsMouseCaptured)
            Viewport.ReleaseMouseCapture();
    }

    private void UpdateModuleMarqueeOverlay()
    {
        var (minX, minY, maxX, maxY) = SceneModuleSelectionService.NormalizeScreenRect(
            _moduleMarqueeStart.X,
            _moduleMarqueeStart.Y,
            _moduleMarqueeEnd.X,
            _moduleMarqueeEnd.Y);

        if (_moduleMarqueeRect == null)
        {
            _moduleMarqueeRect = new System.Windows.Shapes.Rectangle
            {
                // Paridade Promob: caixa de seleção Ctrl+arraste em vermelho.
                Stroke = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xE0, 0x2B, 0x2B)),
                StrokeThickness = 1.5,
                Fill = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(0x38, 0xE0, 0x2B, 0x2B)),
                IsHitTestVisible = false
            };
            ModuleMarqueeCanvas.Children.Add(_moduleMarqueeRect);
        }

        Canvas.SetLeft(_moduleMarqueeRect, minX);
        Canvas.SetTop(_moduleMarqueeRect, minY);
        _moduleMarqueeRect.Width = Math.Max(0, maxX - minX);
        _moduleMarqueeRect.Height = Math.Max(0, maxY - minY);
    }

    private bool BeginModuleMarqueeSelection(Point position)
    {
        _moduleMarqueePending = true;
        _moduleMarqueeActive = false;
        _moduleMarqueeStart = position;
        _moduleMarqueeEnd = position;
        _moduleMarqueeClickCandidateId = null;

        if (TryPickModuleAtScreen(position.X, position.Y, out ModuleInstance? hit) && hit != null)
            _moduleMarqueeClickCandidateId = hit.Id;

        Viewport.CaptureMouse();
        return true;
    }

    private void FinishModuleMarqueeSelection()
    {
        if (!_moduleMarqueePending && !_moduleMarqueeActive)
            return;

        bool wasActive = _moduleMarqueeActive;
        Guid? clickId = _moduleMarqueeClickCandidateId;
        ClearModuleMarqueeState();

        if (wasActive)
        {
            ApplyMarqueeModuleSelection();
            return;
        }

        if (clickId.HasValue && _project.FindModule(clickId.Value) is { } module)
            ToggleModuleInSelection(module);
    }

    private void ApplyModuleSelectionUi(ModuleInstance module, int selectedCount, bool syncList)
    {
        _floorSelected = false;
        _selectedFloorZoneId = null;
        _selectedModuleId = module.Id;
        _wallGroupSelected = false;
        _selectedWallId = null;
        _selectedOpeningId = null;
        _openModuleGroupId = null;
        _selectedPartLabel = null;
        _selectedPartHandle = null;

        WallPropertiesPanel.Visibility = Visibility.Collapsed;
        FloorPropertiesPanel.Visibility = Visibility.Collapsed;
        ModulePropertiesPanel.Visibility = Visibility.Visible;

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        UpdateModulePropertyPanel(module, definition);

        if (selectedCount > 1)
        {
            Title = $"Tra?os 3D - {selectedCount} m?dulos selecionados | Delete remove todos";
            string? hint = SceneModuleSelectionService.FormatMultiSelectHint(selectedCount);
            if (hint != null)
                SetStatusBarOverrides(hint: hint);
        }
        else
        {
            Title =
                $"Tra?os 3D - {definition.DisplayName} | L: {module.Width:0} A: {module.Height:0} P: {module.Depth:0} mm | R gira 90? | Delete remove";
            UpdateStatusBarSelection("Módulo", module.Width, definition.DisplayName);
        }

        if (syncList)
            SyncSceneModuleListMultiSelection();

        SyncDimensionConfiguratorSelectionState();
    }

    private void UpdateModulePropertyPanel(ModuleInstance module, ModuleDefinition definition)
    {
        _syncingPropertyPanel = true;

        PropertyLengthLabel.Text = "Largura (mm)";
        PropertyHeightLabel.Text = "Altura (mm)";
        PropertyDepthLabel.Text = "Profundidade (mm)";

        // Sem peça aberta, o painel volta a representar o módulo inteiro.
        PartParametrizationExpander.Visibility = Visibility.Collapsed;

        bool isCornerL = module.CornerL != null ||
            definition.ShapeKind is ModuleShapeKind.CornerLLeft or ModuleShapeKind.CornerLRight;
        bool isOblique = definition.ShapeKind == ModuleShapeKind.Oblique;
        bool isEndTerminal = definition.ShapeKind is ModuleShapeKind.EndDiagonal or ModuleShapeKind.EndChamfer;
        bool isSpecialColumn = definition.ShapeKind == ModuleShapeKind.ColumnDoors && module.SpecialColumn != null;
        bool isDrawerModule = definition.DrawerCount > 0 &&
            string.Equals(definition.LibrarySubGroup, ModuleLibraryHierarchy.SubGaveteiros,
                StringComparison.OrdinalIgnoreCase);
        if (isCornerL && module.CornerL != null)
        {
            PropertyLengthLabel.Text = "Largura A (mm)";
            PropertyDepthLabel.Text = "Largura B (mm)";
            PropertyLengthBox.Text = module.CornerL.ComprimentoDireito
                .ToString("0", CultureInfo.InvariantCulture);
            PropertyHeightBox.Text = module.Height.ToString("0", CultureInfo.InvariantCulture);
            PropertyDepthBox.Text = module.CornerL.ComprimentoEsquerdo
                .ToString("0", CultureInfo.InvariantCulture);
            PropertyCornerMeasuresPanel.Visibility = Visibility.Visible;
            PropertyCornerMeasureABox.Text = module.CornerL.ProfundidadeDireita
                .ToString("0", CultureInfo.InvariantCulture);
            PropertyCornerMeasureBBox.Text = module.CornerL.ProfundidadeEsquerda
                .ToString("0", CultureInfo.InvariantCulture);
        }
        else
        {
            PropertyLengthBox.Text = module.Width.ToString("0", CultureInfo.InvariantCulture);
            PropertyHeightBox.Text = module.Height.ToString("0", CultureInfo.InvariantCulture);
            PropertyDepthBox.Text = module.Depth.ToString("0", CultureInfo.InvariantCulture);
            PropertyCornerMeasuresPanel.Visibility = Visibility.Collapsed;
            PropertyCornerMeasureABox.Text = "";
            PropertyCornerMeasureBBox.Text = "";
        }

        if (isEndTerminal)
        {
            module.EndTerminal ??= EndTerminalParams.FromDefinition(definition);
            module.EndTerminal.ClampToModule(module.Width, module.Depth,
                definition.ShapeKind == ModuleShapeKind.EndChamfer);
            PropertyCornerMeasuresPanel.Visibility = Visibility.Visible;
            PropertyCornerMeasureALabel.Text = "Medida A — profundidade da lateral menor (mm)";
            PropertyCornerMeasureABox.Text = module.EndTerminal.SmallSideDepthMm
                .ToString("0", CultureInfo.InvariantCulture);
            bool showB = definition.ShapeKind == ModuleShapeKind.EndChamfer;
            PropertyCornerMeasureBPanel.Visibility = showB ? Visibility.Visible : Visibility.Collapsed;
            PropertyCornerMeasureBLabel.Text = "Medida B — frente reta até o encontro das travessas (mm)";
            PropertyCornerMeasureBBox.Text = showB
                ? module.EndTerminal.FrontStraightWidthMm.ToString("0", CultureInfo.InvariantCulture)
                : "";
        }
        else
        {
            PropertyCornerMeasureALabel.Text = "Medida A (mm)";
            PropertyCornerMeasureBLabel.Text = "Medida B (mm)";
            PropertyCornerMeasureBPanel.Visibility = isCornerL ? Visibility.Visible : Visibility.Collapsed;
        }

        PropertyObliquePanel.Visibility = isOblique || isEndTerminal ? Visibility.Visible : Visibility.Collapsed;
        PropertyObliqueDoorCountCombo.SelectedIndex = isEndTerminal
            ? Math.Clamp(module.EndTerminal?.DoorCount ?? 1, 1, 2) - 1
            : Math.Clamp(module.ObliqueDoorCount, 1, 2) - 1;
        PropertyObliqueHingeSideCombo.SelectedIndex = module.ObliqueHingesOnLeft ? 0 : 1;
        PropertyObliqueHingeSidePanel.Visibility = isOblique && module.ObliqueDoorCount == 1
            ? Visibility.Visible
            : Visibility.Collapsed;

        PropertySpecialColumnPanel.Visibility = isSpecialColumn ? Visibility.Visible : Visibility.Collapsed;
        if (isSpecialColumn && module.SpecialColumn != null)
        {
            PropertySpecialColumnPositionText.Text = module.SpecialColumn.Position switch
            {
                SpecialColumnPosition.Left => "Recorte para coluna — esquerda",
                SpecialColumnPosition.Right => "Recorte para coluna — direita",
                _ => "Recorte para coluna — central"
            };
            PropertySpecialColumnWidthBox.Text = module.SpecialColumn.WidthMm.ToString("0", CultureInfo.InvariantCulture);
            PropertySpecialColumnDepthBox.Text = module.SpecialColumn.DepthMm.ToString("0", CultureInfo.InvariantCulture);
            PropertySpecialColumnOffsetBox.Text = module.SpecialColumn.LeftOffsetMm.ToString("0", CultureInfo.InvariantCulture);
            PropertySpecialColumnOffsetPanel.Visibility = module.SpecialColumn.Position == SpecialColumnPosition.Center
                ? Visibility.Visible : Visibility.Collapsed;
            PropertySpecialColumnShelfCombo.SelectedIndex = module.SpecialColumn.ShelfNotched ? 0 : 1;
        }

        var dimSettings = DimensionConfiguratorService.GetSettings(_project);
        ModelsExpander.Visibility = isDrawerModule ? Visibility.Visible : Visibility.Collapsed;
        PropertyDrawerSlideTypeCombo.SelectedIndex = module.DrawerSlideType == DrawerSlideType.Concealed ? 1 : 0;
        if (isDrawerModule)
        {
            GavetasConfiguratorService.EnsureInitialized(dimSettings);
            string telKey = GavetasConfiguratorService.MakeKey("folgas", "folg-cor-tel");
            string invKey = GavetasConfiguratorService.MakeKey("folgas", "folg-cor-inv");
            float telGap = dimSettings.CozinhaGavetas.Numeric.GetValueOrDefault(telKey, 13.5f);
            float invGap = dimSettings.CozinhaGavetas.Numeric.GetValueOrDefault(invKey, 5f);
            PropertyTelescopicSlideItem.Content = $"Telescópica — folga {telGap:0.##} mm";
            PropertyConcealedSlideItem.Content = $"Invisível — folga {invGap:0.##} mm";
            PropertyDrawerSlideHintText.Text = module.DrawerSlideType == DrawerSlideType.Concealed
                ? "Usando A2 — Folga Corrediça Invisível do Configurador de Dimensões."
                : "Usando A1 — Folga Corrediça Telescópica do Configurador de Dimensões.";
        }

        PropertyHintText.Text = isCornerL
            ? "Canto L: Largura A/B = comprimentos das asas; Medida A/B = profundidades (Promob). Enter confirma cada campo."
            : isOblique
                ? "Canto oblíquo: largura e profundidade correspondem às Medidas A/B. Escolha uma ou duas portas."
                : isEndTerminal
                    ? definition.ShapeKind == ModuleShapeKind.EndChamfer
                        ? "Chanfrado: A é a profundidade da lateral menor; B é a frente reta até o encontro angular das travessas. I espelha também a identificação das peças."
                        : "Diagonal: A é a profundidade da lateral menor. I espelha também a identificação das peças."
                : isSpecialColumn
                    ? "Especial para coluna: informe largura e profundidade do recorte. Na coluna central, informe também sua posição a partir da esquerda. Enter confirma."
                : $"Dimensões livres até {dimSettings.MaxWidthMm:0} × {dimSettings.MaxHeightMm:0} × {dimSettings.MaxDepthMm:0} mm. Enter confirma.";

        var material = MaterialCatalog.TryGet(module.MaterialId, out var mat) && mat != null
            ? mat
            : MaterialCatalog.GetDefault();
        PropertyMaterialCombo.SelectedItem = material;
        PropertyMaterialHintText.Text = $"Acabamento: {material.DisplayName}.";

        PopulateModuleLayerCombo();
        _syncingPropertyPanel = true;
        string moduleLayerId = WallLayerCatalog.NormalizeModuleLayerId(module.LayerId);
        WallLayerDefinition? selectedModuleLayer = WallLayerCatalog.GetDefinitions(_project.Metadata)
            .FirstOrDefault(l => l.Id == moduleLayerId);
        ModuleLayerCombo.SelectedItem = selectedModuleLayer;
        _syncingPropertyPanel = false;

        UpdateModuleCotasPanel(module);
        UpdateModulePropertyPanelEditableState(module);

        _syncingPropertyPanel = false;
    }

    private void UpdateModulePropertyPanelEditableState(ModuleInstance module)
    {
        bool editable = SceneModuleVisibilityService.IsEditable(module);
        PropertyLengthBox.IsEnabled = editable;
        PropertyHeightBox.IsEnabled = editable;
        PropertyDepthBox.IsEnabled = editable;
        PropertyCornerMeasureABox.IsEnabled = editable;
        PropertyCornerMeasureBBox.IsEnabled = editable;
        PropertyObliqueDoorCountCombo.IsEnabled = editable;
        PropertyObliqueHingeSideCombo.IsEnabled = editable;
        PropertySpecialColumnWidthBox.IsEnabled = editable;
        PropertySpecialColumnDepthBox.IsEnabled = editable;
        PropertySpecialColumnOffsetBox.IsEnabled = editable;
        PropertySpecialColumnShelfCombo.IsEnabled = editable;
        PropertyDrawerSlideTypeCombo.IsEnabled = editable;
        PropertyMaterialCombo.IsEnabled = editable;
        ModuleLayerCombo.IsEnabled = editable;
        RotateModule90Button.IsEnabled = editable;
        ModuleCotaAnteriorBox.IsEnabled = editable;
        ModuleCotaPosteriorBox.IsEnabled = editable;
        ModuleCotaInferiorBox.IsEnabled = editable;
        ModuleCotaSuperiorBox.IsEnabled = editable;
    }

    private void UpdateModuleCotasPanel(ModuleInstance module)
    {
        var wall = ModulePlacementService.FindBackingWall(module, _project.Room.Walls);

        if (wall == null)
        {
            ModuleCotasExpander.Visibility = Visibility.Collapsed;
            return;
        }

        var cotas = ModulePlacementService.ComputeDisplayWallCotas(module, wall, _project.Room.Walls);

        ModuleCotasExpander.Visibility = Visibility.Visible;
        ModuleCotaAnteriorBox.Text = cotas.Anterior.ToString("0", CultureInfo.InvariantCulture);
        ModuleCotaPosteriorBox.Text = cotas.Posterior.ToString("0", CultureInfo.InvariantCulture);
        ModuleCotaInferiorBox.Text = cotas.Inferior.ToString("0", CultureInfo.InvariantCulture);
        ModuleCotaSuperiorBox.Text = cotas.Superior.ToString("0", CultureInfo.InvariantCulture);
        var innerFace = WallInnerFaceService.GetInnerFace(wall, _project.Room.Walls);
        ModuleCotasHintText.Text =
            $"Face interna: {innerFace.Length:0} mm. Enter confirma cada cota (a partir do encontro interno).";
    }

    private void ApplyModuleCotaFromPanel(System.Windows.Controls.TextBox sourceBox)
    {
        if (_syncingPropertyPanel || !_selectedModuleId.HasValue)
            return;

        var module = _project.FindModule(_selectedModuleId.Value);

        if (module == null || !SceneModuleVisibilityService.IsEditable(module))
            return;

        var wall = ModulePlacementService.FindBackingWall(module, _project.Room.Walls);

        if (wall == null)
            return;

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);

        if (!PropertyPanelInput.TryParseMm(sourceBox.Text, out float value))
        {
            ModuleCotasHintText.Text = "Informe a cota em mm e pressione Enter.";
            return;
        }

        // Módulo sem vínculo (ou órfão): vincula à parede de referência antes de cotar.
        if (module.AttachedWallId != wall.Id)
            ModulePlacementService.AttachModuleToWall(module, wall, _project.Room.Walls, definition);

        ModuleCotaAxis axis = sourceBox.Name switch
        {
            nameof(ModuleCotaAnteriorBox) => ModuleCotaAxis.Anterior,
            nameof(ModuleCotaPosteriorBox) => ModuleCotaAxis.Posterior,
            nameof(ModuleCotaInferiorBox) => ModuleCotaAxis.Inferior,
            nameof(ModuleCotaSuperiorBox) => ModuleCotaAxis.Superior,
            _ => ModuleCotaAxis.Anterior
        };

        if (!ModulePlacementService.TryApplyWallCota(
                module, wall, _project.Room.Walls, definition, axis, value, out string? error))
        {
            ModuleCotasHintText.Text = error ?? "Cota inv?lida.";
            UpdateModuleCotasPanel(module);
            return;
        }

        MarkProjectDirty();
        RefreshCollisionState();
        UpdateModulePropertyPanel(module, definition);

        Title =
            $"Tra?os 3D - {definition.DisplayName} | Cotas atualizadas | R gira 90? | Delete remove";
        UpdateStatusBarSelection("Módulo", module.Width, ModuleCatalog.GetRequired(module.DefinitionId).DisplayName);
    }

    private void RotateSelectedModule90()
    {
        if (!_selectedModuleId.HasValue)
            return;

        var module = _project.FindModule(_selectedModuleId.Value);

        if (module == null || !SceneModuleVisibilityService.IsEditable(module))
            return;

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        module.RotationYDegrees = PropertyPanelInput.Rotate90Degrees(module.RotationYDegrees);
        module.RebuildMesh(definition, DimensionConfiguratorService.GetSettings(_project));
        MarkProjectDirty();

        UpdateModulePropertyPanel(module, definition);
        Title =
            $"Tra?os 3D - {definition.DisplayName} | Rota??o: {module.RotationYDegrees:0}? | R gira 90? | Delete remove";
        UpdateStatusBarSelection("Módulo", module.Width, ModuleCatalog.GetRequired(module.DefinitionId).DisplayName);
    }

    private void RotateModule90Button_Click(object sender, RoutedEventArgs e) =>
        RotateSelectedModule90();

    private void ApplyPartPropertiesFromPanel(float width, float height, float depth)
    {
        if (_openModuleGroupId == null || string.IsNullOrEmpty(_selectedPartLabel))
            return;

        var module = _project.FindModule(_openModuleGroupId.Value);

        if (module == null || !SceneModuleVisibilityService.IsEditable(module))
            return;

        if (width <= 0f || height <= 0f || depth <= 0f)
        {
            PropertyHintText.Text = "Largura, altura e profundidade devem ser maiores que zero.";
            return;
        }

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);

        if (!ModulePartEditService.TryApplyDimensions(
                module, definition, _selectedPartLabel, width, height, depth, out string? error))
        {
            PropertyHintText.Text = error ?? "Não foi possível aplicar a dimensão nesta peça.";
            UpdatePartPropertyPanel(module);
            return;
        }

        module.RebuildMesh(definition, DimensionConfiguratorService.GetSettings(_project));
        MarkProjectDirty();
        RefreshCollisionState();
        Viewport.InvalidateVisual();

        UpdatePartPropertyPanel(module);
        SetStatusBarOverrides(hint: $"Peça {_selectedPartLabel} atualizada.");
    }

    private void ApplyModulePropertiesFromPanel(float width, float height, float depth)
    {
        if (!_selectedModuleId.HasValue)
            return;

        var module = _project.FindModule(_selectedModuleId.Value);

        if (module == null || !SceneModuleVisibilityService.IsEditable(module))
            return;

        if (width <= 0f || height <= 0f || depth <= 0f)
        {
            PropertyHintText.Text = "Largura, altura e profundidade devem ser maiores que zero.";
            return;
        }

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        bool isCornerL = module.CornerL != null ||
            definition.ShapeKind is ModuleShapeKind.CornerLLeft or ModuleShapeKind.CornerLRight;
        bool isEndTerminal = definition.ShapeKind is ModuleShapeKind.EndDiagonal or ModuleShapeKind.EndChamfer;

        float? cornerMedidaA = null;
        float? cornerMedidaB = null;
        float? cornerLarguraA = null;
        float? cornerLarguraB = null;
        if (isCornerL)
        {
            if (!PropertyPanelInput.TryParseMm(PropertyCornerMeasureABox.Text, out float medidaA) ||
                !PropertyPanelInput.TryParseMm(PropertyCornerMeasureBBox.Text, out float medidaB) ||
                medidaA <= 0f || medidaB <= 0f)
            {
                PropertyHintText.Text = "Medida A e Medida B devem ser maiores que zero (profundidades das asas).";
                return;
            }

            cornerMedidaA = medidaA;
            cornerMedidaB = medidaB;
            cornerLarguraA = width;
            cornerLarguraB = depth;
        }

        float? endSmallSideDepth = null;
        float? endFrontStraightWidth = null;
        if (isEndTerminal)
        {
            if (!PropertyPanelInput.TryParseMm(PropertyCornerMeasureABox.Text, out float medidaA) || medidaA <= 0f)
            {
                PropertyHintText.Text = "A profundidade da lateral menor (Medida A) deve ser maior que zero.";
                return;
            }
            endSmallSideDepth = medidaA;
            if (definition.ShapeKind == ModuleShapeKind.EndChamfer)
            {
                if (!PropertyPanelInput.TryParseMm(PropertyCornerMeasureBBox.Text, out float medidaB) || medidaB < 0f)
                {
                    PropertyHintText.Text = "A frente reta (Medida B) deve ser zero ou maior.";
                    return;
                }
                endFrontStraightWidth = medidaB;
            }
        }

        float? specialColumnWidth = null;
        float? specialColumnDepth = null;
        float? specialColumnOffset = null;
        if (module.SpecialColumn != null)
        {
            if (!PropertyPanelInput.TryParseMm(PropertySpecialColumnWidthBox.Text, out float columnWidth) ||
                !PropertyPanelInput.TryParseMm(PropertySpecialColumnDepthBox.Text, out float columnDepth) ||
                columnWidth <= 0f || columnDepth <= 0f)
            {
                PropertyHintText.Text = "Largura e profundidade da coluna devem ser maiores que zero.";
                return;
            }

            float offset = module.SpecialColumn.LeftOffsetMm;
            if (module.SpecialColumn.Position == SpecialColumnPosition.Center &&
                (!PropertyPanelInput.TryParseMm(PropertySpecialColumnOffsetBox.Text, out offset) || offset < 0f))
            {
                PropertyHintText.Text = "A posição da coluna central deve ser zero ou maior.";
                return;
            }
            specialColumnWidth = columnWidth;
            specialColumnDepth = columnDepth;
            specialColumnOffset = offset;
        }

        float? preserveAnterior = null;
        WallSegment? attachedWall = null;

        if (module.AttachedWallId.HasValue)
        {
            attachedWall = FindWallById(module.AttachedWallId.Value);
            var cotas = attachedWall != null
                ? ModulePlacementService.TryComputeWallCotas(module, attachedWall, _project.Room.Walls)
                : null;

            if (cotas != null)
                preserveAnterior = cotas.Value.Anterior;
        }

        PropertyPanelInput.ApplyModuleDimensions(
            module,
            definition,
            width,
            height,
            depth,
            DimensionConfiguratorService.GetSettings(_project),
            cornerMedidaA,
            cornerMedidaB,
            cornerLarguraA,
            cornerLarguraB);

        if (isEndTerminal && endSmallSideDepth.HasValue)
        {
            module.EndTerminal ??= EndTerminalParams.FromDefinition(definition);
            module.EndTerminal.SmallSideDepthMm = endSmallSideDepth.Value;
            if (endFrontStraightWidth.HasValue)
                module.EndTerminal.FrontStraightWidthMm = endFrontStraightWidth.Value;
            module.EndTerminal.ClampToModule(module.Width, module.Depth,
                definition.ShapeKind == ModuleShapeKind.EndChamfer);
            module.RebuildMesh(definition, DimensionConfiguratorService.GetSettings(_project));
        }

        if (module.SpecialColumn != null && specialColumnWidth.HasValue && specialColumnDepth.HasValue)
        {
            module.SpecialColumn.WidthMm = specialColumnWidth.Value;
            module.SpecialColumn.DepthMm = specialColumnDepth.Value;
            if (specialColumnOffset.HasValue)
                module.SpecialColumn.LeftOffsetMm = specialColumnOffset.Value;
            module.SpecialColumn.ClampToModule(module.Width, module.Depth);
            module.RebuildMesh(definition, DimensionConfiguratorService.GetSettings(_project));
        }

        if (preserveAnterior.HasValue && attachedWall != null)
        {
            ModulePlacementService.TryApplyWallCota(
                module,
                attachedWall,
                _project.Room.Walls,
                definition,
                ModuleCotaAxis.Anterior,
                preserveAnterior.Value,
                out _);
        }

        MarkProjectDirty();
        RefreshCollisionState();

        UpdateModulePropertyPanel(module, definition);
        Title =
            $"Tra?os 3D - {definition.DisplayName} | L: {module.Width:0} A: {module.Height:0} P: {module.Depth:0} mm | R gira 90? | Delete remove";
        UpdateStatusBarSelection("Módulo", module.Width, ModuleCatalog.GetRequired(module.DefinitionId).DisplayName);
        RefreshSceneModuleList();
    }

    private void BeginModuleInsertMode(string definitionId)
    {
        CancelWallMode();
        CancelOpeningInsertMode();
        ClearSelection();

        _moduleInsertDefinitionId = definitionId;
        _hasModulePreview = false;
        _previewModuleWallId = null;

        var definition = ModuleCatalog.GetRequired(definitionId);
        MeasureBox.Visibility = Visibility.Collapsed;
        Keyboard.Focus(this);

        Title =
            $"Tra?os 3D - {definition.DisplayName} | Arraste para a parede e solte | Esc cancela";
        SetStatusBarOverrides(context: "Face: Nenhuma");
    }

    private DimensionConfiguratorSettings GetEffectiveDimensionSettings()
    {
        if (_dimensionConfiguratorWindow != null)
            return _dimensionConfiguratorWindow.GetCommittedSettings();

        return DimensionConfiguratorService.GetSettings(_project);
    }

    private (float Width, float Height, float Depth) GetActiveModuleInsertionDimensions()
    {
        if (_moduleInsertDefinitionId == null)
            return (0f, 0f, 0f);

        var definition = ModuleCatalog.GetRequired(_moduleInsertDefinitionId);
        return DimensionConfiguratorService.ResolveInsertionDimensions(
            definition,
            GetEffectiveDimensionSettings());
    }

    private string ModuleInsertConfirmHint => "Soltar na parede";

    private bool TryComputeModulePlacementFromScreen(
        double mouseX,
        double mouseY,
        out ModulePlacementResult placement)
    {
        placement = default;

        if (_moduleInsertDefinitionId == null || _project.Room.Walls.Count == 0)
            return false;

        EnsureCameraMatricesForPicking();

        var definition = ModuleCatalog.GetRequired(_moduleInsertDefinitionId);
        var (previewWidth, previewHeight, previewDepth) = GetActiveModuleInsertionDimensions();

        var result = ModulePlacementService.TryComputeFromScreenRay(
            mouseX,
            mouseY,
            Viewport.ActualWidth,
            Viewport.ActualHeight,
            _camera.View,
            _camera.Projection,
            _project.Room.Walls,
            BuildWallPickTargets(),
            definition,
            previewWidth,
            previewDepth,
            previewHeight);

        if (result == null)
            return false;

        placement = result.Value;

        if (placement.WallId.HasValue)
        {
            Guid wallId = placement.WallId.Value;
            var wall = _project.Room.Walls.FirstOrDefault(w => w.Id == wallId);

            (float snappedAlong, float snappedMountY) = ModuleWallFaceService.ApplyEdgeSnaps(
                placement.DistanceAlongWall,
                placement.Position.Y,
                previewWidth,
                previewHeight,
                wallId,
                definition.IsWallMounted,
                movingModuleId: Guid.Empty,
                _project.Modules,
                verticalMoveDelta: 0f,
                wallFloorY: wall?.FloorOffset);

            if (wall != null)
            {
                var innerFace = WallInnerFaceService.GetInnerFace(wall, _project.Room.Walls);
                placement = ModulePlacementService.PlaceOnInsertionFace(
                    wall,
                    _project.Room.Walls,
                    definition,
                    previewWidth,
                    previewDepth,
                    snappedAlong,
                    innerFace.InteriorNormal,
                    snappedMountY,
                    previewHeight);
            }
        }

        return true;
    }

    private void ConfirmModuleInsert(double mouseX, double mouseY)
    {
        if (_moduleInsertDefinitionId == null)
            return;

        var definition = ModuleCatalog.GetRequired(_moduleInsertDefinitionId);

        EnsureCameraMatricesForPicking();

        if (!ModuleInsertDropService.TryInsertFromScreen(
                _project,
                _moduleInsertDefinitionId,
                mouseX,
                mouseY,
                Viewport.ActualWidth,
                Viewport.ActualHeight,
                _camera.View,
                _camera.Projection,
                BuildWallPickTargets(),
                _collisionEnabled,
                IsModuleCollisionBypassActive(),
                GetEffectiveDimensionSettings(),
                out ModuleInstance? instance,
                out string? error) ||
            instance == null)
        {
            Title = $"Tra?os 3D - {definition.DisplayName} | Aponte para a face da parede | Esc cancela";

            if (!string.IsNullOrWhiteSpace(error))
                SetStatusBarOverrides(hint: error);

            return;
        }

        SelectModule(instance);
        MarkProjectDirty();
        RefreshCollisionState();
        RefreshSceneModuleList();
        Keyboard.Focus(this);

        // O clique confirma na mesma posição do último preview; limpar até o próximo
        // MouseMove evita sobrepor o fantasma translúcido ao módulo recém-inserido.
        _hasModulePreview = false;
        _previewModuleWallId = null;

        if (_collisionEnabled && _collidingModuleIds.Contains(instance.Id))
        {
            Title = $"Tra?os 3D - {definition.DisplayName} | Colis?o detectada | Clique para inserir outro | Esc cancela";
            return;
        }

        Title = $"Tra?os 3D - {definition.DisplayName} encostado na parede | Clique para inserir outro | Esc cancela";
    }

    private void UpdateModulePreview(double mouseX, double mouseY)
    {
        _hasModulePreview = false;
        _previewModuleWallId = null;
        _previewModuleCotas = null;

        if (_moduleInsertDefinitionId == null)
            return;

        var definition = ModuleCatalog.GetRequired(_moduleInsertDefinitionId);

        if (!TryComputeModulePlacementFromScreen(mouseX, mouseY, out var placement))
        {
            Title = $"Tra?os 3D - {definition.DisplayName} | Arraste para a parede e solte | Esc cancela";
            SetStatusBarOverrides(context: "Face: Nenhuma");
            return;
        }

        _previewModulePosition = placement.Position;
        _previewModuleRotationY = placement.RotationYDegrees;
        _previewModuleSnappedToWall = placement.SnappedToWall;
        _previewModuleWallId = placement.WallId;
        _previewModuleDistanceAlong = placement.DistanceAlongWall;
        _hasModulePreview = true;

        bool wouldCollide = WouldPreviewCollide();
        var (previewWidth, previewHeight, _) = GetActiveModuleInsertionDimensions();

        if (placement.WallId.HasValue)
        {
            var wall = FindWallById(placement.WallId.Value);

            if (wall != null)
            {
                var cotas = ModulePlacementService.ComputeWallCotasFromPlacement(
                    wall,
                    _project.Room.Walls,
                    definition,
                    previewWidth,
                    previewHeight,
                    placement);

                _previewModuleCotas = cotas;

                SetStatusBarOverrides(context:
                    $"Face: Parede   ·   Ant: {cotas.Anterior:0} mm   Post: {cotas.Posterior:0} mm   " +
                    $"Base: {cotas.Inferior:0} mm   Topo: {cotas.Superior:0} mm");
            }

            Title = wouldCollide
                ? $"Tra?os 3D - {definition.DisplayName} | Colis?o no preview | {ModuleInsertConfirmHint} | Esc cancela"
                : $"Tra?os 3D - {definition.DisplayName} | Encosta na parede | {ModuleInsertConfirmHint} | Esc cancela";
            return;
        }

        SetStatusBarOverrides(context: "Face: Nenhuma");
        Title = wouldCollide
            ? $"Tra?os 3D - {definition.DisplayName} | Colis?o no preview | {ModuleInsertConfirmHint} | Esc cancela"
            : $"Tra?os 3D - {definition.DisplayName} | Arraste para a parede e solte | Esc cancela";
    }

    private bool TryPickModuleAtScreen(double mouseX, double mouseY, out ModuleInstance? picked)
    {
        picked = null;

        if (_project.Modules.Count == 0)
            return false;

        EnsureCameraMatricesForPicking();

        if (!Geometry3D.TryCreateWorldRay(
                mouseX,
                mouseY,
                Viewport.ActualWidth,
                Viewport.ActualHeight,
                _camera.View,
                _camera.Projection,
                out Vector3 origin,
                out Vector3 direction))
            return false;

        if (!ModulePickService.TryPickRay(
                origin,
                direction,
                GetPickableModules(),
                out picked,
                out _,
                _selectedModuleId) ||
            picked == null)
            return false;

        return true;
    }

    private void PrepareModuleWallDrag(ModuleInstance module)
    {
        if (_selectedModuleIds.Count > 1)
            return;

        if (module.IsLocked || !SceneModuleVisibilityService.IsEditable(module))
            return;

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        var wall = ModulePlacementService.FindBackingWall(module, _project.Room.Walls);

        if (wall == null)
            return;

        if (!module.AttachedWallId.HasValue || module.AttachedWallId.Value != wall.Id)
            ModulePlacementService.AttachModuleToWall(module, wall, _project.Room.Walls, definition);

        _moduleWallDragPending = true;
        _moduleWallDragModuleId = module.Id;
        _moduleWallDragWallId = wall.Id;
        _moduleWallDragStartScreen = Mouse.GetPosition(Viewport);
        _moduleWallDragOriginalPosition = module.Position;
        _moduleWallDragOriginalRotationY = module.RotationYDegrees;
        _moduleWallDragOriginalDistanceAlong = module.DistanceAlongWall;
        _moduleWallDragOriginalWallId = module.AttachedWallId;
        _moduleWallDragLastMountY = module.Position.Y;
        _moduleWallDragLastMouseX = _moduleWallDragStartScreen.X;
        _moduleWallDragLastMouseY = _moduleWallDragStartScreen.Y;
        _moduleWallDragCotas = null;
    }

    private static bool IsModuleCollisionBypassActive() =>
        Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

    private bool WouldModulePlacementCollide(ModuleInstance module, ModulePlacementResult placement)
    {
        if (!_collisionEnabled || IsModuleCollisionBypassActive())
            return false;

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);

        return ModuleCollisionService.WouldCollide(
            placement.Position,
            module.Width,
            module.Height,
            module.Depth,
            placement.RotationYDegrees,
            _project.Modules,
            ignoreModuleId: module.Id,
            candidateWallId: placement.WallId ?? _moduleWallDragWallId,
            distanceAlongWall: placement.DistanceAlongWall,
            candidateDefinition: definition,
            dimensionSettings: DimensionConfiguratorService.GetSettings(_project));
    }

    private void TryBeginModuleWallDragFromPending(double mouseX, double mouseY)
    {
        if (!_moduleWallDragPending || _moduleWallDragging)
            return;

        if (Mouse.LeftButton != MouseButtonState.Pressed)
            return;

        Vector delta = new Vector(mouseX - _moduleWallDragStartScreen.X, mouseY - _moduleWallDragStartScreen.Y);

        if (Math.Abs(delta.X) < ModuleWallDragThresholdPx &&
            Math.Abs(delta.Y) < ModuleWallDragThresholdPx)
            return;

        var module = _project.FindModule(_moduleWallDragModuleId);

        if (module == null || !module.AttachedWallId.HasValue)
        {
            _moduleWallDragPending = false;
            return;
        }

        _moduleWallDragPending = false;
        _moduleWallDragging = true;

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        Title = IsModuleCollisionBypassActive()
            ? $"Traços 3D - Movendo {definition.DisplayName} | Ctrl: sem colisão | direito troca parede | solte confirma | Esc cancela"
            : $"Traços 3D - Movendo {definition.DisplayName} | direito troca parede | solte confirma | Esc cancela";
        UpdateModuleWallDrag(mouseX, mouseY);
    }

    private void UpdateModuleWallDrag(double mouseX, double mouseY)
    {
        if (!_moduleWallDragging)
            return;

        var module = _project.FindModule(_moduleWallDragModuleId);

        if (module == null || _moduleWallDragWallId == Guid.Empty)
            return;

        EnsureCameraMatricesForPicking();

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        ModulePlacementResult? placement = ModulePlacementService.TryComputeFromScreenRay(
            mouseX,
            mouseY,
            Viewport.ActualWidth,
            Viewport.ActualHeight,
            _camera.View,
            _camera.Projection,
            _project.Room.Walls,
            BuildWallPickTargets(),
            definition,
            module.Width,
            module.Depth,
            module.Height,
            _moduleWallDragWallId,
            snapMountY: false,
            snapAlongWall: false);

        if (placement == null)
            return;

        ModulePlacementResult resolved = placement.Value;
        var wall = FindWallById(_moduleWallDragWallId);

        double mouseDeltaX = mouseX - _moduleWallDragLastMouseX;
        double mouseDeltaY = mouseY - _moduleWallDragLastMouseY;
        _moduleWallDragLastMouseX = mouseX;
        _moduleWallDragLastMouseY = mouseY;

        if (wall != null)
        {
            bool verticalDrag = Math.Abs(mouseDeltaY) >= Math.Abs(mouseDeltaX);
            bool horizontalDrag = Math.Abs(mouseDeltaX) > Math.Abs(mouseDeltaY);
            float alongInput = verticalDrag ? module.DistanceAlongWall : (horizontalDrag ? resolved.DistanceAlongWall : module.DistanceAlongWall);
            float rawMountY = resolved.Position.Y;
            float verticalDelta = rawMountY - _moduleWallDragLastMountY;
            (float snappedAlong, float snappedMountY) = ModuleWallFaceService.ApplyEdgeSnaps(
                alongInput,
                rawMountY,
                module.Width,
                module.Height,
                _moduleWallDragWallId,
                definition.IsWallMounted,
                module.Id,
                _project.Modules,
                verticalDelta,
                verticalDirectionHint: (float)mouseDeltaY,
                lockHorizontal: verticalDrag,
                wallFloorY: wall.FloorOffset);

            var innerFace = WallInnerFaceService.GetInnerFace(wall, _project.Room.Walls);
            resolved = ModulePlacementService.PlaceOnInsertionFace(
                wall,
                _project.Room.Walls,
                definition,
                module.Width,
                module.Depth,
                verticalDrag ? module.DistanceAlongWall : snappedAlong,
                innerFace.InteriorNormal,
                snappedMountY,
                module.Height);
        }

        if (WouldModulePlacementCollide(module, resolved))
        {
            Title = $"Traços 3D - Movendo {definition.DisplayName} | colisão | Ctrl sobrepõe | direito troca parede | Esc cancela";
            RefreshCollisionState();
            Viewport.InvalidateVisual();
            return;
        }

        Title = IsModuleCollisionBypassActive()
            ? $"Traços 3D - Movendo {definition.DisplayName} | Ctrl: sem colisão | direito troca parede | solte confirma | Esc cancela"
            : $"Traços 3D - Movendo {definition.DisplayName} | direito troca parede | solte confirma | Esc cancela";
        module.ApplyPlacement(
            resolved.Position,
            resolved.RotationYDegrees,
            definition,
            resolved.WallId,
            resolved.DistanceAlongWall,
            GetEffectiveDimensionSettings());

        _moduleWallDragLastMountY = module.Position.Y;

        wall = FindWallById(_moduleWallDragWallId);

        if (wall != null)
        {
            _moduleWallDragCotas = ModulePlacementService.ComputeWallCotasFromPlacement(
                wall,
                _project.Room.Walls,
                definition,
                module.Width,
                module.Height,
                resolved);

            SetStatusBarOverrides(context:
                $"Face: Parede   ·   Ant: {_moduleWallDragCotas.Value.Anterior:0} mm   Post: {_moduleWallDragCotas.Value.Posterior:0} mm   " +
                $"Base: {_moduleWallDragCotas.Value.Inferior:0} mm   Topo: {_moduleWallDragCotas.Value.Superior:0} mm");
        }

        UpdateModuleCotasPanel(module);
        RefreshCollisionState();
        Viewport.InvalidateVisual();
    }

    /// <summary>
    /// Promob — troca de plano de inserção durante o arraste:
    /// botão esquerdo pressionado + clique direito na nova parede.
    /// </summary>
    private bool TrySwitchModuleWallDragInsertionPlane(double mouseX, double mouseY)
    {
        if (!_moduleWallDragging)
            return false;

        var module = _project.FindModule(_moduleWallDragModuleId);

        if (module == null)
            return false;

        EnsureCameraMatricesForPicking();

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        ModulePlacementResult? placement = ModulePlacementService.TryComputeFromScreenRay(
            mouseX,
            mouseY,
            Viewport.ActualWidth,
            Viewport.ActualHeight,
            _camera.View,
            _camera.Projection,
            _project.Room.Walls,
            BuildWallPickTargets(),
            definition,
            module.Width,
            module.Depth,
            module.Height,
            restrictToWallId: null,
            snapMountY: false,
            snapAlongWall: false);

        if (placement == null || !placement.Value.WallId.HasValue)
        {
            SetStatusBarOverrides(hint: "Clique com o direito sobre a parede de destino para trocar o plano.");
            return true;
        }

        ModulePlacementResult resolved = placement.Value;

        if (resolved.WallId == _moduleWallDragWallId)
        {
            SetStatusBarOverrides(hint: "Módulo já está neste plano de inserção.");
            return true;
        }

        if (WouldModulePlacementCollide(module, resolved))
        {
            Title =
                $"Traços 3D - Movendo {definition.DisplayName} | colisão ao trocar parede | Ctrl sobrepõe | Esc cancela";
            SetStatusBarOverrides(hint: "Colisão na nova parede. Segure Ctrl e clique direito de novo para forçar.");
            RefreshCollisionState();
            Viewport.InvalidateVisual();
            return true;
        }

        _moduleWallDragWallId = resolved.WallId.Value;
        _moduleWallDragLastMouseX = mouseX;
        _moduleWallDragLastMouseY = mouseY;

        module.ApplyPlacement(
            resolved.Position,
            resolved.RotationYDegrees,
            definition,
            resolved.WallId,
            resolved.DistanceAlongWall,
            GetEffectiveDimensionSettings());

        _moduleWallDragLastMountY = module.Position.Y;

        var wall = FindWallById(_moduleWallDragWallId);

        if (wall != null)
        {
            _moduleWallDragCotas = ModulePlacementService.ComputeWallCotasFromPlacement(
                wall,
                _project.Room.Walls,
                definition,
                module.Width,
                module.Height,
                resolved);
            SetStatusBarOverrides(context:
                $"Plano trocado · Ant: {_moduleWallDragCotas.Value.Anterior:0} mm   Post: {_moduleWallDragCotas.Value.Posterior:0} mm   " +
                $"Base: {_moduleWallDragCotas.Value.Inferior:0} mm   Topo: {_moduleWallDragCotas.Value.Superior:0} mm");
        }

        UpdateModuleCotasPanel(module);
        Title =
            $"Traços 3D - Movendo {definition.DisplayName} | plano trocado | solte para confirmar | Esc cancela";
        RefreshCollisionState();
        Viewport.InvalidateVisual();
        return true;
    }

    private void CommitModuleWallDrag()
    {
        if (!_moduleWallDragging)
            return;

        _moduleWallDragging = false;
        _moduleWallDragPending = false;
        _moduleWallDragCotas = null;
        _moduleWallDragWallId = Guid.Empty;

        var module = _project.FindModule(_moduleWallDragModuleId);

        if (module == null)
            return;

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);

        if (_collisionEnabled && !IsModuleCollisionBypassActive() &&
            ModuleCollisionService.WouldCollide(module, _project.Modules))
        {
            RestoreModuleWallDragOriginal();
            SetStatusBarOverrides(hint: "Colisão — posição restaurada. Segure Ctrl para sobrepor módulos.");
        }
        else
        {
            MarkProjectDirty();
            UpdateModulePropertyPanel(module, definition);
            Title =
                $"Traços 3D - {definition.DisplayName} | L: {module.Width:0} A: {module.Height:0} P: {module.Depth:0} mm | R gira 90° | Delete remove";
        }

        RefreshCollisionState();
        Viewport.InvalidateVisual();
    }

    private bool CancelModuleWallDragIfActive()
    {
        if (!_moduleWallDragging)
            return false;

        RestoreModuleWallDragOriginal();
        _moduleWallDragging = false;
        _moduleWallDragPending = false;
        _moduleWallDragCotas = null;
        _moduleWallDragWallId = Guid.Empty;

        var module = _project.FindModule(_moduleWallDragModuleId);

        if (module != null)
        {
            var definition = ModuleCatalog.GetRequired(module.DefinitionId);
            UpdateModulePropertyPanel(module, definition);
            Title =
                $"Traços 3D - {definition.DisplayName} | L: {module.Width:0} A: {module.Height:0} P: {module.Depth:0} mm | R gira 90° | Delete remove";
        }

        RefreshCollisionState();
        Viewport.InvalidateVisual();
        return true;
    }

    private void RestoreModuleWallDragOriginal()
    {
        var module = _project.FindModule(_moduleWallDragModuleId);

        if (module == null)
            return;

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        module.ApplyPlacement(
            _moduleWallDragOriginalPosition,
            _moduleWallDragOriginalRotationY,
            definition,
            _moduleWallDragOriginalWallId,
            _moduleWallDragOriginalDistanceAlong,
            GetEffectiveDimensionSettings());
    }

    private void ClearModuleWallDragPending()
    {
        if (_moduleWallDragging)
            return;

        _moduleWallDragPending = false;
        _moduleWallDragCotas = null;
    }

    /// <summary>Duplo-clique: abre o grupo do módulo e seleciona a peça sob o cursor.</summary>
    private bool TryOpenModuleGroupAtScreen(double mouseX, double mouseY)
    {
        if (_project.Modules.Count == 0)
            return false;

        EnsureCameraMatricesForPicking();

        if (!Geometry3D.TryCreateWorldRay(
                mouseX, mouseY, Viewport.ActualWidth, Viewport.ActualHeight,
                _camera.View, _camera.Projection, out Vector3 origin, out Vector3 direction))
            return false;

        if (!ModulePickService.TryPickRay(origin, direction, GetPickableModules(), out var picked, out _) ||
            picked == null)
            return false;

        bool sameOpenModule = _openModuleGroupId == picked.Id;
        string? previousSelection = sameOpenModule ? _selectedPartLabel : null;
        SelectModule(picked);
        _openModuleGroupId = picked.Id;
        _selectedPartHandle = null;

        if (!ModulePartPickService.TryPickPart(origin, direction, picked, out string label, out _))
            _selectedPartLabel = null;
        else if (sameOpenModule && DrawerPartNaming.IsAssemblySelection(previousSelection) &&
                 DrawerPartNaming.BelongsToAssembly(label, previousSelection))
            // Segundo duplo-clique: entra nas peças da gaveta já selecionada.
            _selectedPartLabel = label;
        else if (DrawerPartNaming.TryGetAssembly(label, out string assembly))
            // Primeiro duplo-clique: seleciona a gaveta inteira.
            _selectedPartLabel = assembly;
        else
            _selectedPartLabel = label;

        UpdatePartSelectionStatus(picked);
        return true;
    }

    /// <summary>Clique numa seta de dimensão da peça: destaca e passa a ser o ponto de referência.</summary>
    private bool TryPickPartHandleInOpenGroup(double mouseX, double mouseY)
    {
        if (_openModuleGroupId == null || string.IsNullOrEmpty(_selectedPartLabel) ||
            DrawerPartNaming.IsAssemblySelection(_selectedPartLabel))
            return false;

        var module = _project.FindModule(_openModuleGroupId.Value);

        if (module == null)
            return false;

        EnsureCameraMatricesForPicking();

        if (!Geometry3D.TryCreateWorldRay(
                mouseX, mouseY, Viewport.ActualWidth, Viewport.ActualHeight,
                _camera.View, _camera.Projection, out Vector3 origin, out Vector3 direction))
            return false;

        if (!ModulePartHandleService.TryPickHandle(origin, direction, module, _selectedPartLabel!, out var handle))
            return false;

        // A seta define o eixo e a face (ponto de referência) do ajuste.
        _selectedPartHandle = handle;
        FocusPartDeltaBox(handle.Axis);
        Viewport.InvalidateVisual();
        return true;
    }

    private void FocusPartDeltaBox(PartHandleAxis axis)
    {
        bool swap = TryGetPartFaceWidthSwap(out _);
        // Destaque/foco na linha do painel (Largura/Espessura), não só no eixo geométrico.
        PartHandleAxis panelRow = axis switch
        {
            PartHandleAxis.Height => PartHandleAxis.Height,
            PartHandleAxis.Width => swap ? PartHandleAxis.Depth : PartHandleAxis.Width,
            PartHandleAxis.Depth => swap ? PartHandleAxis.Width : PartHandleAxis.Depth,
            _ => PartHandleAxis.Width
        };

        var box = panelRow switch
        {
            PartHandleAxis.Height => PartHeightDeltaBox,
            PartHandleAxis.Depth => PartDepthDeltaBox,
            _ => PartWidthDeltaBox
        };

        HighlightActivePartDeltaBox(axis);

        string eixo = panelRow switch
        {
            PartHandleAxis.Height => "altura",
            PartHandleAxis.Depth => swap ? "espessura" : "profundidade",
            _ => swap ? "largura (vão)" : "largura"
        };

        PartParametrizationHintText.Text =
            $"Seta de {eixo} selecionada — digite o ajuste (ex.: -20) e Aplicar.";
        box.Focus();
        box.SelectAll();
    }

    private static readonly System.Windows.Media.Brush PartDeltaActiveBrush =
        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD6, 0xF2, 0xD9));

    private void HighlightActivePartDeltaBox(PartHandleAxis? geometricAxis)
    {
        bool swap = TryGetPartFaceWidthSwap(out _);
        PartHandleAxis? panelRow = geometricAxis switch
        {
            null => null,
            PartHandleAxis.Height => PartHandleAxis.Height,
            PartHandleAxis.Width => swap ? PartHandleAxis.Depth : PartHandleAxis.Width,
            PartHandleAxis.Depth => swap ? PartHandleAxis.Width : PartHandleAxis.Depth,
            _ => geometricAxis
        };

        PartWidthDeltaBox.Background = panelRow == PartHandleAxis.Width
            ? PartDeltaActiveBrush : System.Windows.Media.Brushes.White;
        PartDepthDeltaBox.Background = panelRow == PartHandleAxis.Depth
            ? PartDeltaActiveBrush : System.Windows.Media.Brushes.White;
        PartHeightDeltaBox.Background = panelRow == PartHandleAxis.Height
            ? PartDeltaActiveBrush : System.Windows.Media.Brushes.White;
    }

    private bool TryGetPartFaceWidthSwap(out Vector3 dims)
    {
        dims = default;
        if (_openModuleGroupId == null || string.IsNullOrEmpty(_selectedPartLabel))
            return false;

        var module = _project.FindModule(_openModuleGroupId.Value);
        if (module == null ||
            !ModulePartDimensionService.TryComputeLocalDimensions(module, _selectedPartLabel, out dims))
            return false;

        return ModulePartAxisDisplay.FaceWidthIsDepth(_selectedPartLabel, dims);
    }

    /// <summary>Clique com grupo aberto: seleciona a peça individual sob o cursor.</summary>
    private bool TryPickPartInOpenGroup(double mouseX, double mouseY)
    {
        if (_openModuleGroupId == null)
            return false;

        var module = _project.FindModule(_openModuleGroupId.Value);

        if (module == null)
        {
            _openModuleGroupId = null;
            _selectedPartLabel = null;
            return false;
        }

        EnsureCameraMatricesForPicking();

        if (!Geometry3D.TryCreateWorldRay(
                mouseX, mouseY, Viewport.ActualWidth, Viewport.ActualHeight,
                _camera.View, _camera.Projection, out Vector3 origin, out Vector3 direction))
            return false;

        if (!ModulePartPickService.TryPickPart(origin, direction, module, out string label, out _))
            return false;

        if (DrawerPartNaming.IsAssemblySelection(_selectedPartLabel))
        {
            // Um clique troca a gaveta selecionada, mas não perfura o conjunto.
            if (DrawerPartNaming.TryGetAssembly(label, out string clickedAssembly))
                _selectedPartLabel = clickedAssembly;
            UpdatePartSelectionStatus(module);
            Viewport.InvalidateVisual();
            return true;
        }

        if (DrawerPartNaming.TryGetAssembly(_selectedPartLabel, out string openAssembly) &&
            !DrawerPartNaming.BelongsToAssembly(label, openAssembly))
            return true;

        _selectedModuleId = module.Id;
        _selectedModuleIds.Clear();
        _selectedModuleIds.Add(module.Id);
        _selectedPartLabel = label;
        _selectedPartHandle = null;
        UpdatePartSelectionStatus(module);
        SyncDimensionConfiguratorSelectionState();
        return true;
    }

    private void UpdatePartSelectionStatus(ModuleInstance module)
    {
        var definition = ModuleCatalog.GetRequired(module.DefinitionId);

        if (string.IsNullOrEmpty(_selectedPartLabel))
        {
            Title = $"Tra?os 3D - {definition.DisplayName} | Grupo aberto | Clique numa pe?a | Esc fecha";
            SetStatusBarOverrides(hint: "Grupo aberto — clique numa peça para selecioná-la.");
        }
        else if (DrawerPartNaming.IsAssemblySelection(_selectedPartLabel))
        {
            Title = $"Tra?os 3D - {definition.DisplayName} \u2192 {_selectedPartLabel} | Gaveta selecionada | Dois cliques entram nas peças";
            SetStatusBarOverrides(hint: $"{_selectedPartLabel} inteira — dois cliques entram nas peças; O oculta o conjunto.");
        }
        else
        {
            Title = $"Tra?os 3D - {definition.DisplayName} \u2192 {_selectedPartLabel} | Pe?a selecionada | Esc fecha grupo";
            SetStatusBarOverrides(hint: $"Peça: {_selectedPartLabel}");
        }

        if (DrawerPartNaming.IsAssemblySelection(_selectedPartLabel))
            UpdateModulePropertyPanel(module, definition);
        else
            UpdatePartPropertyPanel(module);
    }

    /// <summary>
    /// Com uma peça selecionada, o painel lateral exibe as dimensões da peça
    /// (não as do módulo), medidas nos eixos locais do módulo.
    /// </summary>
    private void UpdatePartPropertyPanel(ModuleInstance module)
    {
        if (string.IsNullOrEmpty(_selectedPartLabel))
        {
            PartParametrizationExpander.Visibility = Visibility.Collapsed;
            return;
        }

        if (!ModulePartDimensionService.TryComputeLocalDimensions(module, _selectedPartLabel, out var dims))
        {
            PartParametrizationExpander.Visibility = Visibility.Collapsed;
            return;
        }

        _syncingPropertyPanel = true;

        PropertyLengthLabel.Text = "Largura peça (mm)";
        PropertyHeightLabel.Text = "Altura peça (mm)";
        PropertyDepthLabel.Text = "Profundidade peça (mm)";

        PropertyLengthBox.Text = dims.X.ToString("0", CultureInfo.InvariantCulture);
        PropertyHeightBox.Text = dims.Y.ToString("0", CultureInfo.InvariantCulture);
        PropertyDepthBox.Text = dims.Z.ToString("0", CultureInfo.InvariantCulture);

        PropertyHintText.Text = $"Peça \u201c{_selectedPartLabel}\u201d — dimensões atuais em mm.";

        // Painel "Seta" (dimensão + ajuste −/+) da peça.
        PartParametrizationExpander.Visibility = Visibility.Visible;

        bool swap = ModulePartAxisDisplay.FaceWidthIsDepth(_selectedPartLabel, dims);
        PartWidthLabel.Text = ModulePartAxisDisplay.WidthLabel(swap);
        PartDepthLabel.Text = ModulePartAxisDisplay.DepthLabel(swap);

        // Coluna de dimensão = valor efetivo (já com o ajuste acumulado).
        // Porta esq.: Largura = vão (Z), Espessura = X.
        PartWidthValueBox.Text = ModulePartAxisDisplay.WidthValue(dims, swap)
            .ToString("0", CultureInfo.InvariantCulture);
        PartDepthValueBox.Text = ModulePartAxisDisplay.DepthValue(dims, swap)
            .ToString("0", CultureInfo.InvariantCulture);
        PartHeightValueBox.Text = dims.Y.ToString("0", CultureInfo.InvariantCulture);

        // Campo "+" = ajuste acumulado da face (norte visual); não zerar após Aplicar.
        PartHandleAxis widthAxis = ModulePartAxisDisplay.PanelWidthAxis(swap);
        PartHandleAxis depthAxis = ModulePartAxisDisplay.PanelDepthAxis(swap);
        PartWidthDeltaBox.Text = FormatPartDeltaDisplay(
            ModulePartEditService.GetDisplayOffsetForAxis(
                module, _selectedPartLabel, widthAxis, PreferredPositiveForAxis(widthAxis)));
        PartDepthDeltaBox.Text = FormatPartDeltaDisplay(
            ModulePartEditService.GetDisplayOffsetForAxis(
                module, _selectedPartLabel, depthAxis, PreferredPositiveForAxis(depthAxis)));
        PartHeightDeltaBox.Text = FormatPartDeltaDisplay(
            ModulePartEditService.GetDisplayOffsetForAxis(
                module, _selectedPartLabel, PartHandleAxis.Height,
                PreferredPositiveForAxis(PartHandleAxis.Height)));

        PartParametrizationHintText.Text = swap
            ? $"Peça \u201c{_selectedPartLabel}\u201d — Largura = vão; o campo + guarda o ajuste (ex.: -150)."
            : $"Peça \u201c{_selectedPartLabel}\u201d — o campo + mantém o ajuste acumulado (ex.: -150).";

        HighlightActivePartDeltaBox(_selectedPartHandle?.Axis);

        _syncingPropertyPanel = false;
    }

    private bool? PreferredPositiveForAxis(PartHandleAxis axis) =>
        _selectedPartHandle is { } h && h.Axis == axis ? h.Positive : null;

    private static string FormatPartDeltaDisplay(float offsetMm) =>
        offsetMm.ToString("0", CultureInfo.InvariantCulture);

    private void ApplyPartDeltasFromPanel()
    {
        if (_syncingPropertyPanel || _openModuleGroupId == null || string.IsNullOrEmpty(_selectedPartLabel))
            return;

        if (_selectedPartHandle == null)
        {
            PartParametrizationHintText.Text = "Clique numa seta no 3D (ou foque o campo +) para escolher o lado e depois aplique o ajuste.";
            return;
        }

        var module = _project.FindModule(_openModuleGroupId.Value);

        if (module == null || !SceneModuleVisibilityService.IsEditable(module))
            return;

        bool swap = ModulePartDimensionService.TryComputeLocalDimensions(module, _selectedPartLabel, out var dims)
                    && ModulePartAxisDisplay.FaceWidthIsDepth(_selectedPartLabel, dims);

        // O incremento vem da linha do painel correspondente ao eixo geométrico da seta.
        var handle = _selectedPartHandle.Value;
        var activeBox = handle.Axis switch
        {
            PartHandleAxis.Height => PartHeightDeltaBox,
            PartHandleAxis.Width => swap ? PartDepthDeltaBox : PartWidthDeltaBox,
            PartHandleAxis.Depth => swap ? PartWidthDeltaBox : PartDepthDeltaBox,
            _ => PartWidthDeltaBox
        };

        if (!TryParseDeltaField(activeBox.Text, out float? absoluteOffset) || absoluteOffset == null)
        {
            PartParametrizationHintText.Text = "Informe o ajuste em mm (ex.: -150, 0, +18) e pressione Enter.";
            return;
        }

        var definition = ModuleCatalog.GetRequired(module.DefinitionId);

        // Valor do campo = deslocamento acumulado desejado (não incremento a somar de novo).
        if (!ModulePartEditService.TrySetFaceOffset(
                module, _selectedPartLabel, handle, absoluteOffset.Value, out string? error))
        {
            PartParametrizationHintText.Text = error ?? "Não foi possível aplicar o ajuste nesta peça.";
            return;
        }

        // Rebuild com settings em cache do módulo (Canto L respeita PartOverrides).
        module.RebuildMesh(definition, DimensionConfiguratorService.GetSettings(_project));
        MarkProjectDirty();
        RefreshCollisionState();
        Viewport.InvalidateVisual();

        UpdatePartPropertyPanel(module);
        SetStatusBarOverrides(hint: $"Ajuste {FormatPartDeltaDisplay(absoluteOffset.Value)} mm na peça {_selectedPartLabel}.");
    }

    private static bool TryParseDeltaField(string text, out float? delta)
    {
        delta = null;
        string trimmed = (text ?? string.Empty).Trim();

        if (trimmed.Length == 0 || trimmed == "0")
        {
            delta = 0f;
            return true;
        }

        if (float.TryParse(trimmed, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
            float.TryParse(trimmed, System.Globalization.NumberStyles.Float, CultureInfo.CurrentCulture, out value))
        {
            delta = value;
            return true;
        }

        return false;
    }

    private void DeleteSelectedModules()
    {
        var idsToDelete = _selectedModuleIds.Count > 0
            ? _selectedModuleIds.ToList()
            : _selectedModuleId.HasValue
                ? [_selectedModuleId.Value]
                : [];

        if (idsToDelete.Count == 0)
            return;

        var modulesToDelete = idsToDelete
            .Select(id => _project.FindModule(id))
            .Where(module => module != null)
            .Cast<ModuleInstance>()
            .ToList();

        if (!SceneModuleVisibilityService.CanDeleteSelection(modulesToDelete))
        {
            SetStatusBarOverrides(hint: "Módulo bloqueado — desbloqueie na aba Ambiente para excluir.");
            return;
        }

        _project.Modules.RemoveAll(module => idsToDelete.Contains(module.Id));
        _selectedModuleId = null;
        _selectedModuleIds.Clear();
        ClearPropertyPanelSelection();
        RefreshSceneModuleList();
        Title = idsToDelete.Count == 1
            ? "Tra?os 3D - M?dulo removido"
            : $"Tra?os 3D - {idsToDelete.Count} m?dulos removidos";
        MarkProjectDirty();
        RefreshStatusBarAfterViewChange();
        SetStatusBarOverrides(hint: idsToDelete.Count == 1
            ? "Módulo excluído."
            : $"{idsToDelete.Count} módulos excluídos.");
        SyncDimensionConfiguratorSelectionState();
    }

    private void ModuleWardrobeButton_Click(object sender, RoutedEventArgs e) =>
        ConsumeModuleLibraryClick();

    private void ModuleNightstandButton_Click(object sender, RoutedEventArgs e) =>
        ConsumeModuleLibraryClick();

    private void ModuleChestButton_Click(object sender, RoutedEventArgs e) =>
        ConsumeModuleLibraryClick();

    // groupSelection=true  ? clique no topo ? seleciona grupo (todas as paredes em vermelho, edi??es aplicam a todas)
    // groupSelection=false ? clique na face ? seleciona parede individual
    private void SelectWall(WallSegment wall, bool groupSelection = false)
    {
        _floorSelected = false;
        _selectedFloorZoneId = null;
        _wallGroupSelected = groupSelection;
        _selectedWallId = wall.Id;
        _selectedOpeningId = null;
        _selectedModuleId = null;
        _selectedModuleIds.Clear();
        UpdateSelectedWallStatus(wall);
        SyncDimensionConfiguratorSelectionState();
    }

    private void SelectFloor(Guid? zoneId = null)
    {
        if (_project.Room.Floor == null)
            return;

        _floorSelected = true;
        _selectedFloorZoneId = zoneId;
        _wallGroupSelected = false;
        _selectedWallId = null;
        _selectedOpeningId = null;
        _selectedModuleId = null;
        _selectedModuleIds.Clear();
        MeasureBox.Visibility = Visibility.Collapsed;

        UpdateFloorPropertyPanel();
        UpdateFloorStatus();
        SyncDimensionConfiguratorSelectionState();
    }

    private void SelectFloorZone(FloorZone zone)
    {
        SelectFloor(zone.Id);
        FloorRegionsExpander.IsExpanded = true;
    }

    private void UpdateFloorPropertyPanel()
    {
        _syncingPropertyPanel = true;

        HideWallConstructionPanel();
        WallPropertiesPanel.Visibility = Visibility.Collapsed;
        ModulePropertiesPanel.Visibility = Visibility.Collapsed;
        FloorPropertiesPanel.Visibility = Visibility.Visible;

        if (_project.Room.TryGetFloorBounds(out Vector2 min, out Vector2 max))
        {
            FloorWidthBox.Text = (max.X - min.X).ToString("0", CultureInfo.InvariantCulture);
            FloorDepthBox.Text = (max.Y - min.Y).ToString("0", CultureInfo.InvariantCulture);
        }
        else
        {
            FloorWidthBox.Text = "";
            FloorDepthBox.Text = "";
        }

        var (cols, rows, _, _) = _project.Room.TryGetFloorBounds(out Vector2 gmin, out Vector2 gmax)
            ? GridLayoutService.ComputeUniformDivisions(gmin, gmax, GridStep)
            : (0, 0, 0f, 0f);

        var floor = _project.Room.Floor!;
        FloorShowGridCheck.IsChecked = _project.Room.ShowFloorGrid;

        string materialId = _selectedFloorZoneId.HasValue
            ? floor.Zones.FirstOrDefault(z => z.Id == _selectedFloorZoneId.Value)?.MaterialId ?? floor.DefaultMaterialId
            : floor.DefaultMaterialId;

        var selectedMaterial = FloorMaterialCatalog.TryGet(materialId, out var mat) && mat != null
            ? mat
            : FloorMaterialCatalog.GetDefault();

        FloorMaterialCombo.SelectedItem = selectedMaterial;

        if (_selectedFloorZoneId.HasValue)
        {
            var zone = floor.Zones.FirstOrDefault(z => z.Id == _selectedFloorZoneId.Value);

            if (zone != null)
            {
                FloorHintText.Text =
                    $"Região «{zone.Name}»: {FloorZoneGeometry.FormatSummary(zone)} " +
                    $"({FloorZoneService.AreaSquareMeters(zone):0.##} m²).";
            }
        }
        else
        {
            FloorHintText.Text = cols > 0
                ? $"Piso base. Grade: {cols}?{rows}."
                : "Piso base selecionado.";
        }

        if (floor.Zones.Count == 0)
        {
            FloorZonesSummaryText.Text = "Nenhuma região extra — use Regiões no painel.";
        }
        else
        {
            FloorZonesSummaryText.Text = string.Join("\n",
                floor.Zones.Select(z =>
                {
                    var name = FloorMaterialCatalog.TryGet(z.MaterialId, out var zm) && zm != null
                        ? zm.DisplayName
                        : z.MaterialId;
                    return $"• {z.Name}: {FloorZoneGeometry.FormatSummary(z)} — {name}";
                }));
        }

        PopulateFloorZoneSelector();
        UpdateFloorRegionsSummary();

        bool edgeOffset = GetSelectedFloorZone()?.Shape == WallRegionShape.Rectangular;
        FloorAddRegionButton.IsEnabled = true;
        FloorAddCircleRegionButton.IsEnabled = true;
        FloorAddPolygonRegionButton.IsEnabled = true;
        FloorDeleteZoneButton.IsEnabled = _selectedFloorZoneId.HasValue;
        FloorRegionOffsetStartAlongBox.IsEnabled = edgeOffset && FloorZoneSelectorCombo.IsEnabled;
        FloorRegionOffsetEndAlongBox.IsEnabled = edgeOffset && FloorZoneSelectorCombo.IsEnabled;
        FloorRegionOffsetBottomBox.IsEnabled = edgeOffset && FloorZoneSelectorCombo.IsEnabled;
        FloorRegionOffsetTopBox.IsEnabled = edgeOffset && FloorZoneSelectorCombo.IsEnabled;

        _syncingPropertyPanel = false;
    }

    private void UpdateFloorStatus()
    {
        if (!_project.Room.TryGetFloorBounds(out Vector2 min, out Vector2 max))
            return;

        float width = max.X - min.X;

        if (_selectedFloorZoneId.HasValue)
        {
            var zone = _project.Room.Floor?.Zones.FirstOrDefault(z => z.Id == _selectedFloorZoneId.Value);

            if (zone != null)
            {
                var matName = FloorMaterialCatalog.TryGet(zone.MaterialId, out var zm) && zm != null
                    ? zm.DisplayName
                    : zone.MaterialId;

                Title = $"Tra?os 3D - {zone.Name} | {zone.Width:0}?{zone.Depth:0} mm | {matName}";
                UpdateStatusBarSelection(zone.Name, zone.Width);
                return;
            }
        }

        var baseMat = FloorMaterialCatalog.TryGet(_project.Room.Floor!.DefaultMaterialId, out var bm) && bm != null
            ? bm.DisplayName
            : "Piso";

        Title = $"Tra?os 3D - Piso | {width:0}?{max.Y - min.Y:0} mm | {baseMat}";
        UpdateStatusBarSelection("Piso", width);
    }

    private void CancelWallMode()
    {
        if (_wallChamferMode)
            CancelWallChamferMode();

        // Só sincroniza draft→sala quando estávamos desenhando paredes.
        // Caso contrário (ex.: começar inserção de módulo), SyncRoomFromDraft
        // sobrescrevia o ambiente padrão L e apagava a segunda parede.
        if (!_wallMode)
        {
            _wallAppendMode = false;
            _hasLastPoint = false;
            _hasPreview = false;
            ClearWallReferenceState();
            MeasureBox.Visibility = Visibility.Collapsed;
            return;
        }

        bool wasAppend = _wallAppendMode;

        if (wasAppend)
        {
            if (_wallDraft.BuildWalls().Count > 0)
                AppendDraftWallsToRoom();
        }
        else if (_wallDraft.BuildWalls().Count > 0)
        {
            SyncRoomFromDraft();
        }

        _wallMode = false;
        _wallAppendMode = false;
        _hasLastPoint = false;
        _hasPreview = false;
        ClearWallReferenceState();

        MeasureBox.Visibility = Visibility.Collapsed;
        Keyboard.Focus(this);

        if (_project.Room.Walls.Count > 0)
        {
            if (!wasAppend)
                _project.Room.RebuildAutomaticFloor();

            SelectWall(_project.Room.Walls[^1]);
            return;
        }

        if (_wallEditorActive)
            UpdateWallEditorStatus();
        else
        {
            Title = "Tra?os 3D";
            UpdateViewTitle();
        }
    }

    private void ClearWallReferenceState()
    {
        _wallReferencePending = false;
        _hasWallReferencePick = false;
        _wallReferenceOffsetPreview = 0f;
        _wallReferencePick = default;
    }

    private void AppendDraftWallsToRoom()
    {
        var newWalls = _wallDraft.BuildWalls();

        if (newWalls.Count == 0)
            return;

        _project.Room.AppendPartitionWalls(newWalls);
    }

    private void ConfirmWallReferenceOffset(float measureMm)
    {
        float sign = Math.Sign(_wallReferenceOffsetPreview);

        if (sign == 0f)
            sign = 1f;

        float signedOffset = Math.Abs(measureMm) * sign;
        Vector2 start = WallReferenceService.ComputeDraftStartReferenceCorner(_wallReferencePick, signedOffset);

        _wallReferencePending = false;
        _hasWallReferencePick = false;

        _wallDraft.Reset();
        _wallDraft.Thickness = DefaultWallThickness;
        _wallDraft.Height = DefaultWallHeight;

        var referenceWall = FindWallById(_wallReferencePick.WallId);

        if (referenceWall != null)
        {
            _wallDraft.Thickness = referenceWall.Thickness;
            _wallDraft.Height = referenceWall.Height;
            _wallDraft.Orientation = referenceWall.Orientation;
            _wallDraft.MeasureSide = referenceWall.MeasureSide;
        }

        _wallDraft.Start(start);
        _lastPoint = start;
        _previewPoint = start;
        _hasLastPoint = true;
        _hasPreview = false;

        ShowMeasureBox();
        UpdateWallModeTitle();
        MarkProjectDirty();
    }

    private void BeginWallMove(WallSegment wall, Vector2 floorPoint)
    {
        _wallMoveDragging = true;
        _wallMoveWallId = wall.Id;
        _wallMoveDragStartFloor = floorPoint;
        _wallMoveOriginalStart = wall.Start;
        _wallMoveOriginalEnd = wall.End;
        _wallMovePreviewDelta = Vector2.Zero;
        Title = "Tra?os 3D - Movendo parede: 0 mm | solte para confirmar | Esc cancela";
    }

    private void UpdateWallMovePreview(Vector2 currentFloor)
    {
        var wall = FindWallById(_wallMoveWallId);

        if (wall == null)
            return;

        currentFloor = Snap(currentFloor, 100);
        _wallMovePreviewDelta = WallMoveService.ComputePerpendicularDragDelta(
            wall,
            _wallMoveDragStartFloor,
            currentFloor);

        wall.Start = _wallMoveOriginalStart + _wallMovePreviewDelta;
        wall.End = _wallMoveOriginalEnd + _wallMovePreviewDelta;

        float offset = MathF.Abs(WallMoveService.ComputeSignedOffsetMm(wall, _wallMovePreviewDelta));
        Title = $"Tra?os 3D - Movendo parede: {offset:0} mm | solte para confirmar | Esc cancela";
    }

    private void CommitWallMove()
    {
        if (!_wallMoveDragging)
            return;

        _wallMoveDragging = false;

        var wall = FindWallById(_wallMoveWallId);

        if (wall != null)
        {
            UpdateSelectedWallStatus(wall);
            MarkProjectDirty();
        }

        _wallMoveWallId = Guid.Empty;
        _wallMovePreviewDelta = Vector2.Zero;
    }

    private bool CancelWallMoveIfActive()
    {
        if (!_wallMoveDragging)
            return false;

        RestoreWallMoveOriginal();
        _wallMoveDragging = false;
        _wallMoveWallId = Guid.Empty;
        _wallMovePreviewDelta = Vector2.Zero;

        if (_selectedWallId.HasValue)
        {
            var wall = FindWallById(_selectedWallId.Value);

            if (wall != null)
                UpdateSelectedWallStatus(wall);
        }

        return true;
    }

    private void RestoreWallMoveOriginal()
    {
        var wall = FindWallById(_wallMoveWallId);

        if (wall == null)
            return;

        wall.Start = _wallMoveOriginalStart;
        wall.End = _wallMoveOriginalEnd;
    }

    private void UndoLastWall()
    {
        if (_wallDraft.State == WallDraftState.Idle && _wallDraft.Points.Count == 0)
            return;

        if (!_wallDraft.UndoLastConfirmedPoint())
        {
            if (!_wallAppendMode)
            {
                _project.Room.Clear();
            }
            else if (_wallDraft.Points.Count == 0)
            {
                _wallReferencePending = true;
                _hasWallReferencePick = false;
                _hasLastPoint = false;
            }
            else
            {
                _hasLastPoint = false;
            }
        }
        else
        {
            _lastPoint = _wallDraft.Points[^1];
            _previewPoint = _lastPoint;
            _hasLastPoint = true;
        }

        _hasPreview = false;

        MeasureBox.Text = "";
        MeasureBox.Focus();
        MarkProjectDirty();
    }

    private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Keyboard.Focus(this);

        var position = e.GetPosition(Viewport);

        if (TryHandleMaterialCopyViewportClick(position.X, position.Y))
        {
            e.Handled = true;
            return;
        }

        if (_openingInsertMode != OpeningInsertMode.None)
        {
            TryInsertOpeningAt(position.X, position.Y);
            return;
        }

        if (_moduleInsertDefinitionId != null)
        {
            // Inserção por arrasto da biblioteca: soltar o botão confirma (paridade Promob).
            return;
        }

        // Duplo-clique num módulo abre o grupo para edição por peça.
        if (e.ClickCount == 2 && !_wallMode && _openingInsertMode == OpeningInsertMode.None)
        {
            if (TryOpenModuleGroupAtScreen(position.X, position.Y))
            {
                Viewport.Focus();
                return;
            }
        }

        if (_floorPolygonRegionPickMode && _project.Room.Floor != null)
        {
            if (TryPickFloorAtScreen(position.X, position.Y, out Vector2 floorHit))
            {
                float x = floorHit.X;
                float y = floorHit.Y;

                if (_floorPolygonPickX.Count >= FloorZoneService.MinPolygonVertices &&
                    WallRegionGeometry.IsNearFirstPolygonVertex(
                        _floorPolygonPickX,
                        _floorPolygonPickY,
                        x,
                        y,
                        WallRegionGeometry.CloseVertexToleranceMm))
                {
                    if (!TryCommitFloorPolygonRegion(out string? closeError))
                        Title = closeError ?? "Traços 3D - Polígono inválido | Esc cancela";
                }
                else if (_floorPolygonPickX.Count == 0)
                {
                    _floorPolygonPickX.Add(x);
                    _floorPolygonPickY.Add(y);
                    _floorPolygonPreviewX = x;
                    _floorPolygonPreviewY = y;
                    Title = GetFloorPolygonRegionPickTitle();
                    ShowMeasureBox();
                }
                else
                {
                    _floorPolygonPickX.Add(x);
                    _floorPolygonPickY.Add(y);
                    _floorPolygonPreviewX = x;
                    _floorPolygonPreviewY = y;
                    Title = GetFloorPolygonRegionPickTitle();
                }
            }
            else
                Title = "Traços 3D - Clique no piso | Esc cancela";

            Viewport.Focus();
            return;
        }

        if (_floorCircleRegionPickMode && _project.Room.Floor != null)
        {
            if (TryPickFloorAtScreen(position.X, position.Y, out Vector2 center))
            {
                if (FloorZoneService.TryAddCircleZone(
                        _project.Room.Floor,
                        center.X,
                        center.Y,
                        FloorZoneService.DefaultCircleRadiusMm,
                        out var zone,
                        out string? circleError) &&
                    zone != null)
                {
                    _floorCircleRegionPickMode = false;
                    MarkProjectDirty();
                    SelectFloorZone(zone);
                }
                else
                    Title = circleError ?? "Traços 3D - Região circular inválida | Esc cancela";
            }
            else
                Title = "Traços 3D - Clique no piso | Esc cancela";

            Viewport.Focus();
            return;
        }

        if (_floorZoneDrawMode && _project.Room.Floor != null)
        {
            Vector2 zonePoint = Snap(ScreenToFloor(position.X, position.Y), 100);

            if (!_hasFloorZoneStart)
            {
                _floorZoneStart = zonePoint;
                _floorZonePreview = zonePoint;
                _hasFloorZoneStart = true;
                Title = "Traços 3D - Região retangular: 2º canto oposto | Esc cancela";
                return;
            }

            float minX = MathF.Min(_floorZoneStart.X, zonePoint.X);
            float maxX = MathF.Max(_floorZoneStart.X, zonePoint.X);
            float minY = MathF.Min(_floorZoneStart.Y, zonePoint.Y);
            float maxY = MathF.Max(_floorZoneStart.Y, zonePoint.Y);

            string materialId = FloorZoneMaterialCombo.SelectedItem is FloorMaterialDefinition selectedMat
                ? selectedMat.Id
                : FloorMaterialCatalog.DefaultMaterialId;

            if (FloorZoneService.TryAddRectZone(
                    _project.Room.Floor,
                    minX,
                    maxX,
                    minY,
                    maxY,
                    out var zone,
                    out string? zoneError) &&
                zone != null)
            {
                zone.MaterialId = materialId;
                zone.Name = $"Região {_project.Room.Floor.Zones.Count}";
                MarkProjectDirty();
                CancelFloorZoneDrawMode();
                SelectFloorZone(zone);
            }
            else
            {
                Title = zoneError ?? "Traços 3D - Região fora do piso ou inválida | tente novamente";
            }

            Viewport.Focus();
            return;
        }

        if (_wallEditorActive && _wallEditorDimensionTool != WallEditorDimensionTool.None)
        {
            HandleManualDimensionClick(ScreenToFloor(position.X, position.Y));
            Viewport.Focus();
            return;
        }

        if (_wallFlechaHotpointMode)
        {
            TryBeginWallFlechaDragAtScreen(position.X, position.Y);
            Viewport.Focus();
            return;
        }

        if (_wallJunctionMode)
        {
            HandleWallJunctionClick(position.X, position.Y);
            Viewport.Focus();
            return;
        }

        if (_wallHorizontalBandPickMode && _selectedWallId.HasValue)
        {
            string? bandError = null;

            if (TryPickWallFaceAtScreen(position.X, position.Y, out var bandWall, out _, out float height, out _, out bool hitTop) &&
                !hitTop &&
                bandWall.Id == _selectedWallId.Value)
            {
                if (_wallHorizontalBandPickStep == 0)
                {
                    _wallHorizontalBandPickHeight1 = height;
                    _wallHorizontalBandPickStep = 1;
                    _wallHorizontalBandPreviewHeight2 = height;
                    Title = "Traços 3D - Faixa horizontal: clique a segunda altura | Esc cancela";
                }
                else
                {
                    float bottom = MathF.Min(_wallHorizontalBandPickHeight1, height);
                    float top = MathF.Max(_wallHorizontalBandPickHeight1, height);

                    if (WallBandService.TryAddHorizontalBand(bandWall, bottom, top, out _, out bandError))
                    {
                        CancelWallHorizontalBandPickMode();
                        MarkProjectDirty();
                        UpdateWallPropertyPanel(bandWall);
                        RefreshWallBandsEditor();
                    }
                    else
                        Title = bandError ?? "Traços 3D - Faixa horizontal inválida | Esc cancela";
                }
            }
            else
                Title = "Traços 3D - Clique na face da parede selecionada | Esc cancela";

            Viewport.Focus();
            return;
        }

        if (_wallVerticalBandPickMode && _selectedWallId.HasValue)
        {
            string? bandError = null;

            if (TryPickWallFaceAtScreen(position.X, position.Y, out var bandWall, out float along, out _, out _, out bool hitTop) &&
                !hitTop &&
                bandWall.Id == _selectedWallId.Value)
            {
                if (_wallVerticalBandPickStep == 0)
                {
                    _wallVerticalBandPickAlong1 = along;
                    _wallVerticalBandPickStep = 1;
                    _wallVerticalBandPreviewAlong = along;
                    Title = "Traços 3D - Faixa vertical: clique a segunda posição | Esc cancela";
                }
                else
                {
                    float start = MathF.Min(_wallVerticalBandPickAlong1, along);
                    float end = MathF.Max(_wallVerticalBandPickAlong1, along);

                    if (WallBandService.TryAddVerticalBand(bandWall, start, end, out _, out bandError))
                    {
                        CancelWallVerticalBandPickMode();
                        MarkProjectDirty();
                        UpdateWallPropertyPanel(bandWall);
                        RefreshWallBandsEditor();
                    }
                    else
                        Title = bandError ?? "Traços 3D - Faixa vertical inválida | Esc cancela";
                }
            }
            else
                Title = "Traços 3D - Clique na face da parede selecionada | Esc cancela";

            Viewport.Focus();
            return;
        }

        if (_wallPolygonRegionPickMode && _selectedWallId.HasValue)
        {
            if (TryPickWallFaceAtScreen(position.X, position.Y, out var polyWall, out float along, out float height, out FaceType face, out bool hitTop) &&
                !hitTop &&
                polyWall.Id == _selectedWallId.Value &&
                face == _wallRegionPickFace)
            {
                if (_wallPolygonPickAlong.Count >= WallRegionService.MinPolygonVertices &&
                    WallRegionGeometry.IsNearFirstPolygonVertex(
                        _wallPolygonPickAlong,
                        _wallPolygonPickHeight,
                        along,
                        height,
                        WallRegionGeometry.CloseVertexToleranceMm))
                {
                    if (!TryCommitWallPolygonRegion(polyWall, out string? closeError))
                        Title = closeError ?? "Traços 3D - Polígono inválido | Esc cancela";
                }
                else if (_wallPolygonPickAlong.Count == 0)
                {
                    _wallPolygonPickAlong.Add(along);
                    _wallPolygonPickHeight.Add(height);
                    _wallPolygonPreviewAlong = along;
                    _wallPolygonPreviewHeight = height;
                    Title = GetWallPolygonRegionPickTitle();
                    ShowMeasureBox();
                }
                else
                {
                    _wallPolygonPickAlong.Add(along);
                    _wallPolygonPickHeight.Add(height);
                    _wallPolygonPreviewAlong = along;
                    _wallPolygonPreviewHeight = height;
                    Title = GetWallPolygonRegionPickTitle();
                }
            }
            else
            {
                string faceLabel = _wallRegionPickFace == FaceType.Internal ? "interna" : "externa";
                Title = $"Traços 3D - Clique na face {faceLabel} da parede selecionada | Esc cancela";
            }

            Viewport.Focus();
            return;
        }

        if (_wallCircleRegionPickMode && _selectedWallId.HasValue)
        {
            if (TryPickWallFaceAtScreen(position.X, position.Y, out var circleWall, out float along, out float height, out FaceType face, out bool hitTop) &&
                !hitTop &&
                circleWall.Id == _selectedWallId.Value &&
                face == _wallRegionPickFace)
            {
                if (WallRegionService.TryAddCircleRegion(
                        circleWall,
                        _wallRegionPickFace,
                        along,
                        height,
                        WallRegionService.DefaultCircleRadiusMm,
                        out _,
                        out string? circleError))
                {
                    _wallCircleRegionPickMode = false;
                    MarkProjectDirty();
                    UpdateWallPropertyPanel(circleWall);
                }
                else
                    Title = circleError ?? "Traços 3D - Região circular inválida | Esc cancela";
            }
            else
            {
                string faceLabel = _wallRegionPickFace == FaceType.Internal ? "interna" : "externa";
                Title = $"Traços 3D - Clique na face {faceLabel} da parede selecionada | Esc cancela";
            }

            Viewport.Focus();
            return;
        }

        if (_wallRegionPickMode && _selectedWallId.HasValue)
        {
            if (TryPickWallFaceAtScreen(position.X, position.Y, out var regionWall, out float along, out float height, out FaceType face, out bool hitTop) &&
                !hitTop &&
                regionWall.Id == _selectedWallId.Value &&
                face == _wallRegionPickFace)
            {
                if (_wallRegionPickStep == 0)
                {
                    _wallRegionPickAlong1 = along;
                    _wallRegionPickHeight1 = height;
                    _wallRegionPickStep = 1;
                    Title = "Traços 3D - Região: clique o canto oposto | Esc cancela";
                }
                else
                {
                    _wallRegionPickAlong2 = along;
                    _wallRegionPickHeight2 = height;

                    float startAlong = MathF.Min(_wallRegionPickAlong1, _wallRegionPickAlong2);
                    float endAlong = MathF.Max(_wallRegionPickAlong1, _wallRegionPickAlong2);
                    float bottom = MathF.Min(_wallRegionPickHeight1, _wallRegionPickHeight2);
                    float top = MathF.Max(_wallRegionPickHeight1, _wallRegionPickHeight2);

                    if (WallRegionService.TryAddRectRegion(
                        regionWall,
                        _wallRegionPickFace,
                        startAlong,
                        endAlong,
                        bottom,
                        top,
                        out _,
                        out string? regionError))
                    {
                        _wallRegionPickMode = false;
                        _wallRegionPickStep = 0;
                        MarkProjectDirty();
                        UpdateWallPropertyPanel(regionWall);
                    }
                    else
                    {
                        Title = regionError ?? "Traços 3D - Região inválida | Esc cancela";
                    }
                }
            }
            else
            {
                string faceLabel = _wallRegionPickFace == FaceType.Internal ? "interna" : "externa";
                Title = $"Traços 3D - Clique na face {faceLabel} da parede selecionada | Esc cancela";
            }

            Viewport.Focus();
            return;
        }

        if (_wallChamferMode)
        {
            TryApplyWallChamferAtScreen(position.X, position.Y);
            Viewport.Focus();
            return;
        }

        if (_wallSegmentPickMode)
        {
            if (TryPickWallAtScreen(position.X, position.Y, out var segmentWall, out float distanceAlong, out bool hitTop) &&
                !hitTop &&
                segmentWall.Id == _wallSegmentTargetId &&
                WallSegmentationService.TrySplit(segmentWall, distanceAlong, out var segments))
            {
                ApplyWallSegmentation(segmentWall, distanceAlong, segments);
            }
            else
            {
                Title = "Tra?os 3D - Ponto inv?lido (muito perto da porta ou da extremidade) | Esc cancela";
            }

            Viewport.Focus();
            return;
        }

        if (_wall304050PickMovingMode && _selectedWallId.HasValue)
        {
            if (TryPickWallAtScreen(position.X, position.Y, out var pickWall, out _, out bool hitTop) &&
                !hitTop &&
                pickWall.Id != _selectedWallId.Value)
            {
                var refWall = FindWallById(_selectedWallId.Value);

                if (refWall != null &&
                    WallThirtyFortyFiftyService.TryFindCorner(refWall, pickWall, out _))
                {
                    _wall304050MovingWallId = pickWall.Id;
                    _wall304050PickMovingMode = false;
                    UpdateWallPropertyPanel(refWall);
                    Title = "Tra?os 3D - Parede deslocada selecionada | Aplicar 30-40-50";
                }
                else
                    Title = "Tra?os 3D - Paredes sem canto comum | Esc cancela";
            }

            Viewport.Focus();
            return;
        }

        Vector2 point = ScreenToFloor(position.X, position.Y);

        if (!_wallMode)
        {
            point = Snap(point, 100);

            if (_wallEditorActive && _wallEditorDimensionTool == WallEditorDimensionTool.None)
            {
                point = WallManualDimensionService.SnapPoint(point, _project.Room.Walls);

                if (WallManualDimensionService.TryPick(point, _project.ManualWallDimensions, out Guid dimId))
                {
                    SelectManualDimension(dimId);
                    Viewport.Focus();
                    return;
                }
            }

            if (TrySelectOpeningAtScreen(position.X, position.Y))
                return;

            // Com grupo aberto: 1º tenta clicar numa seta de dimensão da peça, depois
            // troca de peça; se errar tudo, fecha o grupo.
            if (_openModuleGroupId != null)
            {
                if (TryPickPartHandleInOpenGroup(position.X, position.Y))
                {
                    Viewport.Focus();
                    return;
                }

                if (TryPickPartInOpenGroup(position.X, position.Y))
                {
                    Viewport.Focus();
                    return;
                }

                _openModuleGroupId = null;
                _selectedPartLabel = null;
                _selectedPartHandle = null;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                BeginModuleMarqueeSelection(position);
                e.Handled = true;
                return;
            }

            if (TryPickModuleAtScreen(position.X, position.Y, out ModuleInstance? pickedModule))
            {
                SelectModule(pickedModule!);
                PrepareModuleWallDrag(pickedModule!);
                return;
            }

            if (TryApplyWallRegionOffsetArrowClick(position.X, position.Y))
            {
                Viewport.Focus();
                return;
            }

            if (TryInsertPolygonVertexAtScreen(position.X, position.Y))
            {
                Viewport.Focus();
                return;
            }

            if (TryUpdateWallRegionVerticalCutAtScreen(position.X, position.Y))
            {
                Viewport.Focus();
                return;
            }

            if (TryBeginWallRegionRotationAtScreen(position.X, position.Y))
            {
                Viewport.Focus();
                return;
            }

            if (TryBeginWallRegionDragAtScreen(position.X, position.Y))
            {
                Viewport.Focus();
                return;
            }

            if (TryBeginWallRegionBodyDragAtScreen(position.X, position.Y))
            {
                Viewport.Focus();
                return;
            }

            if (TryBeginWallBandDragAtScreen(position.X, position.Y))
            {
                Viewport.Focus();
                return;
            }

            if (TryPickWallAtScreen(position.X, position.Y, out var pickedWall, out _, out bool hitTop))
            {
                if (!hitTop
                    && _selectedWallId == pickedWall.Id
                    && pickedWall.IsMovable
                    && WallMoveService.CanDragInView(_camera.ViewMode, _wallGroupSelected))
                {
                    BeginWallMove(pickedWall, point);
                    Viewport.Focus();
                    return;
                }

                CancelWallMoveIfActive();
                SelectWall(pickedWall, groupSelection: hitTop);
                Viewport.Focus();
                return;
            }

            if (TryApplyFloorZoneOffsetArrowClick(position.X, position.Y))
            {
                Viewport.Focus();
                return;
            }

            if (TryBeginFloorZoneDragAtScreen(position.X, position.Y))
            {
                Viewport.Focus();
                return;
            }

            if (TryPickFloorAtScreen(position.X, position.Y, out Vector2 floorHit))
            {
                if (FloorZoneService.TryPickZone(_project.Room.Floor!.Zones, floorHit, out var zone) && zone != null)
                    SelectFloorZone(zone);
                else
                    SelectFloor();

                Viewport.Focus();
                return;
            }

            _wallGroupSelected = false;
            _floorSelected = false;
            _selectedFloorZoneId = null;
            _selectedWallId = null;
            _selectedOpeningId = null;
            _selectedModuleId = null;
            _selectedModuleIds.Clear();
            _openModuleGroupId = null;
            _selectedPartLabel = null;
            _selectedPartHandle = null;
            Title = "Tra?os 3D - Face: Nenhuma";
            ClearPropertyPanelSelection();
            SyncDimensionConfiguratorSelectionState();
            return;
        }

        point = Snap(point, 100);

        if (_wallReferencePending && !_hasLastPoint)
        {
            if (!_hasWallReferencePick)
            {
                if (WallReferenceService.TryPickInnerFace(point, _project.Room.Walls, out _wallReferencePick))
                {
                    _hasWallReferencePick = true;
                    _wallReferenceOffsetPreview = 1000f;
                    ShowMeasureBox();
                    UpdateWallModeTitle();
                }
                else
                {
                    UpdateWallModeTitle();
                }

                return;
            }

            return;
        }

        if (_hasLastPoint)
        {
            point = ApplyAngleSnap45(_lastPoint, point);
            point = Snap(point, 100);
        }

        if (!_hasLastPoint)
        {
            _lastPoint = point;
            _previewPoint = point;
            _hasLastPoint = true;
            _hasPreview = false;

            _wallDraft.Reset();
            _wallDraft.Thickness = DefaultWallThickness;
            _wallDraft.Height = DefaultWallHeight;
            _wallDraft.Start(point);

            ShowMeasureBox();
            return;
        }

        AddWallTo(point, null);
    }

    private void AddWallTo(Vector2 point, float? innerLengthMm)
    {
        if ((point - _lastPoint).LengthSquared < 10 && innerLengthMm == null)
            return;

        bool shouldClose = false;

        if (_wallDraft.Points.Count >= 3)
        {
            Vector2 firstPoint = _wallDraft.Points[0];

            if (Geometry2D.AlmostEqual(point, firstPoint, 120f))
            {
                point = firstPoint;
                shouldClose = true;
            }
        }

        if (shouldClose && WallCloseConfirmation.ShouldConfirm(_wallDraft.Points.Count, true))
        {
            if (MessageBox.Show(
                    WallCloseConfirmation.DialogMessage,
                    WallCloseConfirmation.DialogTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
        }

        if (shouldClose && !innerLengthMm.HasValue && _lastTypedInnerLength > 0f)
            innerLengthMm = _lastTypedInnerLength;

        _wallDraft.ConfirmPoint(point, innerLengthMm);

        _lastPoint = _wallDraft.Points[^1];
        _previewPoint = _wallDraft.Points[^1];
        _hasPreview = false;

        if (shouldClose || _wallDraft.IsClosed)
        {
            if (_wallAppendMode)
                AppendDraftWallsToRoom();
            else
                SyncRoomFromDraft();

            _wallMode = false;
            _wallAppendMode = false;
            ClearWallReferenceState();
            _hasLastPoint = false;
            _hasPreview = false;

            MeasureBox.Visibility = Visibility.Collapsed;
            Keyboard.Focus(this);

            if (_project.Room.Walls.Count > 0)
                SelectWall(_project.Room.Walls[^1]);
            else
                Title = $"Tra?os 3D - Ambiente fechado | Paredes: {_project.Room.Walls.Count}";

            FrameCameraOnRoom();
            UpdateStatusBarClosedRoom();
            UpdateViewTitle();
            MarkProjectDirty();
            return;
        }

        ShowMeasureBox();
        MarkProjectDirty();
    }

    private void ShowMeasureBox()
    {
        MeasureBox.Visibility = Visibility.Visible;
        MeasureBox.Text = "";
        MeasureBox.Focus();
        MeasureBox.SelectAll();
        SyncWallConstructionPanelFromDraft();
    }

    private void MeasureBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
        {
            UndoLastWall();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            CancelWallMode();
            CancelOpeningInsertMode();
            CancelModuleInsertMode();
            CancelWallRegionVerticalCutMode();
            e.Handled = true;
            return;
        }

        if (_wallRegionVerticalCutMode && _wallRegionVerticalCutHasLine && e.Key == Key.Enter)
        {
            CommitWallRegionVerticalCut();
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
            return;

        string text = MeasureBox.Text.Trim().Replace(",", ".");

        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float measure))
            return;

        if (_floorPolygonRegionPickMode && _floorPolygonPickX.Count > 0 && measure > 0)
        {
            if (TryExtendFloorPolygonPickByLength(measure, out string? polyError))
                Title = GetFloorPolygonRegionPickTitle();
            else
                Title = polyError ?? "Traços 3D - Polígono inválido | Esc cancela";

            MeasureBox.Text = "";
            MeasureBox.Focus();
            e.Handled = true;
            return;
        }

        if (_wallPolygonRegionPickMode && _wallPolygonPickAlong.Count > 0 && measure > 0)
        {
            if (TryExtendWallPolygonPickByLength(measure, out string? polyError))
                Title = GetWallPolygonRegionPickTitle();
            else
                Title = polyError ?? "Traços 3D - Polígono inválido | Esc cancela";

            MeasureBox.Text = "";
            MeasureBox.Focus();
            e.Handled = true;
            return;
        }

        if (!_wallMode && _selectedOpeningId.HasValue)
        {
            SetSelectedOpeningWidth(measure);
            MeasureBox.Text = "";
            MeasureBox.Focus();
            e.Handled = true;
            return;
        }

        if (!_wallMode && _selectedWallId.HasValue)
        {
            if (_wallGroupSelected)
                return;

            SetSelectedWallLength(measure);
            MeasureBox.Text = "";
            MeasureBox.Focus();
            e.Handled = true;
            return;
        }

        if (_wallMode && _wallReferencePending && _hasWallReferencePick && !_hasLastPoint && measure > 0)
        {
            ConfirmWallReferenceOffset(measure);
            MeasureBox.Text = "";
            MeasureBox.Focus();
            e.Handled = true;
            return;
        }

        if (!_wallMode || !_hasLastPoint || measure <= 0)
            return;

        _lastTypedInnerLength = measure;

        Vector2 direction;

        if (_hasPreview && (_previewPoint - _lastPoint).LengthSquared > 1)
            direction = Vector2.Normalize(_previewPoint - _lastPoint);
        else
            direction = Vector2.UnitX;

        bool closingPreview = _wallDraft.Points.Count >= 3 &&
            _hasPreview &&
            Geometry2D.AlmostEqual(_previewPoint, _wallDraft.Points[0], 120f);

        // Medida digitada = comprimento na face de referência (Orientação Promob).
        Vector2 point = closingPreview
            ? _wallDraft.Points[0]
            : _lastPoint + direction * measure;

        AddWallTo(point, measure);

        MeasureBox.Text = "";
        MeasureBox.Focus();
        e.Handled = true;
    }

    private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_wallMode && e.ChangedButton == MouseButton.Right)
        {
            CancelWallMode();
            e.Handled = true;
            return;
        }

        if (_openingInsertMode != OpeningInsertMode.None && e.ChangedButton == MouseButton.Right)
        {
            CancelOpeningInsertMode();
            e.Handled = true;
            return;
        }

        if (_moduleInsertDefinitionId != null && e.ChangedButton == MouseButton.Right)
        {
            CancelModuleInsertMode();
            e.Handled = true;
            return;
        }

        // Promob: durante arraste (esquerdo), clique direito troca o plano de inserção.
        if (e.ChangedButton == MouseButton.Right && _moduleWallDragging)
        {
            Point dragPos = e.GetPosition(Viewport);
            TrySwitchModuleWallDragInsertionPlane(dragPos.X, dragPos.Y);
            _wallContextMenuCandidate = false;
            e.Handled = true;
            return;
        }

        _lastMousePosition = e.GetPosition(Viewport);

        if (e.ChangedButton == MouseButton.Middle)
            _isMiddleDown = true;

        if (e.ChangedButton == MouseButton.Right)
        {
            _isRightDown = true;

            if (!_isMiddleDown && CanShowWallContextMenu())
            {
                _wallContextMenuCandidate = true;
                _wallContextMenuPressPoint = e.GetPosition(Viewport);
            }
        }
    }

    private void Viewport_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!CanShowWallContextMenu() || !_wallContextMenuCandidate || _isMiddleDown)
        {
            _wallContextMenuCandidate = false;
            return;
        }

        _wallContextMenuCandidate = false;

        Point position = e.GetPosition(Viewport);

        if ((position - _wallContextMenuPressPoint).Length > 6d)
            return;

        TryShowWallContextMenu(position);
        e.Handled = true;
    }

    private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && _wallRegionDragging)
        {
            CommitWallRegionDrag();
            e.Handled = true;
        }

        if (e.ChangedButton == MouseButton.Left && _wallRegionBodyDragging)
        {
            CommitWallRegionBodyDrag();
            e.Handled = true;
        }

        if (e.ChangedButton == MouseButton.Left && _wallRegionRotating)
        {
            CommitWallRegionRotation();
            e.Handled = true;
        }

        if (e.ChangedButton == MouseButton.Left && _floorZoneDragging)
        {
            CommitFloorZoneDrag();
            e.Handled = true;
        }

        if (e.ChangedButton == MouseButton.Left && _wallBandDragging)
        {
            CommitWallBandDrag();
            e.Handled = true;
        }

        if (e.ChangedButton == MouseButton.Left && _wallFlechaDragging)
        {
            CommitWallFlechaDrag();
            e.Handled = true;
        }

        if (e.ChangedButton == MouseButton.Left && _moduleWallDragging)
        {
            CommitModuleWallDrag();
            e.Handled = true;
        }

        if (e.ChangedButton == MouseButton.Left && (_moduleMarqueePending || _moduleMarqueeActive))
        {
            FinishModuleMarqueeSelection();
            e.Handled = true;
        }

        if (e.ChangedButton == MouseButton.Left && _moduleWallDragPending)
            ClearModuleWallDragPending();

        if (e.ChangedButton == MouseButton.Left && _wallMoveDragging)
        {
            CommitWallMove();
            e.Handled = true;
        }

        if (e.ChangedButton == MouseButton.Middle)
            _isMiddleDown = false;

        if (e.ChangedButton == MouseButton.Right)
            _isRightDown = false;
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        Point current = e.GetPosition(Viewport);

        double dx = current.X - _lastMousePosition.X;
        double dy = current.Y - _lastMousePosition.Y;

        if (_moduleMarqueePending || _moduleMarqueeActive)
        {
            _moduleMarqueeEnd = current;

            if (!_moduleMarqueeActive &&
                (current - _moduleMarqueeStart).Length >= ModuleMarqueeThresholdPx)
            {
                _moduleMarqueeActive = true;
                // Arraste da caixa: deixa de ser clique-toggle.
                _moduleMarqueeClickCandidateId = null;
            }

            if (_moduleMarqueeActive)
                UpdateModuleMarqueeOverlay();
        }

        if (_wallRegionDragging)
            UpdateWallRegionDragPreview(current.X, current.Y);
        else if (_wallRegionBodyDragging)
            UpdateWallRegionBodyDragPreview(current.X, current.Y);
        else if (_wallRegionRotating)
            UpdateWallRegionRotationPreview(current.X, current.Y);
        else if (_wallRegionVerticalCutMode)
            UpdateWallRegionVerticalCutPreview(current.X, current.Y);
        else if (_floorZoneDragging)
            UpdateFloorZoneDragPreview(current.X, current.Y);
        else if (_wallBandDragging)
            UpdateWallBandDragPreview(current.X, current.Y);

        if (_openingInsertMode != OpeningInsertMode.None)
        {
            UpdateOpeningPreview(current.X, current.Y);
        }
        else if (_moduleInsertDefinitionId != null)
        {
            UpdateModulePreview(current.X, current.Y);
        }

        if (_moduleWallDragPending || _moduleWallDragging)
            TryBeginModuleWallDragFromPending(current.X, current.Y);

        if (_moduleWallDragging)
            UpdateModuleWallDrag(current.X, current.Y);

        if (_floorZoneDrawMode && _hasFloorZoneStart)
        {
            _floorZonePreview = Snap(ScreenToFloor(current.X, current.Y), 100);
        }

        if (_wallMoveDragging)
        {
            UpdateWallMovePreview(ScreenToFloor(current.X, current.Y));
        }
        if (_wallFlechaDragging)
            UpdateWallFlechaFromCursor(ScreenToFloor(current.X, current.Y));
        else if (_wallMode && _wallReferencePending && _hasWallReferencePick && !_hasLastPoint)
        {
            Vector2 refPoint = ScreenToFloor(current.X, current.Y);
            refPoint = Snap(refPoint, 100);
            _wallReferenceOffsetPreview = WallReferenceService.ComputeSignedOffset(_wallReferencePick, refPoint);

            if (Math.Abs(_wallReferenceOffsetPreview) < 50f)
                _wallReferenceOffsetPreview = _wallReferenceOffsetPreview >= 0f ? 50f : -50f;

            UpdateWallModeTitle();
        }
        else if (_wallMode && _hasLastPoint)
        {
            Vector2 point = ScreenToFloor(current.X, current.Y);
            point = Snap(point, 100);
            point = ApplyAngleSnap45(_lastPoint, point);
            point = Snap(point, 100);

            if (_wallDraft.Points.Count >= 3)
            {
                Vector2 firstPoint = _wallDraft.Points[0];

                if (Geometry2D.AlmostEqual(point, firstPoint, 120f))
                    point = firstPoint;
            }

            _previewPoint = point;
            _hasPreview = true;
            _wallDraft.MovePreview(point);

            float referenceLength = TryGetDraftPreviewReferenceLength(out var refPreview)
                ? refPreview
                : (_previewPoint - _lastPoint).Length;
            Title = $"Tra?os 3D - Parede fantasma: {referenceLength:0} mm | Orienta??o: {FormatMeasureSideLabel(_wallDraft.MeasureSide)} | R alterna Orienta??o";
            SyncWallConstructionPanelFromDraft();
        }
        else if (_wallEditorActive && _wallEditorDimensionTool != WallEditorDimensionTool.None)
        {
            _manualDimPreview = WallManualDimensionService.SnapPoint(
                Snap(ScreenToFloor(current.X, current.Y), 100),
                _project.Room.Walls);
        }
        else if (_wallHorizontalBandPickMode && _selectedWallId.HasValue && _wallHorizontalBandPickStep == 1)
        {
            if (TryPickWallFaceAtScreen(current.X, current.Y, out var bandWall, out _, out float height, out _, out bool hitTop) &&
                !hitTop &&
                bandWall.Id == _selectedWallId.Value)
                _wallHorizontalBandPreviewHeight2 = height;
        }
        else if (_wallVerticalBandPickMode && _selectedWallId.HasValue && _wallVerticalBandPickStep == 1)
        {
            if (TryPickWallFaceAtScreen(current.X, current.Y, out var bandWall, out float along, out _, out _, out bool hitTop) &&
                !hitTop &&
                bandWall.Id == _selectedWallId.Value)
                _wallVerticalBandPreviewAlong = along;
        }
        else if (_floorPolygonRegionPickMode && _project.Room.Floor != null)
        {
            if (TryPickFloorAtScreen(current.X, current.Y, out Vector2 floorHit))
            {
                _floorPolygonPreviewX = floorHit.X;
                _floorPolygonPreviewY = floorHit.Y;
            }
        }
        else if (_floorCircleRegionPickMode && _project.Room.Floor != null)
        {
            if (TryPickFloorAtScreen(current.X, current.Y, out Vector2 floorHit))
            {
                _floorCirclePickCenter = floorHit;
                _floorCirclePickRadius = FloorZoneService.DefaultCircleRadiusMm;
            }
        }
        else if (_wallPolygonRegionPickMode && _selectedWallId.HasValue)
        {
            if (TryPickWallFaceAtScreen(current.X, current.Y, out var polyWall, out float along, out float height, out FaceType face, out bool hitTop) &&
                !hitTop &&
                polyWall.Id == _selectedWallId.Value &&
                face == _wallRegionPickFace)
            {
                _wallPolygonPreviewAlong = along;
                _wallPolygonPreviewHeight = height;
            }
        }
        else if (_wallCircleRegionPickMode && _selectedWallId.HasValue)
        {
            if (TryPickWallFaceAtScreen(current.X, current.Y, out var circleWall, out float along, out float height, out FaceType face, out bool hitTop) &&
                !hitTop &&
                circleWall.Id == _selectedWallId.Value &&
                face == _wallRegionPickFace)
            {
                _wallCircleRegionPreviewAlong = along;
                _wallCircleRegionPreviewHeight = height;
            }
        }
        else if (_wallRegionPickMode && _selectedWallId.HasValue)
        {
            if (TryPickWallFaceAtScreen(current.X, current.Y, out var regionWall, out float along, out float height, out FaceType face, out bool hitTop) &&
                !hitTop &&
                regionWall.Id == _selectedWallId.Value &&
                face == _wallRegionPickFace &&
                _wallRegionPickStep == 1)
            {
                _wallRegionPickAlong2 = along;
                _wallRegionPickHeight2 = height;
            }
        }
        else if (_wallSegmentPickMode)
        {
            if (TryPickWallAtScreen(current.X, current.Y, out var segmentWall, out float distanceAlong, out bool hitTop) &&
                !hitTop &&
                segmentWall.Id == _wallSegmentTargetId)
                _wallSegmentPreviewDistance = distanceAlong;
        }
        else if (_wallChamferMode)
            UpdateWallChamferPreview(current.X, current.Y);

        if (!_wallMode)
        {
            if (_camera.ViewMode == CameraViewMode.Perspective)
            {
                if (_isMiddleDown && _isRightDown)
                    _camera.Orbit((float)dx, (float)dy);
                else if (_isMiddleDown)
                    _camera.PanPerspective((float)dx, (float)dy);
            }
            else if (_isMiddleDown)
            {
                _camera.PanOrthographic((float)dx, (float)dy);
            }
        }
        else if (_isMiddleDown)
        {
            _camera.PanTop((float)dx, (float)dy);
        }

        _lastMousePosition = current;
    }

    private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var pos = e.GetPosition(Viewport);
        if (TryGetZoomFocusPoint(pos.X, pos.Y, out Vector3 focus))
            _camera.ZoomToward(e.Delta, focus);
        else
            _camera.Zoom(e.Delta);

        Viewport.InvalidateVisual();
    }

    /// <summary>
    /// Ponto 3D sob o cursor para zoom CAD (módulo → parede → piso → plano do alvo).
    /// </summary>
    private bool TryGetZoomFocusPoint(double mouseX, double mouseY, out Vector3 focus)
    {
        focus = default;
        EnsureCameraMatricesForPicking();

        if (!Geometry3D.TryCreateWorldRay(
                mouseX,
                mouseY,
                Viewport.ActualWidth,
                Viewport.ActualHeight,
                _camera.View,
                _camera.Projection,
                out Vector3 origin,
                out Vector3 direction))
            return false;

        float bestT = float.MaxValue;
        bool hit = false;

        if (ModulePickService.TryPickRay(origin, direction, GetPickableModules(), out _, out float moduleT) &&
            moduleT < bestT)
        {
            bestT = moduleT;
            hit = true;
        }

        var walls = BuildWallPickTargets();
        if (walls.Count > 0 &&
            WallPickService.TryPickRay(origin, direction, walls, out _, out _, out float wallT, out _) &&
            wallT < bestT)
        {
            bestT = wallT;
            hit = true;
        }

        if (_project.Room.Floor is { Points.Count: >= 3 } &&
            FloorPickService.TryPickRay(origin, direction, _project.Room.Floor.Points, out float floorT) &&
            floorT < bestT)
        {
            bestT = floorT;
            hit = true;
        }

        if (hit)
        {
            focus = origin + direction * bestT;
            return true;
        }

        // Fallback: plano horizontal na altura do alvo da câmera.
        if (Geometry3D.TryRayHorizontalPlane(origin, direction, _camera.Target.Y, out float planeT, out Vector3 planeHit) &&
            planeT > 0f)
        {
            focus = planeHit;
            return true;
        }

        return false;
    }

    private static Vector2 Snap(Vector2 point, float step)
    {
        if (step <= 0)
            return point;

        float x = MathF.Round(point.X / step) * step;
        float y = MathF.Round(point.Y / step) * step;

        return new Vector2(x, y);
    }

    private static Vector2 ApplyAngleSnap45(Vector2 start, Vector2 current)
    {
        return Geometry2D.SnapAngle(start, current, 45f);
    }

    private Vector2 ScreenToFloor(double mouseX, double mouseY)
    {
        float x = (float)((2.0 * mouseX) / Viewport.ActualWidth - 1.0);
        float y = (float)(1.0 - (2.0 * mouseY) / Viewport.ActualHeight);

        var nearPoint = new Vector4(x, y, -1f, 1f);
        var farPoint = new Vector4(x, y, 1f, 1f);

        Matrix4 viewProjection = _camera.View * _camera.Projection;
        Matrix4 inverse = Matrix4.Invert(viewProjection);

        Vector4 nearWorld = Vector4.TransformRow(nearPoint, inverse);
        Vector4 farWorld = Vector4.TransformRow(farPoint, inverse);

        nearWorld /= nearWorld.W;
        farWorld /= farWorld.W;

        Vector3 rayStart = nearWorld.Xyz;
        Vector3 rayEnd = farWorld.Xyz;
        Vector3 rayDirection = Vector3.Normalize(rayEnd - rayStart);

        if (Math.Abs(rayDirection.Y) < 0.0001f)
            return Vector2.Zero;

        float t = -rayStart.Y / rayDirection.Y;
        Vector3 hit = rayStart + rayDirection * t;

        return new Vector2(hit.X, hit.Z);
    }

    private void OnRender(TimeSpan delta)
    {
        try
        {
            var dpi = VisualTreeHelper.GetDpi(Viewport);

            int width = Math.Max(1, (int)(Viewport.ActualWidth * dpi.DpiScaleX));
            int height = Math.Max(1, (int)(Viewport.ActualHeight * dpi.DpiScaleY));

            RenderScene(width, height);
            UpdateDimensionLabelsOverlay(width, height);

            if (_captureViewportOnNextRender)
            {
                _lastCapturedViewportPng = ViewportCaptureService.CapturePngBytes(Viewport, width, height);
                _captureViewportOnNextRender = false;
            }
            else if (_pendingViewportCapture is { } request)
            {
                _pendingViewportCapture = null;

                int targetWidth = Math.Max((int)(width * request.Scale), request.TargetMinWidthPx);
                int targetHeight = Math.Max(1, (int)Math.Round(height * (targetWidth / (double)width)));

                _lastCapturedViewportPng = ViewportCaptureService.CaptureOffscreen(
                    RenderScene,
                    targetWidth,
                    targetHeight);
            }
        }
        catch (Exception ex)
        {
            SetStatusTitle($"Erro OpenGL: {ex.Message}");
        }
    }

    private void RenderScene(int width, int height)
    {
        ViewportRenderer.PrepareFrame(width, height);
        SetupCamera(width, height);

        if (!_wallMode &&
            _project.Room.TryGetFloorBounds(out Vector2 floorMin, out Vector2 floorMax))
        {
            ViewportRenderer.DrawAutomaticRoomFloor(_project.Room, _floorSelected, _selectedFloorZoneId);

            if (_project.Room.ShowFloorGrid || _wallEditorActive)
                ViewportRenderer.DrawUniformGridInBounds(floorMin, floorMax, GridStep);
        }
        else
        {
            ViewportRenderer.DrawFloor(GridLimit);
            ViewportRenderer.DrawGrid3D(GridLimit, GridStep);
        }

        if (!WallEditorService.ShouldHideCeiling(_wallEditorActive))
            ViewportRenderer.DrawAutomaticRoomCeiling(_project.Room);

        if (_wallMode)
        {
            WallDraftViewportRenderer.DrawConfirmedGhosts(_wallDraft.BuildWalls());

            if (_wallReferencePending && _hasWallReferencePick)
            {
                ViewportRenderer.DrawWallReferenceGuide(_wallReferencePick, _wallReferenceOffsetPreview);
            }

            if (_hasLastPoint && _hasPreview &&
                WallDraftViewportRenderer.TryGetPreviewInnerFace(_wallDraft, _lastPoint, _previewPoint, out var innerFace))
            {
                bool showCloseMarker = _wallDraft.Points.Count >= 3 &&
                    Geometry2D.AlmostEqual(_previewPoint, _wallDraft.Points[0], 120f);
                WallDraftViewportRenderer.DrawPreviewSegment(
                    _wallDraft,
                    _lastPoint,
                    _previewPoint,
                    innerFace,
                    showCloseMarker);
            }
        }
        else
        {
            DrawProfessionalWalls();
        }

        DrawFloorDecorations();

        if (_hasOpeningPreview && _previewOpeningWallId.HasValue)
            DrawOpeningPreview();

        if (_hasModulePreview && _moduleInsertDefinitionId != null)
        {
            var previewDefinition = ModuleCatalog.GetRequired(_moduleInsertDefinitionId);
            var (previewWidth, previewHeight, previewDepth) = GetActiveModuleInsertionDimensions();
            ViewportRenderer.DrawModulePreview(
                _previewModulePosition,
                previewWidth,
                previewHeight,
                previewDepth,
                _previewModuleRotationY,
                _previewModuleSnappedToWall,
                WouldPreviewCollide());

            if (_previewModuleWallId.HasValue && _previewModuleSnappedToWall)
            {
                var wall = FindWallById(_previewModuleWallId.Value);

                if (wall != null)
                {
                    ViewportRenderer.DrawModuleInsertionCotas(
                        wall,
                        _project.Room.Walls,
                        _previewModulePosition,
                        previewWidth,
                        previewHeight,
                        previewDepth,
                        _previewModuleDistanceAlong);
                }
            }
        }

        RefreshCollisionState();

        if (!WallEditorService.ShouldHideModules(_wallEditorActive))
        {
            ViewportRenderer.DrawModules(
                GetRenderableModules(),
                _selectedModuleIds,
                _project.Metadata,
                _collisionEnabled ? _collidingModuleIds : null,
                _openModuleGroupId,
                _selectedPartLabel,
                _camera.XRayEnabled && _camera.ViewMode == CameraViewMode.Perspective);

            // Setas de dimensão da peça selecionada.
            if (_openModuleGroupId != null && !string.IsNullOrEmpty(_selectedPartLabel) &&
                !DrawerPartNaming.IsAssemblySelection(_selectedPartLabel))
            {
                var handleModule = _project.FindModule(_openModuleGroupId.Value);
                if (handleModule != null)
                    ViewportRenderer.DrawPartHandles(handleModule, _selectedPartLabel, _selectedPartHandle);
            }

            if (_moduleWallDragging)
            {
                var dragModule = _project.FindModule(_moduleWallDragModuleId);

                if (dragModule?.AttachedWallId != null)
                {
                    var dragWall = FindWallById(dragModule.AttachedWallId.Value);

                    if (dragWall != null)
                    {
                        ViewportRenderer.DrawModuleInsertionCotas(
                            dragWall,
                            _project.Room.Walls,
                            dragModule.Position,
                            dragModule.Width,
                            dragModule.Height,
                            dragModule.Depth,
                            dragModule.DistanceAlongWall);
                    }
                }
            }
        }

        if (_floorZoneDrawMode && _hasFloorZoneStart)
            ViewportRenderer.DrawFloorZonePreview(_floorZoneStart, _floorZonePreview);

        if (_floorPolygonRegionPickMode && _floorPolygonPickX.Count > 0)
            FloorSurfaceViewportRenderer.DrawPolygonPickPreview(
                _floorPolygonPickX,
                _floorPolygonPickY,
                _floorPolygonPreviewX,
                _floorPolygonPreviewY);

        if (_floorCircleRegionPickMode && _floorCirclePickRadius > 0f)
            FloorSurfaceViewportRenderer.DrawCirclePickPreview(
                _floorCirclePickCenter.X,
                _floorCirclePickCenter.Y,
                _floorCirclePickRadius);

        if (_floorZoneDragging && _floorZoneDragId != Guid.Empty)
            DrawFloorZoneDragPreview();

        DrawSelectedFloorZoneOffsetArrows();

        DrawAutomaticWallDimensionsForScene();
        DrawManualWallDimensionsForScene();

        if (_wallEditorActive &&
            _wallEditorDimensionTool != WallEditorDimensionTool.None &&
            _manualDimStep >= 0)
        {
            ViewportRenderer.DrawManualDimensionPreview(
                _wallEditorDimensionTool,
                _manualDimStep,
                _manualDimPointA,
                _manualDimPointB,
                _manualDimPreview);
        }

        if (_wallMoveDragging && _wallMoveWallId != Guid.Empty)
        {
            ViewportRenderer.DrawWallMoveGuide(
                _wallMoveOriginalStart,
                _wallMoveOriginalEnd,
                _wallMovePreviewDelta,
                _project.Room.Walls,
                _wallMoveWallId);
        }

        if (_wallSegmentPickMode && _wallSegmentTargetId != Guid.Empty)
        {
            var segmentWall = FindWallById(_wallSegmentTargetId);

            if (segmentWall != null)
                ViewportRenderer.DrawWallSegmentSplitPreview(segmentWall, _wallSegmentPreviewDistance);
        }

        if (_wallHorizontalBandPickMode && _selectedWallId.HasValue && _wallHorizontalBandPickStep == 1)
        {
            var bandWall = FindWallById(_selectedWallId.Value);

            if (bandWall != null)
                DrawWallHorizontalBandPickPreview(bandWall);
        }

        if (_wallVerticalBandPickMode && _selectedWallId.HasValue && _wallVerticalBandPickStep == 1)
        {
            var bandWall = FindWallById(_selectedWallId.Value);

            if (bandWall != null)
                DrawWallVerticalBandPickPreview(bandWall);
        }

        if (_wallPolygonRegionPickMode && _selectedWallId.HasValue && _wallPolygonPickAlong.Count > 0)
        {
            var polyWall = FindWallById(_selectedWallId.Value);

            if (polyWall != null)
                DrawWallPolygonRegionPickPreview(polyWall, _wallRegionPickFace);
        }

        if (_wallCircleRegionPickMode && _selectedWallId.HasValue)
        {
            var circleWall = FindWallById(_selectedWallId.Value);

            if (circleWall != null)
                DrawWallCircleRegionPickPreview(circleWall, _wallRegionPickFace);
        }

        if (_wallRegionPickMode && _selectedWallId.HasValue && _wallRegionPickStep == 1)
        {
            var regionWall = FindWallById(_selectedWallId.Value);

            if (regionWall != null)
                DrawWallRegionPickPreview(regionWall, _wallRegionPickFace);
        }

        if (_wallBandDragging && _wallBandDragWallId != Guid.Empty)
            DrawWallBandDragPreview();

        if (_wallRegionDragging && _wallRegionDragWallId != Guid.Empty)
            DrawWallRegionDragPreview();

        DrawSelectedWallRegionOffsetArrows();
        DrawSelectedWallRegionRotationHandle();
        DrawWallRegionVerticalCutPreview();

        if (_wallChamferMode && _wallChamferPreviewWallId != Guid.Empty)
        {
            var chamferWall = FindWallById(_wallChamferPreviewWallId);

            if (chamferWall != null)
            {
                Vector2 vertex = WallCornerChamferService.GetEndpointVertex(chamferWall, _wallChamferPreviewAtStart);
                ViewportRenderer.DrawWallChamferHotpoint(vertex);
            }
        }

        if (_wallFlechaHotpointMode || _wallFlechaDragging)
        {
            Guid hotWallId = _wallFlechaDragging ? _wallFlechaDragWallId : _selectedWallId ?? Guid.Empty;
            var hotWall = hotWallId != Guid.Empty ? FindWallById(hotWallId) : null;

            if (hotWall != null)
            {
                var arc = WallArcGeometry.FromWall(hotWall);
                Vector2 hotpoint = arc.IsStraight ? arc.Midpoint : arc.BulgePoint;
                ViewportRenderer.DrawWallFlechaHotpoint(hotpoint);
            }
        }
    }

    private void DrawAutomaticWallDimensionsForScene()
    {
        // As medidas finais já ficam disponíveis nas propriedades da parede.
        // Mantemos as cotas automáticas somente durante a construção, quando são
        // necessárias para orientar o próximo segmento. Cotas manuais continuam.
        if (!_wallMode)
        {
            _activeWallDimensions = Array.Empty<WallAutomaticDimension>();
            return;
        }

        IReadOnlyList<WallSegment> wallsForDims =
            WallAutomaticDimensionService.BuildDraftWallsIncludingPreview(
                _wallDraft,
                _previewPoint,
                _hasPreview && _hasLastPoint);

        _activeWallDimensions = wallsForDims.Count > 0
            ? WallAutomaticDimensionService.BuildForWalls(wallsForDims)
            : Array.Empty<WallAutomaticDimension>();

        ViewportRenderer.DrawAutomaticWallDimensions(_activeWallDimensions);
    }

    private void DrawManualWallDimensionsForScene()
    {
        if (_project.ManualWallDimensions.Count == 0)
            return;

        ViewportRenderer.DrawManualWallDimensions(
            _project.ManualWallDimensions,
            _selectedManualDimId);
    }

    private void UpdateDimensionLabelsOverlay(int renderWidth, int renderHeight)
    {
        DimensionLabelsCanvas.Children.Clear();

        if (renderWidth < 1 || renderHeight < 1)
            return;

        double viewW = Viewport.ActualWidth;
        double viewH = Viewport.ActualHeight;

        if (viewW < 1 || viewH < 1)
            return;

        double scaleX = viewW / renderWidth;
        double scaleY = viewH / renderHeight;

        foreach (var dim in _activeWallDimensions)
        {
            AddDimensionLabelOverlay(
                dim.LabelWorldPosition,
                $"{dim.LengthMm:0}",
                System.Windows.Media.Color.FromRgb(0x2E, 0x6B, 0xAD),
                renderWidth,
                renderHeight,
                scaleX,
                scaleY);
        }

        foreach (var dim in _project.ManualWallDimensions)
        {
            bool selected = _selectedManualDimId.HasValue && dim.Id == _selectedManualDimId.Value;
            var color = selected
                ? System.Windows.Media.Color.FromRgb(0xF2, 0x8C, 0x0A)
                : System.Windows.Media.Color.FromRgb(0x1A, 0x8F, 0x45);

            AddDimensionLabelOverlay(
                WallManualDimensionService.GetLabelWorldPosition(dim),
                WallManualDimensionService.FormatLabel(dim),
                color,
                renderWidth,
                renderHeight,
                scaleX,
                scaleY);
        }

        if (_hasModulePreview &&
            _previewModuleCotas.HasValue &&
            _previewModuleWallId.HasValue &&
            _previewModuleSnappedToWall &&
            _moduleInsertDefinitionId != null)
        {
            var wall = FindWallById(_previewModuleWallId.Value);

            if (wall != null)
            {
                var definition = ModuleCatalog.GetRequired(_moduleInsertDefinitionId);
                var (previewWidth, previewHeight, _) = GetActiveModuleInsertionDimensions();

                AddModuleInsertionCotaLabels(
                    wall,
                    _previewModulePosition,
                    previewWidth,
                    previewHeight,
                    _previewModuleDistanceAlong,
                    _previewModuleCotas.Value,
                    renderWidth,
                    renderHeight,
                    scaleX,
                    scaleY);
            }
        }

        if (_moduleWallDragging &&
            _moduleWallDragCotas.HasValue &&
            _moduleWallDragModuleId != Guid.Empty)
        {
            var dragModule = _project.FindModule(_moduleWallDragModuleId);

            if (dragModule?.AttachedWallId != null)
            {
                var wall = FindWallById(dragModule.AttachedWallId.Value);

                if (wall != null)
                {
                    AddModuleInsertionCotaLabels(
                        wall,
                        dragModule.Position,
                        dragModule.Width,
                        dragModule.Height,
                        dragModule.DistanceAlongWall,
                        _moduleWallDragCotas.Value,
                        renderWidth,
                        renderHeight,
                        scaleX,
                        scaleY);
                }
            }
        }
    }

    private void AddModuleInsertionCotaLabels(
        WallSegment wall,
        Vector3 modulePosition,
        float moduleWidth,
        float moduleHeight,
        float distanceAlongInner,
        ModulePlacementService.ModuleWallCotas cotas,
        int renderWidth,
        int renderHeight,
        double scaleX,
        double scaleY)
    {
        var innerFace = WallInnerFaceService.GetInnerFace(wall, _project.Room.Walls);
        float halfWidth = moduleWidth * 0.5f;
        float leftAlong = distanceAlongInner - halfWidth;
        float rightAlong = distanceAlongInner + halfWidth;
        float dimY = modulePosition.Y + moduleHeight * 0.5f;
        float wallTop = wall.FloorOffset + MathF.Max(wall.HeightStart, wall.HeightEnd);
        var cotaColor = System.Windows.Media.Color.FromRgb(0xD9, 0x26, 0x26);

        Vector3 FacePoint(float alongInner, float y)
        {
            Vector2 floor = innerFace.PointAtDistance(alongInner);
            return new Vector3(floor.X, y, floor.Y);
        }

        static Vector3 Midpoint(Vector3 a, Vector3 b) => (a + b) * 0.5f;

        if (cotas.Anterior >= 0.5f)
        {
            AddDimensionLabelOverlay(
                Midpoint(FacePoint(0f, dimY), FacePoint(leftAlong, dimY)),
                $"{cotas.Anterior:0}",
                cotaColor,
                renderWidth,
                renderHeight,
                scaleX,
                scaleY);
        }

        if (cotas.Posterior >= 0.5f)
        {
            AddDimensionLabelOverlay(
                Midpoint(FacePoint(rightAlong, dimY), FacePoint(innerFace.Length, dimY)),
                $"{cotas.Posterior:0}",
                cotaColor,
                renderWidth,
                renderHeight,
                scaleX,
                scaleY);
        }

        if (cotas.Inferior >= 0.5f)
        {
            AddDimensionLabelOverlay(
                Midpoint(FacePoint(leftAlong, wall.FloorOffset), FacePoint(leftAlong, modulePosition.Y)),
                $"{cotas.Inferior:0}",
                cotaColor,
                renderWidth,
                renderHeight,
                scaleX,
                scaleY);
        }

        if (cotas.Superior >= 0.5f)
        {
            AddDimensionLabelOverlay(
                Midpoint(
                    FacePoint(leftAlong, modulePosition.Y + moduleHeight),
                    FacePoint(leftAlong, wallTop)),
                $"{cotas.Superior:0}",
                cotaColor,
                renderWidth,
                renderHeight,
                scaleX,
                scaleY);
        }
    }

    private void AddDimensionLabelOverlay(
        Vector3 worldPosition,
        string text,
        System.Windows.Media.Color foreground,
        int renderWidth,
        int renderHeight,
        double scaleX,
        double scaleY)
    {
        if (!Geometry3D.TryProjectToScreen(
                worldPosition,
                _camera.View,
                _camera.Projection,
                renderWidth,
                renderHeight,
                out double sx,
                out double sy,
                out bool inFront) || !inFront)
            return;

        var label = new System.Windows.Controls.TextBlock
        {
            Text = text,
            Foreground = new System.Windows.Media.SolidColorBrush(foreground),
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0xD9, 0xFF, 0xFF, 0xFF)),
            FontSize = 11,
            Padding = new Thickness(3, 1, 3, 1)
        };

        Canvas.SetLeft(label, sx * scaleX - 20);
        Canvas.SetTop(label, sy * scaleY - 10);
        DimensionLabelsCanvas.Children.Add(label);
    }

    private void SetupCamera(int width, int height)
    {
        _camera.SetupForViewport(width, height, _wallMode);
        RenderEngine.SetViewProjection(_camera.View, _camera.Projection);
    }

    private void DrawFloorDecorations()
    {
        var floor = _project.Room.Floor;

        if (floor == null || floor.Points.Count < 3)
            return;

        // Linhas verticais nos cantos s? com Raio X ? sen?o atravessam as paredes (efeito fantasma)
        if (_camera.XRayEnabled && _camera.ViewMode == CameraViewMode.Perspective)
        {
            float maxH = _project.Room.Walls.Count > 0
                ? _project.Room.Walls.Max(w => MathF.Max(w.HeightStart, w.HeightEnd))
                : DefaultWallHeight;

            ViewportRenderer.DrawInnerCornerEdges(floor.Points, 11f, maxH);
        }

        ViewportRenderer.DrawFloorPerimeter(floor.Points, _floorSelected && !_selectedFloorZoneId.HasValue, depthTest: !_camera.XRayEnabled);

        if (_selectedFloorZoneId.HasValue)
        {
            var zone = floor.Zones.FirstOrDefault(z => z.Id == _selectedFloorZoneId.Value);

            if (zone != null)
                FloorSurfaceViewportRenderer.DrawZoneOutlines(zone);
        }

        foreach (var zone in floor.Zones)
        {
            if (_selectedFloorZoneId.HasValue && zone.Id == _selectedFloorZoneId.Value)
                continue;

            FloorSurfaceViewportRenderer.DrawZoneOutlines(zone);
        }
    }

    private void DrawFloorZoneDragPreview()
    {
        var floor = _project.Room.Floor;
        var zone = floor?.Zones.FirstOrDefault(z => z.Id == _floorZoneDragId);

        if (zone == null)
            return;

        FloorSurfaceViewportRenderer.DrawZoneDragPreview(zone, _floorZoneDragEdge, _floorZoneDragPreviewValue);
    }

    private void DrawSelectedFloorZoneOffsetArrows()
    {
        var zone = GetSelectedFloorZone();
        if (zone == null || zone.Shape != WallRegionShape.Rectangular)
            return;

        FloorSurfaceViewportRenderer.DrawZoneOffsetArrows(zone);
    }

    private void DrawProfessionalWalls()
    {
        if (_project.Room.Walls.Count == 0)
            return;

        var visualSegments = WallVisualBuilder.BuildWithCorners(_project.Room.Walls);

        // Grupo selecionado quando qualquer parede est? selecionada
        bool groupSelected = _wallGroupSelected;
        bool xRay = _camera.XRayEnabled && _camera.ViewMode == CameraViewMode.Perspective;

        // Passo 1: faces sólidas (todas antes das arestas)
        foreach (var segment in visualSegments)
        {
            if (!ShouldRenderWall(segment.Wall))
                continue;

            bool faceSelected = !groupSelected &&
                                _selectedWallId.HasValue &&
                                _selectedWallId.Value == segment.Wall.Id;
            bool insertionHighlight =
                (_hasModulePreview &&
                 _previewModuleWallId.HasValue &&
                 _previewModuleWallId.Value == segment.Wall.Id)
                || (_moduleWallDragging &&
                    _moduleWallDragWallId != Guid.Empty &&
                    _moduleWallDragWallId == segment.Wall.Id);
            bool thirtyFortyMoving = _wall304050MovingWallId.HasValue &&
                                       _wall304050MovingWallId.Value == segment.Wall.Id;
            WallViewportRenderer.DrawSegmentSolid(
                segment,
                faceSelected,
                groupSelected,
                xRay,
                insertionHighlight,
                thirtyFortyMoving,
                WallLayerCatalog.GetLayerFillMode(_project.Metadata, segment.Wall.LayerId));
        }

        // Passo 2: arestas por cima — evita oclusão entre paredes adjacentes e z-fighting
        foreach (var segment in visualSegments)
        {
            if (!ShouldRenderWall(segment.Wall))
                continue;

            bool faceSelected = !groupSelected &&
                                _selectedWallId.HasValue &&
                                _selectedWallId.Value == segment.Wall.Id;
            bool insertionHighlight =
                (_hasModulePreview &&
                 _previewModuleWallId.HasValue &&
                 _previewModuleWallId.Value == segment.Wall.Id)
                || (_moduleWallDragging &&
                    _moduleWallDragWallId != Guid.Empty &&
                    _moduleWallDragWallId == segment.Wall.Id);

            var layerFillMode = WallLayerCatalog.GetLayerFillMode(_project.Metadata, segment.Wall.LayerId);

            WallViewportRenderer.DrawSegmentEdges(
                segment,
                faceSelected,
                groupSelected,
                xRay,
                insertionHighlight,
                layerFillMode,
                segment.Wall.LayerId);
            OpeningViewportRenderer.DrawOutlines(segment, _selectedOpeningId, xRay);
            WallSurfaceViewportRenderer.DrawBandsAndRegions(segment, layerFillMode);

            if (faceSelected)
                WallViewportRenderer.DrawSelectedMeasurement(segment);
        }

        RenderEngine.EnableDepthTest();
    }


    private bool TryGetDraftPreviewReferenceLength(out float referenceLength)
    {
        referenceLength = 0f;

        if (!TryGetDraftPreviewReferenceFace(out var referenceFace))
            return false;

        referenceLength = referenceFace.Length;
        return referenceLength > 0.5f;
    }

    private bool TryGetDraftPreviewReferenceFace(out WallInnerFaceGeometry referenceFace)
    {
        referenceFace = default;

        if (!_wallMode || !_hasLastPoint || _wallDraft.Points.Count == 0)
            return false;

        return WallDraftViewportRenderer.TryGetPreviewReferenceFace(
            _wallDraft,
            _wallDraft.Points[^1],
            _previewPoint,
            out referenceFace);
    }

    private bool TryGetDraftPreviewInnerFace(out WallInnerFaceGeometry innerFace)
    {
        innerFace = default;

        if (!_wallMode || !_hasLastPoint || _wallDraft.Points.Count == 0)
            return false;

        return WallDraftViewportRenderer.TryGetPreviewInnerFace(
            _wallDraft,
            _wallDraft.Points[^1],
            _previewPoint,
            out innerFace);
    }












    // Topo trapezoidal: y_top_left e y_top_right podem diferir (p?-direito vari?vel)


    private void DrawOpeningPreview()
    {
        if (!_previewOpeningWallId.HasValue)
            return;

        var wall = FindWallById(_previewOpeningWallId.Value);

        if (wall == null)
            return;

        var opening = WallOpeningPlacement.CreateOpening(
            _openingInsertMode == OpeningInsertMode.Window ? OpeningType.Window : OpeningType.Door,
            _previewOpeningDistance);

        if (!WallOpeningPlacement.CanPlace(wall, opening))
            return;

        var segment = WallSurfaceViewportRenderer.FindSegment(_project.Room.Walls, wall.Id);

        if (segment == null)
            return;

        OpeningViewportRenderer.DrawPlacementPreview(segment, opening);
    }

    private void DrawWallVerticalBandPickPreview(WallSegment wall)
    {
        var visual = WallSurfaceViewportRenderer.FindSegment(_project.Room.Walls, wall.Id);

        if (visual == null)
            return;

        WallSurfaceViewportRenderer.DrawVerticalBandPickPreview(
            visual,
            _wallVerticalBandPickAlong1,
            _wallVerticalBandPreviewAlong);
    }

    private void DrawWallHorizontalBandPickPreview(WallSegment wall)
    {
        var visual = WallSurfaceViewportRenderer.FindSegment(_project.Room.Walls, wall.Id);

        if (visual == null)
            return;

        WallSurfaceViewportRenderer.DrawHorizontalBandPickPreview(
            visual,
            _wallHorizontalBandPickHeight1,
            _wallHorizontalBandPreviewHeight2);
    }

    private void DrawWallPolygonRegionPickPreview(WallSegment wall, FaceType face)
    {
        var visual = WallSurfaceViewportRenderer.FindSegment(_project.Room.Walls, wall.Id);

        if (visual == null)
            return;

        WallSurfaceViewportRenderer.DrawPolygonRegionPickPreview(
            visual,
            face,
            _wallPolygonPickAlong,
            _wallPolygonPickHeight,
            _wallPolygonPreviewAlong,
            _wallPolygonPreviewHeight);
    }

    private void DrawWallCircleRegionPickPreview(WallSegment wall, FaceType face)
    {
        var visual = WallSurfaceViewportRenderer.FindSegment(_project.Room.Walls, wall.Id);

        if (visual == null)
            return;

        WallSurfaceViewportRenderer.DrawCircleRegionPreview(
            visual,
            face,
            _wallCircleRegionPreviewAlong,
            _wallCircleRegionPreviewHeight,
            WallRegionService.DefaultCircleRadiusMm);
    }

    private void DrawWallRegionPickPreview(WallSegment wall, FaceType face)
    {
        var visual = WallSurfaceViewportRenderer.FindSegment(_project.Room.Walls, wall.Id);

        if (visual == null)
            return;

        WallSurfaceViewportRenderer.DrawRegionPickPreview(
            visual,
            face,
            _wallRegionPickAlong1,
            _wallRegionPickAlong2,
            _wallRegionPickHeight1,
            _wallRegionPickHeight2);
    }

    private void DrawWallBandDragPreview()
    {
        var wall = FindWallById(_wallBandDragWallId);

        if (wall == null)
            return;

        var band = wall.Bands.FirstOrDefault(b => b.Id == _wallBandDragBandId);

        if (band == null)
            return;

        var visual = WallSurfaceViewportRenderer.FindSegment(_project.Room.Walls, wall.Id);

        if (visual == null)
            return;

        WallSurfaceViewportRenderer.DrawBandDragPreview(
            visual,
            band,
            _wallBandDragEdge,
            _wallBandDragPreviewValue);
    }

    private void DrawWallRegionDragPreview()
    {
        var wall = FindWallById(_wallRegionDragWallId);

        if (wall == null)
            return;

        var region = wall.Regions.FirstOrDefault(r => r.Id == _wallRegionDragRegionId);

        if (region == null)
            return;

        var visual = WallSurfaceViewportRenderer.FindSegment(_project.Room.Walls, wall.Id);

        if (visual == null)
            return;

        WallSurfaceViewportRenderer.DrawRegionDragPreview(
            visual,
            region,
            _wallRegionDragEdge,
            _wallRegionDragPreviewValue);
    }

    private void DrawSelectedWallRegionOffsetArrows()
    {
        if (!_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);
        if (wall == null)
            return;

        var region = GetSelectedWallRegion(wall);
        if (region == null || region.Shape != WallRegionShape.Rectangular)
            return;

        if (MathF.Abs(region.RotationDegrees) > 0.01f)
            return;

        var visual = WallSurfaceViewportRenderer.FindSegment(_project.Room.Walls, wall.Id);
        if (visual == null)
            return;

        WallSurfaceViewportRenderer.DrawRegionOffsetArrows(visual, region);
    }

    private void DrawSelectedWallRegionRotationHandle()
    {
        if (!_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);
        if (wall == null)
            return;

        var region = GetSelectedWallRegion(wall);
        if (region == null || region.Shape == WallRegionShape.Circular)
            return;

        var visual = WallSurfaceViewportRenderer.FindSegment(_project.Room.Walls, wall.Id);
        if (visual == null)
            return;

        WallSurfaceViewportRenderer.DrawRegionRotationHandle(visual, region);
    }

    private void DrawWallRegionVerticalCutPreview()
    {
        if (!_wallRegionVerticalCutMode || !_wallRegionVerticalCutHasLine || !_selectedWallId.HasValue)
            return;

        var wall = FindWallById(_selectedWallId.Value);
        if (wall == null)
            return;

        var region = wall.Regions.FirstOrDefault(r => r.Id == _wallRegionVerticalCutRegionId);
        if (region == null)
            return;

        var visual = WallSurfaceViewportRenderer.FindSegment(_project.Room.Walls, wall.Id);
        if (visual == null)
            return;

        WallSurfaceViewportRenderer.DrawRegionVerticalCutPreview(visual, region, _wallRegionVerticalCutAlongMm);
    }

    private bool TryApplyWallRegionOffsetArrowClick(double mouseX, double mouseY)
    {
        if (_wallGroupSelected || !_selectedWallId.HasValue)
            return false;

        var wall = FindWallById(_selectedWallId.Value);
        if (wall == null)
            return false;

        if (!TryPickWallRegionOffsetArrowAtScreen(
                mouseX,
                mouseY,
                wall,
                out WallRegion region,
                out WallRegionEdgeKind edge,
                out float deltaMm))
            return false;

        if (WallRegionService.TryAdjustRegionEdgeOffset(wall, region.Id, edge, deltaMm, out string? error))
        {
            MarkProjectDirty();
            UpdateWallPropertyPanel(wall);
            Viewport.InvalidateVisual();
            Title = "Traços 3D - Offset aresta ajustado (+10 mm por clique na seta)";
            return true;
        }

        if (error != null)
            WallRegionsSummaryText.Text = error;

        return false;
    }

    private bool TryPickWallRegionOffsetArrowAtScreen(
        double mouseX,
        double mouseY,
        WallSegment wall,
        out WallRegion region,
        out WallRegionEdgeKind edge,
        out float deltaMm)
    {
        region = null!;
        edge = WallRegionEdgeKind.StartAlong;
        deltaMm = 10f;

        var selected = GetSelectedWallRegion(wall);
        if (selected == null || selected.Shape != WallRegionShape.Rectangular)
            return false;

        if (MathF.Abs(selected.RotationDegrees) > 0.01f)
            return false;

        if (!TryPickWallFaceAtScreen(
                mouseX,
                mouseY,
                out WallSegment pickWall,
                out float along,
                out float height,
                out FaceType face,
                out bool hitTop) ||
            hitTop ||
            pickWall.Id != wall.Id ||
            face != selected.Face)
            return false;

        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        var (start, end, bottom, top) = WallRegionGeometry.GetEffectiveBounds(selected, wall.Length, wallTop);
        float midAlong = (start + end) * 0.5f;
        float midH = (bottom + top) * 0.5f;
        float spacing = WallSurfaceViewportRenderer.RegionOffsetArrowSpacingMm;
        float tolerance = WallSurfaceViewportRenderer.RegionOffsetArrowPickToleranceMm;

        (float alongPos, float heightPos, WallRegionEdgeKind edgeKind, float delta)[] candidates = [
            (start - spacing, midH, WallRegionEdgeKind.StartAlong, 10f),
            (start + spacing, midH, WallRegionEdgeKind.StartAlong, -10f),
            (end + spacing, midH, WallRegionEdgeKind.EndAlong, 10f),
            (end - spacing, midH, WallRegionEdgeKind.EndAlong, -10f),
            (midAlong, bottom - spacing, WallRegionEdgeKind.Bottom, 10f),
            (midAlong, bottom + spacing, WallRegionEdgeKind.Bottom, -10f),
            (midAlong, top + spacing, WallRegionEdgeKind.Top, 10f),
            (midAlong, top - spacing, WallRegionEdgeKind.Top, -10f)
        ];

        float bestDist = tolerance;
        WallRegionEdgeKind bestEdge = WallRegionEdgeKind.StartAlong;
        float bestDelta = 10f;

        foreach (var (alongPos, heightPos, edgeKind, delta) in candidates)
        {
            float dx = along - alongPos;
            float dy = height - heightPos;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist >= bestDist)
                continue;

            bestDist = dist;
            bestEdge = edgeKind;
            bestDelta = delta;
        }

        if (bestDist >= tolerance)
            return false;

        region = selected;
        edge = bestEdge;
        deltaMm = bestDelta;
        return true;
    }

    private void TryInsertOpeningAt(double mouseX, double mouseY)
    {
        if (_openingInsertMode == OpeningInsertMode.None)
            return;

        if (_project.Room.Walls.Count == 0)
        {
            Title = "Tra?os 3D - Desenhe paredes antes de inserir aberturas";
            return;
        }

        if (!TryPickWallAtScreen(mouseX, mouseY, out var wall, out float clickDistance, out _))
        {
            Title = _openingInsertMode == OpeningInsertMode.Window
                ? "Tra?os 3D - Janela | Clique na face da parede"
                : "Tra?os 3D - Porta | Clique na face da parede";
            return;
        }

        var type = _openingInsertMode == OpeningInsertMode.Window
            ? OpeningType.Window
            : OpeningType.Door;

        var prototype = WallOpeningPlacement.CreateOpening(type, 0f);
        float startDistance = WallOpeningPlacement.ComputeStartDistance(
            clickDistance,
            prototype.Width,
            wall.Length);

        var opening = WallOpeningPlacement.CreateOpening(type, startDistance);

        if (!WallOpeningPlacement.TryAddOpening(wall, opening))
        {
            Title =
                $"Tra?os 3D - Abertura n?o cabe | Parede: {wall.Length:0} mm | " +
                $"Necess?rio: = {prototype.Width + WallOpeningPlacement.MinEdgeMargin * 2f:0} mm";
            return;
        }

        _wallGroupSelected = false;
        _floorSelected = false;
        _selectedFloorZoneId = null;
        _selectedWallId = null;
        _selectedOpeningId = opening.Id;
        UpdateSelectedOpeningStatus(wall, opening);
        MarkProjectDirty();
    }

    private void UpdateOpeningPreview(double mouseX, double mouseY)
    {
        _hasOpeningPreview = false;
        _previewOpeningWallId = null;

        if (_openingInsertMode == OpeningInsertMode.None)
            return;

        if (_project.Room.Walls.Count == 0)
            return;

        if (!TryPickWallAtScreen(mouseX, mouseY, out var wall, out float clickDistance, out _))
        {
            Title = _openingInsertMode == OpeningInsertMode.Window
                ? "Tra?os 3D - Janela | Clique na face da parede | Esc cancela"
                : "Tra?os 3D - Porta | Clique na face da parede | Esc cancela";
            return;
        }

        var type = _openingInsertMode == OpeningInsertMode.Window
            ? OpeningType.Window
            : OpeningType.Door;

        var prototype = WallOpeningPlacement.CreateOpening(type, 0f);
        float startDistance = WallOpeningPlacement.ComputeStartDistance(
            clickDistance,
            prototype.Width,
            wall.Length);

        var preview = WallOpeningPlacement.CreateOpening(type, startDistance);

        if (!WallOpeningPlacement.CanPlace(wall, preview))
        {
            Title =
                $"Tra?os 3D - Sem espa?o aqui | Parede: {wall.Length:0} mm | " +
                $"Abertura: {prototype.Width:0} mm | Esc cancela";
            return;
        }

        _previewOpeningWallId = wall.Id;
        _previewOpeningDistance = startDistance;
        _hasOpeningPreview = true;

        Title = _openingInsertMode == OpeningInsertMode.Window
            ? $"Tra?os 3D - Janela | Posi??o: {startDistance:0} mm | Clique para confirmar"
            : $"Tra?os 3D - Porta | Posi??o: {startDistance:0} mm | Clique para confirmar";
    }

    private bool TrySelectOpeningAtScreen(double mouseX, double mouseY)
    {
        if (!TryPickWallAtScreen(mouseX, mouseY, out var wall, out float distanceAlong, out _))
            return false;

        foreach (var opening in wall.Openings)
        {
            if (distanceAlong < opening.DistanceFromStart - 40f ||
                distanceAlong > opening.EndDistance + 40f)
            {
                continue;
            }

            _selectedOpeningId = opening.Id;
            _wallGroupSelected = false;
            _selectedWallId = null;
            UpdateSelectedOpeningStatus(wall, opening);
            return true;
        }

        return false;
    }

    private bool TryBeginFloorZoneDragAtScreen(double mouseX, double mouseY)
    {
        if (!_floorSelected || _project.Room.Floor == null)
            return false;

        var zone = GetSelectedFloorZone();
        if (zone == null || zone.Shape == WallRegionShape.Polygon)
            return false;

        if (!TryPickFloorZoneEdgeAtScreen(
                mouseX,
                mouseY,
                zone,
                out WallRegionEdgeKind edge,
                out float edgeValue))
            return false;

        _floorZoneDragging = true;
        _floorZoneDragId = zone.Id;
        _floorZoneDragEdge = edge;
        _floorZoneDragPreviewValue = edgeValue;
        Viewport.CaptureMouse();
        Title = "Traços 3D - Arraste a borda da região no piso | Esc cancela";
        return true;
    }

    private void UpdateFloorZoneDragPreview(double mouseX, double mouseY)
    {
        if (!_floorZoneDragging || _project.Room.Floor == null)
            return;

        var zone = _project.Room.Floor.Zones.FirstOrDefault(z => z.Id == _floorZoneDragId);
        if (zone == null)
            return;

        if (!TryPickFloorAtScreen(mouseX, mouseY, out Vector2 hit))
            return;

        float raw = _floorZoneDragEdge switch
        {
            WallRegionEdgeKind.StartAlong or WallRegionEdgeKind.EndAlong => hit.X,
            WallRegionEdgeKind.Radius => MathF.Sqrt(
                MathF.Pow(hit.X - zone.CenterX, 2f) +
                MathF.Pow(hit.Y - zone.CenterY, 2f)),
            _ => hit.Y
        };

        _floorZoneDragPreviewValue = MathF.Round(raw / 10f) * 10f;
    }

    private void CommitFloorZoneDrag()
    {
        if (!_floorZoneDragging || _project.Room.Floor == null)
            return;

        string? error = null;

        if (FloorZoneService.TrySetZoneEdge(
                _project.Room.Floor,
                _floorZoneDragId,
                _floorZoneDragEdge,
                _floorZoneDragPreviewValue,
                out error))
        {
            MarkProjectDirty();
            UpdateFloorPropertyPanel();
        }
        else if (error != null)
            Title = $"Traços 3D - {error}";

        CancelFloorZoneDrag();
    }

    private bool TryApplyFloorZoneOffsetArrowClick(double mouseX, double mouseY)
    {
        if (!_floorSelected || _project.Room.Floor == null)
            return false;

        var zone = GetSelectedFloorZone();
        if (zone == null)
            return false;

        if (!TryPickFloorZoneOffsetArrowAtScreen(
                mouseX,
                mouseY,
                zone,
                out WallRegionEdgeKind edge,
                out float deltaMm))
            return false;

        if (FloorZoneService.TryAdjustZoneEdgeOffset(_project.Room.Floor, zone.Id, edge, deltaMm, out string? error))
        {
            MarkProjectDirty();
            UpdateFloorPropertyPanel();
            Viewport.InvalidateVisual();
            Title = "Traços 3D - Offset aresta no piso (+10 mm por clique na seta)";
            return true;
        }

        if (error != null)
            FloorRegionsSummaryText.Text = error;

        return false;
    }

    private bool TryPickFloorZoneOffsetArrowAtScreen(
        double mouseX,
        double mouseY,
        FloorZone zone,
        out WallRegionEdgeKind edge,
        out float deltaMm)
    {
        edge = WallRegionEdgeKind.StartAlong;
        deltaMm = 10f;

        if (zone.Shape != WallRegionShape.Rectangular)
            return false;

        if (!TryPickFloorAtScreen(mouseX, mouseY, out Vector2 hit))
            return false;

        var (minX, maxX, minY, maxY) = FloorZoneGeometry.GetEffectiveBounds(
            zone, float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);

        float midX = (minX + maxX) * 0.5f;
        float midY = (minY + maxY) * 0.5f;
        float spacing = FloorSurfaceViewportRenderer.OffsetArrowSpacingMm;
        float tolerance = FloorSurfaceViewportRenderer.OffsetArrowPickToleranceMm;

        (float x, float y, WallRegionEdgeKind edgeKind, float delta)[] candidates = [
            (minX - spacing, midY, WallRegionEdgeKind.StartAlong, 10f),
            (minX + spacing, midY, WallRegionEdgeKind.StartAlong, -10f),
            (maxX + spacing, midY, WallRegionEdgeKind.EndAlong, 10f),
            (maxX - spacing, midY, WallRegionEdgeKind.EndAlong, -10f),
            (midX, minY - spacing, WallRegionEdgeKind.Bottom, 10f),
            (midX, minY + spacing, WallRegionEdgeKind.Bottom, -10f),
            (midX, maxY + spacing, WallRegionEdgeKind.Top, 10f),
            (midX, maxY - spacing, WallRegionEdgeKind.Top, -10f)
        ];

        float bestDist = tolerance;
        WallRegionEdgeKind bestEdge = WallRegionEdgeKind.StartAlong;
        float bestDelta = 10f;

        foreach (var (x, y, edgeKind, delta) in candidates)
        {
            float dx = hit.X - x;
            float dy = hit.Y - y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist >= bestDist)
                continue;

            bestDist = dist;
            bestEdge = edgeKind;
            bestDelta = delta;
        }

        if (bestDist >= tolerance)
            return false;

        edge = bestEdge;
        deltaMm = bestDelta;
        return true;
    }

    private bool TryPickFloorZoneEdgeAtScreen(
        double mouseX,
        double mouseY,
        FloorZone zone,
        out WallRegionEdgeKind edge,
        out float edgeValue)
    {
        edge = WallRegionEdgeKind.StartAlong;
        edgeValue = 0f;

        const float toleranceMm = 120f;

        if (!TryPickFloorAtScreen(mouseX, mouseY, out Vector2 hit))
            return false;

        if (_project.Room.Floor == null || !_project.Room.Floor.TryGetBounds(out Vector2 min, out Vector2 max))
            return false;

        if (zone.Shape == WallRegionShape.Circular)
        {
            float dist = FloorZoneGeometry.DistanceToBoundary(zone, hit.X, hit.Y, min.X, min.Y, max.X, max.Y);

            if (dist < toleranceMm)
            {
                edge = WallRegionEdgeKind.Radius;
                edgeValue = zone.RadiusMm;
                return true;
            }

            return false;
        }

        if (zone.Shape == WallRegionShape.Polygon)
            return false;

        var (eMinX, eMaxX, eMinY, eMaxY) = FloorZoneGeometry.GetEffectiveBounds(
            zone, min.X, min.Y, max.X, max.Y);

        float bestDist = toleranceMm;
        ConsiderFloorEdge(zone, WallRegionEdgeKind.StartAlong, MathF.Abs(hit.X - eMinX), zone.MinX,
            ref edge, ref bestDist, ref edgeValue);
        ConsiderFloorEdge(zone, WallRegionEdgeKind.EndAlong, MathF.Abs(hit.X - eMaxX), zone.MaxX,
            ref edge, ref bestDist, ref edgeValue);
        ConsiderFloorEdge(zone, WallRegionEdgeKind.Bottom, MathF.Abs(hit.Y - eMinY), zone.MinY,
            ref edge, ref bestDist, ref edgeValue);
        ConsiderFloorEdge(zone, WallRegionEdgeKind.Top, MathF.Abs(hit.Y - eMaxY), zone.MaxY,
            ref edge, ref bestDist, ref edgeValue);

        return bestDist < toleranceMm;
    }

    private static void ConsiderFloorEdge(
        FloorZone zone,
        WallRegionEdgeKind edgeKind,
        float distance,
        float value,
        ref WallRegionEdgeKind bestEdge,
        ref float bestDist,
        ref float edgeValue)
    {
        if (distance >= bestDist)
            return;

        bestDist = distance;
        bestEdge = edgeKind;
        edgeValue = value;
    }

    private bool TryPickFloorAtScreen(double mouseX, double mouseY, out Vector2 floorHit)
    {
        floorHit = Vector2.Zero;

        if (_project.Room.Floor == null || _project.Room.Floor.Points.Count < 3)
            return false;

        EnsureCameraMatricesForPicking();

        if (!Geometry3D.TryCreateWorldRay(
                mouseX,
                mouseY,
                Viewport.ActualWidth,
                Viewport.ActualHeight,
                _camera.View,
                _camera.Projection,
                out Vector3 origin,
                out Vector3 direction))
            return false;

        if (!FloorPickService.TryPickRay(origin, direction, _project.Room.Floor.Points, out float floorT))
            return false;

        var pickTargets = BuildWallPickTargets();

        if (WallPickService.TryPickRay(origin, direction, pickTargets, out _, out _, out float wallT, out _))
        {
            if (wallT < floorT)
                return false;
        }

        floorHit = Geometry3D.HitPointToFloor(origin + direction * floorT);
        return true;
    }

    private bool TryPickWallAtScreen(double mouseX, double mouseY, out WallSegment wall, out float distanceAlong, out bool hitTopFace)
    {
        wall = null!;
        distanceAlong = 0f;
        hitTopFace = false;

        if (_project.Room.Walls.Count == 0)
            return false;

        EnsureCameraMatricesForPicking();

        if (Geometry3D.TryCreateWorldRay(
                mouseX,
                mouseY,
                Viewport.ActualWidth,
                Viewport.ActualHeight,
                _camera.View,
                _camera.Projection,
                out Vector3 origin,
                out Vector3 direction))
        {
            var pickTargets = BuildWallPickTargets();

            if (WallPickService.TryPickRay(origin, direction, pickTargets, out Guid wallId, out distanceAlong, out _, out bool topFace))
            {
                wall = FindWallById(wallId)!;

                if (wall != null)
                {
                    hitTopFace = topFace;
                    if (hitTopFace && _camera.ViewMode == CameraViewMode.Top)
                        hitTopFace = false;

                    Vector2 pickPoint = ScreenToFloor(mouseX, mouseY);
                    distanceAlong = Math.Clamp(wall.GetDistanceAlongWall(pickPoint), 0f, wall.Length);
                    return true;
                }
            }
        }

        Vector2 floorPoint = ScreenToFloor(mouseX, mouseY);

        if (WallPickService.TryPickFloor(floorPoint, _project.Room.Walls, out Guid floorWallId, out distanceAlong))
        {
            wall = FindWallById(floorWallId)!;
            return IsWallPickable(wall);
        }

        if (WallReferenceService.TryPickInnerFace(floorPoint, _project.Room.Walls, out WallReferencePick innerPick))
        {
            wall = FindWallById(innerPick.WallId)!;

            if (IsWallPickable(wall))
            {
                distanceAlong = Math.Clamp(wall.GetDistanceAlongWall(innerPick.AnchorOnInnerFace), 0f, wall.Length);
                return true;
            }
        }

        return false;
    }

    private bool TryPickWallFaceAtScreen(
        double mouseX,
        double mouseY,
        out WallSegment wall,
        out float distanceAlong,
        out float heightFromFloor,
        out FaceType face,
        out bool hitTopFace)
    {
        wall = null!;
        distanceAlong = 0f;
        heightFromFloor = 0f;
        face = FaceType.Internal;
        hitTopFace = false;

        if (_project.Room.Walls.Count == 0)
            return false;

        EnsureCameraMatricesForPicking();

        if (!Geometry3D.TryCreateWorldRay(
                mouseX,
                mouseY,
                Viewport.ActualWidth,
                Viewport.ActualHeight,
                _camera.View,
                _camera.Projection,
                out Vector3 origin,
                out Vector3 direction))
            return false;

        var pickTargets = BuildWallPickTargets();

        if (!WallPickService.TryPickRayDetailed(
                origin,
                direction,
                pickTargets,
                out Guid wallId,
                out distanceAlong,
                out heightFromFloor,
                out WallPickService.WallPickFaceKind faceKind,
                out _))
            return false;

        wall = FindWallById(wallId)!;

        if (wall == null)
            return false;

        hitTopFace = faceKind == WallPickService.WallPickFaceKind.Top;

        if (hitTopFace && _camera.ViewMode == CameraViewMode.Top)
            hitTopFace = false;

        face = faceKind == WallPickService.WallPickFaceKind.LateralB
            ? FaceType.External
            : FaceType.Internal;

        return true;
    }

    private bool TryPickWallBandEdgeAtScreen(
        double mouseX,
        double mouseY,
        WallSegment expectedWall,
        out WallBand band,
        out WallBandEdgeKind edge,
        out float edgeValue)
    {
        band = null!;
        edge = WallBandEdgeKind.Start;
        edgeValue = 0f;

        const float toleranceMm = 120f;

        if (!TryPickWallFaceAtScreen(
                mouseX,
                mouseY,
                out WallSegment pickWall,
                out float along,
                out float height,
                out _,
                out bool hitTop) ||
            hitTop ||
            pickWall.Id != expectedWall.Id)
            return false;

        WallBand? bestBand = null;
        WallBandEdgeKind bestEdge = WallBandEdgeKind.Start;
        float bestDist = toleranceMm;

        foreach (var b in expectedWall.Bands)
        {
            if (b.IsHorizontal)
            {
                float dStart = Math.Abs(height - b.StartMm);

                if (dStart < bestDist)
                {
                    bestDist = dStart;
                    bestBand = b;
                    bestEdge = WallBandEdgeKind.Start;
                    edgeValue = b.StartMm;
                }

                float dEnd = Math.Abs(height - b.EndMm);

                if (dEnd < bestDist)
                {
                    bestDist = dEnd;
                    bestBand = b;
                    bestEdge = WallBandEdgeKind.End;
                    edgeValue = b.EndMm;
                }
            }
            else
            {
                float dStart = Math.Abs(along - b.StartMm);

                if (dStart < bestDist)
                {
                    bestDist = dStart;
                    bestBand = b;
                    bestEdge = WallBandEdgeKind.Start;
                    edgeValue = b.StartMm;
                }

                float dEnd = Math.Abs(along - b.EndMm);

                if (dEnd < bestDist)
                {
                    bestDist = dEnd;
                    bestBand = b;
                    bestEdge = WallBandEdgeKind.End;
                    edgeValue = b.EndMm;
                }
            }
        }

        if (bestBand == null)
            return false;

        band = bestBand;
        edge = bestEdge;
        return true;
    }

    private bool TryPickWallRegionEdgeAtScreen(
        double mouseX,
        double mouseY,
        WallSegment expectedWall,
        out WallRegion region,
        out WallRegionEdgeKind edge,
        out float edgeValue)
    {
        region = null!;
        edge = WallRegionEdgeKind.StartAlong;
        edgeValue = 0f;

        const float toleranceMm = 120f;
        float wallTop = MathF.Max(expectedWall.HeightStart, expectedWall.HeightEnd);

        if (!TryPickWallFaceAtScreen(
                mouseX,
                mouseY,
                out WallSegment pickWall,
                out float along,
                out float height,
                out FaceType face,
                out bool hitTop) ||
            hitTop ||
            pickWall.Id != expectedWall.Id)
            return false;

        WallRegion? bestRegion = null;
        WallRegionEdgeKind bestEdge = WallRegionEdgeKind.StartAlong;
        float bestDist = toleranceMm;

        foreach (var r in expectedWall.Regions)
        {
            if (r.Face != face)
                continue;

            if (r.Shape == WallRegionShape.Polygon)
                continue;

            if (r.Shape == WallRegionShape.Circular)
            {
                float dist = WallRegionGeometry.DistanceToBoundary(r, along, height, expectedWall.Length, wallTop);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestRegion = r;
                    bestEdge = WallRegionEdgeKind.Radius;
                    edgeValue = r.RadiusMm;
                }

                continue;
            }

            if (r.Shape == WallRegionShape.Rectangular && MathF.Abs(r.RotationDegrees) > 0.01f)
                continue;

            var (start, end, bottom, top) = WallRegionGeometry.GetEffectiveBounds(r, expectedWall.Length, wallTop);

            ConsiderRegionEdge(r, WallRegionEdgeKind.StartAlong, Math.Abs(along - start), r.StartAlongMm,
                ref bestRegion, ref bestEdge, ref bestDist, ref edgeValue);
            ConsiderRegionEdge(r, WallRegionEdgeKind.EndAlong, Math.Abs(along - end), r.EndAlongMm,
                ref bestRegion, ref bestEdge, ref bestDist, ref edgeValue);
            ConsiderRegionEdge(r, WallRegionEdgeKind.Bottom, Math.Abs(height - bottom), r.BottomMm,
                ref bestRegion, ref bestEdge, ref bestDist, ref edgeValue);
            ConsiderRegionEdge(r, WallRegionEdgeKind.Top, Math.Abs(height - top), r.TopMm,
                ref bestRegion, ref bestEdge, ref bestDist, ref edgeValue);
        }

        if (bestRegion == null)
            return false;

        region = bestRegion;
        edge = bestEdge;
        return true;
    }

    private static void ConsiderRegionEdge(
        WallRegion region,
        WallRegionEdgeKind edgeKind,
        float distance,
        float value,
        ref WallRegion? bestRegion,
        ref WallRegionEdgeKind bestEdge,
        ref float bestDist,
        ref float edgeValue)
    {
        if (distance >= bestDist)
            return;

        bestDist = distance;
        bestRegion = region;
        bestEdge = edgeKind;
        edgeValue = value;
    }

    private List<WallPickTarget> BuildWallPickTargets()
    {
        var result = new List<WallPickTarget>();
        var visualSegments = WallVisualBuilder.BuildWithCorners(_project.Room.Walls);

        foreach (var segment in visualSegments)
        {
            if (!IsWallPickable(segment.Wall))
                continue;

            if (segment.IsCurved &&
                segment.TessellatedFaceA != null &&
                segment.TessellatedFaceB != null)
            {
                for (int i = 0; i < segment.TessellatedFaceA.Count - 1; i++)
                {
                    result.Add(WallPickService.FromSegment(
                        segment.Wall,
                        segment.TessellatedFaceA[i],
                        segment.TessellatedFaceA[i + 1],
                        segment.TessellatedFaceB[i],
                        segment.TessellatedFaceB[i + 1]));
                }

                continue;
            }

            result.Add(WallPickService.FromSegment(
                segment.Wall,
                segment.A1,
                segment.A2,
                segment.B1,
                segment.B2));
        }

        return result;
    }

    private List<ModuleInstance> GetRenderableModules()
    {
        var result = new List<ModuleInstance>();

        foreach (var module in _project.Modules)
        {
            if (ShouldRenderModule(module))
                result.Add(module);
        }

        return result;
    }

    private List<ModuleInstance> GetPickableModules()
    {
        var result = new List<ModuleInstance>();

        foreach (var module in _project.Modules)
        {
            if (IsModulePickable(module))
                result.Add(module);
        }

        return result;
    }

    private void EnsureCameraMatricesForPicking()
    {
        if (Viewport.ActualWidth < 1 || Viewport.ActualHeight < 1)
            return;

        var dpi = VisualTreeHelper.GetDpi(Viewport);
        int width = Math.Max(1, (int)(Viewport.ActualWidth * dpi.DpiScaleX));
        int height = Math.Max(1, (int)(Viewport.ActualHeight * dpi.DpiScaleY));
        SetupCamera(width, height);
    }

    private (WallSegment Wall, WallOpening Opening)? FindOpeningById(Guid openingId)
    {
        foreach (var wall in _project.Room.Walls)
        {
            var opening = wall.FindOpeningById(openingId);

            if (opening != null)
                return (wall, opening);
        }

        return null;
    }

    private void UpdateSelectedOpeningStatus(WallSegment wall, WallOpening opening)
    {
        MeasureBox.Visibility = Visibility.Visible;
        MeasureBox.Text = opening.Width.ToString("0", CultureInfo.InvariantCulture);

        UpdateOpeningPropertyPanel(opening);

        string typeLabel = opening.Type == OpeningType.Window ? "Janela" : "Porta";

        Title =
            $"Tra?os 3D - {typeLabel} selecionada | Largura: {opening.Width:0} mm | " +
            $"Altura: {opening.Height:0} mm | Peitoril: {opening.SillHeight:0} mm | " +
            $"Posi??o: {opening.DistanceFromStart:0} mm | Painel lateral + Enter | Delete remove";

        UpdateStatusBarSelection(typeLabel, opening.Width);
    }

    private void UpdateOpeningPropertyPanel(WallOpening opening)
    {
        _syncingPropertyPanel = true;

        PropertyLengthLabel.Text = "Largura (mm)";
        PropertyHeightLabel.Text = "Altura (mm)";
        PropertyDepthLabel.Text = opening.Type == OpeningType.Window
            ? "Peitoril (mm)"
            : "Posi??o (mm)";

        PropertyLengthBox.Text = opening.Width.ToString("0", CultureInfo.InvariantCulture);
        PropertyHeightBox.Text = opening.Height.ToString("0", CultureInfo.InvariantCulture);
        PropertyDepthBox.Text = opening.Type == OpeningType.Window
            ? opening.SillHeight.ToString("0", CultureInfo.InvariantCulture)
            : opening.DistanceFromStart.ToString("0", CultureInfo.InvariantCulture);

        PropertyHintText.Text = opening.Type == OpeningType.Window
            ? "Largura/altura/peitoril da janela. Enter confirma."
            : "Largura/altura/posi??o da porta. Enter confirma. " +
              "Para comprimento da parede, clique na parede.";

        _syncingPropertyPanel = false;
    }

    private void SetSelectedOpeningWidth(float newWidth)
    {
        if (!_selectedOpeningId.HasValue || newWidth <= 0f)
            return;

        var found = FindOpeningById(_selectedOpeningId.Value);

        if (found == null)
            return;

        var (wall, opening) = found.Value;
        float previousStart = opening.DistanceFromStart;
        float previousWidth = opening.Width;
        opening.Width = newWidth;
        opening.DistanceFromStart = WallOpeningPlacement.ClampStart(
            previousStart,
            opening.Width,
            wall.Length);
        opening.DistanceFromStart = WallOpeningPlacement.SnapDistance(opening.DistanceFromStart);

        if (!WallOpeningPlacement.CanPlace(wall, opening))
        {
            opening.Width = previousWidth;
            opening.DistanceFromStart = previousStart;
            return;
        }

        UpdateSelectedOpeningStatus(wall, opening);
    }

    private void DeleteSelectedOpening()
    {
        if (!_selectedOpeningId.HasValue)
            return;

        foreach (var wall in _project.Room.Walls)
        {
            if (!wall.RemoveOpening(_selectedOpeningId.Value))
                continue;

            _selectedOpeningId = null;
            MeasureBox.Visibility = Visibility.Collapsed;
            Title = "Tra?os 3D - Abertura removida";
            ClearPropertyPanelSelection();
            MarkProjectDirty();
            return;
        }
    }

    private WallSegment? FindWallById(Guid id)
    {
        foreach (var wall in _project.Room.Walls)
        {
            if (wall.Id == id)
                return wall;
        }

        return null;
    }

    private void UpdateSelectedWallStatus(WallSegment wall, string? hintOverride = null)
    {
        float referenceLength = WallInnerFaceService.GetDisplayReferenceLength(wall, _project.Room.Walls);

        if (_wallGroupSelected)
        {
            MeasureBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            MeasureBox.Visibility = Visibility.Visible;
            MeasureBox.Text = referenceLength.ToString("0", CultureInfo.InvariantCulture);
        }

        UpdateWallPropertyPanel(wall, hintOverride);

        int wallCount = _project.Room.Walls.Count;

        if (_wallGroupSelected)
        {
            Title =
                $"Traços 3D - GRUPO ({wallCount} paredes) | " +
                $"Espessura: {wall.Thickness:0} mm | Pé-direito: {wall.Height:0} mm | " +
                "Clique na face para editar um segmento | Delete remove o grupo";

            UpdateStatusBarSelection($"Grupo ({wallCount} paredes)", wall.Height);
        }
        else
        {
            Title =
                $"Traços 3D - FACE | Comprimento: {referenceLength:0} mm | " +
                $"Orientação: {FormatMeasureSideLabel(wall.MeasureSide)} | " +
                $"Espessura: {wall.Thickness:0} mm | Altura: {wall.Height:0} mm | " +
                "Painel lateral + Enter | R alterna Orientação | Clique no topo para o grupo | Delete remove esta parede";

            UpdateStatusBarSelection("Parede", referenceLength);
        }
    }

    private void UpdateWallPropertyPanel(WallSegment wall, string? hintOverride = null)
    {
        _syncingPropertyPanel = true;

        // Mostrar painel de paredes, ocultar demais
        HideWallConstructionPanel();
        WallPropertiesPanel.Visibility = Visibility.Visible;
        FloorPropertiesPanel.Visibility = Visibility.Collapsed;
        ModulePropertiesPanel.Visibility = Visibility.Collapsed;

        float referenceLength = WallInnerFaceService.GetDisplayReferenceLength(wall, _project.Room.Walls);

        // Se??o Dimens?es (comprimento = face de referência / Orientação Promob)
        WallLengthBox.Text        = referenceLength.ToString("0", CultureInfo.InvariantCulture);

        _syncingMeasureSideCombo = true;
        WallMeasureSideCombo.SelectedIndex = wall.MeasureSide == WallMeasureSide.Interior ? 0 : 1;
        _syncingMeasureSideCombo = false;
        WallThicknessBox.Text     = wall.Thickness.ToString("0", CultureInfo.InvariantCulture);
        WallHeightStartBox.Text   = wall.HeightStart.ToString("0", CultureInfo.InvariantCulture);
        WallHeightEndBox.Text     = wall.HeightEnd.ToString("0", CultureInfo.InvariantCulture);
        WallAngleAbsoluteBox.Text = wall.AngleAbsoluteDegrees.ToString("0.0", CultureInfo.InvariantCulture);

        float relAngle = ComputeWallRelativeAngle(wall);
        WallAngleRelativeBox.Text = relAngle.ToString("0.0", CultureInfo.InvariantCulture);

        WallFlechaBox.Text = wall.FlechaMm.ToString("0", CultureInfo.InvariantCulture);
        WallArcAngleBox.Text = WallArcGeometry.FromWall(wall).GetArcAngleDegrees().ToString("0.0", CultureInfo.InvariantCulture);

        // Se??o Cotas
        WallFloorOffsetBox.Text   = wall.FloorOffset.ToString("0", CultureInfo.InvariantCulture);
        WallCotaAnteriorBox.Text  = wall.CotaAnterior.ToString("0", CultureInfo.InvariantCulture);
        WallCotaPosteriorBox.Text = wall.CotaPosterior.ToString("0", CultureInfo.InvariantCulture);
        WallCotaInferiorBox.Text  = wall.CotaInferior.ToString("0", CultureInfo.InvariantCulture);
        WallCotaSuperiorBox.Text  = wall.CotaSuperior.ToString("0", CultureInfo.InvariantCulture);

        // Se??o Desenho
        WallDrawBottomFaceCheck.IsChecked = wall.DrawBottomFace;

        // Se??o Outras
        WallIsMovableCheck.IsChecked = wall.IsMovable;
        WallIsVisibleCheck.IsChecked = wall.IsVisible;
        WallConstructionTypeCombo.SelectedIndex =
            wall.ConstructionType == WallConstructionType.DryWall ? 1 : 0;

        PopulateWallLayerCombo();
        PopulateWallCompartmentCombo();
        _syncingPropertyPanel = true;
        string layerId = WallLayerCatalog.NormalizeLayerId(wall.LayerId);
        WallLayerDefinition? selectedLayer = WallLayerCatalog.GetDefinitions(_project.Metadata)
            .FirstOrDefault(l => l.Id == layerId);
        WallLayerCombo.SelectedItem = selectedLayer;

        Guid compartmentId = RoomCompartmentService.ResolveWallCompartmentId(wall, _project.Room.Compartments);
        WallCompartmentCombo.SelectedItem = RoomCompartmentService.FindCompartment(_project.Room.Compartments, compartmentId);
        _syncingPropertyPanel = false;

        UpdateWallBandsSummary(wall);
        UpdateWallRegionsSummary(wall);
        PopulateWallBandSelector(wall);
        PopulateWallRegionSelector(wall);

        if (string.IsNullOrWhiteSpace(Wall304050ABox.Text))
            Wall304050ABox.Text = WallThirtyFortyFiftyService.DefaultAmm.ToString("0", CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(Wall304050BBox.Text))
            Wall304050BBox.Text = WallThirtyFortyFiftyService.DefaultBmm.ToString("0", CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(Wall304050CBox.Text))
            Wall304050CBox.Text = WallThirtyFortyFiftyService.DefaultCmm.ToString("0", CultureInfo.InvariantCulture);

        if (_wall304050MovingWallId.HasValue)
            Wall304050HintText.Text = "Parede deslocada em vermelho. Ajuste C e clique Aplicar.";
        else
            Wall304050HintText.Text = "A e B a partir do canto na parede de referência; C entre os pontos medidos.";

        int wallCount = _project.Room.Walls.Count;
        WallDimensionHintText.Text = hintOverride ?? (_wallGroupSelected
            ? $"GRUPO: Pé-direito, Espessura e Afastamento Piso aplicam a todas as {wallCount} paredes. Comprimento e Orientação são por face — clique na lateral. Delete remove o grupo."
            : wall.IsMovable
                ? "FACE movível: na vista Planta, clique novamente na parede e arraste perpendicularmente. Esc cancela."
                : "FACE: Comprimento, Orientação e cotas desta parede. Segmentar divide no ponto clicado. Clique no topo horizontal para editar o grupo. Delete remove esta parede.");

        SyncWallPropertyPanelForSelectionMode(_wallGroupSelected);

        if (!_wallGroupSelected)
        {
            WallBandsExpander.IsExpanded = true;
            WallRegionsExpander.IsExpanded = true;
        }

        _syncingPropertyPanel = false;
    }

    private void SyncWallPropertyPanelForSelectionMode(bool groupSelected)
    {
        bool faceEditable = !groupSelected;

        WallLengthBox.IsReadOnly = groupSelected;
        WallLengthBox.Background = groupSelected ? ReadOnlyFieldBrush : EditableFieldBrush;

        WallMeasureSideCombo.IsEnabled = faceEditable;

        WallThicknessBox.IsReadOnly = false;
        WallThicknessBox.Background = EditableFieldBrush;

        WallHeightStartBox.IsReadOnly = false;
        WallHeightStartBox.Background = EditableFieldBrush;

        // Grupo Promob: pé-direito inicial uniformiza ambas as alturas ao confirmar
        WallHeightEndBox.IsReadOnly = groupSelected;
        WallHeightEndBox.Background = groupSelected ? ReadOnlyFieldBrush : EditableFieldBrush;

        WallFlechaBox.IsReadOnly = groupSelected;
        WallFlechaBox.Background = groupSelected ? ReadOnlyFieldBrush : EditableFieldBrush;

        WallFloorOffsetBox.IsReadOnly = false;
        WallFloorOffsetBox.Background = EditableFieldBrush;

        WallCotaAnteriorBox.IsReadOnly = groupSelected;
        WallCotaAnteriorBox.Background = groupSelected ? ReadOnlyFieldBrush : EditableFieldBrush;
        WallCotaPosteriorBox.IsReadOnly = groupSelected;
        WallCotaPosteriorBox.Background = groupSelected ? ReadOnlyFieldBrush : EditableFieldBrush;
        WallCotaInferiorBox.IsReadOnly = groupSelected;
        WallCotaInferiorBox.Background = groupSelected ? ReadOnlyFieldBrush : EditableFieldBrush;
        WallCotaSuperiorBox.IsReadOnly = groupSelected;
        WallCotaSuperiorBox.Background = groupSelected ? ReadOnlyFieldBrush : EditableFieldBrush;

        WallDrawBottomFaceCheck.IsEnabled = faceEditable;
        WallIsMovableCheck.IsEnabled = faceEditable;
        WallIsVisibleCheck.IsEnabled = faceEditable;
        WallConstructionTypeCombo.IsEnabled = faceEditable;
        WallLayerCombo.IsEnabled = faceEditable;
        WallCompartmentCombo.IsEnabled = faceEditable;
        WallAddBandButton.IsEnabled = faceEditable;
        WallAddVerticalBandButton.IsEnabled = faceEditable;
        WallAddRegionButton.IsEnabled = faceEditable;
        WallAddRegionByClickButton.IsEnabled = faceEditable;
        WallAddCircleRegionByClickButton.IsEnabled = faceEditable;
        WallAddPolygonRegionByClickButton.IsEnabled = faceEditable;
        WallRegionFaceCombo.IsEnabled = faceEditable;
        WallFaceMaterialCombo.IsEnabled = faceEditable;
        WallBandSelectorCombo.IsEnabled = faceEditable && WallBandSelectorCombo.Items.Count > 0;
        WallBandMaterialCombo.IsEnabled = faceEditable && WallBandSelectorCombo.IsEnabled;
        WallRegionSelectorCombo.IsEnabled = faceEditable && WallRegionSelectorCombo.Items.Count > 0;
        WallRegionMaterialCombo.IsEnabled = faceEditable && WallRegionSelectorCombo.IsEnabled;
        PropertyRegionOffsetBox.IsEnabled = faceEditable && WallRegionSelectorCombo.IsEnabled;

        bool regionEdgeOffsetEnabled = false;
        bool polygonRegionSelected = false;
        bool cutRegionSelected = false;
        if (faceEditable && WallRegionSelectorCombo.IsEnabled && _selectedWallId.HasValue)
        {
            var selectedWall = FindWallById(_selectedWallId.Value);
            if (selectedWall != null)
            {
                var selectedRegion = GetSelectedWallRegion(selectedWall);
                regionEdgeOffsetEnabled = selectedRegion?.Shape == WallRegionShape.Rectangular &&
                                          MathF.Abs(selectedRegion.RotationDegrees) < 0.01f;
                polygonRegionSelected = selectedRegion?.Shape == WallRegionShape.Polygon;
                cutRegionSelected = selectedRegion != null && selectedRegion.Shape != WallRegionShape.Circular;
            }
        }

        WallAddPolygonVertexButton.IsEnabled = faceEditable && polygonRegionSelected;
        WallRotateRegion90Button.IsEnabled = faceEditable && cutRegionSelected;
        WallVerticalCutRegionButton.IsEnabled = faceEditable && cutRegionSelected;
        if (!_wallRegionVerticalCutMode)
            WallApplyVerticalCutButton.IsEnabled = false;
        PropertyRegionOffsetStartAlongBox.IsEnabled = regionEdgeOffsetEnabled;
        PropertyRegionOffsetEndAlongBox.IsEnabled = regionEdgeOffsetEnabled;
        PropertyRegionOffsetBottomBox.IsEnabled = regionEdgeOffsetEnabled;
        PropertyRegionOffsetTopBox.IsEnabled = regionEdgeOffsetEnabled;
        WallSegmentButton.IsEnabled = faceEditable;
        Wall304050PickMovingButton.IsEnabled = faceEditable;
        Wall304050ApplyButton.IsEnabled = faceEditable;
        Wall304050ABox.IsReadOnly = groupSelected;
        Wall304050BBox.IsReadOnly = groupSelected;
        Wall304050CBox.IsReadOnly = groupSelected;
    }

    private static readonly System.Windows.Media.SolidColorBrush ReadOnlyFieldBrush =
        new(System.Windows.Media.Color.FromRgb(0xEF, 0xEF, 0xEF));

    private static readonly System.Windows.Media.SolidColorBrush EditableFieldBrush =
        System.Windows.Media.Brushes.White;

    private float ComputeWallRelativeAngle(WallSegment wall)
    {
        var walls = _project.Room.Walls;
        int idx = walls.IndexOf(wall);

        if (idx <= 0 || walls.Count < 2)
            return wall.AngleAbsoluteDegrees;

        var prev = walls[idx - 1];
        float delta = wall.AngleAbsoluteDegrees - prev.AngleAbsoluteDegrees;

        while (delta >  180f) delta -= 360f;
        while (delta < -180f) delta += 360f;
        return delta;
    }

    private void SetSelectedWallLength(float newLength)
    {
        if (!_selectedWallId.HasValue || newLength <= 0 || _wallGroupSelected)
            return;

        var wall = FindWallById(_selectedWallId.Value);

        if (wall == null)
            return;

        WallInnerFaceService.ApplyReferenceLengthToWall(wall, _project.Room.Walls, newLength);
        _project.Room.RebuildAutomaticFloor();
        MarkProjectDirty();
        UpdateSelectedWallStatus(wall);
    }

    private static bool PointInsideQuad(Vector2 p, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        return PointInTriangle(p, a, b, c) || PointInTriangle(p, a, c, d);
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNegative = d1 < 0 || d2 < 0 || d3 < 0;
        bool hasPositive = d1 > 0 || d2 > 0 || d3 > 0;

        return !(hasNegative && hasPositive);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }


    private void DeleteSelectedWall()
    {
        if (!_selectedWallId.HasValue)
            return;

        // Com grupo selecionado: Delete remove TODAS as paredes
        if (_wallGroupSelected && _project.Room.Walls.Count > 1)
        {
            _project.Room.Walls.Clear();
            _project.Room.RecalculateClosedState();
            _project.Room.RebuildAutomaticFloor();
            _wallGroupSelected = false;
            _selectedWallId = null;
            MeasureBox.Visibility = Visibility.Collapsed;
            Title = "Tra?os 3D - Todas as paredes removidas";
            ClearPropertyPanelSelection();
            MarkProjectDirty();
            return;
        }

        // Face individual: remove s? a parede selecionada
        WallSegment? found = null;

        foreach (var wall in _project.Room.Walls)
        {
            if (wall.Id == _selectedWallId.Value)
            {
                found = wall;
                break;
            }
        }

        if (found != null)
        {
            _project.Room.Walls.Remove(found);
            _project.Room.RecalculateClosedState();
            _project.Room.RebuildAutomaticFloor();
            _wallGroupSelected = false;
            _selectedWallId = null;
            MeasureBox.Visibility = Visibility.Collapsed;
            Title = "Tra?os 3D - Parede removida";
            ClearPropertyPanelSelection();
            MarkProjectDirty();
        }
    }

    private static float DistancePointToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.LengthSquared;

        if (lengthSquared < 0.001f)
            return (point - start).Length;

        float t = Vector2.Dot(point - start, segment) / lengthSquared;
        t = Math.Clamp(t, 0f, 1f);

        Vector2 projected = start + segment * t;

        return (point - projected).Length;
    }

    private static void DrawLine(Vector2 p1, Vector2 p2, float y, Vector4? color = null)
    {
        RenderEngine.Line(p1, p2, y, color);
    }

    // Aresta de topo com alturas diferentes em cada extremidade (p?-direito vari?vel)
    private static void DrawLine(Vector2 p1, Vector2 p2, float y1, float y2, Vector4? color = null)
    {
        RenderEngine.Line(new Vector3(p1.X, y1, p1.Y), new Vector3(p2.X, y2, p2.Y), color);
    }

    private static void DrawVertical(Vector2 p, float h, Vector4? color = null)
    {
        RenderEngine.Line(new Vector3(p.X, 0, p.Y), new Vector3(p.X, h, p.Y), color);
    }

    // Aresta vertical de yStart at? yEnd (para FloorOffset > 0 e p?-direito vari?vel)
    private static void DrawVerticalRange(Vector2 p, float yStart, float yEnd, Vector4? color = null)
    {
        RenderEngine.Line(new Vector3(p.X, yStart, p.Y), new Vector3(p.X, yEnd, p.Y), color);
    }
}
