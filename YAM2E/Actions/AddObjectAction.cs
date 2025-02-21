using LAMP.Classes;
using LAMP.Controls;
using LAMP.Interfaces;
using System.Drawing;

namespace LAMP.Actions;

public class AddObjectAction : IAction
{
    public AddObjectAction(Enemy targetEnemy, int bank, int screen, RoomViewer roomViewer)
    {
        this.TargetEnemy = targetEnemy;
        this.TargetRoom = roomViewer;
        this.TargetBank = bank;
        this.TargetScreen = screen;

        OldEnemyNumber = TargetEnemy.Number;
        OldEnemyType = TargetEnemy.Type;
        OldEnemyScreenX = TargetEnemy.sX;
        OldEnemyScreenY = TargetEnemy.sY;
    }

    private int TargetBank;
    private int TargetScreen;

    //Enemy info
    private byte OldEnemyNumber;
    private byte OldEnemyType;
    private byte OldEnemyScreenX;
    private byte OldEnemyScreenY;

    private Enemy TargetEnemy;
    private RoomViewer TargetRoom;

    public string Description => throw new System.NotImplementedException();

    private void SetEnemyCoords()
    {
        TargetEnemy.Number = OldEnemyNumber;
        TargetEnemy.Type = OldEnemyType;
        TargetEnemy.sX = OldEnemyScreenX;
        TargetEnemy.sY = OldEnemyScreenY;
    }

    public void Do()
    {
        SetEnemyCoords();

        int screen = TargetScreen + 256 * TargetBank;
        Globals.Objects[screen].Add(TargetEnemy);

        //Invalidating graphics
        int X = (TargetScreen % 16) * 256;
        int Y = (TargetScreen / 16) * 256;
        TargetRoom.Invalidate(new Rectangle((X + TargetEnemy.sX - 8) * TargetRoom.Zoom, (Y + TargetEnemy.sY - 8) * TargetRoom.Zoom, TargetRoom.TileSize, TargetRoom.TileSize));
    }

    public void Undo()
    {
        SetEnemyCoords();

        new DeleteObjectAction(TargetEnemy, TargetBank, TargetScreen, TargetRoom).Do();
    }
}
