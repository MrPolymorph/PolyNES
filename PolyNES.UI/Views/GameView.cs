using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Poly6502.Microprocessor;
using PolyNES.Managers;
using PolyNES.Memory;
using PolyNES.PPU;
using PolyNES.UI.Enums;
using PolyNES.UI.Managers;

namespace PolyNES.UI.Views
{
    public class GameView
    {
        private GraphicsDevice _graphics;
        private GameManager _stateManager;
        private KeyboardManager _keyboardManager;
        private Texture2D _screenTexture;
        
        private Cartridge.Cartridge _cartridge;
        private M6502 _cpu;
        private Poly2C02 _ppu;
        private WorkRam _workRam;
        private SystemManager _systemManager;
        private Random _random;

        private bool _emulate;

        public GameView(GameManager stateManager, GraphicsDevice gd, KeyboardManager keyboardManager, M6502 cpu)
        {
            _graphics = gd;
            _keyboardManager = keyboardManager;
            _stateManager = stateManager;
            // Content.RootDirectory = "Content";
            // IsMouseVisible = true;
            
            _cartridge = new Cartridge.Cartridge();
            _cpu = new M6502();
            _ppu = new Poly2C02(_cartridge);
            _workRam = new WorkRam();
            _systemManager = new SystemManager(_cpu, _cartridge, _ppu, _workRam);

            _cpu = cpu;
            _emulate = true;
            _stateManager = stateManager;

            _screenTexture = new Texture2D(gd, 256, 240, false, SurfaceFormat.Color);
            _keyboardManager = keyboardManager;
            
            _keyboardManager.SubscribeForKeyDown(Keys.Space, ToggleEmulation);
            _keyboardManager.SubscribeForKeyDown(Keys.Back, () => { _stateManager.ChangeState(GameState.Menu, string.Empty);});
            _keyboardManager.SubscribeForKeyDown(Keys.F, _cpu.Clock);
            _keyboardManager.SubscribeForKeyDown(Keys.F1, () => {_stateManager.ChangeState(GameState.Debug, string.Empty);});
            _keyboardManager.SubscribeForKeyDown(Keys.F, _cpu.Fetch);
            _keyboardManager.SubscribeForKeyDown(Keys.E, () =>
            {
                _cpu.Clock();
            });
        }

        public void Update()
        {
            DealWithKeyboardInput();
            
            _emulate = true;
            
            if (_emulate)
            {
                _systemManager.ClockSystem();
            }
        }

        public void Draw(SpriteBatch sb, bool fullscreen)
        {
            _screenTexture.SetData(_ppu.Screen, 0, 256 * 240 );
            
            var screenHeight = _graphics.Viewport.Height;
            var screenWidth = _graphics.Viewport.Width;
            
            if (fullscreen)
            {

                var scale = screenWidth / 256;
                sb.Draw(_screenTexture, new Vector2(0, 0), new Rectangle(0, 0, screenWidth, screenHeight),
                    Color.White, 0.0f, new Vector2(0, 0), scale, SpriteEffects.None, 0f);
            }
            else
            {
                sb.Draw(_screenTexture, new Vector2(120, 20), new Rectangle(0, 0, 256, 240),
                    Color.White, 0.0f, new Vector2(0, 0), 10f, SpriteEffects.None, 0f);
            }
        }
        
        private void ToggleEmulation()
        {
            _emulate = !_emulate;
            _systemManager.Run = _emulate;
        }
        
        private void DealWithKeyboardInput()
        {
            var currentKeyboardState = Keyboard.GetState();
        }
        
    }
}