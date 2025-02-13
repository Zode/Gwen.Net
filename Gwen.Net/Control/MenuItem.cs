﻿using System;
using Gwen.Net.Control.Internal;

namespace Gwen.Net.Control
{
    /// <summary>
    /// Menu item.
    /// </summary>
    [Xml.XmlControl(CustomHandler = "XmlElementHandler")]
    public class MenuItem : Button
    {
        private bool m_Checkable;
        private bool m_Checked;
        private Menu m_Menu;
        private ControlBase m_SubmenuArrow;
        private Label m_Accelerator;

        /// <summary>
        /// Indicates whether the item is on a menu strip.
        /// </summary>
        public bool IsOnStrip { get { return Parent is MenuStrip; } }

        /// <summary>
        /// Determines if the menu item is checkable.
        /// </summary>
        [Xml.XmlProperty]
        public bool IsCheckable { get { return m_Checkable; } set { m_Checkable = value; } }

        /// <summary>
        /// Indicates if the parent menu is open.
        /// </summary>
        public bool IsMenuOpen { get { if (m_Menu == null) return false; return !m_Menu.IsCollapsed; } }

        /// <summary>
        /// Gets or sets the check value.
        /// </summary>
        [Xml.XmlProperty]
        public bool IsChecked
        {
            get { return m_Checked; }
            set
            {
                if (value == m_Checked)
                    return;

                m_Checked = value;

                if (CheckChanged != null)
                    CheckChanged.Invoke(this, EventArgs.Empty);

                if (value)
                {
                    if (Checked != null)
                        Checked.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    if (UnChecked != null)
                        UnChecked.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets the parent menu.
        /// </summary>
        public Menu Menu
        {
            get
            {
                if (null == m_Menu)
                {
                    m_Menu = new Menu(GetCanvas());
                    m_Menu.ParentMenuItem = this;

                    if (!IsOnStrip)
                    {
                        if (m_SubmenuArrow != null)
                            m_SubmenuArrow.Dispose();
                        m_SubmenuArrow = new RightArrow(this);
                    }
                }

                return m_Menu;
            }
        }

        /// <summary>
        /// Invoked when the item is selected.
        /// </summary>
        [Xml.XmlEvent]
        public event GwenEventHandler<ItemSelectedEventArgs> Selected;

        /// <summary>
        /// Invoked when the item is checked.
        /// </summary>
        [Xml.XmlEvent]
        public event GwenEventHandler<EventArgs> Checked;

        /// <summary>
        /// Invoked when the item is unchecked.
        /// </summary>
        [Xml.XmlEvent]
        public event GwenEventHandler<EventArgs> UnChecked;

        /// <summary>
        /// Invoked when the item's check value is changed.
        /// </summary>
        [Xml.XmlEvent]
        public event GwenEventHandler<EventArgs> CheckChanged;

        /// <summary>
        /// Invoked just before when the menu is opened
        /// </summary>
        public event GwenEventHandler<EventArgs> Opened;

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuItem"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public MenuItem(ControlBase parent)
            : base(parent)
        {
            IsTabable = false;
            IsCheckable = false;
            IsChecked = false;
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            skin.DrawMenuItem(this, IsMenuOpen, m_Checkable ? m_Checked : false);
        }

        protected override Size Measure(Size availableSize)
        {
            Size size = base.Measure(availableSize);
            if (m_Accelerator != null)
            {
                Size accSize = m_Accelerator.DoMeasure(availableSize);
                size.Width += accSize.Width;
            }
            if (m_SubmenuArrow != null)
            {
                m_SubmenuArrow.DoMeasure(availableSize);
            }

            return size;
        }

        protected override Size Arrange(Size finalSize)
        {
            if (m_SubmenuArrow != null)
                m_SubmenuArrow.DoArrange(new Rectangle(finalSize.Width - Padding.Right - m_SubmenuArrow.MeasuredSize.Width, (finalSize.Height - m_SubmenuArrow.MeasuredSize.Height) / 2, m_SubmenuArrow.MeasuredSize.Width, m_SubmenuArrow.MeasuredSize.Height));

            if (m_Accelerator != null)
                m_Accelerator.DoArrange(new Rectangle(finalSize.Width - Padding.Right - m_Accelerator.MeasuredSize.Width, (finalSize.Height - m_Accelerator.MeasuredSize.Height) / 2, m_Accelerator.MeasuredSize.Width, m_Accelerator.MeasuredSize.Height));

            return base.Arrange(finalSize);
        }

        /// <summary>
        /// Internal OnPressed implementation.
        /// </summary>
        protected override void OnClicked(int x, int y, bool virtualClick = false)
        {
            Point reverse = CanvasPosToLocal(new(x, y));

            if((reverse.X < 0 || reverse.X > ActualWidth ||
                reverse.Y < 0 || reverse.Y > ActualHeight) && !virtualClick)
            {
                return;
            }

            if (m_Menu != null)
            {
                if (!IsMenuOpen)
                    OpenMenu();
            }
            else if (!IsOnStrip)
            {
                IsChecked = !IsChecked;
                Selected?.Invoke(this, new ItemSelectedEventArgs(this));
                GetCanvas().CloseMenus();
            }
            
            base.OnClicked(x, y, virtualClick);
        }

        /// <summary>
        /// Toggles the menu open state.
        /// </summary>
        public void ToggleMenu()
        {
            if (IsMenuOpen)
                CloseMenu();
            else
                OpenMenu();
        }

        /// <summary>
        /// Opens the menu.
        /// </summary>
        public void OpenMenu()
        {
            if (null == m_Menu) return;

            Opened?.Invoke(this, EventArgs.Empty);

            m_Menu.Show();
            m_Menu.BringToFront();

            Point p = LocalPosToCanvas(Point.Zero);

            // Strip menus open downwards
            if (IsOnStrip)
            {
                m_Menu.Position = new Point(p.X, p.Y + ActualHeight - 2);
            }
            // Submenus open sidewards
            else
            {
                m_Menu.Position = new Point(p.X + ActualWidth, p.Y);
            }

            // TODO: Option this.
            // TODO: Make sure on screen, open the other side of the 
            // parent if it's better...
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        public void CloseMenu()
        {
            if (null == m_Menu) return;
            m_Menu.Close();
            m_Menu.CloseAll();
        }

        public MenuItem SetAction(GwenEventHandler<EventArgs> handler)
        {
            if (m_Accelerator != null)
            {
                AddAccelerator(m_Accelerator.Text, handler);
            }

            Selected += handler;
            return this;
        }

        public void SetAccelerator(string acc)
        {
            if (m_Accelerator != null)
            {
                m_Accelerator = null;
            }

            if (acc == String.Empty)
                return;

            m_Accelerator = new Label(this);
            m_Accelerator.Text = acc;
            m_Accelerator.Margin = new Margin(0, 0, 16, 0);
        }

        public override ControlBase FindChildByName(string name, bool recursive = false)
        {
            ControlBase item = base.FindChildByName(name, recursive);
            if (item == null && m_Menu != null)
            {
                item = m_Menu.FindChildByName(name, recursive);
            }

            return item;
        }

        internal static ControlBase XmlElementHandler(Xml.Parser parser, Type type, ControlBase parent)
        {
            MenuItem element = new MenuItem(parent);
            parser.ParseAttributes(element);
            if (parser.MoveToContent())
            {
                ControlBase e = parent;
                while (e != null && e.Component == null)
                    e = e.Parent;

                foreach (string elementName in parser.NextElement())
                {
                    if (elementName == "MenuItem")
                        element.Menu.AddItem(parser.ParseElement<MenuItem>(element));
                    else if (elementName == "MenuDivider")
                        element.Menu.AddDivider();

                    element.Menu.Component = e != null ? e.Component : null;
                }
            }
            return element;
        }
    }
}