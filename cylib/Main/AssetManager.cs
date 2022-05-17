using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using System.IO;
using System.Numerics;

using cyUtility;

using BepuUtilities;
using System.Reflection.Metadata.Ecma335;

namespace cylib
{
    public delegate VertexBuffer VBLoader(Renderer renderer);
    public delegate IDisposable AssetLoader(Renderer renderer);

    public class AssetManager
    {
        public enum AssetTypes
        {
            ERR = -1,
            SHADER = 0,
            TEXTURE = 1,
            VERTEX_BUFFER = 2,
            FONT = 3,
            BUFFER = 4,
            CUSTOM = 5,
        }

        #region AssetBlob Reader
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

        /// <summary>
        /// A class for reading from one of our asset blob files.
        /// Keeps a file handle open until disposed.
        /// </summary>
        class AssetBlob : IDisposable
        {
            public static List<(string name, AssetTypes type, long offset, long len)> ParseHeader(string file)
            {
                var toRet = new List<(string name, AssetTypes type, long offset, long len)>();

                using var fs = new FileStream(file, FileMode.Open);
                using var fr = new BinaryReader(fs, Encoding.Unicode, true);

                int numAssets = fr.ReadInt32();
                long headerLen = fr.ReadInt64();

                for (int i = 0; i < numAssets; i++)
                {
                    string name = fr.ReadString();
                    AssetTypes t = (AssetTypes)fr.ReadInt32();
                    long offset = fr.ReadInt64();
                    long len = fr.ReadInt64();

                    toRet.Add((name, t, offset + headerLen, len));
                }

                return toRet;
            }

            readonly string file;
            readonly AssetDat[] assetToOffset;
            Stream stream;
            LimitStream pubStream;

            public readonly int startID;

            /// <summary>
            /// non-inclusive -- so if startID is 0 and there's 1 asset, then endID will be 1
            /// </summary>
            public readonly int endID;

            public AssetBlob(string file, int startID, AssetDat[] assetToOffset)
            {
                this.file = file;
                this.startID = startID;
                this.assetToOffset = assetToOffset;
                endID = startID + assetToOffset.Length;
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

            public Stream GetAssetStream(int a)
            {
                if (a < 0 || a >= assetToOffset.Length)
                {
                    Logger.WriteLine(LogType.ERROR, "Getting asset stream for an asset that doesn't exist: " + a);
                    return null;
                }

                if (stream == null)
                {
                    Logger.WriteLine(LogType.ERROR, "Calling getAssetStream while AssetBlob is closed...");
                    throw new Exception("Can't read asset, AssetBlob is closed.");
                }

                var dat = assetToOffset[a];
                pubStream.SetLimits(dat.offset, dat.len);
                return pubStream;
            }

            public void Dispose()
            {
                CloseStream();
            }
        }
        #endregion

        private readonly Dictionary<string, IDisposable> loadedAssets = new Dictionary<string, IDisposable>();
        private readonly Renderer renderer;

        private readonly List<AssetBlob> assetBlobs = new List<AssetBlob>();
        private readonly Dictionary<string, (int, AssetTypes)> assetNameToID = new Dictionary<string, (int, AssetTypes)>();

        private readonly Dictionary<int, VBLoader> vertexLoaders = new Dictionary<int, VBLoader>();
        private readonly Dictionary<int, AssetLoader> bufferLoaders = new Dictionary<int, AssetLoader>();
        private readonly Dictionary<int, AssetLoader> customLoaders = new Dictionary<int, AssetLoader>();

        private bool inPreload = false;
        private bool inLoad = false;
        private readonly HashSet<string> assetsAddedDuringLoad = new HashSet<string>();

        public AssetManager(Renderer renderer)
        {
            this.renderer = renderer;
        }

        public void AddAssetBlob(string blob)
        {
            int startID = assetNameToID.Count;
            var assets = AssetBlob.ParseHeader(blob);
            AssetDat[] assetData = new AssetDat[assets.Count];
            for (int i = 0; i < assetData.Length; i++)
            {
                var a = assets[i];
                assetData[i] = new AssetDat(a.offset, a.len);
                assetNameToID.Add(a.name, (i + startID, a.type));

                Logger.WriteLine(LogType.VERBOSE2, "Added asset: " + a.name + " " + a.type);
            }

            var b = new AssetBlob(blob, startID, assetData);
            assetBlobs.Add(b);
        }

        public void AddVertexBuffers(IEnumerable<(string, VBLoader)> loaders)
        {
            int i = assetNameToID.Count;
            foreach (var (name, del) in loaders)
            {
                assetNameToID.Add(name, (i, AssetTypes.VERTEX_BUFFER));
                vertexLoaders.Add(i, del);
                i++;
            }
        }

        public void AddBufferLoaders(IEnumerable<(string, AssetLoader)> loaders)
        {
            int i = assetNameToID.Count;
            foreach (var (name, del) in loaders)
            {
                assetNameToID.Add(name, (i, AssetTypes.BUFFER));
                bufferLoaders.Add(i, del);
                i++;
            }
        }

        public void AddCustomLoaders(IEnumerable<(string, AssetLoader)> loaders)
        {
            int i = assetNameToID.Count;
            foreach (var (name, del) in loaders)
            {
                assetNameToID.Add(name, (i, AssetTypes.CUSTOM));
                customLoaders.Add(i, del);
                i++;
            }
        }

        private (int ID, AssetTypes type) GetAssetInfo(string a)
        {
            if (assetNameToID.TryGetValue(a, out var dat))
            {
                return dat;
            }

            Logger.WriteLine(LogType.ERROR, "Can't find asset name in blob: " + a);
            return (-1, AssetTypes.ERR);
        }

        private Stream GetAssetStream(int a)
        {
            foreach (var b in assetBlobs)
            {
                if (a >= b.startID && a < b.endID)
                {
                    return b.GetAssetStream(a - b.startID);
                }
            }

            Logger.WriteLine(LogType.ERROR, "Can't find asset stream: " + a);
            return null;
        }

        /// <summary>
        /// Called before the main load, to verify all assets required for the loading screen are already in memory.
        /// Should be called from main thread.
        /// </summary>
        /// <param name="preloadAssets">Assets needed for use while loading</param>
        public void PreLoad(HashSet<string> preloadAssets)
        {
            if (inLoad)
                throw new ArgumentException("Called PreLoad while inLoad is true");
            if (inPreload)
                throw new ArgumentException("Called PreLoad while inPreload is true");

            inPreload = true;

            foreach(var b in assetBlobs)
            {
                b.OpenStream();
            }

            //we want to load loading screen assets
            var s_toLoad = new HashSet<string>(preloadAssets.Except(loadedAssets.Keys));

            foreach (var a in s_toLoad)
            {
                loadedAssets.Add(a, LoadAsset(a));
            }

            assetsAddedDuringLoad.Clear();
        }

        /// <summary>
        /// Should be called from load thread.
        /// </summary>
        /// <param name="keepAssets"></param>
        public void StartLoad(HashSet<string> keepAssets, HashSet<string> preloadAssets)
        {
            if (inLoad)
                throw new ArgumentException("Called StartLoad while inLoad is true");
            if (!inPreload)
                throw new ArgumentException("Called StartLoad while inPreload is false");

            inPreload = false;
            inLoad = true;

#if DEBUG
            foreach (var a in assetsAddedDuringLoad.Except(preloadAssets))
            {
                Logger.WriteLine(LogType.DEBUG, "Asset added dynamically during preload: " + a);
            }
#endif

            //here we want to load everything requested, and dispose things not needed anymore
            var s_Keep = new HashSet<string>(keepAssets.Union(preloadAssets.Union(assetsAddedDuringLoad)));
            var s_toLoad = new HashSet<string>(s_Keep.Except(loadedAssets.Keys));
            var s_toDipose = new HashSet<string>(loadedAssets.Keys.Except(s_Keep));

            foreach (var s in s_toDipose)
            {
                loadedAssets[s].Dispose();
                loadedAssets.Remove(s);
            }

            foreach (var a in s_toLoad)
            {
                loadedAssets.Add(a, LoadAsset(a));
            }

            assetsAddedDuringLoad.Clear();
        }

        public bool LoadHasWorkToDo(HashSet<string> keepAssets, HashSet<string> preloadAssets)
        {
            var s_Keep = new HashSet<string>(keepAssets.Union(preloadAssets.Union(assetsAddedDuringLoad)));
            var s_toLoad = new HashSet<string>(s_Keep.Except(loadedAssets.Keys));
            var s_toDipose = new HashSet<string>(loadedAssets.Keys.Except(s_Keep));

            if (s_toLoad.Count == 0 && s_toDipose.Count == 0)
                return false;
            return true;
        }

        /// <summary>
        /// Called after load is finished, to dispose of assets needed during load but not elsewhere.
        /// Should be called from main thread.
        /// </summary>
        /// <param name="keepAssets">Assets needed in general</param>
        /// <param name="preloadAssets">Assets needed during load</param>
        public void EndLoad(HashSet<string> keepAssets, HashSet<string> preloadAssets)
        {
            if (!inLoad)
                throw new ArgumentException("Called EndLoad while inLoad is false");
            if (inPreload)
                throw new ArgumentException("Called EndLoad while inPreload is true"); //???

            inLoad = false;

            //the assets we needed during load, but not those needed after
            //have to be careful with assets that were dynamically added during load time
            var s_toDipose = new HashSet<string>(preloadAssets.Except(keepAssets.Union(assetsAddedDuringLoad)));

#if DEBUG
            foreach (var a in assetsAddedDuringLoad.Except(keepAssets))
            {
                Logger.WriteLine(LogType.DEBUG, "Asset added dynamically during load: " + a);
            }
#endif

            foreach (var s in s_toDipose)
            {
                loadedAssets[s].Dispose();
                loadedAssets.Remove(s);
            }

            assetsAddedDuringLoad.Clear();

            foreach (var b in assetBlobs)
            {
                b.CloseStream();
            }
        }

        private IDisposable LoadAsset(string name)
        {
#if DEBUG
            if (!inLoad && !inPreload)
                Logger.WriteLine(LogType.ERROR, "Loading resource during runtime, add it to load list: " + name);
#else
            if (!inLoad && !inPreload)
                throw new Exception("Loading an asset while not in asset load mode? " + name);
#endif

            var inf = GetAssetInfo(name);
            var a = inf.ID;

            switch (inf.type)
            {
                case AssetTypes.SHADER:
                    return LoadShader(a);
                case AssetTypes.TEXTURE:
                    return LoadTexture(a);
                case AssetTypes.VERTEX_BUFFER:
                    return LoadVertexBuffer(a);
                case AssetTypes.FONT:
                    return LoadFont(a);
                case AssetTypes.BUFFER:
                    return LoadBuffer(a);
                case AssetTypes.CUSTOM:
                    return LoadCustom(a);
            }

            throw new Exception("Invalid asset, can't load: " + name + " " + a + " " + inf.type);
        }

        public IDisposable GetAsset(string a)
        {
            IDisposable toReturn = null;

            if (inLoad || inPreload)
                assetsAddedDuringLoad.Add(a);

            if (loadedAssets.TryGetValue(a, out toReturn))
                return toReturn;

            //if we hit here, the asset we're trying to get hasn't been loaded.
#if DEBUG
            {
                //in debug mode, we don't care if we're currently loading -- just load the asset anyways and take the frame hit
                //this will display an error, because release mode doesn't allow any asset loading outside of load time.
                toReturn = LoadAsset(a);
                loadedAssets.Add(a, toReturn);
                return toReturn;
            }
#else

            if (inLoad)
            {
                toReturn = LoadAsset(a);
                loadedAssets.Add(a, toReturn);
                return toReturn;
            }
#endif

            throw new Exception("Asset not loaded, attempting to be used: " + a);
        }

        public Shader GetShader(string a)
        {
            var ret = GetAsset(a);
            if (ret is Shader shader)
                return shader;

            throw new Exception("Wrong asset type, this is not a shader " + a);
        }

        public Texture GetTexture(string a)
        {
            var ret = GetAsset(a);
            if (ret is Texture texture)
                return texture;

            throw new Exception("Wrong asset type, this is not a texture " + a);
        }

        public VertexBuffer GetVertexBuffer(string a)
        {
            var ret = GetAsset(a);
            if (ret is VertexBuffer vertexBuffer)
                return vertexBuffer;

            throw new Exception("Wrong asset type, this is not a Vertex Buffer " + a);
        }

        public Font GetFont(string a)
        {
            var ret = GetAsset(a);
            if (ret is Font font)
                return font;

            throw new Exception("Wrong asset type, this is not a Font " + a);
        }

        public ConstBuffer<T> GetBuffer<T>(string a) where T : struct
        {
            var ret = GetAsset(a);
            if (ret is ConstBuffer<T> buf)
                return buf;

            throw new Exception("Wrong asset type, this is not the right type of buffer " + a);
        }

        private Shader LoadShader(int shader)
        {
            Stream str = GetAssetStream(shader);
            if (str == null)
            {
                Logger.WriteLine(LogType.ERROR, "Can't find shader in asset blob: " + shader);
                return null;
            }

            return new Shader(renderer, str);
        }

        private Texture LoadTexture(int tex)
        {
            Stream str = GetAssetStream(tex);
            if (str == null)
            {
                Logger.WriteLine(LogType.ERROR, "Can't find texture in asset blob: " + tex);
                return null; //welp, probably just gonna crash here
            }

            return new Texture(renderer, str);
        }

        private Font LoadFont(int font)
        {
            Stream str = GetAssetStream(font);
            if (str == null)
            {
                Logger.WriteLine(LogType.ERROR, "Can't find font in asset blob: " + font);
                return null; //welp, probably just gonna crash here
            }

            return new Font(renderer, str);
        }

        private VertexBuffer LoadVertexBuffer(int vb)
        {
            if (vertexLoaders.TryGetValue(vb, out var loader))
            {
                return loader(renderer);
            }

            Logger.WriteLine(LogType.ERROR, "Invalid vertex buffer loading ID: " + vb);
            return null;
        }

        private IDisposable LoadBuffer(int buf)
        {
            if (bufferLoaders.TryGetValue(buf, out var loader))
            {
                return loader(renderer);
            }

            Logger.WriteLine(LogType.ERROR, "Invalid buffer loading ID: " + buf);
            return null;

            /*
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
                
                default:
                    throw new Exception("Missing buffer case! " + buf);
            }
            */
        }

        private IDisposable LoadCustom(int c)
        {
            if (customLoaders.TryGetValue(c, out var loader))
            {
                return loader(renderer);
            }

            Logger.WriteLine(LogType.ERROR, "Invalid custom loading ID: " + c);
            return null;
        }
    }
}
