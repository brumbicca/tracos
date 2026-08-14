using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class WallDraftTests
{
    [Fact]
    public void BuildRoom_RetanguloFechado_GeraQuatroParedes()
    {
        var draft = new WallDraft
        {
            Thickness = 150f,
            Height = 2600f
        };

        draft.Start(new Vector2(0, 0));
        draft.ConfirmPoint(new Vector2(2000, 0));
        draft.ConfirmPoint(new Vector2(2000, 1500));
        draft.ConfirmPoint(new Vector2(0, 1500));
        draft.CloseSmart();

        var room = draft.BuildRoom();

        Assert.Equal(WallDraftState.Closed, draft.State);
        Assert.Equal(4, room.Walls.Count);
        Assert.True(room.IsClosed);
    }

    [Fact]
    public void UndoLastConfirmedPoint_RemoveUltimoVertice_ReconstróiRoom()
    {
        var draft = new WallDraft();
        draft.Start(new Vector2(0, 0));
        draft.ConfirmPoint(new Vector2(2000, 0));
        draft.ConfirmPoint(new Vector2(2000, 1500));

        Assert.Equal(2, draft.BuildRoom().Walls.Count);

        Assert.True(draft.UndoLastConfirmedPoint());
        Assert.Single(draft.BuildRoom().Walls);
        Assert.Equal(new Vector2(2000, 0), draft.Points[^1]);
    }

    [Fact]
    public void BuildWalls_ParedeComComprimentoMinimo_IgnoraSegmentoCurto()
    {
        var draft = new WallDraft();
        draft.Start(new Vector2(0, 0));
        draft.ConfirmPoint(new Vector2(0, 0));

        var walls = draft.BuildWalls();

        Assert.Empty(walls);
    }

    [Fact]
    public void SetLengthAndConfirm_DefineComprimentoExato()
    {
        var draft = new WallDraft();
        draft.Start(new Vector2(0, 0));
        draft.MovePreview(new Vector2(2500f, 0));
        draft.SetLengthAndConfirm(2500f);

        Assert.Equal(2, draft.Points.Count);
        Assert.Equal(2500f, draft.Points[1].X, precision: 1);
        Assert.Equal(0f, draft.Points[1].Y, precision: 1);
    }
}
