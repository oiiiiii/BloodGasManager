using System.Windows.Forms;

namespace BGManager.Views;

public class CustomRenderer : ToolStripProfessionalRenderer
{
    private readonly Color _backgroundColor = Color.White;
    private readonly Color _foregroundColor = Color.Black;
    private readonly Color _hoverColor = Color.FromArgb(230, 230, 230);
    private readonly Color _pressedColor = Color.FromArgb(200, 200, 200);
    private readonly Font _font = new Font("微软雅黑", 12f);

    public CustomRenderer() : base(new CustomColorTable())
    {
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        e.Graphics.FillRectangle(new SolidBrush(_backgroundColor), e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
    }

    protected override void OnRenderLabelBackground(ToolStripItemRenderEventArgs e)
    {
        var label = e.Item as ToolStripStatusLabel;
        if (label != null)
        {
            e.Graphics.FillRectangle(new SolidBrush(_backgroundColor), e.Item.Bounds);
        }
        else
        {
            base.OnRenderLabelBackground(e);
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = _foregroundColor;
        e.Item.Font = _font;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
    {
        var button = e.Item as ToolStripButton;
        if (button == null)
        {
            base.OnRenderButtonBackground(e);
            return;
        }

        var g = e.Graphics;
        var bounds = new Rectangle(Point.Empty, e.Item.Size);

        if (button.Selected || button.Pressed)
        {
            g.FillRectangle(new SolidBrush(_pressedColor), bounds);
        }
        else if (e.Item.Selected)
        {
            g.FillRectangle(new SolidBrush(_hoverColor), bounds);
        }
        else
        {
            g.FillRectangle(new SolidBrush(_backgroundColor), bounds);
        }
    }
}

public class CustomColorTable : ProfessionalColorTable
{
    public override Color ToolStripGradientBegin => Color.White;
    public override Color ToolStripGradientMiddle => Color.White;
    public override Color ToolStripGradientEnd => Color.White;
    public override Color StatusStripGradientBegin => Color.White;
    public override Color StatusStripGradientEnd => Color.White;
    public override Color MenuItemSelected => Color.FromArgb(230, 230, 230);
    public override Color MenuItemSelectedGradientBegin => Color.FromArgb(230, 230, 230);
    public override Color MenuItemSelectedGradientEnd => Color.FromArgb(230, 230, 230);
    public override Color MenuItemPressedGradientBegin => Color.FromArgb(200, 200, 200);
    public override Color MenuItemPressedGradientEnd => Color.FromArgb(200, 200, 200);
    public override Color ButtonSelectedGradientBegin => Color.FromArgb(230, 230, 230);
    public override Color ButtonSelectedGradientEnd => Color.FromArgb(230, 230, 230);
    public override Color ButtonPressedGradientBegin => Color.FromArgb(200, 200, 200);
    public override Color ButtonPressedGradientEnd => Color.FromArgb(200, 200, 200);
}