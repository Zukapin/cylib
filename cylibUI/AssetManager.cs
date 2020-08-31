using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using System.IO;
using System.Numerics;

using log;

using BepuUtilities;

namespace cylib
{
    /// <summary>
    /// Asset enums used internally by the AssetManager.
    /// Typed assets can be accessed elsewhere using the typed enums.
    /// 
    /// Assets are generally /shared/ resources, this entire bit is here only to save multiple loads / copies in memory.
    /// Some places will create assets individually because they're not made to be shared.
    /// </summary>
    public enum Asset
    {
        SH_START = 0,
        SH_POS_NORM_MAP_TEX,
        SH_POS_NORM_TEX,
        SH_POS_NORM_SCOLOR,
        SH_POS_TEX,
        SH_DISPLACEMENT_MAP,
        SH_LIGHT_POINT,
        SH_LIGHT_DIRECTIONAL,
        SH_COMPILE,
        SH_DEBUG_COLOR,
        SH_DEBUG_DEPTH,
        SH_DEBUG_NORMAL,
        SH_DEBUG_LIGHT,
        SH_FONT_BITMAP,
        SH_FONT_SDF,
        SH_ROUNDED_RECTANGLE_2D,
        SH_ROUNDED_RECTANGLE_3D,
        SH_LINE,
        SH_LINE_COLORED,
        SH_GRADIENT_CIRCLE,
        SH_DASHED_CIRCLE,
        SH_BORDERED_CIRCLE,
        SH_TEST,
        SH_END,

        TEX_START = 4096,
        TEX_WHITEPIXEL,
        TEX_DUCK,
        TEX_TEST,
        TEX_END,

        VB_START = 8192,
        VB_QUAD_POS_TEX_UNIT,
        VB_CIRLCE_POS_TEX_UNIT,
        VB_CIRLCE_POS_TEX_NORM_UNIT,
        VB_BOX_POS_NORM_UNIT,
        VB_CYLINDER_POS_NORM_UNIT,
        VB_END,

        F_START = 12288,
        F_CALLI_SDF_128,
        F_CALLI_SDF_64,
        F_CALLI_SDF_32,
        F_CALLI_SDF_16,
        F_CALLI_BMP_128,
        F_CALLI_BMP_64,
        F_CALLI_BMP_32,
        F_CALLI_BMP_16,
        F_SEGOEUI_SDF_128,
        F_SEGOEUI_SDF_64,
        F_SEGOEUI_SDF_32,
        F_SEGOEUI_SDF_16,
        F_SEGOEUI_BMP_128,
        F_SEGOEUI_BMP_64,
        F_SEGOEUI_BMP_32,
        F_SEGOEUI_BMP_16,
        F_END,

        B_START = 16384,
        B_WORLD,
        B_CAM_VIEWPROJ,
        B_CAM_INVVIEWPROJ,
        B_FONT,
        B_COLOR,
        B_POINT_LIGHT,
        B_DIRECTIONAL_LIGHT,
        B_ROUNDED_RECT,
        B_ROUNDED_RECT_3D,
        B_LINE_SEGMENT,
        B_LINE,
        B_QUAD_INDEX,
        B_LINE_COLORED_SEGMENT,
        B_LINE_COLORED,
        B_GRADIENT_CIRCLE_DATA,
        B_GRADIENT_CIRCLE,
        B_DASHED_CIRCLE_DATA,
        B_DASHED_CIRCLE,
        B_BORDERED_CIRCLE_DATA,
        B_BORDERED_CIRCLE,
        B_ENEMY_DRAW,
        B_ENEMY_DATA,
        B_END
    }

    public enum TextureAssets
    {
        WHITEPIXEL = Asset.TEX_WHITEPIXEL,
        DUCK = Asset.TEX_DUCK,
        TEST = Asset.TEX_TEST
    }

    public enum ShaderAssets
    {
        POS_NORM_MAP_TEX = Asset.SH_POS_NORM_MAP_TEX,
        POS_NORM_TEX = Asset.SH_POS_NORM_TEX,
        POS_NORM_SCOLOR = Asset.SH_POS_NORM_SCOLOR,
        POS_TEX = Asset.SH_POS_TEX,
        DISPLACEMENT_MAP = Asset.SH_DISPLACEMENT_MAP,
        LIGHT_POINT = Asset.SH_LIGHT_POINT,
        LIGHT_DIRECTIONAL = Asset.SH_LIGHT_DIRECTIONAL,
        COMPILE = Asset.SH_COMPILE,
        DEBUG_COLOR = Asset.SH_DEBUG_COLOR,
        DEBUG_DEPTH = Asset.SH_DEBUG_DEPTH,
        DEBUG_NORMAL = Asset.SH_DEBUG_NORMAL,
        DEBUG_LIGHT = Asset.SH_DEBUG_LIGHT,
        FONT_BITMAP = Asset.SH_FONT_BITMAP,
        FONT_SDF = Asset.SH_FONT_SDF,
        ROUNDED_RECTANGLE_2D = Asset.SH_ROUNDED_RECTANGLE_2D,
        ROUNDED_RECTANGLE_3D = Asset.SH_ROUNDED_RECTANGLE_3D,
        LINE = Asset.SH_LINE,
        LINE_COLORED = Asset.SH_LINE_COLORED,
        GRADIENT_CIRCLE = Asset.SH_GRADIENT_CIRCLE,
        DASHED_CIRCLE = Asset.SH_DASHED_CIRCLE,
        BORDERED_CIRCLE = Asset.SH_BORDERED_CIRCLE,
        TEST = Asset.SH_TEST,
    }

    public enum VertexBufferAssets
    {
        QUAD_POS_TEX_UNIT = Asset.VB_QUAD_POS_TEX_UNIT,
        CIRCLE_POS_TEX_UNIT = Asset.VB_CIRLCE_POS_TEX_UNIT,
        CIRCLE_POS_TEX_NORM_UNIT = Asset.VB_CIRLCE_POS_TEX_NORM_UNIT,
        BOX_POS_NORM_UNIT = Asset.VB_BOX_POS_NORM_UNIT,
        CYLINDER_POS_NORM_UNIT = Asset.VB_CYLINDER_POS_NORM_UNIT
    }

    public enum FontAssets
    {
        CALLI_SDF_128 = Asset.F_CALLI_SDF_128,
        CALLI_SDF_64 = Asset.F_CALLI_SDF_64,
        CALLI_SDF_32 = Asset.F_CALLI_SDF_32,
        CALLI_SDF_16 = Asset.F_CALLI_SDF_16,
        CALLI_BMP_128 = Asset.F_CALLI_BMP_128,
        CALLI_BMP_64 = Asset.F_CALLI_BMP_64,
        CALLI_BMP_32 = Asset.F_CALLI_BMP_32,
        CALLI_BMP_16 = Asset.F_CALLI_BMP_16,
        SEGOEUI_SDF_128 = Asset.F_SEGOEUI_SDF_128,
        SEGOEUI_SDF_64 = Asset.F_SEGOEUI_SDF_64,
        SEGOEUI_SDF_32 = Asset.F_SEGOEUI_SDF_32,
        SEGOEUI_SDF_16 = Asset.F_SEGOEUI_SDF_16,
        SEGOEUI_BMP_128 = Asset.F_SEGOEUI_BMP_128,
        SEGOEUI_BMP_64 = Asset.F_SEGOEUI_BMP_64,
        SEGOEUI_BMP_32 = Asset.F_SEGOEUI_BMP_32,
        SEGOEUI_BMP_16 = Asset.F_SEGOEUI_BMP_16,
    }

    public enum BufferAssets
    {
        WORLD = Asset.B_WORLD,
        CAM_VIEWPROJ = Asset.B_CAM_VIEWPROJ,
        CAM_INVVIEWPROJ = Asset.B_CAM_INVVIEWPROJ,
        FONT = Asset.B_FONT,
        COLOR = Asset.B_COLOR,
        POINT_LIGHT = Asset.B_POINT_LIGHT,
        DIRECTIONAL_LIGHT = Asset.B_DIRECTIONAL_LIGHT,
        ROUNDED_RECT = Asset.B_ROUNDED_RECT,
        ROUNDED_RECT_3D = Asset.B_ROUNDED_RECT_3D,
        LINE_SEGMENT = Asset.B_LINE_SEGMENT,
        LINE = Asset.B_LINE,
        QUAD_INDEX = Asset.B_QUAD_INDEX,
        LINE_COLORED_SEGMENT = Asset.B_LINE_COLORED_SEGMENT,
        LINE_COLORED = Asset.B_LINE_COLORED,
        GRADIENT_CIRCLE_DATA = Asset.B_GRADIENT_CIRCLE_DATA,
        GRADIENT_CIRCLE = Asset.B_GRADIENT_CIRCLE,
        DASHED_CIRCLE_DATA = Asset.B_DASHED_CIRCLE_DATA,
        DASHED_CIRCLE = Asset.B_DASHED_CIRCLE,
        BORDERED_CIRCLE_DATA = Asset.B_BORDERED_CIRCLE_DATA,
        BORDERED_CIRCLE = Asset.B_BORDERED_CIRCLE,
    }

    public abstract class AssetHelper
    {
        public static string getPath(Asset asset)
        {
            switch (asset)
            {
                case Asset.SH_COMPILE:
                    return "Content/Effects/Compile.fx";
                case Asset.SH_DEBUG_COLOR:
                    return "Content/Effects/ColorDebug.fx";
                case Asset.SH_DEBUG_DEPTH:
                    return "Content/Effects/DepthDebug.fx";
                case Asset.SH_DEBUG_LIGHT:
                    return "Content/Effects/LightDebug.fx";
                case Asset.SH_DEBUG_NORMAL:
                    return "Content/Effects/NormalDebug.fx";
                case Asset.SH_DISPLACEMENT_MAP:
                    return "Content/Effects/DisplacementMap.fx";
                case Asset.SH_LIGHT_POINT:
                    return "Content/Effects/PointLight.fx";
                case Asset.SH_LIGHT_DIRECTIONAL:
                    return "Content/Effects/DirectionalLight.fx";
                case Asset.SH_POS_NORM_MAP_TEX:
                    return "Content/Effects/PosNormMapTex.fx";
                case Asset.SH_POS_NORM_TEX:
                    return "Content/Effects/PosNormTex.fx";
                case Asset.SH_POS_NORM_SCOLOR:
                    return "Content/Effects/PosNormSColor.fx";
                case Asset.SH_POS_TEX:
                    return "Content/Effects/PosTex.fx";
                case Asset.SH_FONT_BITMAP:
                    return "Content/Effects/FontBitmap.fx";
                case Asset.SH_FONT_SDF:
                    return "Content/Effects/FontSDF.fx";
                case Asset.SH_ROUNDED_RECTANGLE_2D:
                    return "Content/Effects/RoundedRectangle2D.fx";
                case Asset.SH_ROUNDED_RECTANGLE_3D:
                    return "Content/Effects/RoundedRectangle.fx";
                case Asset.SH_LINE:
                    return "Content/Effects/Line.fx";
                case Asset.SH_LINE_COLORED:
                    return "Content/Effects/LineColored.fx";
                case Asset.SH_GRADIENT_CIRCLE:
                    return "Content/Effects/GradientCircle.fx";
                case Asset.SH_DASHED_CIRCLE:
                    return "Content/Effects/DashedCircle.fx";
                case Asset.SH_BORDERED_CIRCLE:
                    return "Content/Effects/BorderedCircle.fx";
                case Asset.SH_TEST:
                    return "Content/Effects/TestEffect.fx";
                case Asset.TEX_WHITEPIXEL:
                    return "Content/Textures/WhitePixel.bmp";
                case Asset.TEX_DUCK:
                    return "Content/Textures/angelduck.png";
                case Asset.TEX_TEST:
                    return "Content/Textures/test.png";
                case Asset.F_CALLI_SDF_128:
                    return "Content/Fonts/callisdf128.cyf";
                case Asset.F_CALLI_SDF_64:
                    return "Content/Fonts/callisdf64.cyf";
                case Asset.F_CALLI_SDF_32:
                    return "Content/Fonts/callisdf32.cyf";
                case Asset.F_CALLI_SDF_16:
                    return "Content/Fonts/callisdf16.cyf";
                case Asset.F_CALLI_BMP_128:
                    return "Content/Fonts/callibmp128.cyf";
                case Asset.F_CALLI_BMP_64:
                    return "Content/Fonts/callibmp64.cyf";
                case Asset.F_CALLI_BMP_32:
                    return "Content/Fonts/callibmp32.cyf";
                case Asset.F_CALLI_BMP_16:
                    return "Content/Fonts/callibmp16.cyf";
                case Asset.F_SEGOEUI_SDF_128:
                    return "Content/Fonts/segoeuisdf128.cyf";
                case Asset.F_SEGOEUI_SDF_64:
                    return "Content/Fonts/segoeuisdf64.cyf";
                case Asset.F_SEGOEUI_SDF_32:
                    return "Content/Fonts/segoeuisdf32.cyf";
                case Asset.F_SEGOEUI_SDF_16:
                    return "Content/Fonts/segoeuisdf16.cyf";
                case Asset.F_SEGOEUI_BMP_128:
                    return "Content/Fonts/segoeuibmp128.cyf";
                case Asset.F_SEGOEUI_BMP_64:
                    return "Content/Fonts/segoeuibmp64.cyf";
                case Asset.F_SEGOEUI_BMP_32:
                    return "Content/Fonts/segoeuibmp32.cyf";
                case Asset.F_SEGOEUI_BMP_16:
                    return "Content/Fonts/segoeuibmp16.cyf";
                default:
                    throw new Exception("Missing asset path case! " + asset);
            }
        }

        public static InputElement[] getVertexElements(ShaderAssets shader)
        {
            switch (shader)
            {
                case ShaderAssets.COMPILE:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.DEBUG_COLOR:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.DEBUG_DEPTH:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.DEBUG_LIGHT:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.DEBUG_NORMAL:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.DISPLACEMENT_MAP:
                    return VertexPositionNormalMapTexture.vertexElements;
                case ShaderAssets.LIGHT_POINT:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.LIGHT_DIRECTIONAL:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.POS_NORM_MAP_TEX:
                    return VertexPositionNormalMapTexture.vertexElements;
                case ShaderAssets.POS_NORM_TEX:
                    return VertexPositionNormalTexture.vertexElements;
                case ShaderAssets.POS_NORM_SCOLOR:
                    return VertexPositionNormal.vertexElements;
                case ShaderAssets.POS_TEX:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.FONT_BITMAP:
                    return null;
                case ShaderAssets.FONT_SDF:
                    return null;
                case ShaderAssets.LINE:
                    return null;
                case ShaderAssets.LINE_COLORED:
                    return null;
                case ShaderAssets.GRADIENT_CIRCLE:
                    return null;
                case ShaderAssets.DASHED_CIRCLE:
                    return null;
                case ShaderAssets.BORDERED_CIRCLE:
                    return null;
                case ShaderAssets.ROUNDED_RECTANGLE_3D:
                    return null;
                case ShaderAssets.ROUNDED_RECTANGLE_2D:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.TEST:
                    return VertexPositionTexture.vertexElements;
                default:
                    throw new Exception("Missing shader vert ele case! " + shader);
            }
        }
    }

    public class AssetManager
    {
        #region AssetBlob Reader
        /// <summary>
        /// A class for reading from one of our asset blob files.
        /// Keeps a file handle open until disposed.
        /// </summary>
        class AssetBlob : IDisposable
        {
            struct AssetDat
            {
                public long offset;
                public long len;

                public AssetDat(long offset, long len)
                {
                    this.offset = offset;
                    this.len = len;
                }
            }

            readonly string file;
            Dictionary<Asset, AssetDat> assetToOffset = new Dictionary<Asset, AssetDat>();
            Stream stream;
            long headerOffset;
            LimitStream pubStream;

            public AssetBlob(string file)
            {
                this.file = file;

                OpenStream();

                //read the header
                BinaryReader fr = new BinaryReader(stream, Encoding.Unicode, true);
                int numAssets = fr.ReadInt32();

                for (int i = 0; i < numAssets; i++)
                {
                    Asset a = (Asset)fr.ReadInt32();
                    long offset = fr.ReadInt64();
                    long len = fr.ReadInt64();

                    assetToOffset.Add(a, new AssetDat(offset, len));
                }

                fr.Dispose();

                //finish init
                headerOffset = stream.Position;

                CloseStream();
            }

            /// <summary>
            /// Opens the underlying stream, allowing assets to be read from the blob.
            /// </summary>
            public void OpenStream()
            {
                if (stream != null)
                {
                    Logger.WriteLine(LogType.POSSIBLE_ERROR, "Calling OpenStream on AssetBlob while the stream is already opened?");
                    return;
                }

                //We have open/close semantics here so we're not indefinitely keeping the file opened in this process.
                //It's Bad Practice, and, more importantly, it stops multiple clients from being run at the same time.

                //Should probably do a multi-process mutex that locks/releases the file
                stream = new FileStream(file, FileMode.Open);
                pubStream = new LimitStream(stream);
            }

            /// <summary>
            /// Closes the underlying stream. Assets can't be read until OpenStream is called again.
            /// </summary>
            public void CloseStream()
            {
                if (stream == null)
                {
                    Logger.WriteLine(LogType.POSSIBLE_ERROR, "Calling CloseStream on AssetBlob while the stream is already closed?");
                    return;
                }

                pubStream.Dispose();
                pubStream = null;

                stream.Dispose();
                stream = null;
            }

            public Stream GetAssetStream(Asset a)
            {
                //doing some IfDebug here to allow asset blob to be read at inappropriate times, with an accompanying error saying 'stop it'
#if DEBUG
                bool tempStream = false;
#endif
                if (stream == null)
                {
                    Logger.WriteLine(LogType.ERROR, "Calling getAssetStream while AssetBlob is closed...");
#if DEBUG
                    tempStream = true;
                    OpenStream();
#else
                    throw new Exception("Can't read asset...");
#endif
                }

#if DEBUG
                try
                {
#endif
                    AssetDat dat;
                    if (assetToOffset.TryGetValue(a, out dat))
                    {
                        pubStream.SetLimits(headerOffset + dat.offset, dat.len);
                        return pubStream;
                    }

                    return null;
#if DEBUG
                }
                finally
                {
                    if (tempStream)
                    {
                        CloseStream();
                    }
                }
#endif
            }

            public void Dispose()
            {
                CloseStream();
            }
        }
        #endregion

        private Dictionary<Asset, IDisposable> loadedAssets = new Dictionary<Asset, IDisposable>();
        private Renderer renderer;
        private AssetBlob blob;

        private bool inPreload = false;
        private bool inLoad = false;
        private HashSet<Asset> assetsAddedDuringLoad = new HashSet<Asset>();

        public AssetManager(Renderer renderer, string blob)
        {
            this.renderer = renderer;
            this.blob = new AssetBlob(blob);
        }

        /// <summary>
        /// Called before the main load, to verify all assets required for the loading screen are already in memory.
        /// Should be called from main thread.
        /// </summary>
        /// <param name="preloadAssets">Assets needed for use while loading</param>
        public void PreLoad(HashSet<Asset> preloadAssets)
        {
            if (inLoad)
                throw new ArgumentException("Called PreLoad while inLoad is true");
            if (inPreload)
                throw new ArgumentException("Called PreLoad while inPreload is true");

            inPreload = true;
            blob.OpenStream();

            //we want to load loading screen assets
            var s_toLoad = new HashSet<Asset>(preloadAssets.Except(loadedAssets.Keys));

            foreach (Asset a in s_toLoad)
            {
                loadedAssets.Add(a, loadAsset(a));
            }
        }

        /// <summary>
        /// Should be called from load thread.
        /// </summary>
        /// <param name="keepAssets"></param>
        public void StartLoad(HashSet<Asset> keepAssets, HashSet<Asset> preloadAssets)
        {
            if (inLoad)
                throw new ArgumentException("Called StartLoad while inLoad is true");
            if (!inPreload)
                throw new ArgumentException("Called StartLoad while inPreload is false");

            inPreload = false;
            inLoad = true;

            //here we want to load everything requested, and dispose things not needed anymore
            var s_Keep = new HashSet<Asset>(keepAssets.Union(preloadAssets));
            var s_toLoad = new HashSet<Asset>(s_Keep.Except(loadedAssets.Keys));
            var s_toDipose = new HashSet<Asset>(loadedAssets.Keys.Except(s_Keep));

            foreach (Asset s in s_toDipose)
            {
                loadedAssets[s].Dispose();
                loadedAssets.Remove(s);
            }

            foreach (Asset a in s_toLoad)
            {
                loadedAssets.Add(a, loadAsset(a));
            }

            assetsAddedDuringLoad.Clear();
        }

        /// <summary>
        /// Called after load is finished, to dispose of assets needed during load but not elsewhere.
        /// Should be called from main thread.
        /// </summary>
        /// <param name="keepAssets">Assets needed in general</param>
        /// <param name="preloadAssets">Assets needed during load</param>
        public void EndLoad(HashSet<Asset> keepAssets, HashSet<Asset> preloadAssets)
        {
            if (!inLoad)
                throw new ArgumentException("Called EndLoad while inLoad is false");
            if (inPreload)
                throw new ArgumentException("Called EndLoad while inPreload is true"); //???

            inLoad = false;

            //the assets we needed during load, but not those needed after
            //have to be careful with assets that were dynamically added during load time
            var s_toDipose = new HashSet<Asset>(preloadAssets.Except(keepAssets.Union(assetsAddedDuringLoad)));

#if DEBUG
            foreach (Asset a in assetsAddedDuringLoad.Except(keepAssets))
            {
                Logger.WriteLine(LogType.DEBUG, "Asset added dynamically during load: " + a);
            }
#endif

            foreach (Asset s in s_toDipose)
            {
                loadedAssets[s].Dispose();
                loadedAssets.Remove(s);
            }

            assetsAddedDuringLoad.Clear();

            blob.CloseStream();
        }

        private IDisposable loadAsset(Asset a)
        {
#if DEBUG
            if (!inLoad && !inPreload)
                Logger.WriteLine(LogType.ERROR, "Loading resource during runtime, add it to load list: " + a);
#else
            if (!inLoad && !inPreload)
                throw new Exception("Loading an asset while not in asset load mode?");
#endif

            if (a > Asset.SH_START && a < Asset.SH_END)
                return loadShader((ShaderAssets)a);
            if (a > Asset.TEX_START && a < Asset.TEX_END)
                return loadTexture((TextureAssets)a);
            if (a > Asset.VB_START && a < Asset.VB_END)
                return loadVertexBuffer((VertexBufferAssets)a);
            if (a > Asset.F_START && a < Asset.F_END)
                return loadFont((FontAssets)a);
            if (a > Asset.B_START && a < Asset.B_END)
                return loadBuffer((BufferAssets)a);

            throw new Exception("Invalid asset type, can't load");
        }

        private IDisposable getAsset(Asset a)
        {
            IDisposable toReturn = null;

            if (inLoad)
                assetsAddedDuringLoad.Add(a);

            if (loadedAssets.TryGetValue(a, out toReturn))
                return toReturn;

            //if we hit here, the asset we're trying to get hasn't been loaded.
#if DEBUG
            {
                //in debug mode, we don't care if we're currently loading -- just load the asset anyways and take the frame hit
                //this will display an error, because release mode doesn't allow any asset loading outside of load time.
                toReturn = loadAsset(a);
                loadedAssets.Add(a, toReturn);
                return toReturn;
            }
#else

            if (inLoad)
            {
                toReturn = loadAsset(a);
                loadedAssets.Add(a, toReturn);
                return toReturn;
            }
#endif

            throw new Exception("Asset not loaded, attempting to be used: " + a);
        }

        public Shader getAsset(ShaderAssets a)
        {
            return (Shader)getAsset((Asset)a);
        }

        public Texture getAsset(TextureAssets a)
        {
            return (Texture)getAsset((Asset)a);
        }

        public VertexBuffer getAsset(VertexBufferAssets a)
        {
            return (VertexBuffer)getAsset((Asset)a);
        }

        public Font getAsset(FontAssets a)
        {
            return (Font)getAsset((Asset)a);
        }

        public ConstBuffer<T> getAsset<T>(BufferAssets a) where T : struct
        {
            return (ConstBuffer<T>)getAsset((Asset)a);
        }

        private Shader loadShader(ShaderAssets shader)
        {
#if DEBUG
            return new Shader(renderer, AssetHelper.getPath((Asset)shader), AssetHelper.getVertexElements(shader));
#else
            return new Shader(renderer, blob.GetAssetStream((Asset)shader), AssetHelper.getVertexElements(shader));
#endif
        }

        private Texture loadTexture(TextureAssets tex)
        {
            Stream str = blob.GetAssetStream((Asset)tex);
            if (str == null)
            {
#if DEBUG
                Logger.WriteLine(LogType.DEBUG, "Can't find shader in asset blob, loading from file. " + tex);
                return new Texture(renderer, AssetHelper.getPath((Asset)tex));
#else
                Logger.WriteLine(LogType.ERROR, "Can't find shader in asset blob, loading from file. " + tex);
                return null; //welp, probably just gonna crash here
#endif
            }

            return new Texture(renderer, str);
        }

        private Font loadFont(FontAssets font)
        {
            Stream str = blob.GetAssetStream((Asset)font);
            if (str == null)
            {
#if DEBUG
                Logger.WriteLine(LogType.DEBUG, "Can't find shader in asset blob, loading from file. " + font);
                return new Font(renderer, AssetHelper.getPath((Asset)font));
#else
                Logger.WriteLine(LogType.ERROR, "Can't find shader in asset blob, loading from file. " + font);
                return null; //welp, probably just gonna crash here
#endif
            }

            return new Font(renderer, str);
        }

        private VertexBuffer loadVertexBuffer(VertexBufferAssets vb)
        {
            switch (vb)
            {
                case VertexBufferAssets.QUAD_POS_TEX_UNIT:
                    return VertexBuffer.CreatePosTexQuad(renderer, Vector2.Zero, new Vector2(1, 1));
                case VertexBufferAssets.CIRCLE_POS_TEX_UNIT:
                    return VertexBuffer.CreatePosTexCircle(renderer, Vector3.Zero, new Vector3(0, 0.5f, 0), new Vector3(0, 0, -1), 36);
                case VertexBufferAssets.CIRCLE_POS_TEX_NORM_UNIT:
                    return VertexBuffer.CreatePosTexNormCircle(renderer, Vector3.Zero, new Vector3(0, 0.5f, 0), new Vector3(0, 0, -1), 36);
                case VertexBufferAssets.BOX_POS_NORM_UNIT:
                    return VertexBuffer.CreatePosNormBox(renderer, Vector3.Zero, new Vector3(1, 1, 1));
                case VertexBufferAssets.CYLINDER_POS_NORM_UNIT:
                    return VertexBuffer.CreatePosNormCylinder(renderer, Vector3.Zero, 0.5f, 1f, Vector3.UnitY, 36);
                default:
                    throw new Exception("Missing vb case! " + vb);
            }
        }

        private IDisposable loadBuffer(BufferAssets buf)
        {
            switch (buf)
            {
                // This is hellzone until all of the buffers are defined.
                case BufferAssets.WORLD:
                    return new ConstBuffer<Matrix>(renderer, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
                case BufferAssets.CAM_VIEWPROJ:
                    return new ConstBuffer<CameraBuffer>(renderer, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
                case BufferAssets.CAM_INVVIEWPROJ:
                    return new ConstBuffer<Matrix>(renderer, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
                case BufferAssets.FONT:
                    return new ConstBuffer<FontGlyphBuffer>(renderer, 6 * 32, ResourceUsage.Dynamic, BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured);
                case BufferAssets.POINT_LIGHT:
                    return new ConstBuffer<PointLightBuffer>(renderer, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
                case BufferAssets.DIRECTIONAL_LIGHT:
                    return new ConstBuffer<DirectionalLightBuffer>(renderer, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
                case BufferAssets.COLOR:
                    return new ConstBuffer<ColorBuffer>(renderer, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
                case BufferAssets.ROUNDED_RECT:
                    return new ConstBuffer<RoundedRectData>(renderer, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
                case BufferAssets.QUAD_INDEX:
                    {
                        int ibLen = 128 * 6;
                        ushort[] ib = new ushort[ibLen];
                        for (int i = 0; i < ib.Length / 6; i++)
                        {
                            ib[i * 6 + 0] = (ushort)(i * 4 + 0);
                            ib[i * 6 + 1] = (ushort)(i * 4 + 1);
                            ib[i * 6 + 2] = (ushort)(i * 4 + 2);
                            ib[i * 6 + 3] = (ushort)(i * 4 + 2);
                            ib[i * 6 + 4] = (ushort)(i * 4 + 1);
                            ib[i * 6 + 5] = (ushort)(i * 4 + 3);
                        }
                        return new ConstBuffer<ushort>(renderer, ibLen, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, ib);
                    }
                /*
                case BufferAssets.LINE_COLORED:
                    return new ConstBuffer<ColoredLineData>(renderer, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
                case BufferAssets.GRADIENT_CIRCLE_DATA:
                    return new ConstBuffer<GradientCircleDrawData>(renderer, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
                case BufferAssets.DASHED_CIRCLE_DATA:
                    return new ConstBuffer<DashedCircleDrawData>(renderer, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
                case BufferAssets.BORDERED_CIRCLE_DATA:
                    return new ConstBuffer<BorderedCircleDrawData>(renderer, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
                case BufferAssets.LINE:
                    return new ConstBuffer<LineData>(renderer, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
                case BufferAssets.LINE_SEGMENT:
                    return new ConstBuffer<LineSegmentData>(renderer, 128, ResourceUsage.Dynamic, BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured);
                case BufferAssets.LINE_COLORED_SEGMENT:
                    return new ConstBuffer<ColoredLineSegmentData>(renderer, 128, ResourceUsage.Dynamic, BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured);
                case BufferAssets.GRADIENT_CIRCLE:
                    return new ConstBuffer<GradientCircleData>(renderer, 128, ResourceUsage.Dynamic, BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured);
                case BufferAssets.DASHED_CIRCLE:
                    return new ConstBuffer<DashedCircleData>(renderer, 128, ResourceUsage.Dynamic, BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured);
                case BufferAssets.BORDERED_CIRCLE:
                    return new ConstBuffer<BorderedCircleData>(renderer, 128, ResourceUsage.Dynamic, BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured);
                case BufferAssets.ROUNDED_RECT_3D:
                    return new ConstBuffer<RoundedRectangleData>(renderer, 128, ResourceUsage.Dynamic, BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured);
                */
                default:
                    throw new Exception("Missing buffer case! " + buf);
            }
        }
    }
}
