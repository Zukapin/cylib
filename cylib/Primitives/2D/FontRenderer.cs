using System;
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

        public Vector2 pos;
        public string text;
        public float scale;
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

        public FontAnchor anchor = FontAnchor.BOTTOM_LEFT;

        public bool enabled = true;

        Renderer renderer;
        EventManager em;

        public FontRenderer(Renderer renderer, EventManager em, int priority, Font font)
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

            pos = new Vector2();
            text = "";
            scale = 16f;
            color = Color.White;
            offset = new Vector2(0, 0);
            boundsPos = new Vector2(-1, -1);
            boundsScale = new Vector2(-1, -1);

            em.addDraw2D(priority, Draw2D);
        }

        void Draw2D()
        {
            if (!enabled)
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
                renderer.Context.Rasterizer.SetScissorRectangle((int)boundsPos.X, (int)boundsPos.Y, (int)Math.Ceiling(boundsPos.X + boundsScale.X), (int)Math.Ceiling(boundsPos.Y + boundsScale.Y));
            }

            int index = 0;
            float penX = pos.X + calcAnchorX() + offset.X;
            float penY = pos.Y + calcAnchorY() + offset.Y;
            float startPenX = penX;

            while (index < text.Length)
            {
                int loops = glyphBuf.numElements / 6;
                int i = 0;

                for (; i < loops && i + index < text.Length; i++)
                {
                    var c = text[i + index];
                    if (c == '\r')
                    {
                        index++;
                        i--;
                        continue;
                    }
                    if (c == '\n')
                    {//special case character controls here
                        penX = startPenX;
                        penY += (font.ascenderHeight - font.descenderHeight) * scale;
                        index++;
                        i--;
                        continue;
                    }
                    var t = font.glyphs.GetValueOrDefault(c, font.unsupportedGlyph);

                    float bear = t.bearingX * scale;
                    if (penX != startPenX)
                        penX -= bear;

                    float tdx = (font.packingBuffer / 2) / (float)font.atlas.width;
                    float tdy = (font.packingBuffer / 2) / (float)font.atlas.height;

                    float tx = t.aPosX - tdx;
                    float txn = t.aPosX + t.aWidth + tdx;

                    float ty = t.aPosY - tdy;
                    float tyn = t.aPosY + t.aHeight + tdy;

                    float dx = tdx / t.aWidth * t.gWidth * scale;
                    float dy = tdy / t.aHeight * t.gHeight * scale;

                    float x = penX + bear - dx;
                    float xn = x + t.gWidth * scale + dx * 2;

                    float y = penY - t.bearingY * scale - dy;
                    float yn = y + t.gHeight * scale + dy * 2;

                    glyphBuf.dat[i * 6 + 0] = new FontGlyphBuffer(new Vector3(x, y, 0), new Vector2(tx, ty));
                    glyphBuf.dat[i * 6 + 1] = new FontGlyphBuffer(new Vector3(xn, y, 0), new Vector2(txn, ty));
                    glyphBuf.dat[i * 6 + 2] = new FontGlyphBuffer(new Vector3(x, yn, 0), new Vector2(tx, tyn));
                    glyphBuf.dat[i * 6 + 3] = new FontGlyphBuffer(new Vector3(x, yn, 0), new Vector2(tx, tyn));
                    glyphBuf.dat[i * 6 + 4] = new FontGlyphBuffer(new Vector3(xn, y, 0), new Vector2(txn, ty));
                    glyphBuf.dat[i * 6 + 5] = new FontGlyphBuffer(new Vector3(xn, yn, 0), new Vector2(txn, tyn));

                    penX += t.advanceX * scale;

                    if (i + index != text.Length - 1)
                    {
                        penX += t.kerningMap.GetValueOrDefault(text[i + index + 1], 0f) * scale;
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

                if (i == 0)
                    width -= t.bearingX * scale;

                width += t.advanceX * scale;

                if (i != str.Length - 1)
                {
                    width += t.kerningMap.GetValueOrDefault(str[i + 1], 0) * scale;
                }
            }

            return width;
        }

        private float calcAnchorX()
        {
            if (anchor == FontAnchor.TOP_LEFT || anchor == FontAnchor.CENTER_LEFT || anchor == FontAnchor.BASELINE_LEFT || anchor == FontAnchor.BOTTOM_LEFT)
                return 0;

            float width = getRenderWidth(text);

            if (anchor == FontAnchor.TOP_CENTER || anchor == FontAnchor.CENTER_CENTER || anchor == FontAnchor.BASELINE_CENTER || anchor == FontAnchor.BOTTOM_CENTER)
                return -width / 2;

            //Anchor = RIGHT
            return -width;
        }

        /// <summary>
        /// Not terribly representative of the actual string height. Scale alone is probably what you want.
        /// </summary>
        public float getRenderHeight(string str)
        {
            return (font.ascenderHeight - font.descenderHeight) * scale;
        }

        private float calcAnchorY()
        {
            if (anchor == FontAnchor.BASELINE_LEFT || anchor == FontAnchor.BASELINE_CENTER || anchor == FontAnchor.BASELINE_RIGHT)
                return 0;

            //doing a small fudge here because fonts are dumb
            float mul = 1 / (font.ascenderHeight - font.descenderHeight);

            if (anchor == FontAnchor.BOTTOM_LEFT || anchor == FontAnchor.BOTTOM_CENTER || anchor == FontAnchor.BOTTOM_RIGHT)
                return font.descenderHeight * scale * mul;

            if (anchor == FontAnchor.TOP_LEFT || anchor == FontAnchor.TOP_CENTER || anchor == FontAnchor.TOP_RIGHT)
                return font.ascenderHeight * scale * mul;

            //Anchor = CENTER
            return (font.ascenderHeight + font.descenderHeight) * scale * mul / 2;
        }

        public void Dispose()
        {
            em.remove2D(Draw2D);
        }
    }
}
