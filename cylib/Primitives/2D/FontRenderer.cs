﻿using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Numerics;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using SharpDX.Direct3D11;

namespace cylib
{
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    struct FontGlyphBuffer
    {
        [FieldOffset(0)]
        internal Vector3 pos;
        [FieldOffset(12)]
        internal Vector2 tex;
        internal FontGlyphBuffer(Vector3 pos, Vector2 tex)
        {
            this.pos = pos;
            this.tex = tex;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    struct ColorBuffer
    {
        [FieldOffset(0)]
        internal Vector4 color;
        internal ColorBuffer(Color color)
        {
            this.color = Texture.convertToLinear(color);
        }
    }

    /// <summary>
    /// The FontAnchor enum defines where the text should be rendered relative to the given position.
    /// For example 'top left' means that the position given will correspond to the top left of the rendered text.
    /// 
    /// The top/center/bottom anchors are going to be /slightly/ imperfect, depending on the font.
    /// There's no real way to get the 'bottom' or 'top' of all possible text from a font, because of things like vertical kerning.
    /// Instead, we use the ascender/descender limits given by the font designer, which don't really correspond to any real measure.
    /// 
    /// Addendum: 'slightly' imperfect may be an understatement, depending on your requirements.
    /// 
    /// 'Baseline' is the normal method of writing a font, where descending glyphs will dip below the baseline, like 'y', 'g', 'q', etc.
    /// Horizontal alignment should behave as expected.
    /// </summary>
    public enum FontAnchor
    {
        TOP_LEFT,
        CENTER_LEFT,
        BASELINE_LEFT,
        BOTTOM_LEFT,
        TOP_CENTER,
        CENTER_CENTER,
        BASELINE_CENTER,
        BOTTOM_CENTER,
        TOP_RIGHT,
        CENTER_RIGHT,
        BASELINE_RIGHT,
        BOTTOM_RIGHT
    }

    public class FontRenderer : IDisposable
    {
        ConstBuffer<FontGlyphBuffer> glyphBuf;
        ConstBuffer<ColorBuffer> colorBuf;

        Shader shader;
        public Font font;
        SamplerState sampler;

        public Vector2 Pos;
        public string Text;
        public float Scale;
        public Color color;

        /// <summary>
        /// Offset from position to begin drawing.
        /// Useful in conjuction with bounds, to slide text along a set box.
        /// </summary>
        public Vector2 offset;

        /// <summary>
        /// Bounds in a rectangle from position to limit rendering.
        /// Set any scale to negative to disable bounds limitting.
        /// 
        /// Bounds scale of 0 will disable rendering altogether.
        /// </summary>
        public Vector2 boundsPos;
        public Vector2 boundsScale;

        public FontAnchor Anchor = FontAnchor.BOTTOM_LEFT;

        public bool Enabled = true;

        Renderer renderer;
        EventManager em;

        float UIScaleX;
        float UIScaleY;

        public FontRenderer(Renderer renderer, EventManager em, int priority, Font font, float UIScaleX = -1, float UIScaleY = -1)
        {
            this.renderer = renderer;
            this.em = em;
            this.font = font;

            if (font.isSDF)
                shader = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_FONT_SDF);
            else
                shader = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_FONT_BITMAP);
            glyphBuf = renderer.Assets.GetBuffer<FontGlyphBuffer>(Renderer.DefaultAssets.BUF_FONT);
            colorBuf = renderer.Assets.GetBuffer<ColorBuffer>(Renderer.DefaultAssets.BUF_COLOR);
            sampler = renderer.samplerLinear;

            Pos = new Vector2();
            Text = "";
            Scale = 16f;
            color = Color.White;
            offset = new Vector2(0, 0);
            boundsPos = new Vector2(-1, -1);
            boundsScale = new Vector2(-1, -1);

            em.addDraw2D(priority, Draw2D);

            this.UIScaleX = UIScaleX < 0 ? renderer.ResolutionWidth : UIScaleX;
            this.UIScaleY = UIScaleY < 0 ? renderer.ResolutionHeight : UIScaleY;
        }

        void Draw2D()
        {
            if (!Enabled)
                return;

            if (boundsScale.X == 0 || boundsScale.Y == 0)
                return;

            shader.Bind(renderer.Context);
            renderer.Context.VertexShader.SetShaderResource(0, glyphBuf.srv);
            renderer.Context.PixelShader.SetShaderResource(0, font.atlas.view);
            renderer.Context.PixelShader.SetSampler(0, sampler);

            colorBuf.dat[0].color = Texture.convertToLinear(color);
            colorBuf.Write(renderer.Context);

            renderer.Context.PixelShader.SetConstantBuffer(1, colorBuf.buf);

            RasterizerState preRast = renderer.Context.Rasterizer.State;
            if (boundsScale.X > 0 && boundsScale.Y > 0)
            {
                renderer.Context.Rasterizer.State = renderer.rasterNormalScissor;
                renderer.Context.Rasterizer.SetScissorRectangle(
                    (int)(boundsPos.X / UIScaleX * renderer.ResolutionWidth), 
                    (int)(boundsPos.Y / UIScaleY * renderer.ResolutionHeight), 
                    (int)Math.Ceiling((boundsPos.X + boundsScale.X) / UIScaleX * renderer.ResolutionWidth), 
                    (int)Math.Ceiling((boundsPos.Y + boundsScale.Y) / UIScaleY * renderer.ResolutionHeight));
            }

            int index = 0;
            float penX = Pos.X + calcAnchorX() + offset.X;
            float penY = Pos.Y + calcAnchorY() + offset.Y;
            float startPenX = penX;

            while (index < Text.Length)
            {
                int loops = glyphBuf.numElements / 6;
                int i = 0;

                for (; i < loops && i + index < Text.Length; i++)
                {
                    var c = Text[i + index];
                    if (c == '\r')
                    {
                        index++;
                        i--;
                        continue;
                    }
                    if (c == '\n')
                    {//special case character controls here
                        penX = startPenX;
                        penY += (font.ascenderHeight - font.descenderHeight) * Scale;
                        index++;
                        i--;
                        continue;
                    }
                    var t = font.glyphs.GetValueOrDefault(c, font.unsupportedGlyph);

                    float bear = t.bearingX * Scale;
                    if (penX != startPenX)
                        penX -= bear;

                    float tdx = (font.packingBuffer / 2) / (float)font.atlas.width;
                    float tdy = (font.packingBuffer / 2) / (float)font.atlas.height;

                    float tx = t.aPosX - tdx;
                    float txn = t.aPosX + t.aWidth + tdx;

                    float ty = t.aPosY - tdy;
                    float tyn = t.aPosY + t.aHeight + tdy;

                    float dx = tdx / t.aWidth * t.gWidth * Scale;
                    float dy = tdy / t.aHeight * t.gHeight * Scale;

                    float x = penX + bear - dx;
                    float xn = x + t.gWidth * Scale + dx * 2;

                    float y = penY - t.bearingY * Scale - dy;
                    float yn = y + t.gHeight * Scale + dy * 2;

                    glyphBuf.dat[i * 6 + 0] = new FontGlyphBuffer(new Vector3(x, y, 0), new Vector2(tx, ty));
                    glyphBuf.dat[i * 6 + 1] = new FontGlyphBuffer(new Vector3(xn, y, 0), new Vector2(txn, ty));
                    glyphBuf.dat[i * 6 + 2] = new FontGlyphBuffer(new Vector3(x, yn, 0), new Vector2(tx, tyn));
                    glyphBuf.dat[i * 6 + 3] = new FontGlyphBuffer(new Vector3(x, yn, 0), new Vector2(tx, tyn));
                    glyphBuf.dat[i * 6 + 4] = new FontGlyphBuffer(new Vector3(xn, y, 0), new Vector2(txn, ty));
                    glyphBuf.dat[i * 6 + 5] = new FontGlyphBuffer(new Vector3(xn, yn, 0), new Vector2(txn, tyn));

                    penX += t.advanceX * Scale;

                    if (i + index != Text.Length - 1)
                    {
                        penX += t.kerningMap.GetValueOrDefault(Text[i + index + 1], 0f) * Scale;
                    }
                }

                glyphBuf.Write(renderer.Context, 0, i * 6);
                renderer.Context.Draw(6 * i, 0);

                index += i;
            }

            renderer.Context.Rasterizer.State = preRast;
        }

        public float getRenderWidth(string str)
        {
            float width = 0;

            for (int i = 0; i < str.Length; i++)
            {
                var t = font.glyphs.GetValueOrDefault(str[i], font.unsupportedGlyph);

                if (i != 0)
                    width -= t.bearingX * Scale;

                width += t.advanceX * Scale;

                if (i != str.Length - 1)
                {
                    width += t.kerningMap.GetValueOrDefault(str[i + 1], 0) * Scale;
                }
            }

            return width;
        }

        private float calcAnchorX()
        {
            if (Anchor == FontAnchor.TOP_LEFT || Anchor == FontAnchor.CENTER_LEFT || Anchor == FontAnchor.BASELINE_LEFT || Anchor == FontAnchor.BOTTOM_LEFT)
                return 0;

            float width = getRenderWidth(Text);

            if (Anchor == FontAnchor.TOP_CENTER || Anchor == FontAnchor.CENTER_CENTER || Anchor == FontAnchor.BASELINE_CENTER || Anchor == FontAnchor.BOTTOM_CENTER)
                return -width / 2;

            //Anchor = RIGHT
            return -width;
        }

        /// <summary>
        /// Not terribly representative of the actual string height. Scale alone is probably what you want.
        /// </summary>
        public float getRenderHeight(string str)
        {
            return (font.ascenderHeight - font.descenderHeight) * Scale;
        }

        private float calcAnchorY()
        {
            if (Anchor == FontAnchor.BASELINE_LEFT || Anchor == FontAnchor.BASELINE_CENTER || Anchor == FontAnchor.BASELINE_RIGHT)
                return 0;

            //doing a small fudge here because fonts are dumb
            float mul = 1 / (font.ascenderHeight - font.descenderHeight);

            if (Anchor == FontAnchor.BOTTOM_LEFT || Anchor == FontAnchor.BOTTOM_CENTER || Anchor == FontAnchor.BOTTOM_RIGHT)
                return font.descenderHeight * Scale * mul;

            if (Anchor == FontAnchor.TOP_LEFT || Anchor == FontAnchor.TOP_CENTER || Anchor == FontAnchor.TOP_RIGHT)
                return font.ascenderHeight * Scale * mul;

            //Anchor = CENTER
            return (font.ascenderHeight + font.descenderHeight) * Scale * mul / 2;
        }

        public void Dispose()
        {
            em.remove2D(Draw2D);
        }
    }
}
