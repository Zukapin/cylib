﻿using System;
using System.Numerics;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;

using BepuUtilities;

namespace cylib
{

    public static class VertexHelper
    {
        public static VertexPositionNormalMapTexture[] CalcTangentMapping(VertexPositionNormalTexture[] tri, int offset)
        {
            //takes a triangle and outputs a triangle with binormals and tangents
            VertexPositionNormalMapTexture[] toReturn = new VertexPositionNormalMapTexture[3];

            Vector3 D = tri[1 + offset].pos - tri[offset].pos;
            Vector3 E = tri[2 + offset].pos - tri[offset].pos;

            Vector2 F = tri[1 + offset].tex - tri[offset].tex;
            Vector2 G = tri[2 + offset].tex - tri[offset].tex;

            float r = 1f / (F.X * G.Y - G.X * F.Y);
            Vector3 tan = new Vector3((G.Y * D.X - F.Y * E.X) * r, (G.Y * D.Y - F.Y * E.Y) * r, (G.Y * D.Z - F.Y * E.Z) * r);
            Vector3 bin = new Vector3((F.X * E.X - G.X * D.X) * r, (F.X * E.Y - G.X * D.Y) * r, (F.X * E.Z - G.X * D.Z) * r);

            tan = tan / tan.Length();
            bin = bin / bin.Length();

            for (int x = 0; x < 3; x++)
            {
                toReturn[x] = new VertexPositionNormalMapTexture(tri[x + offset].pos, tri[x + offset].norm, bin, tan, tri[x + offset].tex);
            }

            return toReturn;
        }
    }

    public class VertexBuffer : IDisposable
    {
        public VertexBufferBinding vbBinding;
        public Buffer vb;
        public int numVerts;

        private VertexBuffer(Buffer vb, VertexBufferBinding vbBinding, int numVerts)
        {
            this.vb = vb;
            this.vbBinding = vbBinding;
            this.numVerts = numVerts;
        }

        public static VertexBuffer CreateFromData<T>(Renderer renderer, T[] data, int vertexStride) where T : struct
        {
            if (data.Length == 0)
                throw new Exception("Vertex buffer requires at least a single vertex.");

            Buffer vb = Buffer.Create(renderer.Device, BindFlags.VertexBuffer, data, vertexStride * data.Length);
            VertexBufferBinding vbBinding = new VertexBufferBinding(vb, vertexStride, 0);
            return new VertexBuffer(vb, vbBinding, data.Length);
        }

        public static VertexBuffer CreatePosTexQuad(Renderer renderer, Vector2 start, Vector2 end)
        {
            VertexPositionTexture[] quad = new VertexPositionTexture[6];
            quad[0] = new VertexPositionTexture(new Vector3(start.X, start.Y, 0), new Vector2(0, 0));
            quad[1] = new VertexPositionTexture(new Vector3(end.X, start.Y, 0), new Vector2(1, 0));
            quad[2] = new VertexPositionTexture(new Vector3(start.X, end.Y, 0), new Vector2(0, 1));
            quad[3] = new VertexPositionTexture(new Vector3(start.X, end.Y, 0), new Vector2(0, 1));
            quad[4] = new VertexPositionTexture(new Vector3(end.X, start.Y, 0), new Vector2(1, 0));
            quad[5] = new VertexPositionTexture(new Vector3(end.X, end.Y, 0), new Vector2(1, 1));

            return CreateFromData(renderer, quad, VertexPositionTexture.sizeOf);
        }

        /// <summary>
        /// Creates a circle.
        /// </summary>
        /// <param name="stage">Context to create the buffer in</param>
        /// <param name="center">The Center of the circle, usually Vector3.Zero</param>
        /// <param name="point">A point on edge of the circle, matching texture coord (0.5, 1), will be rotated around the face vector</param>
        /// <param name="face">The direction the face should be pointing</param>
        /// <param name="numTris">Number of triangles in the circle. Must be at least 3</param>
        /// <returns></returns>
        public static VertexBuffer CreatePosTexCircle(Renderer renderer, Vector3 center, Vector3 point, Vector3 face, int numTris)
        {
            if (numTris < 3)
                throw new ArgumentException("Number of Triangles for a circle must be at least 3");

            VertexPositionTexture[] cir = new VertexPositionTexture[numTris * 3];

            Vector3 curPoint = point;
            Vector3 nextPoint;

            Vector2 cenTex = new Vector2(0.5f, 0.5f);
            Vector2 curTex = new Vector2(0.5f, 0.0f);
            Vector2 nextTex;

            float rot = (float)(Math.PI * 2.0 / numTris);
            Matrix3x3 rotMat = Matrix3x3.CreateFromAxisAngle(face, rot);
            Matrix3x3 texRot = Matrix3x3.CreateFromAxisAngle(Vector3.UnitZ, rot);

            for (int x = 0; x < numTris; x++)
            {
                Matrix3x3.Transform(curPoint - center, rotMat, out nextPoint);
                nextPoint += center;

                Vector3 tTex = new Vector3(curTex - cenTex, 0);
                Matrix3x3.Transform(tTex, texRot, out tTex);
                nextTex = new Vector2(tTex.X, tTex.Y) + cenTex;

                cir[x * 3 + 0] = new VertexPositionTexture(center, cenTex);
                cir[x * 3 + 1] = new VertexPositionTexture(nextPoint, nextTex);
                cir[x * 3 + 2] = new VertexPositionTexture(curPoint, curTex);

                curPoint = nextPoint;
                curTex = nextTex;
            }

            return CreateFromData(renderer, cir, VertexPositionTexture.sizeOf);
        }

        /// <summary>
        /// Creates a circle.
        /// </summary>
        /// <param name="stage">Context to create the buffer in</param>
        /// <param name="center">The Center of the circle, usually Vector3.Zero</param>
        /// <param name="point">A point on edge of the circle, matching texture coord (0.5, 1), will be rotated around the face vector</param>
        /// <param name="face">The direction the face should be pointing</param>
        /// <param name="numTris">Number of triangles in the circle. Must be at least 3</param>
        /// <returns></returns>
        public static VertexBuffer CreatePosTexNormCircle(Renderer renderer, Vector3 center, Vector3 point, Vector3 face, int numTris)
        {
            if (numTris < 3)
                throw new ArgumentException("Number of Triangles for a circle must be at least 3");

            VertexPositionNormalTexture[] cir = new VertexPositionNormalTexture[numTris * 3];

            Vector3 curPoint = point;
            Vector3 nextPoint;

            Vector2 cenTex = new Vector2(0.5f, 0.5f);
            Vector2 curTex = new Vector2(0.5f, 0.0f);
            Vector2 nextTex;

            float rot = (float)(Math.PI * 2.0 / numTris);
            Matrix3x3 rotMat = Matrix3x3.CreateFromAxisAngle(face, rot);
            Matrix3x3 texRot = Matrix3x3.CreateFromAxisAngle(Vector3.UnitZ, rot);

            for (int x = 0; x < numTris; x++)
            {
                Matrix3x3.Transform(curPoint - center, rotMat, out nextPoint);
                nextPoint += center;

                Vector3 tTex = new Vector3(curTex - cenTex, 0);
                Matrix3x3.Transform(tTex, texRot, out tTex);
                nextTex = new Vector2(tTex.X, tTex.Y) + cenTex;

                cir[x * 3 + 0] = new VertexPositionNormalTexture(center, face, cenTex);
                cir[x * 3 + 1] = new VertexPositionNormalTexture(nextPoint, face, nextTex);
                cir[x * 3 + 2] = new VertexPositionNormalTexture(curPoint, face, curTex);

                curPoint = nextPoint;
                curTex = nextTex;
            }

            return CreateFromData(renderer, cir, VertexPositionNormalTexture.sizeOf);
        }

        public static VertexBuffer CreatePosNormCylinder(Renderer renderer, Vector3 center, float radius, float height, Vector3 upDir, int numTris)
        {
            if (numTris < 3)
                throw new ArgumentException("Number of Triangles for a circle must be at least 3");

            VertexPositionNormal[] cir = new VertexPositionNormal[numTris * 12];

            Vector3 up = upDir / upDir.Length();

            float halfHeight = height * 0.5f;
            Vector3 centOffset = up * halfHeight;

            Vector3 edgePoint = Vector3.Cross(Vector3.UnitX, up) * radius;
            if (Vector3.Dot(Vector3.UnitX, up) < 1e-7)
                edgePoint = Vector3.Cross(Vector3.UnitZ, up) * radius;

            Vector3 curPoint = center + edgePoint;
            Vector3 nextPoint;

            Vector3 curNorm = edgePoint / edgePoint.Length();

            Vector3 nextNorm;

            float rot = (float)(Math.PI * 2.0 / numTris);
            Matrix3x3 rotMat = Matrix3x3.CreateFromAxisAngle(up, rot);

            for (int x = 0; x < numTris; x++)
            {
                Matrix3x3.Transform(curPoint - center, rotMat, out nextPoint);
                nextNorm = nextPoint / nextPoint.Length();

                nextPoint += center;

                //top face
                cir[x * 12 + 0] = new VertexPositionNormal(center + centOffset, up);
                cir[x * 12 + 1] = new VertexPositionNormal(nextPoint + centOffset, up);
                cir[x * 12 + 2] = new VertexPositionNormal(curPoint + centOffset, up);

                //edges
                cir[x * 12 + 3] = new VertexPositionNormal(curPoint + centOffset, curNorm);
                cir[x * 12 + 4] = new VertexPositionNormal(nextPoint + centOffset, nextNorm);
                cir[x * 12 + 5] = new VertexPositionNormal(curPoint - centOffset, curNorm);
                cir[x * 12 + 6] = new VertexPositionNormal(curPoint - centOffset, curNorm);
                cir[x * 12 + 7] = new VertexPositionNormal(nextPoint + centOffset, nextNorm);
                cir[x * 12 + 8] = new VertexPositionNormal(nextPoint - centOffset, nextNorm);

                //bottom face
                cir[x * 12 + 9] = new VertexPositionNormal(center - centOffset, -up);
                cir[x * 12 + 10] = new VertexPositionNormal(curPoint - centOffset, -up);
                cir[x * 12 + 11] = new VertexPositionNormal(nextPoint - centOffset, -up);

                curPoint = nextPoint;
                curNorm = nextNorm;
            }

            return CreateFromData(renderer, cir, VertexPositionNormal.sizeOf);
        }

        public static VertexBuffer CreatePosNormBox(Renderer renderer, Vector3 center, Vector3 dims)
        {
            VertexPositionNormal[] box = new VertexPositionNormal[36];

            float dX = dims.X * 0.5f;
            float dY = dims.Y * 0.5f;
            float dZ = dims.Z * 0.5f;

            //Top Face
            box[0] = new VertexPositionNormal(center + new Vector3(-dX, dY, -dZ), Vector3.UnitY);
            box[1] = new VertexPositionNormal(center + new Vector3(dX, dY, -dZ), Vector3.UnitY);
            box[2] = new VertexPositionNormal(center + new Vector3(-dX, dY, dZ), Vector3.UnitY);
            box[3] = new VertexPositionNormal(center + new Vector3(-dX, dY, dZ), Vector3.UnitY);
            box[4] = new VertexPositionNormal(center + new Vector3(dX, dY, -dZ), Vector3.UnitY);
            box[5] = new VertexPositionNormal(center + new Vector3(dX, dY, dZ), Vector3.UnitY);

            //Bottom Face
            box[6] = new VertexPositionNormal(center + new Vector3(-dX, -dY, -dZ), -Vector3.UnitY);
            box[7] = new VertexPositionNormal(center + new Vector3(-dX, -dY, dZ), -Vector3.UnitY);
            box[8] = new VertexPositionNormal(center + new Vector3(dX, -dY, -dZ), -Vector3.UnitY);
            box[9] = new VertexPositionNormal(center + new Vector3(dX, -dY, -dZ), -Vector3.UnitY);
            box[10] = new VertexPositionNormal(center + new Vector3(-dX, -dY, dZ), -Vector3.UnitY);
            box[11] = new VertexPositionNormal(center + new Vector3(dX, -dY, dZ), -Vector3.UnitY);

            //Right Face
            box[12] = new VertexPositionNormal(center + new Vector3(dX, -dY, -dZ), Vector3.UnitX);
            box[13] = new VertexPositionNormal(center + new Vector3(dX, -dY, dZ), Vector3.UnitX);
            box[14] = new VertexPositionNormal(center + new Vector3(dX, dY, -dZ), Vector3.UnitX);
            box[15] = new VertexPositionNormal(center + new Vector3(dX, dY, -dZ), Vector3.UnitX);
            box[16] = new VertexPositionNormal(center + new Vector3(dX, -dY, dZ), Vector3.UnitX);
            box[17] = new VertexPositionNormal(center + new Vector3(dX, dY, dZ), Vector3.UnitX);

            //Left Face
            box[18] = new VertexPositionNormal(center + new Vector3(-dX, -dY, -dZ), -Vector3.UnitX);
            box[19] = new VertexPositionNormal(center + new Vector3(-dX, dY, -dZ), -Vector3.UnitX);
            box[20] = new VertexPositionNormal(center + new Vector3(-dX, -dY, dZ), -Vector3.UnitX);
            box[21] = new VertexPositionNormal(center + new Vector3(-dX, -dY, dZ), -Vector3.UnitX);
            box[22] = new VertexPositionNormal(center + new Vector3(-dX, dY, -dZ), -Vector3.UnitX);
            box[23] = new VertexPositionNormal(center + new Vector3(-dX, dY, dZ), -Vector3.UnitX);

            //Forward Face
            box[24] = new VertexPositionNormal(center + new Vector3(-dX, -dY, dZ), Vector3.UnitZ);
            box[25] = new VertexPositionNormal(center + new Vector3(-dX, dY, dZ), Vector3.UnitZ);
            box[26] = new VertexPositionNormal(center + new Vector3(dX, -dY, dZ), Vector3.UnitZ);
            box[27] = new VertexPositionNormal(center + new Vector3(dX, -dY, dZ), Vector3.UnitZ);
            box[28] = new VertexPositionNormal(center + new Vector3(-dX, dY, dZ), Vector3.UnitZ);
            box[29] = new VertexPositionNormal(center + new Vector3(dX, dY, dZ), Vector3.UnitZ);

            //Back Face
            box[30] = new VertexPositionNormal(center + new Vector3(-dX, -dY, -dZ), -Vector3.UnitZ);
            box[31] = new VertexPositionNormal(center + new Vector3(dX, -dY, -dZ), -Vector3.UnitZ);
            box[32] = new VertexPositionNormal(center + new Vector3(-dX, dY, -dZ), -Vector3.UnitZ);
            box[33] = new VertexPositionNormal(center + new Vector3(-dX, dY, -dZ), -Vector3.UnitZ);
            box[34] = new VertexPositionNormal(center + new Vector3(dX, -dY, -dZ), -Vector3.UnitZ);
            box[35] = new VertexPositionNormal(center + new Vector3(dX, dY, -dZ), -Vector3.UnitZ);

            return CreateFromData(renderer, box, VertexPositionNormal.sizeOf);
        }

        public static VertexBuffer CreatePosNormSphere(Renderer renderer, float radius, int numHorizDivisions = 12, int numVertDivisions = 12)
        {
            if (numHorizDivisions < 3) //3 would be a diamond-looking thing
                throw new Exception("Can't have a sphere with less than 3 horizontal divisions. " + numHorizDivisions);

            if (numVertDivisions < 2) //2 would be a diamond-looking thing -- 2 points on the poles that go to a fat middle with horizDiv number of points
                throw new Exception("Can't have a sphere with less than 2 vertical divisions. " + numVertDivisions);

            //number of tris is 2 * numHoriz for the caps, then (numVert - 2) * numHoriz * 2 for center
            int numTris = (2 * numHorizDivisions) + ((numVertDivisions - 2) * numHorizDivisions * 2);

            //not using an index buffer, could greatly reduce number of verts if we did
            VertexPositionNormal[] sphere = new VertexPositionNormal[numTris * 3];

            int i = 0;
            Matrix3x3 rotDown = Matrix3x3.CreateFromAxisAngle(Vector3.UnitX, (float)(Math.PI / numVertDivisions));
            Matrix3x3 rotForward = Matrix3x3.CreateFromAxisAngle(Vector3.UnitY, (float)(Math.PI * 2.0 / numHorizDivisions));
            
            //top cap
            Vector3 centerPoint = Vector3.UnitY * radius;
            Matrix3x3.Transform(Vector3.UnitY, rotDown, out var curPoint);
            Vector3 nextPoint;
            var firstPoint = curPoint;
            for (int t = 0; t < numHorizDivisions - 1; t++)
            {
                Matrix3x3.Transform(curPoint, rotForward, out nextPoint);

                sphere[i++] = new VertexPositionNormal(centerPoint, Vector3.UnitY);
                sphere[i++] = new VertexPositionNormal(nextPoint * radius, nextPoint);
                sphere[i++] = new VertexPositionNormal(curPoint * radius, curPoint);

                curPoint = nextPoint;
            }

            //use the first point here so we don't accumulate some rotation error and have the verts not match
            nextPoint = firstPoint;
            sphere[i++] = new VertexPositionNormal(centerPoint, Vector3.UnitY);
            sphere[i++] = new VertexPositionNormal(nextPoint * radius, nextPoint);
            sphere[i++] = new VertexPositionNormal(curPoint * radius, curPoint);

            //body of the sphere now
            for (int t = 0; t < numVertDivisions - 2; t++)
            {
                curPoint = firstPoint;
                Matrix3x3.Transform(curPoint, rotDown, out var downPoint);
                Vector3 firstDownPoint = downPoint;
                Vector3 nextDownPoint;

                for (int r = 0; r < numHorizDivisions - 1; r++)
                {
                    Matrix3x3.Transform(curPoint, rotForward, out nextPoint);
                    Matrix3x3.Transform(downPoint, rotForward, out nextDownPoint);

                    sphere[i++] = new VertexPositionNormal(curPoint * radius, curPoint);
                    sphere[i++] = new VertexPositionNormal(nextPoint * radius, nextPoint);
                    sphere[i++] = new VertexPositionNormal(downPoint * radius, downPoint);

                    sphere[i++] = new VertexPositionNormal(downPoint * radius, downPoint);
                    sphere[i++] = new VertexPositionNormal(nextPoint * radius, nextPoint);
                    sphere[i++] = new VertexPositionNormal(nextDownPoint * radius, nextDownPoint);

                    curPoint = nextPoint;
                    downPoint = nextDownPoint;
                }

                nextPoint = firstPoint;
                nextDownPoint = firstDownPoint;

                sphere[i++] = new VertexPositionNormal(curPoint * radius, curPoint);
                sphere[i++] = new VertexPositionNormal(nextPoint * radius, nextPoint);
                sphere[i++] = new VertexPositionNormal(downPoint * radius, downPoint);

                sphere[i++] = new VertexPositionNormal(downPoint * radius, downPoint);
                sphere[i++] = new VertexPositionNormal(nextPoint * radius, nextPoint);
                sphere[i++] = new VertexPositionNormal(nextDownPoint * radius, nextDownPoint);

                firstPoint = firstDownPoint;
            }

            //bottom cap
            centerPoint = -Vector3.UnitY * radius;
            curPoint = firstPoint;

            for (int t = 0; t < numHorizDivisions - 1; t++)
            {
                Matrix3x3.Transform(curPoint, rotForward, out nextPoint);

                sphere[i++] = new VertexPositionNormal(centerPoint, -Vector3.UnitY);
                sphere[i++] = new VertexPositionNormal(curPoint * radius, curPoint);
                sphere[i++] = new VertexPositionNormal(nextPoint * radius, nextPoint);

                curPoint = nextPoint;
            }

            //use the first point here so we don't accumulate some rotation error and have the verts not match
            nextPoint = firstPoint;
            sphere[i++] = new VertexPositionNormal(centerPoint, -Vector3.UnitY);
            sphere[i++] = new VertexPositionNormal(curPoint * radius, curPoint);
            sphere[i++] = new VertexPositionNormal(nextPoint * radius, nextPoint);

            return CreateFromData(renderer, sphere, VertexPositionNormal.sizeOf);
        }

        public static VertexBuffer CreatePosNormCapsule(Renderer renderer, float radius, float length, int numHorizDivisions = 12, int numVertDivisions = 6)
        {
            if (numHorizDivisions < 3) //3 would be a triangle capsule
                throw new Exception("Can't have a capsule with less than 3 horizontal divisions. " + numHorizDivisions);

            if (numVertDivisions < 1) //1 is endpoint -> body -> endpoint with no smoothing points, very pointy line
                throw new Exception("Can't have a capsule with less than 1 vertical divisions. " + numVertDivisions);

            //number of tris is 2 * numHoriz for the tip of the caps
            //with 4 * (numVert - 1) * numHoriz for the rest of the caps, then numHoriz * 2 for the body
            int numTris = (2 * numHorizDivisions) + (4 * (numVertDivisions - 1) * numHorizDivisions) + (numHorizDivisions * 2);

            //not using an index buffer, could greatly reduce number of verts if we did
            VertexPositionNormal[] capsule = new VertexPositionNormal[numTris * 3];

            int i = 0;
            Matrix3x3 rotDown = Matrix3x3.CreateFromAxisAngle(Vector3.UnitX, (float)(Math.PI * 0.5 / numVertDivisions));
            Matrix3x3 rotForward = Matrix3x3.CreateFromAxisAngle(Vector3.UnitY, (float)(Math.PI * 2.0 / numHorizDivisions));
            float halfLen = length * 0.5f;

            //top cap
            Vector3 centerPoint = Vector3.UnitY * radius;
            Vector3 capOffset = Vector3.UnitY * halfLen;
            Matrix3x3.Transform(Vector3.UnitY, rotDown, out var curPoint);
            Vector3 nextPoint;
            var firstPoint = curPoint;
            for (int t = 0; t < numHorizDivisions - 1; t++)
            {
                Matrix3x3.Transform(curPoint, rotForward, out nextPoint);

                capsule[i++] = new VertexPositionNormal(centerPoint + capOffset, Vector3.UnitY);
                capsule[i++] = new VertexPositionNormal(nextPoint * radius + capOffset, nextPoint);
                capsule[i++] = new VertexPositionNormal(curPoint * radius + capOffset, curPoint);

                curPoint = nextPoint;
            }

            //use the first point here so we don't accumulate some rotation error and have the verts not match
            nextPoint = firstPoint;
            capsule[i++] = new VertexPositionNormal(centerPoint + capOffset, Vector3.UnitY);
            capsule[i++] = new VertexPositionNormal(nextPoint * radius + capOffset, nextPoint);
            capsule[i++] = new VertexPositionNormal(curPoint * radius + capOffset, curPoint);

            //top sphere-half
            for (int t = 0; t < numVertDivisions - 1; t++)
            {
                curPoint = firstPoint;
                Matrix3x3.Transform(curPoint, rotDown, out var downPoint);
                Vector3 firstDownPoint = downPoint;
                Vector3 nextDownPoint;

                for (int r = 0; r < numHorizDivisions - 1; r++)
                {
                    Matrix3x3.Transform(curPoint, rotForward, out nextPoint);
                    Matrix3x3.Transform(downPoint, rotForward, out nextDownPoint);

                    capsule[i++] = new VertexPositionNormal(curPoint * radius + capOffset, curPoint);
                    capsule[i++] = new VertexPositionNormal(nextPoint * radius + capOffset, nextPoint);
                    capsule[i++] = new VertexPositionNormal(downPoint * radius + capOffset, downPoint);

                    capsule[i++] = new VertexPositionNormal(downPoint * radius + capOffset, downPoint);
                    capsule[i++] = new VertexPositionNormal(nextPoint * radius + capOffset, nextPoint);
                    capsule[i++] = new VertexPositionNormal(nextDownPoint * radius + capOffset, nextDownPoint);

                    curPoint = nextPoint;
                    downPoint = nextDownPoint;
                }

                nextPoint = firstPoint;
                nextDownPoint = firstDownPoint;

                capsule[i++] = new VertexPositionNormal(curPoint * radius + capOffset, curPoint);
                capsule[i++] = new VertexPositionNormal(nextPoint * radius + capOffset, nextPoint);
                capsule[i++] = new VertexPositionNormal(downPoint * radius + capOffset, downPoint);

                capsule[i++] = new VertexPositionNormal(downPoint * radius + capOffset, downPoint);
                capsule[i++] = new VertexPositionNormal(nextPoint * radius + capOffset, nextPoint);
                capsule[i++] = new VertexPositionNormal(nextDownPoint * radius + capOffset, nextDownPoint);

                firstPoint = firstDownPoint;
            }

            //body
            curPoint = firstPoint;
            for (int t = 0; t < numHorizDivisions - 1; t++)
            {
                Matrix3x3.Transform(curPoint, rotForward, out nextPoint);

                capsule[i++] = new VertexPositionNormal(curPoint * radius + capOffset, curPoint);
                capsule[i++] = new VertexPositionNormal(nextPoint * radius + capOffset, nextPoint);
                capsule[i++] = new VertexPositionNormal(curPoint * radius - capOffset, curPoint);

                capsule[i++] = new VertexPositionNormal(curPoint * radius - capOffset, curPoint);
                capsule[i++] = new VertexPositionNormal(nextPoint * radius + capOffset, nextPoint);
                capsule[i++] = new VertexPositionNormal(nextPoint * radius - capOffset, nextPoint);

                curPoint = nextPoint;
            }

            nextPoint = firstPoint;
            capsule[i++] = new VertexPositionNormal(curPoint * radius + capOffset, curPoint);
            capsule[i++] = new VertexPositionNormal(nextPoint * radius + capOffset, nextPoint);
            capsule[i++] = new VertexPositionNormal(curPoint * radius - capOffset, curPoint);

            capsule[i++] = new VertexPositionNormal(curPoint * radius - capOffset, curPoint);
            capsule[i++] = new VertexPositionNormal(nextPoint * radius + capOffset, nextPoint);
            capsule[i++] = new VertexPositionNormal(nextPoint * radius - capOffset, nextPoint);

            //bottom half of sphere
            for (int t = 0; t < numVertDivisions - 1; t++)
            {
                curPoint = firstPoint;
                Matrix3x3.Transform(curPoint, rotDown, out var downPoint);
                Vector3 firstDownPoint = downPoint;
                Vector3 nextDownPoint;

                for (int r = 0; r < numHorizDivisions - 1; r++)
                {
                    Matrix3x3.Transform(curPoint, rotForward, out nextPoint);
                    Matrix3x3.Transform(downPoint, rotForward, out nextDownPoint);

                    capsule[i++] = new VertexPositionNormal(curPoint * radius - capOffset, curPoint);
                    capsule[i++] = new VertexPositionNormal(nextPoint * radius - capOffset, nextPoint);
                    capsule[i++] = new VertexPositionNormal(downPoint * radius - capOffset, downPoint);

                    capsule[i++] = new VertexPositionNormal(downPoint * radius - capOffset, downPoint);
                    capsule[i++] = new VertexPositionNormal(nextPoint * radius - capOffset, nextPoint);
                    capsule[i++] = new VertexPositionNormal(nextDownPoint * radius - capOffset, nextDownPoint);

                    curPoint = nextPoint;
                    downPoint = nextDownPoint;
                }

                nextPoint = firstPoint;
                nextDownPoint = firstDownPoint;

                capsule[i++] = new VertexPositionNormal(curPoint * radius - capOffset, curPoint);
                capsule[i++] = new VertexPositionNormal(nextPoint * radius - capOffset, nextPoint);
                capsule[i++] = new VertexPositionNormal(downPoint * radius - capOffset, downPoint);

                capsule[i++] = new VertexPositionNormal(downPoint * radius - capOffset, downPoint);
                capsule[i++] = new VertexPositionNormal(nextPoint * radius - capOffset, nextPoint);
                capsule[i++] = new VertexPositionNormal(nextDownPoint * radius - capOffset, nextDownPoint);

                firstPoint = firstDownPoint;
            }

            //bottom cap
            centerPoint = -Vector3.UnitY * radius;
            curPoint = firstPoint;

            for (int t = 0; t < numHorizDivisions - 1; t++)
            {
                Matrix3x3.Transform(curPoint, rotForward, out nextPoint);

                capsule[i++] = new VertexPositionNormal(centerPoint - capOffset, -Vector3.UnitY);
                capsule[i++] = new VertexPositionNormal(curPoint * radius - capOffset, curPoint);
                capsule[i++] = new VertexPositionNormal(nextPoint * radius - capOffset, nextPoint);

                curPoint = nextPoint;
            }

            //use the first point here so we don't accumulate some rotation error and have the verts not match
            nextPoint = firstPoint;
            capsule[i++] = new VertexPositionNormal(centerPoint - capOffset, -Vector3.UnitY);
            capsule[i++] = new VertexPositionNormal(curPoint * radius - capOffset, curPoint);
            capsule[i++] = new VertexPositionNormal(nextPoint * radius - capOffset, nextPoint);

            return CreateFromData(renderer, capsule, VertexPositionNormal.sizeOf);
        }

        public void Dispose()
        {
            vb.Dispose();
        }
    }
}
