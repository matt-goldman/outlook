using Font = Microsoft.Maui.Graphics.Font;

namespace OutlookClone.Controls;

public class Unselected : IDrawable
{
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.BlendMode = BlendMode.DestinationOut;

        canvas.FontSize = 18;

        canvas.FontColor = Colors.White;

        canvas.DrawString("Homer", 10, 0, 60, 25, HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.DrawString("Other", 75, 0, 70, 25, HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.FillColor = Color.FromHex("f1edec");

        canvas.FillRoundedRectangle(0, -2, 72.5f, 30, 15f);
    }
}