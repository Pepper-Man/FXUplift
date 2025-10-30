using Bungie;

namespace H2ToH3EffectConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string h3ekPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\H3EK";
            ManagedBlamSystem.InitializeProject(InitializationType.TagsOnly, h3ekPath);

            string h2EffectPath = "G:\\Steam\\steamapps\\common\\H2EK\\tags\\effects\\scenarios\\solo\\alphagasgiant\\cables\\cables_cut_major.xml";
            ReadFromH2.GetDataFromXML(h2EffectPath);
        }
    }
}
