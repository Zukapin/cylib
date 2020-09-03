using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Runtime.InteropServices;

using BepuUtilities;

namespace cylib
{

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct VertexPositionColor
    {
        [FieldOffset(0)]
        public Vector3 pos;
        [FieldOffset(12)]
        public int color;

        public VertexPositionColor(Vector3 pos, Color color)
        {
            this.pos = pos;
            this.color = color.ToArgb(); //this was changed from argb -- at some point, then changed back i have no idea if this will work
        }

        public readonly static InputElement[] vertexElements = new[]
        {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElement("COLOR", 0, Format.B8G8R8A8_UNorm, 12, 0)
        };

        public const int sizeOf = 16;
    }

    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct VertexPositionTexture
    {
        [FieldOffset(0)]
        public Vector3 pos;
        [FieldOffset(12)]
        public Vector2 tex;

        public VertexPositionTexture(Vector3 pos, Vector2 tex)
        {
            this.pos = pos;
            this.tex = tex;

        }

        public readonly static InputElement[] vertexElements = new[]
        {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElement("TEXTURE", 0, Format.R32G32_Float, 12, 0)
        };

        public const int sizeOf = 20;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct VertexPositionNormalTexture
    {
        [FieldOffset(0)]
        public Vector3 pos;
        [FieldOffset(12)]
        public Vector3 norm;
        [FieldOffset(24)]
        public Vector2 tex;

        public VertexPositionNormalTexture(Vector3 pos, Vector3 norm, Vector2 tex)
        {
            this.pos = pos;
            this.norm = norm;
            this.tex = tex;

        }

        public readonly static InputElement[] vertexElements = new[]
        {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new InputElement("TEXTURE", 0, Format.R32G32_Float, 24, 0)
        };

        public const int sizeOf = 32;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct VertexPositionNormal
    {
        [FieldOffset(0)]
        public Vector3 pos;
        [FieldOffset(12)]
        public Vector3 norm;

        public VertexPositionNormal(Vector3 pos, Vector3 norm)
        {
            this.pos = pos;
            this.norm = norm;
        }

        public readonly static InputElement[] vertexElements = new[]
        {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0)
        };

        public const int sizeOf = 24;
    }

    [StructLayout(LayoutKind.Explicit, Size = 56)]
    public struct VertexPositionNormalMapTexture
    {
        [FieldOffset(0)]
        public Vector3 pos;
        [FieldOffset(12)]
        public Vector3 norm;
        [FieldOffset(24)]
        public Vector3 binorm;
        [FieldOffset(36)]
        public Vector3 tangent;
        [FieldOffset(48)]
        public Vector2 tex;

        public VertexPositionNormalMapTexture(Vector3 pos, Vector3 norm, Vector3 binorm, Vector3 tangent, Vector2 tex)
        {
            this.pos = pos;
            this.norm = norm;
            this.binorm = binorm;
            this.tangent = tangent;
            this.tex = tex;
        }

        public readonly static InputElement[] vertexElements = new[]
        {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new InputElement("BINORMAL", 0, Format.R32G32B32_Float, 24, 0),
            new InputElement("TANGENT", 0, Format.R32G32B32_Float, 36, 0),
            new InputElement("TEXTURE", 0, Format.R32G32_Float, 48, 0)
        };

        public const int sizeOf = 56;
    }
}
