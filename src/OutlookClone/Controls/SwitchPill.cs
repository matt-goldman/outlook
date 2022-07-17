namespace OutlookClone.Controls;

public class SwitchPill : IDrawable
{
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.BlendMode = BlendMode.Xor;

        canvas.FillColor = Color.FromHex("f1edec");

        canvas.FillRoundedRectangle(0, 0, 72.5f, 30, 15f);
    }
}
