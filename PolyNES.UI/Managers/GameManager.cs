using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Poly6502.Microprocessor;
using PolyNES.Managers;
using PolyNES.Memory;
using PolyNES.PPU;
using PolyNES.UI.Enums;
using PolyNES.UI.Views;

namespace PolyNES.UI.Managers
{
    public class GameManager
    {
        private GameState _currentState;
        private KeyboardManager _keyboardManager;
        private MainMenuView _mainMenuView;
        private DebugOutputView _debugOutputView;
        private GameView _gameView;
        
        private Cartridge.Cartridge _cartridge;
        private M6502 _cpu;
        private Poly2C02 _ppu;
        private WorkRam _workRam;
        private SystemManager _systemManager;
        private Random _random;

        private float _fps;
        
        public GameManager(GraphicsDevice gd, SpriteFont font)
        {
            _cartridge = new Cartridge.Cartridge();
            _cpu = new M6502();
            _ppu = new Poly2C02(_cartridge);
            _workRam = new WorkRam();
            _systemManager = new SystemManager(_cpu, _cartridge, _ppu, _workRam);
            _systemManager.LoadRom(Environment.GetCommandLineArgs()[1]);
            
            _keyboardManager = new KeyboardManager();
            _mainMenuView = new MainMenuView(this, font, _keyboardManager, gd);
            _debugOutputView = new DebugOutputView(font, gd, _keyboardManager, _systemManager);
            _gameView = new GameView(this, gd, _keyboardManager, _cpu);
            _fps = 0;
            _currentState = GameState.Debug;
            _systemManager.Reset();
        }

        public void Update(GameTime gameTime)
        {
            _fps = (float) (1 / gameTime.ElapsedGameTime.TotalSeconds);
            
            _keyboardManager.Update();

            switch (_currentState)
            {
                case GameState.Menu:
                    break;
                case GameState.Running:
                case GameState.Debug:
                    //_gameView.Update();
                    //_debugOutputView.Update();
                    break;
                case GameState.Pause:
                    break;
                
                case GameState.LoadRom:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ChangeState(GameState state, string rom)
        {
            var previous = _currentState;
            _currentState = state;

            if (_currentState == GameState.LoadRom)
            {
                _systemManager.Reset();
                _currentState = GameState.Running;
            }

            if (previous != _currentState && _currentState == GameState.Debug)
            {
                _systemManager.Reset();
            }
        }

        public void Draw(SpriteBatch sb)
        {
            switch (_currentState)
            {
                case GameState.Menu:
                    _mainMenuView.Draw(sb);
                    break;
                case GameState.Running:
                    _gameView.Draw(sb, true);
                    break;
                case GameState.Pause:
                    break;
                case GameState.Debug:
                    //_gameView.Draw(sb, false);
                    _debugOutputView.DrawAll(sb, _fps);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}