using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace OutlookClone.Controls;

public class OutlookToggle : SKCanvasView
{
    #region BIndable Properties
    public static readonly BindableProperty OnTextProperty = BindableProperty.Create(
        nameof(OnText),
        typeof(string),
        typeof(OutlookToggle),
        "Focused",
        propertyChanged: Redraw);
    public string OnText { get => (string)GetValue(OnTextProperty); set => SetValue(OnTextProperty, value); }
    
    public static readonly BindableProperty OffTextProperty = BindableProperty.Create(
        nameof(OffText),
        typeof(string),
        typeof(OutlookToggle),
        "Other",
        propertyChanged: Redraw);
    public string OffText { get => (string)GetValue(OffTextProperty); set => SetValue(OffTextProperty, value); }
    
    public static readonly BindableProperty IsToggledProperty = BindableProperty.Create(
        nameof(IsToggled),
        typeof(bool),
        typeof(OutlookToggle),
        false,
        propertyChanged: (b, o, n) => ((OutlookToggle)b).AnimateToggle((bool)n));
    public bool IsToggled { get => (bool)GetValue(IsToggledProperty); set => SetValue(IsToggledProperty, value); }
    
    public static readonly BindableProperty SelectedTextColorProperty = BindableProperty.Create(
        nameof(SelectedTextColor),
        typeof(Color),
        typeof(OutlookToggle),
        Colors.White,
        propertyChanged: Redraw);
    public Color SelectedTextColor { get => (Color)GetValue(SelectedTextColorProperty); set => SetValue(SelectedTextColorProperty, value); }
    
    public static readonly BindableProperty UnselectedTextColorProperty = BindableProperty.Create(
        nameof(UnselectedTextColor),
        typeof(Color),
        typeof(OutlookToggle),
        Colors.Blue,
        propertyChanged: Redraw);
    public Color UnselectedTextColor { get => (Color)GetValue(UnselectedTextColorProperty); set => SetValue(UnselectedTextColorProperty, value); }
    
    public static readonly BindableProperty PillColorProperty = BindableProperty.Create(
        nameof(PillColor),
        typeof(Color),
        typeof(OutlookToggle),
        Colors.Blue,
        propertyChanged: Redraw);
    public Color PillColor { get => (Color)GetValue(PillColorProperty); set => SetValue(PillColorProperty, value); }
    
    public static readonly BindableProperty PillBackgroundColorProperty = BindableProperty.Create(
        nameof(PillBackgroundColor),
        typeof(Color),
        typeof(OutlookToggle),
        Colors.LightGray,
        propertyChanged: Redraw);
    public Color PillBackgroundColor { get => (Color)GetValue(PillBackgroundColorProperty); set => SetValue(PillBackgroundColorProperty, value); }
    
    public static readonly BindableProperty PaddingProperty = BindableProperty.Create(
        nameof(Padding),
        typeof(float),
        typeof(OutlookToggle),
        12f,
        propertyChanged: Redraw);
    public float Padding { get => (float)GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }
    
    public event EventHandler<bool> ToggleChanged;
#endregion

    float animationProgress = 0f;

    public OutlookToggle()
    {
        EnableTouchEvents = true;
        Touch += (s, e) => {
            IsToggled = !IsToggled;
            ToggleChanged?.Invoke(this, IsToggled);
            AnimateToggle(IsToggled);
        };
    }

    private static void Redraw(BindableObject bindable, object oldVal, object newVal)
    {
        ((OutlookToggle)bindable).InvalidateSurface();
    }

    private void AnimateToggle(bool newState)
    {
        var start = animationProgress;
        var end = newState ? 1f : 0f;
        var animation = new Animation(v => {
            animationProgress = (float)v;
            InvalidateSurface();
        }, start, end);

        animation.Commit(this, "ToggleAnimation", 16, 200, Easing.SinInOut);
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        canvas.Clear();

        var info = e.Info;
        var width = info.Width;
        var height = info.Height;

        using var pillBgPaint = new SKPaint { Color = PillBackgroundColor.ToSKColor(), IsAntialias = true };
        using var pillPaint = new SKPaint { Color = PillColor.ToSKColor(), IsAntialias = true };
        using var textPaint = new SKPaint { IsAntialias = true };
        using var textFont = new SKFont { Size = height * 0.4f, Typeface = SKTypeface.Default };

        var onWidth = textFont.MeasureText(OnText) + Padding * 4;
        var offWidth = textFont.MeasureText(OffText) + Padding * 3;
        var gap = height * 0.025f;

        var totalWidth = onWidth + offWidth + gap;
        var startX = (width - totalWidth) / 2f;
        var midY = height / 2f;

        var onStart = startX;
        var offStart = onStart + onWidth + gap;

        var pillHeight = height * 0.6f;
        var pillRadius = pillHeight / 2f;
        var pillY = midY - pillRadius;

        var rightEdge = offStart + offWidth;

        // Draw static background pill first
        var bgRect = new SKRoundRect(new SKRect(onStart, pillY, rightEdge, pillY + pillHeight), pillRadius);
        canvas.DrawRoundRect(bgRect, pillBgPaint);

        // Animate active pill
        var animStart = Lerp(offStart, onStart, animationProgress);
        var animWidth = Lerp(offWidth, onWidth, animationProgress);
        var activeRect = new SKRoundRect(new SKRect(animStart, pillY, animStart + animWidth, pillY + pillHeight), pillRadius);
        canvas.DrawRoundRect(activeRect, pillPaint);

        // Draw unselected text on top of background
        textPaint.Color = UnselectedTextColor.ToSKColor();
        canvas.DrawText(OnText, onStart + Padding * 2, midY + pillRadius / 2f, textFont, textPaint);
        canvas.DrawText(OffText, offStart + Padding, midY + pillRadius / 2f, textFont, textPaint);

        // Draw selected text only inside active pill using local SaveLayer mask
        canvas.SaveLayer();

        // Draw mask shape
        using var maskPaint = new SKPaint();
        maskPaint.Color = PillColor.ToSKColor();
        maskPaint.IsAntialias = true;
        canvas.DrawRoundRect(activeRect, maskPaint);

        // Draw selected text with SrcIn blend mode
        textPaint.Color = SelectedTextColor.ToSKColor();
        textPaint.BlendMode = SKBlendMode.SrcIn;
        canvas.DrawText(OnText, onStart + Padding * 2, midY + pillRadius / 2f, textFont, textPaint);
        canvas.DrawText(OffText, offStart + Padding, midY + pillRadius / 2f, textFont, textPaint);

        // Reset blend mode and restore
        textPaint.BlendMode = SKBlendMode.SrcOver;
        canvas.Restore();
    }

    float Lerp(float from, float to, float progress) => from + (to - from) * progress;
}