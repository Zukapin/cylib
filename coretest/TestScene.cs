using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using cyUtility;
using cylib;

using BepuUtilities;
using Matrix = BepuUtilities.Matrix;
using Matrix3x3 = BepuUtilities.Matrix3x3;

namespace coretest
{
    static class Assets
    {
        public const string TEX_DUCK = "TEX_angelduck";
        public const string TEX_WHITEPIXEL = "TEX_WhitePixel";
        public const string TEX_TEST = "TEX_test";

        public const string SH_TEST = "SH_test";

        public const string FONT_CALLI_BMP_16 = "FONT_CALLI_BMP_16";
        public const string FONT_CALLI_BMP_32 = "FONT_CALLI_BMP_32";
        public const string FONT_CALLI_BMP_64 = "FONT_CALLI_BMP_64";
        public const string FONT_CALLI_BMP_128 = "FONT_CALLI_BMP_128";

        public const string FONT_CALLI_SDF_16 = "FONT_CALLI_SDF_16";
        public const string FONT_CALLI_SDF_32 = "FONT_CALLI_SDF_32";
        public const string FONT_CALLI_SDF_64 = "FONT_CALLI_SDF_64";
        public const string FONT_CALLI_SDF_128 = "FONT_CALLI_SDF_128";

        public const string FONT_SEGOEUI_BMP_16 = "FONT_SEGOEUI_BMP_16";
        public const string FONT_SEGOEUI_BMP_32 = "FONT_SEGOEUI_BMP_32";
        public const string FONT_SEGOEUI_BMP_64 = "FONT_SEGOEUI_BMP_64";
        public const string FONT_SEGOEUI_BMP_128 = "FONT_SEGOEUI_BMP_128";

        public const string FONT_SEGOEUI_SDF_16 = "FONT_SEGOEUI_SDF_16";
        public const string FONT_SEGOEUI_SDF_32 = "FONT_SEGOEUI_SDF_32";
        public const string FONT_SEGOEUI_SDF_64 = "FONT_SEGOEUI_SDF_64";
        public const string FONT_SEGOEUI_SDF_128 = "FONT_SEGOEUI_SDF_128";
    }

    static class ActionTypes
    {
        public const string ESCAPE = "ESCAPE";
        public const string FORWARD = "FORWARD";
        public const string BACKWARD = "BACKWARD";
        public const string LEFT = "LEFT";
        public const string RIGHT = "RIGHT";
        public const string ENTER_FPV = "ENTER_FPV";
        public const string LEAVE_FPV = "LEAVE_FPV";
        public const string FIRE = "FIRE";
    }

    class TestPlayer
    {
        GameStage stage;
        Renderer renderer;
        EventManager em;

        FPVCamera cam;
        TexturedQuad_2D playerSprite;

        bool isForwardDown = false;
        bool isBackDown = false;
        bool isLeftDown = false;
        bool isRightDown = false;

        public TestPlayer(GameStage stage, Renderer renderer, EventManager em, FPVCamera cam)
        {
            this.stage = stage;
            this.renderer = renderer;
            this.em = em;
            this.cam = cam;

            playerSprite = new TexturedQuad_2D(renderer, em, -1024, renderer.Assets.GetTexture(Assets.TEX_DUCK));
            playerSprite.position = new Vector2(400, 400);
            playerSprite.scale = new Vector2(100, 100);

            em.addUpdateListener(0, onUpdate);
            em.addEventHandler((int)InterfacePriority.MEDIUM, ActionTypes.ESCAPE, OnExit);
            em.addEventHandler((int)InterfacePriority.MEDIUM, ActionTypes.FORWARD, OnMoveForward);
            em.addEventHandler((int)InterfacePriority.MEDIUM, ActionTypes.BACKWARD, OnMoveBackward);
            em.addEventHandler((int)InterfacePriority.MEDIUM, ActionTypes.LEFT, OnMoveLeft);
            em.addEventHandler((int)InterfacePriority.MEDIUM, ActionTypes.RIGHT, OnMoveRight);
            em.addEventHandler((int)InterfacePriority.MEDIUM, ActionTypes.ENTER_FPV, OnEnterFPV);
            em.addEventHandler((int)InterfacePriority.MEDIUM, ActionTypes.LEAVE_FPV, OnLeaveFPV);
            em.addEventHandler((int)InterfacePriority.LOW, ActionTypes.FIRE, OnFire);
            em.addEventHandler((int)InterfacePriority.HIGHEST, onPointerEvent);
        }

        bool onPointerEvent(PointerEventArgs args)
        {
            if (args.type == PointerEventType.AIM)
            {
                cam.yaw -= args.aimDeltaX;
                cam.pitch -= args.aimDeltaY;
                return true;
            }
            return false;
        }

        bool OnMoveForward(ActionEventArgs args)
        {
            isForwardDown = args.buttonDown;
            return true;
        }
        bool OnMoveBackward(ActionEventArgs args)
        {
            isBackDown = args.buttonDown;
            return true;
        }
        bool OnMoveLeft(ActionEventArgs args)
        {
            isLeftDown = args.buttonDown;
            return true;
        }
        bool OnMoveRight(ActionEventArgs args)
        {
            isRightDown = args.buttonDown;
            return true;
        }

        bool OnEnterFPV(ActionEventArgs args)
        {
            if (args.buttonDown)
                stage.EnterFPVMode();
            return true;
        }

        bool OnLeaveFPV(ActionEventArgs args)
        {
            if (args.buttonDown)
                stage.LeaveFPVMode();
            return true;
        }

        bool OnFire(ActionEventArgs args)
        {
            if (args.buttonDown)
                playerSprite.scale = playerSprite.scale * 2;
            else
                playerSprite.scale = playerSprite.scale * 0.5f;
            return true;
        }

        bool OnExit(ActionEventArgs args)
        {
            stage.Exit();
            return true;
        }

        void onUpdate(float dt)
        {
            float speed = 100;
            Vector2 vel = new Vector2();
            if (isLeftDown)
                vel.X -= speed;
            if (isRightDown)
                vel.X += speed;
            if (isForwardDown)
                vel.Y -= speed;
            if (isBackDown)
                vel.Y += speed;

            playerSprite.position = playerSprite.position + vel * dt;
        }
    }

    class TestScene : IScene
    {
        FPVCamera cam3D;
        OrthoCamera cam2D;

        GameStage stage;
        Renderer renderer;

        TexturedCircle_2D spinDuck;

        FontRenderer[] fonts;
        Textbox textbox;

        PointLight[] lights;

        DropDownMenu dropMenu;
        CycleMenu cycleMenu;

        public TestScene(GameStage stage)
        {
            this.stage = stage;
            this.renderer = stage.renderer;

            cam3D = new FPVCamera(stage.renderer.ResolutionWidth / (float)stage.renderer.ResolutionHeight, Vector3.UnitY, 0, 0);
            cam2D = new OrthoCamera(Vector2.Zero, stage.renderer.ResolutionWidth, stage.renderer.ResolutionHeight);
        }

        public float LoadTime()
        {
            return 0f;
        }

        public HashSet<string> GetAssetList()
        {
            return new HashSet<string>()
            {
                Renderer.DefaultAssets.SH_POS_TEX,
                Renderer.DefaultAssets.SH_POS_NORM_TEX,
                Renderer.DefaultAssets.VB_QUAD_POS_TEX_UNIT,
                Renderer.DefaultAssets.VB_CIRCLE_POS_TEX_UNIT,
                Renderer.DefaultAssets.VB_CIRCLE_POS_TEX_NORM_UNIT,
                Assets.TEX_DUCK,
                Assets.TEX_WHITEPIXEL,
                Assets.FONT_CALLI_BMP_128,
                Assets.FONT_CALLI_BMP_64,
                Assets.FONT_CALLI_BMP_32,
                Assets.FONT_CALLI_BMP_16,
                Assets.FONT_CALLI_SDF_128,
                Assets.FONT_CALLI_SDF_64,
                Assets.FONT_CALLI_SDF_32,
                Assets.FONT_CALLI_SDF_16,
                Assets.FONT_SEGOEUI_BMP_128,
                Assets.FONT_SEGOEUI_BMP_64,
                Assets.FONT_SEGOEUI_BMP_32,
                Assets.FONT_SEGOEUI_BMP_16,
                Assets.FONT_SEGOEUI_SDF_128,
                Assets.FONT_SEGOEUI_SDF_64,
                Assets.FONT_SEGOEUI_SDF_32,
                Assets.FONT_SEGOEUI_SDF_16
            };
        }

        public HashSet<string> GetPreloadAssetList()
        {
            return new HashSet<string>()
            {
                Renderer.DefaultAssets.SH_POS_TEX,
                Assets.TEX_DUCK,
                Renderer.DefaultAssets.VB_QUAD_POS_TEX_UNIT,
                Renderer.DefaultAssets.BUF_ROUNDED_RECT,
                Assets.FONT_SEGOEUI_SDF_128,
                Renderer.DefaultAssets.SH_ROUNDED_RECTANGLE_2D,
                Renderer.DefaultAssets.SH_FONT_SDF,
                Renderer.DefaultAssets.BUF_FONT,
                Renderer.DefaultAssets.BUF_COLOR,
            };
        }

        public void Preload(EventManager em)
        {
            Texture t_duck = renderer.Assets.GetTexture(Assets.TEX_DUCK);
            var testQuad = new TexturedQuad_2D(renderer, em, 0, t_duck);
            testQuad.position = new Vector2(10, 10);
            testQuad.scale = new Vector2(1000, 1000);
        }

        public void LoadUpdate(float dt)
        {

        }

        public void LoadEnd()
        {
        }

        public bool Draw3D()
        {
            return true;
        }

        public void Load(EventManager em)
        {
            var player = new TestPlayer(stage, renderer, em, cam3D);

            Shader s_posTex = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_POS_TEX);
            Texture t_duck = renderer.Assets.GetTexture(Assets.TEX_DUCK);
            Texture t_pixel = renderer.Assets.GetTexture(Assets.TEX_WHITEPIXEL);
            VertexBuffer vb_quad = renderer.Assets.GetVertexBuffer(Renderer.DefaultAssets.VB_QUAD_POS_TEX_UNIT);

            //mip ducks
            var testQuad = new TexturedQuad_2D(renderer, em, 0, t_duck);
            testQuad.position = new Vector2(10, 10);
            testQuad.scale = new Vector2(testQuad.tex.width, testQuad.tex.height);

            testQuad = new TexturedQuad_2D(renderer, em, 0, t_duck);
            testQuad.position = new Vector2(276, 10);
            testQuad.scale = new Vector2(testQuad.tex.width / 2f, testQuad.tex.height / 2f);

            testQuad = new TexturedQuad_2D(renderer, em, 0, t_duck);
            testQuad.position = new Vector2(414, 10);
            testQuad.scale = new Vector2(testQuad.tex.width / 4f, testQuad.tex.height / 4f);

            testQuad = new TexturedQuad_2D(renderer, em, 0, t_duck);
            testQuad.position = new Vector2(488, 10);
            testQuad.scale = new Vector2(testQuad.tex.width / 8f, testQuad.tex.height / 8f);

            testQuad = new TexturedQuad_2D(renderer, em, 0, renderer.Assets.GetTexture(Assets.TEX_TEST));
            testQuad.shader = renderer.Assets.GetShader(Assets.SH_TEST);
            testQuad.position = new Vector2(10, 300);
            testQuad.scale = new Vector2(400, 50);

            //spin duck
            spinDuck = new TexturedCircle_2D(renderer, em, 0, t_duck);
            spinDuck.position = new Vector2(600.5f, 200.5f);
            spinDuck.scale = new Vector2(281, 247);
            spinDuck.rot = (float)(Math.PI / 3);

            //ground duck
            var a = new TexturedCircle_MRT(renderer, em, 0, t_duck);
            a.position = new Vector3(0, 0, 0);
            a.scale = new Vector2(20f, 20f);
            a.face = new Vector3(0, 1, 0);
            a.face = a.face / a.face.Length();

            //default duck
            a = new TexturedCircle_MRT(renderer, em, 0, t_duck);
            a.position = new Vector3(0, 1f, -3);

            //Lights
            MakeLights(em);

            //font tests
            loadFonts(em);

            //font scales
            testQuad = new TexturedQuad_2D(renderer, em, 0, t_pixel);
            testQuad.position = new Vector2(788, 10);
            testQuad.scale = new Vector2(10, 128);

            testQuad = new TexturedQuad_2D(renderer, em, 0, t_pixel);
            testQuad.position = new Vector2(788, 148);
            testQuad.scale = new Vector2(10, 64);

            testQuad = new TexturedQuad_2D(renderer, em, 0, t_pixel);
            testQuad.position = new Vector2(788, 222);
            testQuad.scale = new Vector2(10, 32);

            testQuad = new TexturedQuad_2D(renderer, em, 0, t_pixel);
            testQuad.position = new Vector2(788, 264);
            testQuad.scale = new Vector2(10, 16);

            testQuad = new TexturedQuad_2D(renderer, em, 0, t_pixel);
            testQuad.position = new Vector2(788, 290);
            testQuad.scale = new Vector2(10, 8);

            //sdf scales
            testQuad = new TexturedQuad_2D(renderer, em, 0, t_pixel);
            testQuad.position = new Vector2(788, 436 - 128);
            testQuad.scale = new Vector2(10, 128);

            testQuad = new TexturedQuad_2D(renderer, em, 0, t_pixel);
            testQuad.position = new Vector2(788, 510 - 64);
            testQuad.scale = new Vector2(10, 64);

            testQuad = new TexturedQuad_2D(renderer, em, 0, t_pixel);
            testQuad.position = new Vector2(788, 552 - 32);
            testQuad.scale = new Vector2(10, 32);

            testQuad = new TexturedQuad_2D(renderer, em, 0, t_pixel);
            testQuad.position = new Vector2(788, 578 - 16);
            testQuad.scale = new Vector2(10, 16);

            testQuad = new TexturedQuad_2D(renderer, em, 0, t_pixel);
            testQuad.position = new Vector2(788, 596 - 8);
            testQuad.scale = new Vector2(10, 8);

            //ui crap
            textbox = new Textbox(renderer, em, 0);
            textbox.Position = new Vector2(10, 400);
            textbox.Scale = new Vector2(500, 40);

            textbox.onEnterPressed += onTestEnter;

            var b = new BoxTextButton(renderer, em, 0, "Apply");
            b.Position = new Vector2(520, 400);
            b.Scale = new Vector2(120, 40);

            b.onClick += onTestApply;

            b = new BoxTextButton(renderer, em, 0, "Color Cycle");
            b.Position = new Vector2(520, 460);
            b.Scale = new Vector2(200, 40);
            onTestClick(null);

            b.onClick += onTestClick;

            string[] fontNames = new string[]
            {
                "CALLI 128",
                "CALLI 64",
                "CALLI 32",
                "CALLI 16",
                "SEGOE UI 128",
                "SEGOE UI 64",
                "SEGOE UI 32",
                "SEGOE UI 16"
            };

            dropMenu = new DropDownMenu(renderer, em, 0, fontNames, 2);
            dropMenu.Position = new Vector2(520, 340);
            dropMenu.Scale = new Vector2(240, 40);

            dropMenu.onSelectionChange += onDropSel;

            var slider = new Slider(renderer, em, 0, 10, 128, 2000);
            slider.Position = new Vector2(10, 460);
            slider.Scale = new Vector2(400, 40);

            slider.onValueChanged += onSlider;

            scroll = new ScrollBar(renderer, em, 0);
            scroll.Position = new Vector2(1890, 10);
            scroll.Scale = new Vector2(20, 600);
            scroll.scrollPos = new Vector2(800, 0);
            scroll.scrollScale = new Vector2(1090, 620);

            mainFontWindow = totFontHeight();
            scroll.percentOfScreen = 1f;

            scroll.onValueChanged += onScroll;

            cycleMenu = new CycleMenu(renderer, em, 0, fontNames, 2);
            cycleMenu.Position = new Vector2(10, 510);
            cycleMenu.Scale = new Vector2(400, 40);
            cycleMenu.onSelectionChange += onDropSel;
        }

        ScrollBar scroll;

        private void onScroll(float val)
        {
            float height = totFontHeight();
            for (int x = 0; x < fonts.Length; x++)
            {
                fonts[x].offset.Y = -val * (height - mainFontWindow);
            }
        }

        float mainFontWindow;
        private float totFontHeight()
        {
            float toReturn = 0;
            for (int x = 0; x < fonts.Length; x++)
            {
                toReturn += fonts[x].scale + 10;
            }

            return toReturn;
        }

        private void MakeLights(EventManager em)
        {
            Color[] colors = new Color[] { Color.Red, Color.Green, Color.Blue, Color.Plum, Color.Yellow, Color.Orange, Color.OldLace };
            Vector3 pos = new Vector3(0, 4, 5);
            Matrix3x3 rot = Matrix3x3.CreateFromAxisAngle(Vector3.UnitY, (float)(Math.PI * 2 / colors.Length));

            lights = new PointLight[colors.Length];
            for (int x = 0; x < lights.Length; x++)
            {
                lights[x] = new PointLight(renderer, em, pos, colors[x], 7f, 1f);
                Matrix3x3.Transform(pos, rot, out pos);
            }
        }

        private void onSlider(float val)
        {
            float scale = val;
            Vector2 pos = new Vector2(800, 0);

            for (int x = 0; x < 10; x++)
            {
                if (x == 5)
                {
                    scale = val;
                }

                pos.Y += scale + 10;
                fonts[x].pos = pos;
                fonts[x].scale = scale;

                scale /= 2;
            }

            scroll.percentOfScreen = mainFontWindow / pos.Y;
        }

        int curFontSelection = 2;
        private void onDropSel(int sel)
        {
            if (sel == curFontSelection)
                return;

            Font bmp_font;
            Font sdf_font;

            switch (sel)
            {
                case 0: //calli 128
                    bmp_font = renderer.Assets.GetFont(Assets.FONT_CALLI_BMP_128);
                    sdf_font = renderer.Assets.GetFont(Assets.FONT_CALLI_SDF_128);
                    break;
                case 1: //calli 64
                    bmp_font = renderer.Assets.GetFont(Assets.FONT_CALLI_BMP_64);
                    sdf_font = renderer.Assets.GetFont(Assets.FONT_CALLI_SDF_64);
                    break;
                case 2: //calli 32
                    bmp_font = renderer.Assets.GetFont(Assets.FONT_CALLI_BMP_32);
                    sdf_font = renderer.Assets.GetFont(Assets.FONT_CALLI_SDF_32);
                    break;
                case 3: //calli 16
                    bmp_font = renderer.Assets.GetFont(Assets.FONT_CALLI_BMP_16);
                    sdf_font = renderer.Assets.GetFont(Assets.FONT_CALLI_SDF_16);
                    break;
                case 4: //segoe 128
                    bmp_font = renderer.Assets.GetFont(Assets.FONT_SEGOEUI_BMP_128);
                    sdf_font = renderer.Assets.GetFont(Assets.FONT_SEGOEUI_SDF_128);
                    break;
                case 5: //segoe 64
                    bmp_font = renderer.Assets.GetFont(Assets.FONT_SEGOEUI_BMP_64);
                    sdf_font = renderer.Assets.GetFont(Assets.FONT_SEGOEUI_SDF_64);
                    break;
                case 6: //segoe 32
                    bmp_font = renderer.Assets.GetFont(Assets.FONT_SEGOEUI_BMP_32);
                    sdf_font = renderer.Assets.GetFont(Assets.FONT_SEGOEUI_SDF_32);
                    break;
                case 7: //segoe 16
                    bmp_font = renderer.Assets.GetFont(Assets.FONT_SEGOEUI_BMP_16);
                    sdf_font = renderer.Assets.GetFont(Assets.FONT_SEGOEUI_SDF_16);
                    break;
                default:
                    Logger.WriteLine(LogType.POSSIBLE_ERROR, "Something broke in your dumb selection menu: " + sel);
                    return;
            }

            for (int x = 0; x < fonts.Length; x++)
            {
                if (x < 5)
                    fonts[x].font = bmp_font;
                else
                    fonts[x].font = sdf_font;
            }

            curFontSelection = sel;
            cycleMenu.Selection = sel;
            dropMenu.Selection = sel;
        }

        private void onTestApply(Button obj)
        {
            onTestEnter();
        }

        private void onTestEnter()
        {
            for (int x = 0; x < fonts.Length; x++)
            {
                fonts[x].text = textbox.text;
            }
        }

        Color[] fontColors = new Color[] { Color.White, Color.Red, Color.SkyBlue, Color.Green };
        int fontColorIndex = 0;
        private void onTestClick(Button obj)
        {
            for (int x = 0; x < fonts.Length; x++)
            {
                fonts[x].color = fontColors[fontColorIndex];
            }

            fontColorIndex = (fontColorIndex + 1) % fontColors.Length;
        }

        private void loadFonts(EventManager em)
        {
            Font bmp_font = renderer.Assets.GetFont(Assets.FONT_CALLI_BMP_32);
            Font sdf_font = renderer.Assets.GetFont(Assets.FONT_CALLI_SDF_32);
            fonts = new FontRenderer[10];

            Vector2 pos = new Vector2(800, 0);
            Font curFont = bmp_font;
            float scale = 128f;

            for (int x = 0; x < 10; x++)
            {
                if (x == 5)
                {
                    curFont = sdf_font;
                    scale = 128;
                }

                pos.Y += scale + 10;

                fonts[x] = new FontRenderer(renderer, em, 0, curFont);
                fonts[x].text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed mauris libero, placerat ut vehicula vel, vulputate eu nisi. Sed vestibulum ut velit vel pellentesque.";
                fonts[x].anchor = FontAnchor.BOTTOM_LEFT;
                fonts[x].pos = pos;
                fonts[x].scale = scale;

                scale /= 2;
            }

            float totH = totFontHeight();
            for (int x = 0; x < fonts.Length; x++)
            {
                fonts[x].boundsPos = new Vector2(800, 0);
                fonts[x].boundsScale = new Vector2(1050, totH);
            }
        }

        public void Update(float dt)
        {
            spinDuck.rot += dt;

            Matrix3x3 rot = Matrix3x3.CreateFromAxisAngle(Vector3.UnitY, dt);
            for (int x = 0; x < lights.Length; x++)
            {
                Matrix3x3.Transform(lights[x].pos, rot, out Vector3 pos);
                lights[x].pos = pos;
            }
        }

        public ICamera Get3DCamera()
        {
            return cam3D;
        }

        public ICamera Get2DCamera()
        {
            return cam2D;
        }

        public void Dispose()
        {
            //TODO, probably never.
        }
    }
}
