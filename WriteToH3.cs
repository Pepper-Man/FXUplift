using Bungie;
using Bungie.Tags;
using System;
using System.Collections.Generic;
using System.Linq;

namespace H2ToH3EffectConverter
{
    using Effect = ReadFromH2.Effect;

    internal class WriteToH3
    {
        public static void WriteH3Effect(string h3ekPath, string outputPath, Effect effect)
        {
            ManagedBlamSystem.InitializeProject(InitializationType.TagsOnly, h3ekPath);
            TagPath outputTagPath = TagPath.FromPathAndExtension(outputPath, "effect");
            TagFile tagFile = new TagFile();
            tagFile.New(outputTagPath);

            Console.WriteLine("Writing tag data");

            int markerCount = effect.Markers.Count();
            for (int markerIndex = 0; markerIndex < markerCount; markerIndex++)
            {
                ((TagFieldBlock)tagFile.SelectField("Block:locations")).AddElement();
                ((TagFieldElementOldStringID)tagFile.SelectField($"Block:locations[{markerIndex}]/OldStringId:marker name")).Data = effect.Markers[markerIndex];
            }

            int eventsCount = effect.Events.Count();
            for (int eventIndex = 0; eventIndex < eventsCount; eventIndex++)
            {
                ((TagFieldBlock)tagFile.SelectField("Block:events")).AddElement();
                ((TagFieldElementStringID)tagFile.SelectField($"Block:events[{eventIndex}]/StringId:event name")).Data = "event_" + eventIndex;
                ((TagFieldElementSingle)tagFile.SelectField($"Block:events[{eventIndex}]/RealFraction:skip fraction")).Data = effect.Events[eventIndex].SkipFraction;
                ((TagFieldElementArraySingle)tagFile.SelectField($"Block:events[{eventIndex}]/RealBounds:delay bounds")).Data = effect.Events[eventIndex].DelayBounds;
                ((TagFieldElementArraySingle)tagFile.SelectField($"Block:events[{eventIndex}]/RealBounds:duration bounds")).Data = effect.Events[eventIndex].DurationBounds;

                int partsCount = effect.Events[eventIndex].Parts.Count();
                for (int partsIndex = 0; partsIndex < partsCount; partsIndex++)
                {
                    ((TagFieldBlock)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts")).AddElement();
                    ((TagFieldEnum)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/ShortEnum:create in")).Value = effect.Events[eventIndex].Parts[partsIndex].Environment;
                    ((TagFieldEnum)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/ShortEnum:violence mode")).Value = effect.Events[eventIndex].Parts[partsIndex].Mode;
                    ((TagFieldBlockIndex)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/ShortBlockIndex:location")).Value = effect.Events[eventIndex].Parts[partsIndex].Location;
                    ((TagFieldFlags)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/WordFlags:flags")).RawValue= effect.Events[eventIndex].Parts[partsIndex].Flags;
                    ((TagFieldReference)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/Reference:type")).Path = TagPath.FromPathAndType(effect.Events[eventIndex].Parts[partsIndex].TagRef.Path, effect.Events[eventIndex].Parts[partsIndex].TagRef.Extension);
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/RealBounds:velocity bounds")).Data = effect.Events[eventIndex].Parts[partsIndex].VelocityBounds;
                    ((TagFieldElementSingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/Angle:velocity cone angle")).Data = effect.Events[eventIndex].Parts[partsIndex].VelocityConeAngle;
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/AngleBounds:angular velocity bounds")).Data = effect.Events[eventIndex].Parts[partsIndex].AngularVelocityBounds;
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/RealBounds:radius modifier bounds")).Data = effect.Events[eventIndex].Parts[partsIndex].RadiusModifierBounds;
                    ((TagFieldFlags)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/Flags:A scales values")).RawValue = effect.Events[eventIndex].Parts[partsIndex].AScalesVals;
                    ((TagFieldFlags)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/Flags:B scales values")).RawValue = effect.Events[eventIndex].Parts[partsIndex].BScalesVals;
                }
            }


            tagFile.Save();
        }
    }
}
