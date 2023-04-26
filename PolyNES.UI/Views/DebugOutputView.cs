using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Poly6502.Microprocessor;
using PolyNES.Managers;
using PolyNES.PPU;
using PolyNES.UI.Managers;

namespace PolyNES.UI.Views;

public class DebugOutputView
{
    private SpriteFont _systemFont;
    private SystemManager _systemManager;
    private KeyboardManager _keyboardManager;
    private M6502 _cpu;
    private Poly2C02 _ppu;
    private bool _emulate;
    private Texture2D _leftPatternTable;
    private Texture2D _rightPatternTable;
    
    public DebugOutputView(SpriteFont font, GraphicsDevice gd, KeyboardManager keyboardManager, SystemManager systemManager)
    {
        _systemFont = font;
        _systemManager = systemManager;
        _cpu = systemManager.M6502;
        _ppu = systemManager.PPU;
        _emulate = false;
        _leftPatternTable = new Texture2D(gd, 128, 128, false, SurfaceFormat.Dxt3);
        _rightPatternTable = new Texture2D(gd, 128, 128, false, SurfaceFormat.Dxt3);
        
        keyboardManager.SubscribeForKeyDown(Keys.Space, () =>
        {
            _emulate = !_emulate;
            _systemManager.Run = _emulate;
        });
        
        keyboardManager.SubscribeForKeyDown(Keys.S, () =>
        {
            _systemManager.StepSystem();
        });
    }
    
    
    public void DrawRegisters(SpriteBatch sb)
    {
        var baseX = 70;
        var baseY = 0;
            
            
        sb.DrawString(_systemFont, $"PC: $0x{_cpu.Pc:X4}", new Vector2(baseX + 64, baseY + 25), Color.White);
        sb.DrawString(_systemFont, $"Stack P: 0x{_cpu.SP:X2}", new Vector2(baseX + 64, baseY + 45), Color.White);
        sb.DrawString(_systemFont, $"Adr: 0x{_cpu.AddressBusAddress:X4} ", new Vector2(baseX + 64, baseY + 65), Color.White);
            
        sb.DrawString(_systemFont, $"A: 0x{_cpu.A:X2} ", new Vector2(baseX + 170, baseY + 65), Color.White);
        sb.DrawString(_systemFont, $"X: 0x{_cpu.X:X2} ", new Vector2(baseX + 260, baseY + 65), Color.White);
        sb.DrawString(_systemFont, $"Y: 0x{_cpu.Y:X2} ", new Vector2(baseX + 350, baseY + 65), Color.White);
        sb.DrawString(_systemFont, $"Status: {_cpu.P:X2} ", new Vector2(baseX + 450, baseY + 65), Color.White);
    }
    
    public void DrawFps(SpriteBatch sb, float fps)
    {
        sb.DrawString(_systemFont, $"FPS: {fps}", new Vector2(0, 0), Color.Green);
    }
    

    public void DrawDisassembly(SpriteBatch sb)
    {
        var y = 150;
        var x = 150;
            
        var programCounterHex = _systemManager.M6502.Pc.ToString("X4");
        var instruction = _systemManager.Disassembly.FirstOrDefault(x => x.Contains($"${programCounterHex}:", StringComparison.OrdinalIgnoreCase));

        if (instruction != null)
        {
            var instructionIndex = _systemManager.Disassembly.IndexOf(instruction);

            if (instructionIndex < 26)
            {
                var dissPrint = _systemManager.Disassembly.Take(26);

                foreach (var line in dissPrint)
                {
                    var color = Color.White;

                    if (line == instruction)
                        color = Color.Cyan;
                        
                    y += 20;
                    PrintDisassemblyLine(line, x, y, color, sb);
                }
            }
            else
            {
                var halfLines = 26 / 2;

                var dissBefore = _systemManager.Disassembly.Skip(instructionIndex - halfLines).Take(halfLines);
                var dissAfter = _systemManager.Disassembly.Skip(instructionIndex + 1).Take(halfLines);

                if(!dissAfter.Any())
                    return;
                if (!dissAfter.Any())
                    return;
                
                foreach (var line in dissBefore)
                {
                    y += 20;
                    PrintDisassemblyLine(line, x, y, Color.White, sb);
                }

                y += 20;
                    
                PrintDisassemblyLine(instruction, x, y, Color.Cyan, sb);
                    
                foreach (var line in dissAfter)
                {
                    y += 20;
                    PrintDisassemblyLine(line, x, y, Color.White, sb);
                }
            }
            
        }
    }

    private void DrawLeftPatternTable(SpriteBatch sb)
    {
        _leftPatternTable.SetData(_ppu.LeftPatternTableView, 0, Poly2C02.PatternTableSize);
        _rightPatternTable.SetData(_ppu.RightPatternTableView, 0, Poly2C02.PatternTableSize);
        
        sb.Draw(_leftPatternTable, new Vector2(400, 320), new Rectangle(0, 0, 128, 128),
            Color.White, 0.0f, new Vector2(0, 0), 5f, SpriteEffects.None, 0f);
        
        // sb.Draw(_rightPatternTable, new Vector2(400, 320), new Rectangle(0, 0, 128, 128),
        //     Color.White, 0.0f, new Vector2(0, 0), 5f, SpriteEffects.None, 0f);
    }
    
    private void PrintDisassemblyLine(string line, int x, int y, Color color, SpriteBatch sb)
    {
        if (string.IsNullOrEmpty(line))
            return;
        
        sb.GraphicsDevice.Clear(Color.MidnightBlue);
        sb.DrawString(_systemFont, line, new Vector2(x , y), color);
    }
    
    public void DrawAll(SpriteBatch sb, float fps)
    {
        DrawDisassembly(sb);
        //DrawRam(sb);
        DrawRegisters(sb);
        DrawFps(sb, fps);
        DrawLeftPatternTable(sb);
        _systemManager.ClockSystem();
    }
}