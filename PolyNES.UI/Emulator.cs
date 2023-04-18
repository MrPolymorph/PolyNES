using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Poly6502.Microprocessor;
using PolyNES.Cartridge.Interfaces;
using PolyNES.Managers;
using PolyNES.Memory;
using PolyNES.PPU;

namespace PolyNES.UI;

public class Emulator : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _screenTexture;
    
    bool _fullscreen;
    private ICartridge _cartridge;
    private M6502 _cpu;
    private Poly2C02 _ppu;
    private WorkRam _workRam;
    private SystemManager _systemManager;
    private Random _random;
    int _cpuCycleDuration;
    string _romFile;

    public Emulator()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        _romFile = Environment.GetCommandLineArgs()[1];
        _cartridge = new Cartridge.Cartridge();
        _cpu = new M6502();
        _ppu = new Poly2C02(_cartridge);
        _workRam = new WorkRam();
        _systemManager = new SystemManager(_cpu, _cartridge, _ppu, _workRam);
        _systemManager.LoadRom(_romFile);
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _screenTexture = new Texture2D(GraphicsDevice, 256, 240, false, SurfaceFormat.Color);

        // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here
        _systemManager.ClockSystem();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        
        var screenHeight = GraphicsDevice.Viewport.Height;
        var screenWidth = GraphicsDevice.Viewport.Width;

        _screenTexture.SetData(_ppu.Screen, 0, _ppu.Screen.Length);
        
        _spriteBatch.Begin();
            _spriteBatch.Draw(_screenTexture, new Vector2(0, 0), new Rectangle(0, 0, 256, 240),
                Color.White, 0.0f, new Vector2(0, 0), 10f, SpriteEffects.None, 0f);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}