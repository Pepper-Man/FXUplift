using Bungie;
using Bungie.Game;
using Bungie.Tags;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace H2ToH3EffectConverter
{
    using CurvePoints = ReadFromH2.CurvePoints;

    internal class Utils
    {
        public static string CleanXML(string filePath)
        {
            try
            {
                // Read as text
                string xmlContent = File.ReadAllText(filePath);

                // Clean invalid characters
                string cleanedXML = RemoveInvalidXmlCharacters(xmlContent);

                return cleanedXML;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"XML clean/load error: {ex}", false);
                return null;
            }

            bool IsXmlChar(char ch)
            {
                // Check if the character is a valid XML character
                return ch == 0x9 || ch == 0xA || ch == 0xD ||
                   (ch >= 0x20 && ch <= 0xD7FF) ||
                   (ch >= 0xE000 && ch <= 0xFFFD) ||
                   (ch >= 0x10000 && ch <= 0x10FFFF);
            }

            string RemoveInvalidXmlCharacters(string input)
            {
                // Replace invalid characters with an empty string
                StringBuilder cleanedString = new StringBuilder();
                foreach (char ch in input)
                {
                    if (IsXmlChar(ch))
                    {
                        cleanedString.Append(ch);
                    }
                }

                return cleanedString.ToString();
            }
        }

        public static Dictionary<string, int> funcInputMapping = new Dictionary<string, int>
        {
            { "0,particle age", 0 },
            { "1,particle emit time", 10 },
            { "2,particle random 1", 2 },
            { "3,particle random 2", 2 },
            { "4,emitter age", 1 },
            { "5,emitter random 1", 3 },
            { "6,emitter random 2", 3 },
            { "7,system lod", 8 },
            { "8,game time", 12 },
            { "9,effect a scale", 13 },
            { "10,effect b scale", 14 },
            { "11,particle rotation", 15 },
            { "12,explosion animation", 18 },
            { "13,explosion rotation", 19 },
            { "14,particle random 3", 2 },
            { "15,particle random 4", 2 },
            { "16,location random", 16 }
        };

        public static float ReadHalo2PackedFloat(sbyte[] data, int i)
        {
            byte b0 = (byte)data[i];
            byte b1 = (byte)data[i + 1];
            byte b2 = (byte)data[i + 2];
            byte b3 = (byte)data[i + 3];

            byte[] swappedOrder = { b1, b0, b3, b2 };
            Array.Reverse(swappedOrder);
            return BitConverter.ToSingle(swappedOrder, 0);
        }

        public static void PrintObject(object obj, int indents)
        {
            string indent = "";
            if (indents == 1)
            {
                indent = "\t";
            }
            else if (indents == 2)
            {
                indent = "\t\t";
            }

            Type type = obj.GetType();
            foreach (var prop in type.GetProperties())
            {
                var value = prop.GetValue(obj);
                if (value == null)
                {
                    Console.WriteLine($"{indent}{prop.Name}: <null>");
                    continue;
                }

                // Arrays
                if (value.GetType().IsArray)
                {
                    var array = (IEnumerable)value;
                    var elements = new List<string>();
                    foreach (var item in array)
                        elements.Add(item?.ToString() ?? "<null>");
                    Console.WriteLine($"{indent}{prop.Name}: {string.Join(", ", elements)}");
                }

                // TagReferenceData
                else if (value is ReadFromH2.TagReferenceData tagRef)
                {
                    Console.WriteLine($"{indent}{prop.Name}: {tagRef.Path}.{tagRef.Extension}");
                }

                // Halo2Functions
                else if (value.GetType().GetProperty("FuncType") != null)
                {
                    var funcType = value.GetType().GetProperty("FuncType")?.GetValue(value);
                    var useRange = value.GetType().GetProperty("UseRange")?.GetValue(value);
                    var baseVal = value.GetType().GetProperty("BaseVal")?.GetValue(value);
                    var rangeVal = value.GetType().GetProperty("RangeVal")?.GetValue(value);
                    var colour1 = value.GetType().GetProperty("Colour1")?.GetValue(value);
                    var colour2 = value.GetType().GetProperty("Colour2")?.GetValue(value);
                    var colour3 = value.GetType().GetProperty("Colour3")?.GetValue(value);
                    var colour4 = value.GetType().GetProperty("Colour4")?.GetValue(value);

                    if (prop.Name != "Tint")
                    {
                        Console.WriteLine($"{indent}{prop.Name}: FuncType={Utils.funcTypeMapping[(int)funcType]}, UseRange={useRange}, BaseVal={baseVal}, RangeVal={rangeVal}");
                    }
                    else
                    {
                        string toColourString(object c) => c is GameColor g ? $"{g.Red},{g.Green},{g.Blue}" : "null";

                        string colour1String = toColourString(colour1);
                        string colour2String = toColourString(colour2);
                        string colour3String = toColourString(colour3);
                        string colour4String = toColourString(colour4);

                        Console.WriteLine($"{indent}{prop.Name}: FuncType={Utils.funcTypeMapping[(int)funcType]}, UseRange={useRange}, Colour1={colour1String}, Colour2={colour2String}, Colour3={colour3String}, Colour4={colour4String}");
                    }
                }
                else
                {
                    Console.WriteLine($"{indent}{prop.Name}: {value}");
                }
            }
        }

        public static string ExtractTagXML(string h2TagPath, string type, string h2ek)
        {
            string toolExePath = Path.Combine(h2ek, "tool.exe");
            string subFolder = "";

            if (type == "effect")
            {
                subFolder = "xml\\effects";
            }
            else if (type == "particle physics")
            {
                subFolder = "xml\\particle_phys";
            }
            else if (type == "particle")
            {
                subFolder = "xml\\particles";
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string xmlDir = Path.Combine(baseDir, subFolder);
            Directory.CreateDirectory(xmlDir);

            string tagName = Path.GetFileNameWithoutExtension(h2TagPath);
            string xmlOutPath = Path.Combine(xmlDir, $"{tagName}.xml");

            // Build arguments
            string[] argumentsArray = new string[]
            {
                "export-tag-to-xml",
                h2TagPath,
                xmlOutPath
            };

            string arguments = string.Join(" ", argumentsArray);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = toolExePath,
                Arguments = arguments,
                WorkingDirectory = h2ek,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = psi })
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }

            return xmlOutPath;
        }

        public static string GetH2EKPath(string fullEffectPath)
        {
            return fullEffectPath.Substring(0, fullEffectPath.IndexOf("H2EK") + 4);
        }

        // Convert 0..255 byte -> normalized sRGB 0..1
        public static float Norm(byte b) => b / 255f;

        // sRGB -> linear
        public static float SrgbToLinearF(float c)
        {
            if (c <= 0.04045f) return c / 12.92f;
            return (float)Math.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

        // convenience: byte[] -> linear float[]
        public static float[] ByteRgbToLinearNormalized(byte r, byte g, byte b)
        {
            return new float[] { SrgbToLinearF(Norm(r)), SrgbToLinearF(Norm(g)), SrgbToLinearF(Norm(b))};
        }

        public static Dictionary<int, FunctionEditorMasterType> funcTypeMapping = new Dictionary<int, FunctionEditorMasterType>
        {
            { 0, FunctionEditorMasterType.Transition },
            { 1, FunctionEditorMasterType.Basic },
            { 2, FunctionEditorMasterType.Transition },
            { 3, FunctionEditorMasterType.Periodic },
            { 4, FunctionEditorMasterType.Curve },
            { 5, FunctionEditorMasterType.Curve },
            { 6, FunctionEditorMasterType.Curve },
            { 7, FunctionEditorMasterType.Curve },
            { 8, FunctionEditorMasterType.Curve },
            { 9, FunctionEditorMasterType.Exponent },
            { 10, FunctionEditorMasterType.Curve }
        };

        public static Dictionary<int, int> billboardTypeMapping = new Dictionary<int, int>
        {
            { 0, 0 },
            { 1, 2 },
            { 2, 3 },
            { 3, 4 },
            { 4, 5 },
        };

        public static Dictionary<string, uint> particleMainFlagValues = new Dictionary<string, uint>
        {
            { "dies at rest", 1 },
            { "dies in air", 8 },
            { "dies in media", 4 },
            { "dies on structure collision", 2 },
            { "has sweetener", 16 },
        };

        public static Dictionary<string, uint> particleAppearFlagValues = new Dictionary<string, uint>
        {
            { "bitmap authored vertically", 32 },
            { "disable frame blending", 0 },
            { "random u mirror", 1 },
            { "random v mirror", 2 },
            { "receive lightmap lighting", 8 },
            { "select random sequence", 0 },
            { "spins", 0 },
            { "tint from diffuse texture", 16 },
        };

        public static Dictionary<string, uint> particleAnimFlagValues = new Dictionary<string, uint>
        {
            { "can animate backwards", 2 },
            { "frame animation one shot", 1 },
        };

        // Dictionary for converting H2's particle shader templates to their respective alpha mode
        // Plasma doesn't exist in H3, dunno what to do with that
        // Alpha test is a special case since its a different category, hence 99
        // "Normal" additive mode for H3 particles doesnt let them fade properly, need to use add*srcalpha instead
        // 0 - opaque
        // 1 - additive
        // 2 - multiply
        // 3 - alpha blend
        // 4 - double mult
        // 5 - max
        // 6 - mult add
        // 7 - add times dstalpha
        // 8 - add times srcalpha
        // 9 - inv alpha blend
        // 10 - pre mult alpha
        // 50 - plasma
        // 99 - alpha test
        public static Dictionary<string, int> shaderTemplateBlendMode = new Dictionary<string, int>
        {
            { "particle_alpha_blend_distortion", 3 },
            { "particle_alpha_test", 99 },
            { "particle_plasma", 50 },
            { "particle_alpha_multiply_add_tint", 6 },
            { "particle_alpha_test_no_lighting", 99 },
            { "particle_alpha_test_tint", 99 },
            { "particle_alpha_test_tint_no_lighting", 99 },
            { "particle_additive", 8 },
            { "particle_additive_tint", 8 },
            { "particle_alpha_blend", 3 },
            { "particle_alpha_blend_tint", 3 },
            { "particle_alpha_multiply_add", 6 },
            { "particle_alpha_test_no_lighting_fixed_for_glass", 99 },
        };

        public static class HaloCurveParser
        {
            public static CurvePoints ParseCurve(sbyte[] data)
            {
                // Helper to decode float from 4 bytes starting at an index
                float ReadFloat(int byteIndex)
                {
                    byte[] raw = new byte[4];
                    for (int i = 0; i < 4; i++)
                        raw[i] = (byte)data[byteIndex + i];

                    return BitConverter.ToSingle(raw, 0);
                }

                // First value determines graph type: 4 = linear, 5 = linear key
                int graphType = data[0];

                if (graphType == 4) // Linear
                {
                    // y0 and y1 always exist?
                    float baseY0 = ReadFloat(24);
                    float baseY1 = ReadFloat(32);
                    float rangeY0 = ReadFloat(48);
                    float rangeY1 = ReadFloat(56);

                    return new CurvePoints
                    {
                        BaseY0 = baseY0,
                        BaseY1 = baseY1,
                        BaseY2 = null,
                        BaseY3 = null,
                        RangeY0 = rangeY0,
                        RangeY1 = rangeY1,
                        RangeY2 = null,
                        RangeY3 = null,
                    };
                }
                else if (graphType == 5) // Linear key
                {
                    float baseY0 = ReadFloat(24);
                    float baseX1 = ReadFloat(28);
                    float baseY1 = ReadFloat(32);
                    float baseX2 = ReadFloat(64);
                    float baseY2 = ReadFloat(40);
                    float baseY3 = ReadFloat(48);

                    float rangeY0 = ReadFloat(104);
                    float rangeX1 = ReadFloat(108);
                    float rangeY1 = ReadFloat(112);
                    float rangeX2 = ReadFloat(116);
                    float rangeY2 = ReadFloat(120);
                    float rangeY3 = ReadFloat(128);


                    return new CurvePoints
                    {
                        BaseY0 = baseY0,
                        BaseX1 = baseX1,
                        BaseY1 = baseY1,
                        BaseX2 = baseX2,
                        BaseY2 = baseY2,
                        BaseY3 = baseY3,
                        RangeY0 = rangeY0,
                        RangeX1 = rangeX1,
                        RangeY1 = rangeY1,
                        RangeX2 = rangeX2,
                        RangeY2 = rangeY2,
                        RangeY3 = rangeY3,
                    };
                }
                else if (graphType == 7) // Spline 1
                {
                    float baseY0 = ReadFloat(24);
                    float baseY1 = ReadFloat(48);
                    float baseHandle1X = ReadFloat(28);
                    float baseHandle1Y = ReadFloat(32);
                    float baseHandle2X = ReadFloat(36);
                    float baseHandle2Y = ReadFloat(40);

                    return new CurvePoints
                    {
                        IsSpline1 = true,
                        BaseY0 = baseY0,
                        BaseY1 = baseY1,
                        BaseHandle1X = baseHandle1X,
                        BaseHandle1Y = baseHandle1Y,
                        BaseHandle2X = baseHandle2X,
                        BaseHandle2Y = baseHandle2Y
                    };
                }
                else
                {
                    return new CurvePoints { BaseY0 = 0, BaseY1 = 0, BaseY2 = null, BaseY3 = null };
                }    
            }
        }
    }
}
