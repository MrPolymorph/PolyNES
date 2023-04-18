using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using Poly6502.Microprocessor;
using Poly6502.Microprocessor.Interfaces;
using PolyNES.Cartridge.Interfaces;
using PolyNES.Memory;

namespace PolyNES.ProcessorTests
{
    namespace Poly6502.Microprocessor.Tests.NesTestTests
    {
        public class NesTest
        {
            private M6502 _m6502;
            private ICartridge _cartridge;
            private WorkRam _ram;
            
            private int _currentLine = 0;
            private byte _opCode;
            private ushort _programCounter;
            private byte _flags;
            private byte _a, _x, _y;
            private List<LogLine> _logLines { get; set; }
            private List<OpCodeVerification> _opCodePassFail { get; set; }

            private byte _ramValue;
            private byte _cartValue;

            [SetUp]
            public void Setup()
            {
                _ram = new WorkRam();
                _cartridge = new Cartridge.Cartridge();
                _m6502 = new M6502();
                _m6502.RegisterDevice((IDataBusCompatible) _cartridge, 1);
                _m6502.RegisterDevice((IDataBusCompatible) _ram, 2);
                _cartridge.RegisterDevice(_ram);


                _logLines = new List<LogLine>();
                _opCodePassFail = new List<OpCodeVerification>();

                _cartridge.LoadCartridge("/Users/Kris/Documents/ROMS/NES/nestest.nes");
                _logLines = LoadLog("/Users/Kris/Documents/ROMS/NES/nestest.log");

                _m6502.RES(0xC000);
            }


            [Test]
            public void Processor_Should_Match_NesTest_Log()
            {
                _m6502.FetchComplete += FetchCompleteCallback;
                while (_currentLine < _logLines.Count)
                {
                    //Clock the CPU
                    _m6502.Clock();

                    //this ordering is important to keep propagation ordering
                    _cartridge.Clock();
                    _ram.Clock();
                }
            }
#nullable enable
            private void FetchCompleteCallback(object? sender, EventArgs args)
            {
#nullable  disable
                _ramValue = _ram.Peek(_m6502.AddressBusAddress);
                _opCode = _m6502.OpCode;
                _programCounter = (ushort) (_m6502.Pc -1) ;
                _flags = _m6502.P.Register;
                _a = _m6502.A;
                _x = _m6502.X;
                _y = _m6502.Y;
                
                var verification = Verify(_opCode, _programCounter);
                
                if (!verification.Pass)
                {
                    Assert.Fail($"Processor Instruction Failed at line {_currentLine}. Failure: {verification.FailureType}. Expected: {verification.Expected}  Actual: {verification.Actual}");
                }

                Console.Out.WriteLine($"Test Line {_currentLine} Passed");

                _currentLine++;
            }

            private OpCodeVerification Verify(byte opCode, ushort pc)
            {
                var item = _logLines[_currentLine];
                item.OpCodeName = _m6502.OpCodeLookupTable[item.OpCode].OpCodeMethod.Method.Name;

                var currentOpCode = opCode;
                var currentLo = _m6502.InstructionLoByte;
                var currentHi = _m6502.InstructionHiByte;

                var actual = new LogLine()
                {
                    OpCode = currentOpCode,
                    LoByte = currentLo,
                    HiByte = currentHi,
                    OpCodeName = _m6502.OpCodeLookupTable[currentOpCode].OpCodeMethod.Method.Name,
                    ProgramCounter = pc,
                    Flags = _flags,
                    A = _a,
                    X = _x,
                    Y = _y
                };

                OpCodeVerification currentOp = new OpCodeVerification(item, actual);

                _opCodePassFail.Add(currentOp);

                return currentOp;
            }


            public List<LogLine> LoadLog(string file)
            {
                string text = File.ReadAllText(file);

                var lines = text.Split('\n');

                var logLines = new List<LogLine>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line))
                        continue;

                    byte lo = 0;
                    byte hi = 0;
                    byte p;
                    byte a;
                    byte x;
                    byte y;
                    byte.TryParse(line.Substring(9, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out lo);
                    byte.TryParse(line.Substring(12, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out hi);
                    byte.TryParse(line.Substring(50, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out a);
                    byte.TryParse(line.Substring(55, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out x);
                    byte.TryParse(line.Substring(60, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out y);
                    byte.TryParse(line.Substring(65, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out p);
                    var ll = new LogLine()
                    {
                        ProgramCounter = ushort.Parse(line.Substring(0, 4), NumberStyles.HexNumber),
                        OpCode = byte.Parse(line.Substring(6, 2), NumberStyles.HexNumber),
                        LoByte = lo,
                        HiByte = hi,
                        Flags = (byte) (p),
                        A = a,
                        Y = y,
                        X = x
                        
                    };

                    logLines.Add(ll);
                }

                return logLines;
            }
        }

        public enum FailureType
        {
            ProgramCounter,
            OpCode,
            LoByte,
            HiByte,
            Flags,
            ARegister,
            XRegister,
            YRegister,
            None
        }

        public class LogLine
        {
            public ushort ProgramCounter { get; set; }
            public byte OpCode { get; set; }
            public byte LoByte { get; set; }
            public byte HiByte { get; set; }
            public string OpCodeName { get; set; }
            public byte Flags { get; set; }
            public byte A { get; set; }
            public byte X { get; set; }
            public byte Y { get; set; }
            
            public LogLine()
            {
                ProgramCounter = 0;
                OpCode = 0;
                LoByte = 0;
                HiByte = 0;
                OpCodeName = "NOT INITIALISED";
                Flags = 0;
                A = 0;
                X = 0;
                Y = 0;
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"PC: {ProgramCounter}");
                builder.AppendLine($"OpCode: {OpCode}");
                builder.AppendLine($"LoByte: {LoByte}");
                builder.AppendLine($"HiByte: {HiByte}");
                builder.AppendLine($"OpName: {OpCodeName}");
                builder.AppendLine($"Flags: {Flags}");
                builder.AppendLine($"AReg: {A}");
                builder.AppendLine($"XReg: {X}");
                builder.AppendLine($"YReg: {Y}");

                return builder.ToString();
            }

            public FailureType Compare(LogLine log)
            {
                if (ProgramCounter != log.ProgramCounter)
                {
                    //return FailureType.ProgramCounter;
                }

                if (OpCode != log.OpCode)
                {
                    return  FailureType.OpCode;
                }

                if (OpCodeName != log.OpCodeName)
                {
                    return  FailureType.OpCode;
                }

                if (Flags != log.Flags)
                {
                    return  FailureType.Flags;
                }

                if (A != log.A)
                {
                    return  FailureType.ARegister;
                }

                if (X != log.X)
                {
                    return  FailureType.XRegister;
                }

                if (Y != log.Y)
                {
                    return  FailureType.YRegister;
                }

                return  FailureType.None;
            }
        }

        public class OpCodeVerification
        {
            public LogLine Expected { get; }
            public LogLine Actual { get; }
            public FailureType FailureType { get; }
            public bool Pass { get; }

            public OpCodeVerification(LogLine expected, LogLine actual)
            {
                Expected = expected;
                Actual = actual;
                FailureType = Expected.Compare(Actual);
                Pass = FailureType == FailureType.None;
            }
        }
    }
}