using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Threading;
using System.Numerics;
using System.Runtime.InteropServices;

using BepuUtilities;

namespace cylib
{
    [StructLayout(LayoutKind.Explicit, Size = 128)]
    struct CameraBuffer
    {
        [FieldOffset(0)]
        public Matrix viewMatrix;
        [FieldOffset(64)]
        public Matrix projMatrix;
    }

    public interface ICamera
    {
        Matrix getViewMatrix();
        Matrix getProjMatrix();
        Matrix getInvViewProjMatrix();
        Vector3 getForwardVec();
    }

    /// <summary>
    /// Third person view camera.
    /// </summary>
    public class TPVCamera : ICamera
    {
        Matrix worldMatrix;
        Matrix viewMatrix;
        Matrix projMatrix;
        Matrix viewProjMatrix;
        Matrix viewProjInvMatrix;

        float myYaw;
        float myPitch;
        Vector3 myAnchorPos;
        Vector3 myOffset;
        bool recalcWorld = true;

        public Matrix getViewMatrix()
        {
            recalcMatricies();
            return viewMatrix;
        }

        public Matrix getProjMatrix()
        {
            recalcMatricies();
            return projMatrix;
        }

        public Matrix getInvViewProjMatrix()
        {
            recalcMatricies();
            return viewProjInvMatrix;
        }

        public Vector3 AnchorPos
        {
            get
            {
                return myAnchorPos;
            }
            set
            {
                recalcWorld = true;
                myAnchorPos = value;
            }
        }

        public Vector3 Offset
        {
            get
            {
                return myOffset;
            }
            set
            {
                recalcWorld = true;
                myOffset = value;
            }
        }

        private const float minPitch = (float)(-Math.PI / 2.0 * 7.0 / 10.0);
        private const float maxPitch = (float)(Math.PI / 2.0 * 7.0 / 10.0);
        public float Pitch
        {
            get
            {
                return myPitch;
            }
            set
            {
                recalcWorld = true;
                myPitch = Math.Min(Math.Max(value, minPitch), maxPitch);
            }
        }

        public float Yaw
        {
            get
            {
                return myYaw;
            }
            set
            {
                recalcWorld = true;
                myYaw = value;
                if (myYaw < -Math.PI)
                    myYaw += (float)(2 * Math.PI);
                else if (myYaw > Math.PI)
                    myYaw -= (float)(2 * Math.PI);
            }
        }

        public Vector3 getForwardVec()
        {//row 3 of world, negative
            recalcMatricies();
            return -new Vector3(worldMatrix.Z.X, worldMatrix.Z.Y, worldMatrix.Z.Z);
        }

        public Vector3 getRightVec()
        {//row 1
            recalcMatricies();
            return new Vector3(worldMatrix.X.X, worldMatrix.X.Y, worldMatrix.X.Z);
        }

        public Vector3 getUpVec()
        {//row 2
            recalcMatricies();
            return new Vector3(worldMatrix.Y.X, worldMatrix.Y.Y, worldMatrix.Y.Z);
        }

        public TPVCamera(float aspect, Vector3 anchorPos, Vector3 offset, float yaw, float pitch)
        {
            this.AnchorPos = anchorPos;
            this.Offset = offset;
            this.Yaw = yaw;
            this.Pitch = pitch;
            projMatrix = Matrix.CreatePerspectiveFieldOfView((float)(Math.PI * 41.625 / 180.0), aspect, 1000.0f, 0.1f);

            recalcMatricies();
        }

        private void recalcMatricies()
        {
            if (recalcWorld)
            {
                Matrix.CreateRigid(Matrix3x3.CreateFromAxisAngle(Vector3.UnitX, Pitch) * Matrix3x3.CreateFromAxisAngle(Vector3.UnitY, Yaw), AnchorPos, out worldMatrix);
                Matrix.CreateRigid(Matrix3x3.Identity, Offset, out Matrix offMatrix);
                worldMatrix = offMatrix * worldMatrix;

                viewMatrix = Matrix.Invert(worldMatrix);
                viewProjMatrix = viewMatrix * projMatrix;
                viewProjInvMatrix = Matrix.Invert(viewProjMatrix);

                recalcWorld = false;
            }
        }
    }

    public class FPVCamera : ICamera
    {
        Matrix worldMatrix;
        Matrix viewMatrix;
        Matrix projMatrix;
        Matrix viewProjMatrix;
        Matrix viewProjInvMatrix;

        float myYaw;
        float myPitch;
        Vector3 mypos;
        bool recalcWorld = true;

        public Matrix getViewMatrix()
        {
            recalcMatricies();
            return viewMatrix;
        }

        public Matrix getProjMatrix()
        {
            recalcMatricies();
            return projMatrix;
        }

        public Matrix getInvViewProjMatrix()
        {
            recalcMatricies();
            return viewProjInvMatrix;
        }

        public Vector3 pos
        {
            get
            {
                return mypos;
            }
            set
            {
                recalcWorld = true;
                mypos = value;
            }
        }

        private const float minPitch = (float)(-Math.PI / 2.0 * 7.0 / 10.0);
        private const float maxPitch = (float)(Math.PI / 2.0 * 7.0 / 10.0);
        public float pitch
        {
            get
            {
                return myPitch;
            }
            set
            {
                recalcWorld = true;
                myPitch = Math.Min(Math.Max(value, minPitch), maxPitch);
            }
        }

        public float yaw
        {
            get
            {
                return myYaw;
            }
            set
            {
                recalcWorld = true;
                myYaw = value;
                if (myYaw < -Math.PI)
                    myYaw += (float)(2 * Math.PI);
                else if (myYaw > Math.PI)
                    myYaw -= (float)(2 * Math.PI);
            }
        }

        public Vector3 getForwardVec()
        {//row 3 of world, negative
            recalcMatricies();
            return -new Vector3(worldMatrix.Z.X, worldMatrix.Z.Y, worldMatrix.Z.Z);
        }

        public Vector3 getRightVec()
        {//row 1
            recalcMatricies();
            return new Vector3(worldMatrix.X.X, worldMatrix.X.Y, worldMatrix.X.Z);
        }

        public Vector3 getUpVec()
        {//row 2
            recalcMatricies();
            return new Vector3(worldMatrix.Y.X, worldMatrix.Y.Y, worldMatrix.Y.Z);
        }

        public FPVCamera(float aspect, Vector3 startPos, float yaw, float pitch)
        {
            mypos = startPos;
            this.yaw = yaw;
            this.pitch = pitch;
            projMatrix = Matrix.CreatePerspectiveFieldOfView((float)(Math.PI * 41.625 / 180.0), aspect, 1000.0f, 0.1f);

            recalcMatricies();
        }

        private void recalcMatricies()
        {
            if (recalcWorld)
            {
                Matrix.CreateRigid(Matrix3x3.CreateFromAxisAngle(Vector3.UnitX, pitch) * Matrix3x3.CreateFromAxisAngle(Vector3.UnitY, yaw), mypos, out worldMatrix);

                viewMatrix = Matrix.Invert(worldMatrix);
                viewProjMatrix = viewMatrix * projMatrix;
                viewProjInvMatrix = Matrix.Invert(viewProjMatrix);

                recalcWorld = false;
            }
        }
    }

    public class OrthoCamera : ICamera
    {
        Matrix worldMatrix;
        Matrix viewMatrix;
        Matrix projMatrix;
        Matrix viewProjMatrix;
        Matrix viewProjInvMatrix;

        Vector2 myScale;
        Vector2 mypos;
        bool recalcWorld = true;

        public Matrix getViewMatrix()
        {
            recalcMatricies();
            return viewMatrix;
        }

        public Matrix getProjMatrix()
        {
            recalcMatricies();
            return projMatrix;
        }

        public Matrix getInvViewProjMatrix()
        {
            recalcMatricies();
            return viewProjInvMatrix;
        }

        public Vector3 getForwardVec()
        {
            return -Vector3.UnitZ;
        }

        public Vector2 pos
        {
            get
            {
                return mypos;
            }
            set
            {
                recalcWorld = true;
                mypos = value;
            }
        }

        public Vector2 scale
        {
            get
            {
                return myScale;
            }
            set
            {
                recalcWorld = true;
                myScale = value;
            }
        }

        public OrthoCamera(Vector2 position, float width, float height)
        {
            this.pos = position;
            this.scale = new Vector2(width, height);
        }

        private void recalcMatricies()
        {
            if (recalcWorld)
            {
                Matrix.CreateRigid(Matrix3x3.Identity, new Vector3(mypos.X, mypos.Y, 0), out worldMatrix);
                viewMatrix = Matrix.Invert(worldMatrix); //can just negate world for this? since it's translation only?
                Matrix.CreateOrthographic(0, myScale.X, myScale.Y, 0, 1000.0f, 0.1f, out projMatrix);
                viewProjMatrix = viewMatrix * projMatrix;
                viewProjInvMatrix = Matrix.Invert(viewProjMatrix);

                recalcWorld = false;
            }
        }
    }

    public static class CameraHelper
    {
        /// <summary>
        /// Returns the world-space coordinates of the given screen-space coordinates.
        /// </summary>
        public static Vector3 getWorldSpace(Vector3 screenSpace, Matrix invViewProj)
        {
            Matrix.Transform(screenSpace, invViewProj, out Vector4 result);

            return new Vector3(result.X, result.Y, result.Z) / result.W;
        }

        /// <summary>
        /// Returns the world-space coordinates of the mouse.
        /// Note: Depth out of screen is nonsensical, only really useful for an ortho camera, or normalizing as a direction after.
        /// </summary>
        public static Vector3 getWorldSpace(int mouseX, int mouseY, int screenWidth, int screenHeight, Matrix invViewProj)
        {
            //takes the mouseX and mouseY to screenspace coords, [-1, 1]
            Vector3 src = new Vector3(
                (mouseX / (float)screenWidth - 0.5f) * 2f,
                (mouseY / (float)screenHeight - 0.5f) * -2f,
                0.5f);

            return getWorldSpace(src, invViewProj);
        }

        /// <summary>
        /// Returns the raw pos * viewProj transform
        /// </summary>
        public static Vector3 getScreenSpace(Vector3 pos, Matrix viewProj)
        {
            Matrix.Transform(pos, viewProj, out Vector4 result);

            return new Vector3(result.X, result.Y, result.Z) / result.W;
        }

        /// <summary>
        /// Returns the pixel-space coords
        /// </summary>
        public static Vector2 getScreenSpace(Vector3 pos, int screenWidth, int screenHeight, Matrix viewProj)
        {
            Vector3 screen = getScreenSpace(pos, viewProj);

            return new Vector2(
                (screen.X + 1) * 0.5f * screenWidth,
                (screen.Y + 1) * -0.5f * screenHeight);
        }

        /// <summary>
        /// Returns the pixel-space coords, for a given world-space position.
        /// An insignificant depth is added to the world-space position, verify that you don't actually need that.
        /// </summary>
        public static Vector2 getScreenSpace(Vector2 pos, int screenWidth, int screenHeight, Matrix viewProj)
        {
            Vector3 screen = getScreenSpace(new Vector3(pos, 0.5f), viewProj);

            return new Vector2(
                (screen.X + 1) * 0.5f * screenWidth,
                (-screen.Y + 1) * 0.5f * screenHeight);
        }
    }
}
