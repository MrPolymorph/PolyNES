using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PolyNES.UI.Managers;

namespace PolyNES.UI;

public class Emulator : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteFont _systemFont;
    private SpriteBatch _spriteBatch;
    private GameManager _gameStateManager;
    
    public Emulator()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }
    
    protected override void Initialize()
    {
        _systemFont = Content.Load<SpriteFont>(@"NES_FONT");

        _gameStateManager = new GameManager(GraphicsDevice, _systemFont);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // TODO: use this.Content to load your game content here
    }
    
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
            
        _gameStateManager.Update(gameTime);

        if (Keyboard.GetState().IsKeyDown(Keys.F5))
        {
            IsFixedTimeStep = !IsFixedTimeStep;
            _graphics.SynchronizeWithVerticalRetrace = !_graphics.SynchronizeWithVerticalRetrace;
        }
            
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.MidnightBlue);
        GraphicsDevice.Textures[0] = null;
            
        _spriteBatch.Begin();
            _gameStateManager.Draw(_spriteBatch);
        _spriteBatch.End();
            

        base.Draw(gameTime);
    }

}