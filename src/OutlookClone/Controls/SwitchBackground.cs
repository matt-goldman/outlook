namespace OutlookClone.Controls;

public class SwitchBackground : IDrawable
{
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Color.FromHex("0160a9");

        canvas.FillRoundedRectangle(0, 0, 145, 30, 15f);
    }
}
