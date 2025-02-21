using System.Drawing;
using System.Text.Json.Serialization;

namespace LAMP.Classes.M2_Data;

public class GameScreen
{
    //constructor
    [JsonConstructor]
    public GameScreen() { }
    public GameScreen(Pointer offset)
    {
        Data = new byte[256];
        Editor.ROM.ReadBytes(offset.Offset, Data, 256);
    }

    public byte[] Data { get; set; }

    [JsonIgnore]
    public Bitmap Image
    {
        get => image;
        set
        {
            if (image != null) image.Dispose();
            image = value;
        }
    }
    Bitmap image;
}
