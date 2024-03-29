﻿using System;
using System.Numerics;
using cyUtility;
using System.Drawing;

namespace cylib
{
    public delegate void DropDownEvent(int selected);

    /// <summary>
    /// Drop down menu. Opens when clicked.
    /// 
    /// Needs some small additions:
    ///     For large lists, we need a scrollbar
    ///     Need to open list upwards if we're near the bottom of the screen
    /// </summary>
    public class DropDownMenu : IDisposable
    {
        const float fontShrink = 14;
        const float fontBufferX = 5;
        const float menuBufferY = 3;
        const float dropGap = 4;
        const float borderRadius = 4;

        Renderer renderer;
        EventManager em;
        FontRenderer font;
        RoundedRectangle_2D rect;

        //the background of the dropdown part
        RoundedRectangle_2D dropBg;
        //the selection highlight around the currently selected item in the list
        RoundedRectangle_2D selHl;
        //the mouse highlight over the item currently being touched by the mouse
        RoundedRectangle_2D mouseHl;

        ScrollBar scrollbar;

        private Vector2 pos;
        public Vector2 Position
        {
            get
            {
                return pos;
            }
            set
            {
                pos = value;
                recalcPositions();
            }
        }

        private Vector2 scale;
        public Vector2 Scale
        {
            get
            {
                return scale;
            }
            set
            {
                scale = value;
                recalcPositions();
            }
        }

        FontRenderer[] labelFonts;

        int _sel;
        public int Selection
        {
            get
            {
                return _sel;
            }
            set
            {
                if (_sel == value)
                    return;

                if (Selection < 0 || Selection >= labelFonts.Length)
                {
                    Logger.WriteLine(LogType.ERROR, "Setting CycleMenu selection to an invalid number: " + Selection + " " + labelFonts.Length);
                    return;
                }

                _sel = value;
                font.Text = labelFonts[_sel].Text;
                recalcPositions();

                if (onSelectionChange != null)
                    onSelectionChange(_sel);
            }
        }
        int _mouseHover = -1;
        int mouseHover
        {
            get
            {
                return _mouseHover;
            }
            set
            {
                if (_mouseHover == value)
                    return;

                _mouseHover = value;

                mouseHl.enabled = value >= 0 && isOpen;
                recalcPositions();
            }
        }

        private void recalcPositions()
        {
            rect.position = pos;
            rect.scale = scale;

            font.Pos.X = pos.X + fontBufferX;
            font.Pos.Y = pos.Y + scale.Y / 2;
            font.Scale = scale.Y - fontShrink;

            dropBg.scale.X = scale.X;
            dropBg.scale.Y = Math.Min(getMaxDropHeight(), getTotalDropHeight());

            dropBg.position.X = pos.X;
            dropBg.position.Y = pos.Y + scale.Y + dropGap;

            if (dropBg.position.Y + dropBg.scale.Y > renderer.ResolutionHeight)
            {
                dropBg.position.Y = pos.Y - dropGap - dropBg.scale.Y;
            }

            for (int i = 0; i < labelFonts.Length; i++)
            {
                labelFonts[i].Pos.X = pos.X + fontBufferX;
                labelFonts[i].Pos.Y = dropBg.position.Y + i * scale.Y + scale.Y / 2 + scrollAmount;
                labelFonts[i].Scale = scale.Y - fontShrink - menuBufferY;

                float bPosY = dropBg.position.Y + i * scale.Y + scrollAmount;
                labelFonts[i].boundsPos.X = pos.X;
                labelFonts[i].boundsPos.Y = Math.Max(bPosY, dropBg.position.Y);
                labelFonts[i].boundsScale.X = scale.X - fontBufferX - scale.Y;
                labelFonts[i].boundsScale.Y = Math.Max(Math.Min(bPosY + scale.Y, dropBg.position.Y + dropBg.scale.Y) - labelFonts[i].boundsPos.Y, 0);
            }

            if (Selection >= 0)
            {
                selHl.position = labelFonts[Selection].boundsPos;
                selHl.scale = labelFonts[Selection].boundsScale;
            }

            if (mouseHover >= 0)
            {
                mouseHl.position = labelFonts[mouseHover].boundsPos;
                mouseHl.scale = labelFonts[mouseHover].boundsScale;
            }

            if (getTotalDropHeight() > getMaxDropHeight())
            {
                scrollbar.Position = new Vector2(dropBg.position.X + scale.X - scale.Y * 3f / 4f, dropBg.position.Y + scale.Y / 4);
                scrollbar.Scale = new Vector2(scale.Y / 2, dropBg.scale.Y - scale.Y / 2);
                scrollbar.percentOfScreen = dropBg.scale.Y / getTotalDropHeight();
                scrollbar.scrollPos = new Vector2(dropBg.position.X, dropBg.position.Y);
                scrollbar.scrollScale = dropBg.scale;
            }
        }

        float scrollAmount = 0;

        private float getTotalDropHeight()
        {
            return labelFonts.Length * scale.Y;
        }

        private float getMaxDropHeight()
        {
            return scale.Y * 5;
        }

        bool _isOpen = false;
        bool isOpen
        {
            get
            {
                return _isOpen;
            }
            set
            {
                if (value == _isOpen)
                    return;

                _isOpen = value;

                dropBg.enabled = value;
                selHl.enabled = value;

                for (int i = 0; i < labelFonts.Length; i++)
                {
                    labelFonts[i].Enabled = value;
                }

                if (value)
                {
                    em.changePriority((int)InterfacePriority.HIGHEST, onPointerEvent);

                    //on open, set scroll amount so that we can see the selection
                    if (getTotalDropHeight() > getMaxDropHeight())
                    {
                        scrollbar.enabled = true;
                        scrollAmount = Math.Min(Math.Max(Selection * -scale.Y, scrollAmount), Selection * -scale.Y + dropBg.scale.Y - scale.Y);
                        scrollbar.sliderPos = -scrollAmount / (getTotalDropHeight() - dropBg.scale.Y);
                        recalcPositions();
                    }
                    else
                    {
                        scrollAmount = 0;
                        recalcPositions();
                    }

                }
                else
                {
                    em.changePriority((int)InterfacePriority.MEDIUM, onPointerEvent);
                    mouseHl.enabled = false;
                    scrollbar.enabled = false;
                }
            }
        }

        public event DropDownEvent onSelectionChange;

        Color borderColor = Color.White;
        Color mouseoverColor = Color.DarkBlue;
        Color highlighColor = Color.Blue;

        float UIScaleX;
        float UIScaleY;

        public DropDownMenu(Renderer renderer, EventManager em, int priority, string[] labels, int initial, float UIScaleX = -1, float UIScaleY = -1)
        {
            this.renderer = renderer;
            this.em = em;

            rect = new RoundedRectangle_2D(renderer, em, priority);
            rect.radius = borderRadius;

            int dropP = priority + 1024;
            dropBg = new RoundedRectangle_2D(renderer, em, dropP);
            dropBg.borderColor = highlighColor;
            dropBg.radius = borderRadius;
            dropBg.enabled = false;

            selHl = new RoundedRectangle_2D(renderer, em, dropP + 1);
            selHl.borderColor = highlighColor;
            selHl.radius = borderRadius;
            selHl.enabled = false;

            mouseHl = new RoundedRectangle_2D(renderer, em, dropP + 2);
            mouseHl.borderColor = highlighColor;
            mouseHl.mainColor = mouseoverColor;
            mouseHl.radius = borderRadius;
            mouseHl.enabled = false;

            font = new FontRenderer(renderer, em, priority + 1, renderer.Assets.GetFont(Renderer.DefaultAssets.FONT_DEFAULT));
            font.Anchor = FontAnchor.CENTER_LEFT;
            font.Text = labels[Selection];

            labelFonts = new FontRenderer[labels.Length];
            for (int i = 0; i < labelFonts.Length; i++)
            {
                labelFonts[i] = new FontRenderer(renderer, em, dropP + 3, renderer.Assets.GetFont(Renderer.DefaultAssets.FONT_DEFAULT), UIScaleX, UIScaleY);
                labelFonts[i].Anchor = FontAnchor.CENTER_LEFT;
                labelFonts[i].Text = labels[i];
                labelFonts[i].Enabled = false;
            }

            scrollbar = new ScrollBar(renderer, em, dropP + 1, UIScaleX, UIScaleY);
            scrollbar.enabled = false;
            scrollbar.defaultPriority = (int)InterfacePriority.HIGHEST - 1;
            scrollbar.selectedPriority = (int)InterfacePriority.HIGHEST - 2;
            scrollbar.onValueChanged += onScrollChange;

            em.addEventHandler((int)InterfacePriority.MEDIUM, onPointerEvent);

            this.Selection = initial;

            this.UIScaleX = UIScaleX < 0 ? renderer.ResolutionWidth : UIScaleX;
            this.UIScaleY = UIScaleY < 0 ? renderer.ResolutionHeight : UIScaleY;
        }

        void onScrollChange(float val)
        {
            scrollAmount = -val * (getTotalDropHeight() - dropBg.scale.Y);
            recalcPositions();
        }

        bool onPointerEvent(PointerEventArgs args)
        {
            float mouseX = args.aimDeltaX * UIScaleX;
            float mouseY = args.aimDeltaY * UIScaleY;

            if (args.type == PointerEventType.MOVE)
            {
                if (!isOpen)
                {
                    if (mouseX > pos.X && mouseX <= pos.X + scale.X
                        && mouseY > pos.Y && mouseY <= pos.Y + scale.Y)
                    {
                        rect.borderColor = highlighColor;
                    }
                    else
                        rect.borderColor = borderColor;
                }
                else if (isOpen)
                {//calc mouse highlight selection
                    if (mouseX > pos.X && mouseX <= pos.X + scale.X - scale.Y
                        && mouseY > dropBg.position.Y && mouseY <= dropBg.position.Y + dropBg.scale.Y)
                    {
                        for (int i = 0; i < labelFonts.Length; i++)
                        {
                            float ymin = labelFonts[i].boundsPos.Y;
                            float ymax = ymin + labelFonts[i].boundsScale.Y;

                            if (mouseY > ymin && mouseY <= ymax)
                            {
                                mouseHover = i;
                                return true;
                            }
                        }
                    }

                    mouseHover = -1;
                    return true;
                }

                return false;
            }

            if (args.type == PointerEventType.BUTTON)
            {
                if (args.button == PointerButton.LEFT)
                {
                    if (args.isDown
                        && mouseX > pos.X && mouseX <= pos.X + scale.X
                        && mouseY > pos.Y && mouseY <= pos.Y + scale.Y)
                    {
                        if (!isOpen)
                        {
                            isOpen = true;
                            rect.borderColor = mouseoverColor;
                        }
                        else
                        {
                            isOpen = false;
                            rect.borderColor = highlighColor;
                        }
                        return true;
                    }
                    else if (!args.isDown && isOpen
                        && mouseX > pos.X && mouseX <= pos.X + scale.X
                        && mouseY > pos.Y && mouseY <= pos.Y + scale.Y)
                    {
                        return true;
                    }
                    else if (args.isDown && isOpen)
                    {
                        isOpen = false;
                        rect.borderColor = borderColor;

                        //check here for clicking on an option
                        if (mouseX > pos.X && mouseX <= pos.X + scale.X - scale.Y
                            && mouseY > dropBg.position.Y && mouseY <= dropBg.position.Y + dropBg.scale.Y)
                        {
                            for (int i = 0; i < labelFonts.Length; i++)
                            {
                                float ymin = labelFonts[i].boundsPos.Y;
                                float ymax = ymin + labelFonts[i].boundsScale.Y;

                                if (mouseY > ymin && mouseY <= ymax)
                                {
                                    Selection = i;
                                    return true;
                                }
                            }
                        }
                        return true;
                    }
                    else if (!args.isDown && isOpen)
                    {
                        if (mouseX > pos.X && mouseX <= pos.X + scale.X - scale.Y
                            && mouseY > dropBg.position.Y && mouseY <= dropBg.position.Y + dropBg.scale.Y)
                        {
                            for (int i = 0; i < labelFonts.Length; i++)
                            {
                                float ymin = labelFonts[i].boundsPos.Y;
                                float ymax = ymin + labelFonts[i].boundsScale.Y;

                                if (mouseY > ymin && mouseY <= ymax)
                                {
                                    Selection = i;
                                    isOpen = false;
                                    return true;
                                }
                            }
                        }
                    }
                }

                return false;
            }

            return false;
        }

        public void Dispose()
        {
            em.removeEventHandler(onPointerEvent);
            rect.Dispose();
            font.Dispose();
            dropBg.Dispose();
            selHl.Dispose();
            mouseHl.Dispose();

            for (int i = 0; i < labelFonts.Length; i++)
            {
                labelFonts[i].Dispose();
            }
        }
    }
}
