﻿using System;
using Gwen.Net.Control.Layout;

namespace Gwen.Net.Control
{
    /// <summary>
    /// List box row (selectable).
    /// </summary>
	[Xml.XmlControl(CustomHandler = "XmlElementHandler")]
    public class ListBoxRow : TableRow
    {
        private bool m_Selected;

        private ListBox m_ListBox;
        public ListBox ListBox { get { return m_ListBox; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListBoxRow"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public ListBoxRow(ControlBase parent)
            : base(parent)
        {
            m_ListBox = parent as ListBox;

            MouseInputEnabled = true;
            IsSelected = false;
        }

        /// <summary>
        /// Indicates whether the control is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return m_Selected; }
            set
            {
                m_Selected = value;
                if (value)
                    SetTextColor(Skin.Colors.ListBox.Text_Selected);
                else
                    SetTextColor(Skin.Colors.ListBox.Text_Normal);
            }
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            skin.DrawListBoxLine(this, IsSelected, EvenRow);
        }

        /// <summary>
        /// Handler invoked on mouse click (left) event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="down">If set to <c>true</c> mouse button is down.</param>
        protected override void OnMouseClickedLeft(int x, int y, bool down, bool virtualClick = false)
        {
            base.OnMouseClickedLeft(x, y, down, virtualClick);
            if (down)
            {
                //IsSelected = true; // [omeg] ListBox manages that
                OnRowSelected();
            }
        }

        internal static ControlBase XmlElementHandler(Xml.Parser parser, Type type, ControlBase parent)
        {
            ListBoxRow element = new ListBoxRow(parent);
            parser.ParseAttributes(element);
            if (parser.MoveToContent())
            {
                int colIndex = 1;
                foreach (string elementName in parser.NextElement())
                {
                    if (elementName == "Column")
                    {
                        if (parser.MoveToContent())
                        {
                            ControlBase column = parser.ParseElement(element);
                            element.SetCellContents(colIndex++, column, true);
                        }
                        else
                        {
                            string colText = parser.GetAttribute("Text");
                            element.SetCellText(colIndex++, colText != null ? colText : String.Empty);
                        }
                    }
                }
            }
            return element;
        }
    }
}