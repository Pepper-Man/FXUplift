using System;
using System.IO;

namespace H2ToH3EffectConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Halo 2 to Halo 3 Effect Converter\n\n");

            while (true)
            {
                Console.WriteLine("Single or Bulk effect tag conversion? (S/B):");
                string userInput = Console.ReadLine();
                if (userInput.ToLower() == "s")
                {
                    Console.WriteLine("Selected single tag conversion.\n");
                    SingleTagConversion();
                    break;
                }
                else if (userInput.ToLower() == "b")
                {
                    Console.WriteLine("Selected bulk tag conversion.\n");
                    BulkTagConversion();
                    break;
                }

                Console.WriteLine("Invalid input. Enter \"S\" or \"B\" characters only. Please try again.");
            }
            

            // TEMPORARY HARDCODING
            /*
            string h2EffectPath = "G:\\Steam\\steamapps\\common\\H2EK\\tags\\effects\\scenarios\\solo\\alphagasgiant\\cables\\cables_cut_major.effect";
            string h3ek = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\H3EK";
            string h2ek = Utils.GetH2EKPath(h2EffectPath);
            string h2XmlPath = Utils.ExtractTagXML(h2EffectPath, "effect", h2ek);
            ReadFromH2.Effect effect = ReadFromH2.GetEffectDataFromXML(h2XmlPath, h2ek, h3ek);
            WriteToH3.WriteH3Effect(h3ek, "halo_2\\effe_test\\cables_cut_major", effect);
            */
        }

        static void SingleTagConversion()
        {
            string h2EffectPath = "";
            string h3ek = "";
            
            // Get H2 effect tag path
            while (true)
            {
                Console.WriteLine("Enter full filepath to H2 effect tag, from drive letter to file extension:");
                h2EffectPath = Console.ReadLine().Replace("\"", "");
                if (File.Exists(h2EffectPath))
                {
                    if (h2EffectPath.Contains("H2EK\\tags"))
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Unable to locate \"H2EK\" directory. Make sure file is inside \"H2EK\\tags\".\n");
                    }
                }
                else
                {
                    Console.WriteLine("File does not exist! Please try again.\n");
                }
            }
            
            // Get H3EK directory
            while(true)
            {
                Console.WriteLine("Enter path to your \"H3EK\" folder:");
                h3ek = Console.ReadLine().Replace("\"", "");
                if (Directory.Exists(h3ek))
                {
                    if (new DirectoryInfo(h3ek).Name == "H3EK")
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("This doesn't appear to be the \"H3EK\" folder path. Please try again.\n");
                    }
                }
                else
                {
                    Console.WriteLine("Folder does not exist! Please try again.\n");
                }
            }

            string h2ek = Utils.GetH2EKPath(h2EffectPath);
            string outputFolder = Utils.H2ToH3Path(h2EffectPath);
            string h2XmlPath = Utils.ExtractTagXML(h2EffectPath, "effect", h2ek);
            ReadFromH2.Effect effect = ReadFromH2.GetEffectDataFromXML(h2XmlPath, h2ek, h3ek);
            WriteToH3.WriteH3Effect(h3ek, outputFolder, effect);
        }

        static void BulkTagConversion()
        {
            throw new NotImplementedException();
        }
    }
}
