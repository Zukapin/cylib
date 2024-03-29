﻿using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Numerics;
using System.Threading;
using System.Collections.Generic;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;
using MapFlags = SharpDX.Direct3D11.MapFlags;

using BepuUtilities;
using Matrix = BepuUtilities.Matrix;
using Matrix3x3 = BepuUtilities.Matrix3x3;
using Vector3 = System.Numerics.Vector3;

using cyUtility;

namespace cylib
{
    public delegate void DrawDelegate();
    public delegate void UpdateDelegate(float dt);

    public class GameStage : IDisposable
    {
        public float Timestep = (float)(1 / 60.0);
        public bool UseFixedTimestep = true;
#if DEBUG
        const bool DrawDebug = false;
        public static float DEBUG_TIDI = 1f; //pretends the dt passed in multiplied by this
#endif

        #region DX Main
        DepthStencilState stencilDefault;
        #endregion
        #region Quads
        VertexBuffer quad_postex_unit;
        #endregion
        #region Shaders
        Shader s_Compile;
        Shader s_PointLight;
        Shader s_DirectionalLight;
#if DEBUG
        Shader s_ColorDebug;
        Shader s_DepthDebug;
        Shader s_NormalDebug;
        Shader s_LightDebug;
#endif
        #endregion
        #region Render Targets
        Texture2D depthTarget;
        ShaderResourceView depthSRV;
        DepthStencilView depthDSV;

        Texture2D colorTarget;
        ShaderResourceView colorSRV;
        RenderTargetView colorRTV;

        Texture2D normalTarget;
        ShaderResourceView normalSRV;
        RenderTargetView normalRTV;

        Texture2D lightTarget;
        ShaderResourceView lightSRV;
        RenderTargetView lightRTV;
        #endregion
        #region Buffers
        Buffer fullscreenCameraBuffer;
        ConstBuffer<CameraBuffer> viewProjBuffer;
        ConstBuffer<Matrix> viewProjInvBuffer;
        ConstBuffer<Matrix> worldBuffer;
        #endregion
        #region Event Managers
        /// <summary>
        /// The manager actively being used for inputs/updates/rendering
        /// </summary>
        EventManager activeManager;

        /// <summary>
        /// The manager used for loading/preloading
        /// </summary>
        EventManager loadManager;

        /// <summary>
        /// The manager used by scenes
        /// </summary>
        EventManager sceneManager;
        #endregion

        ICamera cam3D;
        ICamera cam2D;

        InputHandler input;

        public InputHandler Input
        {
            get
            {
                return input;
            }
        }

        IScene currentScene;

        float loadingTime = 0;
        Thread loadingThread;

        public readonly Renderer renderer;
        private Texture2D SwapChainBackBuffer;
        public RenderTargetView renderView;

        public GameStage(Renderer renderer, IEnumerable<ActionInformation> SupportedActions, string ActionBindingsFile)
        {
            this.renderer = renderer;

            input = new InputHandler(this, SupportedActions, ActionBindingsFile);

            loadManager = new EventManager();
            sceneManager = new EventManager();

            activeManager = sceneManager;
            input.events = activeManager;

            loadAssets(null); //load starting assets
            LoadRenderTargets();
            #region Buffer Setup
            {
                fullscreenCameraBuffer = new Buffer(renderer.Device,
                    Utilities.SizeOf<CameraBuffer>(),
                    ResourceUsage.Default,
                    BindFlags.ConstantBuffer,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.None,
                    0);

                CameraBuffer fs = new CameraBuffer();
                fs.viewMatrix = Matrix.Identity;
                Matrix.CreateOrthographic(0.0f, renderer.ResolutionWidth, renderer.ResolutionHeight, 0.0f, 0.0f, 1.0f, out fs.projMatrix);

                updateSubresource(fullscreenCameraBuffer, fs);
            }


            quad_postex_unit = renderer.Assets.GetVertexBuffer(Renderer.DefaultAssets.VB_QUAD_POS_TEX_UNIT);

            viewProjBuffer = renderer.Assets.GetBuffer<CameraBuffer>(Renderer.DefaultAssets.BUF_CAM_VIEWPROJ);
            viewProjInvBuffer = renderer.Assets.GetBuffer<Matrix>(Renderer.DefaultAssets.BUF_CAM_INVVIEWPROJ);
            worldBuffer = renderer.Assets.GetBuffer<Matrix>(Renderer.DefaultAssets.BUF_WORLD);
            #endregion
            #region Shader Setup
            s_Compile = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_COMPILE);
            s_PointLight = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_LIGHT_POINT);
            s_DirectionalLight = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_LIGHT_DIRECTIONAL);

#if DEBUG
            s_ColorDebug = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_DEBUG_COLOR);
            s_DepthDebug = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_DEBUG_DEPTH);
            s_NormalDebug = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_DEBUG_NORMAL);
            s_LightDebug = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_DEBUG_LIGHT);
#endif
            #endregion
            #region Misc Stuff
            stencilDefault = new DepthStencilState(renderer.Device, new DepthStencilStateDescription()
            {
                IsStencilEnabled = false,
                StencilReadMask = 0,
                StencilWriteMask = 0,
                DepthComparison = Comparison.Greater,
                DepthWriteMask = DepthWriteMask.All,
                IsDepthEnabled = true
            });
            #endregion
        }

        public void ChangeResolution(int NewResX, int NewResY)
        {
            renderView.Dispose();
            renderView = null;
            SwapChainBackBuffer.Dispose();
            SwapChainBackBuffer = null;

            renderer.ChangeResolution(NewResX, NewResY);
            OnResolutionChange();
        }

        private void OnResolutionChange()
        {
            LoadRenderTargets();

            CameraBuffer fs = new CameraBuffer();
            fs.viewMatrix = Matrix.Identity;
            Matrix.CreateOrthographic(0.0f, renderer.ResolutionWidth, renderer.ResolutionHeight, 0.0f, 0.0f, 1.0f, out fs.projMatrix);

            updateSubresource(fullscreenCameraBuffer, fs);
        }

        private void LoadRenderTargets()
        {
            if (SwapChainBackBuffer != null)
                SwapChainBackBuffer.Dispose();
            SwapChainBackBuffer = Resource.FromSwapChain<Texture2D>(renderer.SwapChain, 0);

            if (renderView != null)
                renderView.Dispose();
            renderView = new RenderTargetView(renderer.Device, SwapChainBackBuffer,
                new RenderTargetViewDescription()
                {
                    Format = Format.R8G8B8A8_UNorm_SRgb,
                    Dimension = RenderTargetViewDimension.Texture2D,
                });

            if (depthTarget != null)
                depthTarget.Dispose();

            depthTarget = new Texture2D(renderer.Device, new Texture2DDescription()
            {
                Format = Format.R32_Typeless,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                Width = renderer.ResolutionWidth,
                Height = renderer.ResolutionHeight,
                MipLevels = 1,
                ArraySize = 1,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default,
                SampleDescription = new SampleDescription(1, 0)
            });

            if (depthSRV != null)
                depthSRV.Dispose();
            depthSRV = new ShaderResourceView(renderer.Device, depthTarget, new ShaderResourceViewDescription()
            {
                Format = Format.R32_Float,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = { MostDetailedMip = 0, MipLevels = 1 }

            });

            if (depthDSV != null)
                depthDSV.Dispose();
            depthDSV = new DepthStencilView(renderer.Device, depthTarget, new DepthStencilViewDescription()
            {
                Flags = DepthStencilViewFlags.None,
                Format = Format.D32_Float,
                Dimension = DepthStencilViewDimension.Texture2D,
                Texture2D = { MipSlice = 0 }
            });

            if (colorTarget != null)
                colorTarget.Dispose();
            colorTarget = new Texture2D(renderer.Device, new Texture2DDescription()
            {
                Format = Format.R8G8B8A8_UNorm,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Width = renderer.ResolutionWidth,
                Height = renderer.ResolutionHeight,
                MipLevels = 1,
                ArraySize = 1,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default,
                SampleDescription = new SampleDescription(1, 0)
            });

            if (colorSRV != null)
                colorSRV.Dispose();
            colorSRV = new ShaderResourceView(renderer.Device, colorTarget);

            if (colorRTV != null)
                colorRTV.Dispose();
            colorRTV = new RenderTargetView(renderer.Device, colorTarget);

            if (normalTarget != null)
                normalTarget.Dispose();
            normalTarget = new Texture2D(renderer.Device, new Texture2DDescription()
            {
                Format = Format.R16G16_Float,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Width = renderer.ResolutionWidth,
                Height = renderer.ResolutionHeight,
                MipLevels = 1,
                ArraySize = 1,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default,
                SampleDescription = new SampleDescription(1, 0)
            });

            if (normalSRV != null)
                normalSRV.Dispose();
            normalSRV = new ShaderResourceView(renderer.Device, normalTarget);

            if (normalRTV != null)
                normalRTV.Dispose();
            normalRTV = new RenderTargetView(renderer.Device, normalTarget);

            if (lightTarget != null)
                lightTarget.Dispose();
            lightTarget = new Texture2D(renderer.Device, new Texture2DDescription()
            {
                Format = Format.R8G8B8A8_UNorm,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Width = renderer.ResolutionWidth,
                Height = renderer.ResolutionHeight,
                MipLevels = 1,
                ArraySize = 1,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default,
                SampleDescription = new SampleDescription(1, 0)
            });

            if (lightSRV != null)
                lightSRV.Dispose();
            lightSRV = new ShaderResourceView(renderer.Device, lightTarget);

            if (lightRTV != null)
                lightRTV.Dispose();
            lightRTV = new RenderTargetView(renderer.Device, lightTarget);
        }

        private HashSet<string> GetOurAssets()
        {
            return new HashSet<string>()
            {
                Renderer.DefaultAssets.SH_COMPILE,
                Renderer.DefaultAssets.SH_LIGHT_POINT,
                Renderer.DefaultAssets.SH_LIGHT_DIRECTIONAL,
                Renderer.DefaultAssets.VB_QUAD_POS_TEX_UNIT,
                Renderer.DefaultAssets.BUF_WORLD,
                Renderer.DefaultAssets.BUF_CAM_VIEWPROJ,
                Renderer.DefaultAssets.BUF_CAM_INVVIEWPROJ,
#if DEBUG
                Renderer.DefaultAssets.SH_DEBUG_COLOR,
                Renderer.DefaultAssets.SH_DEBUG_DEPTH,
                Renderer.DefaultAssets.SH_DEBUG_NORMAL,
                Renderer.DefaultAssets.SH_DEBUG_LIGHT,
#endif
            };
        }

        private void loadAssets(IScene scene)
        {
            //assets we want during the scene
            //note - any changes here should be reflected equally in the loadthread and endload method
            var assets = GetOurAssets();

            //assets we want during load
            var loadAssets = new HashSet<string>(assets);

            if (scene == null)
            {//if this is the first load without a scene, just do this single-threaded
                renderer.Assets.PreLoad(loadAssets);
                renderer.Assets.StartLoad(assets, loadAssets);
                renderer.Assets.EndLoad(assets, loadAssets);
                return;
            }

            //construct asset list
            loadAssets.UnionWith(scene.GetPreloadAssetList());
            assets.UnionWith(scene.GetAssetList());

            //preload scene
            activeManager = loadManager;
            input.events = loadManager;
            renderer.Assets.PreLoad(loadAssets);
            cam3D = scene.Get3DCamera();
            cam2D = scene.Get2DCamera();
            scene.Preload(loadManager);

            if (!renderer.Assets.LoadHasWorkToDo(assets, loadAssets))
            {//if we don't actually need to do any asset management, just singlethread it
                //may want to create an override for this to force running the load thread
                //for scenes with heavy work in their Load method
                StartLoad(assets, loadAssets);
                FinishLoad(assets, loadAssets);
            }
            else
            {
                //now start the actual loading thread
                loadingTime = 0;
                loadingThread = new Thread(new ThreadStart(LoadThread));
                loadingThread.Start();
            }
        }

        private void LoadThread()
        {
            HashSet<string> assets = GetOurAssets();
            HashSet<string> loadAssets = new HashSet<string>(assets);
            loadAssets.UnionWith(currentScene.GetPreloadAssetList());
            assets.UnionWith(currentScene.GetAssetList());

            StartLoad(assets, loadAssets);
        }

        private void StartLoad(HashSet<string> assets, HashSet<string> loadAssets)
        {
            renderer.Assets.StartLoad(assets, loadAssets);
            currentScene.Load(sceneManager);
        }

        private void FinishLoad(HashSet<string> assets = null, HashSet<string> loadAssets = null)
        {
            if (assets == null)
            {
                assets = GetOurAssets();
                loadAssets = new HashSet<string>(assets);
                loadAssets.UnionWith(currentScene.GetPreloadAssetList());
                assets.UnionWith(currentScene.GetAssetList());
            }

            //cleanup all of the load stuff now
            renderer.Assets.EndLoad(assets, loadAssets);
            currentScene.LoadEnd();
            activeManager = sceneManager;
            input.events = sceneManager;
            loadManager.Clear();
        }

        public void switchToScene(IScene scene)
        {
            sceneManager.Clear();
            if (currentScene != null)
                currentScene.Dispose();

            currentScene = scene;
            loadAssets(scene);

        }

        #region Update
        double timeLeftover = 0;
        Stopwatch frameTimer = new Stopwatch();
        bool StageExit = false;
        double frameDt;
        public double FrameTimer
        {
            get
            {
                return frameDt;
            }
        }
        public bool Update()
        {
            if (StageExit)
            {
                return false;
            }

            frameDt = frameTimer.Elapsed.TotalSeconds;
            frameTimer.Restart();

            input.Update();

            double inputTime = frameTimer.Elapsed.TotalMilliseconds;
            if (inputTime > 1)
                Logger.WriteLine(LogType.VERBOSE2, "Long input update detected: " + inputTime);

#if DEBUG
            timeLeftover += frameDt * DEBUG_TIDI;
#else
            timeLeftover += dt;
#endif

            if (UseFixedTimestep)
            {
                int updatesThisFrame = 0;
                while (timeLeftover > Timestep)
                {
                    var startFrame = frameTimer.Elapsed;

                    InternalUpdate(Timestep);

                    updatesThisFrame++;
                    timeLeftover -= Timestep;

                    double updateTime = (startFrame - frameTimer.Elapsed).TotalMilliseconds;
                    if (updateTime > 16)
                        Logger.WriteLine(LogType.VERBOSE3, "Long update step detected: " + updateTime);
                }

                if (updatesThisFrame > 1)
                {
                    Logger.WriteLine(LogType.VERBOSE2, "Frame miss: " + updatesThisFrame + " " + frameDt);
                }
            }
            else
            {
                InternalUpdate((float)timeLeftover);
                timeLeftover = 0;
            }

            return true;
        }

        private void InternalUpdate(float dt)
        {
            if (activeManager == loadManager)
            {
                currentScene.LoadUpdate(dt);
                loadingTime += dt;
                if (!loadingThread.IsAlive && loadingTime > currentScene.LoadTime())
                {
                    FinishLoad();
                }
            }
            else
                currentScene.Update(dt);

            //UpdateDelegates in the eventmanager are hardcoded to go after the currentScene update
            //we could probably make this nicer by just making the currentScene update auto-added here with priority 0
            foreach (UpdateDelegate d in activeManager.updateList)
            {
                d(dt);
            }
        }
        #endregion

        #region Drawing

        public void Draw(RenderTargetView renderTarget = null, bool present = true)
        {
            if (renderTarget == null)
                renderTarget = renderView;

            //MRT Setup
            renderer.Context.ClearRenderTargetView(colorRTV, new SharpDX.Mathematics.Interop.RawColor4(0.04f, 0.04f, 0.1725f, 0f));
            renderer.Context.ClearRenderTargetView(normalRTV, new SharpDX.Mathematics.Interop.RawColor4());
            renderer.Context.ClearRenderTargetView(renderTarget, new SharpDX.Mathematics.Interop.RawColor4());
            renderer.Context.ClearDepthStencilView(depthDSV, DepthStencilClearFlags.Depth, 0.0f, 0);

            renderer.Context.Rasterizer.SetViewport(0, 0, renderer.ResolutionWidth, renderer.ResolutionHeight);
            renderer.Context.OutputMerger.SetDepthStencilState(stencilDefault);
            renderer.Context.OutputMerger.SetTargets(depthDSV, colorRTV, normalRTV);
            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;


            if (currentScene.Draw3D())
            {
                //Set 3D Camera
                viewProjBuffer.dat[0].viewMatrix = cam3D.getViewMatrix();
                viewProjBuffer.dat[0].projMatrix = cam3D.getProjMatrix();
                viewProjBuffer.Write(renderer.Context);
                renderer.Context.VertexShader.SetConstantBuffer(0, viewProjBuffer.buf);

                viewProjInvBuffer.dat[0] = cam3D.getInvViewProjMatrix();
                viewProjInvBuffer.Write(renderer.Context);

                //MRT Draw
                drawMRT();

                //MRT End
                renderer.Context.OutputMerger.ResetTargets();
                renderer.Context.VertexShader.SetConstantBuffer(0, fullscreenCameraBuffer);
                renderer.Context.PixelShader.SetShaderResources(0, colorSRV, depthSRV, normalSRV);

                //Light Setup
                renderer.Context.OutputMerger.SetTargets(lightRTV);
                renderer.Context.ClearRenderTargetView(lightRTV, new SharpDX.Mathematics.Interop.RawColor4());
                renderer.Context.VertexShader.SetConstantBuffer(0, fullscreenCameraBuffer);
                renderer.Context.PixelShader.SetConstantBuffer(0, viewProjBuffer.buf);
                renderer.Context.PixelShader.SetConstantBuffer(2, viewProjInvBuffer.buf);
                renderer.Context.PixelShader.SetSampler(0, renderer.samplerPoint);

                renderer.Context.OutputMerger.SetBlendState(renderer.blendLight, new SharpDX.Mathematics.Interop.RawColor4(1f, 1f, 1f, 1f), -1);
                renderer.Context.InputAssembler.SetVertexBuffers(0, quad_postex_unit.vbBinding);

                Matrix3x3.CreateScale(new Vector3(renderer.ResolutionWidth, renderer.ResolutionHeight, 1), out Matrix3x3 scaleMat);
                Matrix.CreateRigid(scaleMat, Vector3.Zero, out worldBuffer.dat[0]);
                worldBuffer.Write(renderer.Context);
                renderer.Context.VertexShader.SetConstantBuffer(1, worldBuffer.buf);

                //Light Draw
                drawLights();

                //Compile MRT
                renderer.Context.OutputMerger.SetBlendState(null, null, -1);
                renderer.Context.OutputMerger.ResetTargets();
                renderer.Context.OutputMerger.SetTargets(renderTarget);
                renderer.Context.PixelShader.SetShaderResource(3, lightSRV);
                s_Compile.Bind(renderer.Context);
                renderer.Context.Draw(6, 0);

                //Post Process Setup
                renderer.Context.VertexShader.SetConstantBuffer(0, viewProjBuffer.buf);
                renderer.Context.PixelShader.SetShaderResources(0, 4, null, null, null, null);
                renderer.Context.OutputMerger.SetTargets(renderTarget);
                renderer.Context.OutputMerger.SetBlendState(renderer.blendTransparent, new SharpDX.Mathematics.Interop.RawColor4(1f, 1f, 1f, 1f), -1);

                //Draw Post Process Here
                drawPostProcess3D();
            }

            //2D Setup
            renderer.Context.OutputMerger.SetBlendState(renderer.blendTransparent, new SharpDX.Mathematics.Interop.RawColor4(1f, 1f, 1f, 1f), -1);
            renderer.Context.OutputMerger.SetTargets(renderTarget);

            //2D Camera
            viewProjBuffer.dat[0].viewMatrix = cam2D.getViewMatrix();
            viewProjBuffer.dat[0].projMatrix = cam2D.getProjMatrix();
            viewProjBuffer.Write(renderer.Context);
            renderer.Context.VertexShader.SetConstantBuffer(0, viewProjBuffer.buf);

            //Draw 2D here
            draw2D();
#if DEBUG
            if (DrawDebug && currentScene.Draw3D())
                drawMRTOutput();
#endif

            //End 2D
            renderer.Context.OutputMerger.SetBlendState(null, null, -1);

            if (present)
                renderer.SwapChain.Present(renderer.VSync ? 1 : 0, PresentFlags.None, presentParams);
        }

        PresentParameters presentParams = new PresentParameters()
        {
            DirtyRectangles = null,
            ScrollOffset = null,
            ScrollRectangle = null
        };

        void drawMRT()
        {
            foreach (DrawDelegate d in activeManager.drawMRTList)
            {
                d();
            }
        }

        void drawLights()
        {
            s_PointLight.Bind(renderer.Context);
            foreach (PointLight l in activeManager.pointLightList)
            {
                l.Draw();
            }

            s_DirectionalLight.Bind(renderer.Context);
            foreach (DirectionalLight l in activeManager.directionalLightList)
            {
                l.Draw();
            }
        }

        void drawPostProcess3D()
        {//3D post process
            foreach (DrawDelegate d in activeManager.drawPostProcList)
            {
                d();
            }
        }

        void draw2D()
        {//2D post process
            foreach (DrawDelegate d in activeManager.draw2DList)
            {
                d();
            }
        }

#if DEBUG
        void drawMRTOutput()
        {
            renderer.Context.VertexShader.SetConstantBuffer(0, fullscreenCameraBuffer);

            renderer.Context.PixelShader.SetShaderResources(0, colorSRV, depthSRV, normalSRV, lightSRV);
            renderer.Context.InputAssembler.SetVertexBuffers(0, quad_postex_unit.vbBinding);
            renderer.Context.VertexShader.SetConstantBuffer(1, worldBuffer.buf);

            Matrix3x3.CreateScale(new Vector3(renderer.ResolutionWidth / 4f, renderer.ResolutionHeight / 4f, 1), out Matrix3x3 scaleMat);

            s_ColorDebug.Bind(renderer.Context);
            Matrix.CreateRigid(scaleMat, new Vector3(0, renderer.ResolutionHeight * 3f / 4f, 0), out worldBuffer.dat[0]);
            worldBuffer.Write(renderer.Context);
            renderer.Context.Draw(6, 0);

            s_DepthDebug.Bind(renderer.Context);
            Matrix.CreateRigid(scaleMat, new Vector3(renderer.ResolutionWidth / 4f, renderer.ResolutionHeight * 3f / 4f, 0), out worldBuffer.dat[0]);
            worldBuffer.Write(renderer.Context);
            renderer.Context.Draw(6, 0);

            s_NormalDebug.Bind(renderer.Context);
            Matrix.CreateRigid(scaleMat, new Vector3(renderer.ResolutionWidth / 2f, renderer.ResolutionHeight * 3f / 4f, 0), out worldBuffer.dat[0]);
            worldBuffer.Write(renderer.Context);
            renderer.Context.Draw(6, 0);

            s_LightDebug.Bind(renderer.Context);
            Matrix.CreateRigid(scaleMat, new Vector3(renderer.ResolutionWidth * 3f / 4f, renderer.ResolutionHeight * 3f / 4f, 0), out worldBuffer.dat[0]);
            worldBuffer.Write(renderer.Context);
            renderer.Context.Draw(6, 0);

            renderer.Context.PixelShader.SetShaderResources(0, 4, null, null, null, null);
        }
#endif
        #endregion

        public void updateSubresource<T>(Resource b, T value) where T : struct
        {
            renderer.Context.UpdateSubresource(ref value, b);
        }

        public void mapSubresource<T>(Resource b, T value) where T : struct
        {
            var dataBox = renderer.Context.MapSubresource(b, 0, MapMode.WriteDiscard, MapFlags.None);
            Utilities.Write(dataBox.DataPointer, ref value);
            renderer.Context.UnmapSubresource(b, 0);
        }

        public void Exit()
        {
            SDL2.SDL.SDL_Quit();
            Dispose();
        }

        public void Dispose()
        {//called by Program.CloseForm() through FormClosing event.
            //nicely kill the current scene...
            StageExit = true;
            currentScene.Dispose();

            //TODO: should probably kill all of our stuff too....
        }
    }
}
