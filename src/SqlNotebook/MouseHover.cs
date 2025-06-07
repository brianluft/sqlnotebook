using System;
using System.Windows.Forms;

namespace SqlNotebook;

public enum MouseHoverState
{
    Normal,
    Hover,
    Down,
}

public sealed class MouseHover
{
    private readonly Control _control;

    public bool MouseIsOver { get; private set; }
    public bool MouseIsDown { get; private set; }

    public MouseHoverState State =>
        MouseIsDown && MouseIsOver ? MouseHoverState.Down
        : MouseIsDown || MouseIsOver ? MouseHoverState.Hover
        : MouseHoverState.Normal;

    public bool HoverOrDown => MouseIsDown || MouseIsOver;

    public event EventHandler Click;

    public MouseHover(Control control)
    {
        _control = control;
        control.MouseEnter += Control_MouseEnter;
        control.MouseLeave += Control_MouseLeave;
        control.MouseDown += Control_MouseDown;
        control.MouseUp += Control_MouseUp;
        control.MouseMove += Control_MouseMove;
    }

    private void Control_MouseEnter(object sender, EventArgs e)
    {
        MouseIsOver = true;
        _control.Invalidate();
    }

    private void Control_MouseLeave(object sender, EventArgs e)
    {
        MouseIsOver = false;
        _control.Invalidate();
    }

    private void Control_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            MouseIsDown = true;
            _control.Invalidate();
            _control.Capture = true;
        }
    }

    private void Control_MouseUp(object sender, MouseEventArgs e)
    {
        if (MouseIsDown)
        {
            MouseIsDown = false;
            _control.Invalidate();
            _control.Capture = false;

            if (MouseIsOver)
            {
                Click?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void Control_MouseMove(object sender, MouseEventArgs e)
    {
        if (_control.ClientRectangle.Contains(_control.PointToClient(Control.MousePosition)))
        {
            if (!MouseIsOver)
            {
                MouseIsOver = true;
                _control.Invalidate();
            }
        }
        else
        {
            if (MouseIsOver)
            {
                MouseIsOver = false;
                _control.Invalidate();
            }
        }
    }
}
