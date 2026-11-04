using Bungie;
using Bungie.Game;
using Bungie.Tags;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace H2ToH3EffectConverter
{
    using static H2ToH3EffectConverter.ReadFromH2;
    using Effect = ReadFromH2.Effect;
    using Emitter = ReadFromH2.Emitter;
    using Halo2Function = ReadFromH2.Halo2Function;
    using Particle = ReadFromH2.Particle;
    using ParticlePhysics = ReadFromH2.ParticlePhysics;

    internal class WriteToH3
    {
        // MB can only be init'd once per process, so keep track
        private static bool projectInitialised = false;

        private static void EnsureProjectInitialised(string h3ekPath)
        {
            if (projectInitialised) return;

            ManagedBlamSystem.InitializeProject(InitializationType.TagsOnly, h3ekPath);
            projectInitialised = true;
        }

        public static string GetH3TagFilePath(string h3ekPath, string outputPath, string extension)
        {
            return Path.Combine(h3ekPath, "tags", outputPath + "." + extension);
        }

        public static void WriteH3Effect(string h3ekPath, string outputPath, Effect effect)
        {
            EnsureProjectInitialised(h3ekPath);

            TagPath outputTagPath = TagPath.FromPathAndExtension(outputPath, "effect");
            TagFile tagFile = new TagFile();
            try
            {
                tagFile.New(outputTagPath);
                WriteEffectData(h3ekPath, tagFile, effect);
                tagFile.Save();
            }
            finally
            {
                tagFile.Dispose();
            }
        }

        private static void WriteEffectData(string h3ekPath, TagFile tagFile, Effect effect)
        {
            Console.WriteLine("Writing tag data");

            // MARKERS ----------------------------------------------------------------------------------------
            int markerCount = effect.Markers.Count();
            if (markerCount > 8)
            {
                markerCount = 8;
            }
            for (int markerIndex = 0; markerIndex < markerCount; markerIndex++)
            {
                ((TagFieldBlock)tagFile.SelectField("Block:locations")).AddElement();
                ((TagFieldElementOldStringID)tagFile.SelectField($"Block:locations[{markerIndex}]/OldStringId:marker name")).Data = effect.Markers[markerIndex];
            }

            // EVENTS -----------------------------------------------------------------------------------------
            int eventsCount = effect.Events.Count();
            for (int eventIndex = 0; eventIndex < eventsCount; eventIndex++)
            {
                ((TagFieldBlock)tagFile.SelectField("Block:events")).AddElement();
                ((TagFieldElementStringID)tagFile.SelectField($"Block:events[{eventIndex}]/StringId:event name")).Data = "event_" + eventIndex;
                ((TagFieldElementSingle)tagFile.SelectField($"Block:events[{eventIndex}]/RealFraction:skip fraction")).Data = effect.Events[eventIndex].SkipFraction;
                ((TagFieldElementArraySingle)tagFile.SelectField($"Block:events[{eventIndex}]/RealBounds:delay bounds")).Data = effect.Events[eventIndex].DelayBounds;
                ((TagFieldElementArraySingle)tagFile.SelectField($"Block:events[{eventIndex}]/RealBounds:duration bounds")).Data = effect.Events[eventIndex].DurationBounds;

                // PARTS --------------------------------------------------------------------------------------
                int partsCount = effect.Events[eventIndex].Parts.Count();
                for (int partsIndex = 0; partsIndex < partsCount; partsIndex++)
                {
                    ((TagFieldBlock)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts")).AddElement();
                    ((TagFieldEnum)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/ShortEnum:create in")).Value = effect.Events[eventIndex].Parts[partsIndex].Environment;
                    ((TagFieldEnum)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/ShortEnum:violence mode")).Value = effect.Events[eventIndex].Parts[partsIndex].Mode;
                    ((TagFieldBlockIndex)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/ShortBlockIndex:location")).Value = effect.Events[eventIndex].Parts[partsIndex].Location;
                    ((TagFieldFlags)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/WordFlags:flags")).RawValue = effect.Events[eventIndex].Parts[partsIndex].Flags;
                    if (effect.Events[eventIndex].Parts[partsIndex].TagRef != null)
                    {
                        ((TagFieldReference)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/Reference:type")).Path = TagPath.FromPathAndType(effect.Events[eventIndex].Parts[partsIndex].TagRef.Path, effect.Events[eventIndex].Parts[partsIndex].TagRef.Extension);
                    }
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/RealBounds:velocity bounds")).Data = effect.Events[eventIndex].Parts[partsIndex].VelocityBounds;
                    ((TagFieldElementSingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/Angle:velocity cone angle")).Data = effect.Events[eventIndex].Parts[partsIndex].VelocityConeAngle;
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/AngleBounds:angular velocity bounds")).Data = effect.Events[eventIndex].Parts[partsIndex].AngularVelocityBounds;
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/RealBounds:radius modifier bounds")).Data = effect.Events[eventIndex].Parts[partsIndex].RadiusModifierBounds;
                    ((TagFieldFlags)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/Flags:A scales values")).RawValue = effect.Events[eventIndex].Parts[partsIndex].AScalesVals;
                    ((TagFieldFlags)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/Flags:B scales values")).RawValue = effect.Events[eventIndex].Parts[partsIndex].BScalesVals;
                }

                // PARTICLE SYSTEMS ----------------------------------------------------------------------------
                int particleSysCount = effect.Events[eventIndex].ParticleSystems.Count();
                for (int pSysIndex = 0; pSysIndex < particleSysCount; pSysIndex++)
                {
                    ((TagFieldBlock)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems")).AddElement();
                    if (effect.Events[eventIndex].ParticleSystems[pSysIndex].ParticleRef != null)
                    {
                        ((TagFieldReference)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/Reference:particle")).Path = TagPath.FromPathAndType(Path.Combine("halo_2\\", effect.Events[eventIndex].ParticleSystems[pSysIndex].ParticleRef.Path), effect.Events[eventIndex].ParticleSystems[pSysIndex].ParticleRef.Extension);

                        if (effect.Events[eventIndex].ParticleSystems[pSysIndex].Particle != null)
                        {
                            if (!effect.Events[eventIndex].ParticleSystems[pSysIndex].Particle.AlreadyExists)
                            {
                                WriteH3Particle(h3ekPath, Path.Combine("halo_2\\", effect.Events[eventIndex].ParticleSystems[pSysIndex].ParticleRef.Path), effect.Events[eventIndex].ParticleSystems[pSysIndex].Particle);
                            }
                        }
                    }

                    ((TagFieldBlockIndex)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/LongBlockIndex:location")).Value = effect.Events[eventIndex].ParticleSystems[pSysIndex].Location;
                    ((TagFieldEnum)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/ShortEnum:coordinate system")).Value = effect.Events[eventIndex].ParticleSystems[pSysIndex].CoordSystem;
                    ((TagFieldEnum)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/ShortEnum:environment")).Value = effect.Events[eventIndex].ParticleSystems[pSysIndex].Environment;
                    ((TagFieldEnum)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/ShortEnum:disposition")).Value = effect.Events[eventIndex].ParticleSystems[pSysIndex].Mode;
                    ((TagFieldEnum)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/ShortEnum:camera mode")).Value = effect.Events[eventIndex].ParticleSystems[pSysIndex].CameraMode;
                    ((TagFieldElementInteger)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/ShortInteger:sort bias")).Data = effect.Events[eventIndex].ParticleSystems[pSysIndex].SortBias;
                    ((TagFieldFlags)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/WordFlags:flags")).RawValue = effect.Events[eventIndex].ParticleSystems[pSysIndex].Flags;
                    ((TagFieldElementSingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/Real:LOD in distance")).Data = effect.Events[eventIndex].ParticleSystems[pSysIndex].LodIn;
                    ((TagFieldElementSingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/Real:LOD feather in delta")).Data = effect.Events[eventIndex].ParticleSystems[pSysIndex].LodFInDelta;
                    ((TagFieldElementSingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/Real:LOD out distance")).Data = effect.Events[eventIndex].ParticleSystems[pSysIndex].LodOut;
                    ((TagFieldElementSingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/Real:LOD feather out delta")).Data = effect.Events[eventIndex].ParticleSystems[pSysIndex].LodFOutDelta;

                    // EMITTERS --------------------------------------------------------------------------------
                    int emitterCount = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters.Count();
                    for (int emitterIndex = 0; emitterIndex < emitterCount; emitterIndex++)
                    {
                        string emitter = $"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/Block:emitters[{emitterIndex}]";
                        ((TagFieldBlock)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/Block:emitters")).AddElement();
                        Emitter emitterData = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex];

                        // Emission Shape
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/CharEnum:emission shape")).Value = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].EmissionShape;

                        // Translational Offset
                        ((TagFieldElementArraySingle)tagFile.SelectField($"{emitter}/Struct:translational offset/RealPoint3d:Starting interpolant")).Data = emitterData.TransOffset;
                        ((TagFieldElementArraySingle)tagFile.SelectField($"{emitter}/Struct:translational offset/RealPoint3d:Ending interpolant")).Data = emitterData.TransOffset;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:translational offset/Custom:Mapping")).Value.ClampRangeMin = 0;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:translational offset/Custom:Mapping")).Value.ClampRangeMax = 0;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:translational offset/Custom:Mapping")).Value.IsRanged = false;

                        // Relative Direction
                        ((TagFieldElementArraySingle)tagFile.SelectField($"{emitter}/Struct:relative direction/Struct:Starting interpolant/RealEulerAngles2d:Euler angles")).Data = emitterData.RelativeDir;
                        ((TagFieldElementArraySingle)tagFile.SelectField($"{emitter}/Struct:relative direction/Struct:Ending interpolant/RealEulerAngles2d:Euler angles")).Data = emitterData.RelativeDir;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:relative direction/Custom:Mapping")).Value.ClampRangeMin = 0;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:relative direction/Custom:Mapping")).Value.ClampRangeMax = 0;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:relative direction/Custom:Mapping")).Value.IsRanged = false;

                        // Emission Radius
                        Halo2Function emissionRadius = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].Radius;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:emission radius/CharEnum:Input Variable")).Value = emissionRadius.InputVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:emission radius/CharEnum:Range Variable")).Value = emissionRadius.RangeVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:emission radius/CharEnum:Output Modifier")).Value = emissionRadius.OutputMod;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:emission radius/CharEnum:Output Modifier Input")).Value = emissionRadius.OutputModInput;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission radius/Custom:Mapping")).Value.ClampRangeMin = emissionRadius.BaseVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission radius/Custom:Mapping")).Value.ClampRangeMax = emissionRadius.RangeVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission radius/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[emissionRadius.FuncType];
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission radius/Custom:Mapping")).Value.IsRanged = emissionRadius.UseRange;
                        WriteFunctionCurve(tagFile, emissionRadius, $"{emitter}/Struct:emission radius", emissionRadius.UseRange);

                        // Emission Angle
                        Halo2Function emissionAngle = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].Angle;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:emission angle/CharEnum:Input Variable")).Value = emissionAngle.InputVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:emission angle/CharEnum:Range Variable")).Value = emissionAngle.RangeVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:emission angle/CharEnum:Output Modifier")).Value = emissionAngle.OutputMod;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:emission angle/CharEnum:Output Modifier Input")).Value = emissionAngle.OutputModInput;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission angle/Custom:Mapping")).Value.ClampRangeMin = emissionAngle.BaseVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission angle/Custom:Mapping")).Value.ClampRangeMax = emissionAngle.RangeVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission angle/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[emissionAngle.FuncType];
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission angle/Custom:Mapping")).Value.IsRanged = emissionAngle.UseRange;
                        WriteFunctionCurve(tagFile, emissionAngle, $"{emitter}/Struct:emission angle", emissionAngle.UseRange);

                        // Emission Axis Angle
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission axis angle/Custom:Mapping")).Value.ClampRangeMin = 0;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission axis angle/Custom:Mapping")).Value.ClampRangeMax = 0;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission axis angle/Custom:Mapping")).Value.IsRanged = false;

                        // Emission Rate
                        Halo2Function emissionRate = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].EmissionRate;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle emission rate/CharEnum:Input Variable")).Value = emissionRate.InputVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle emission rate/CharEnum:Range Variable")).Value = emissionRate.RangeVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle emission rate/CharEnum:Output Modifier")).Value = emissionRate.OutputMod;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle emission rate/CharEnum:Output Modifier Input")).Value = emissionRate.OutputModInput;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle emission rate/Custom:Mapping")).Value.ClampRangeMin = emissionRate.BaseVal * 30; // particles per tick -> particles per second
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle emission rate/Custom:Mapping")).Value.ClampRangeMax = emissionRate.RangeVal * 30; // particles per tick -> particles per second
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle emission rate/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[emissionRate.FuncType];
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle emission rate/Custom:Mapping")).Value.IsRanged = emissionRate.UseRange;
                        WriteFunctionCurve(tagFile, emissionRate, $"{emitter}/Struct:particle emission rate", emissionRate.UseRange);

                        // Particle Lifespan
                        Halo2Function lifespan = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].Lifespan;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle lifespan/CharEnum:Input Variable")).Value = lifespan.InputVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle lifespan/CharEnum:Range Variable")).Value = lifespan.RangeVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle lifespan/CharEnum:Output Modifier")).Value = lifespan.OutputMod;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle lifespan/CharEnum:Output Modifier Input")).Value = lifespan.OutputModInput;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle lifespan/Custom:Mapping")).Value.ClampRangeMin = lifespan.BaseVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle lifespan/Custom:Mapping")).Value.ClampRangeMax = lifespan.RangeVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle lifespan/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[lifespan.FuncType];
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle lifespan/Custom:Mapping")).Value.IsRanged = lifespan.UseRange;
                        WriteFunctionCurve(tagFile, lifespan, $"{emitter}/Struct:particle lifespan", lifespan.UseRange);

                        // Particle Physics
                        ParticlePhysics pPhysics = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].ParticlePhys;

                        if (pPhysics != null && pPhysics.Flags != 0)
                        {
                            Halo2Function pPhysGrav = pPhysics.Gravity;
                            Halo2Function pPhysAirFric = pPhysics.AirFriction;
                            Halo2Function pPhysRotFric = pPhysics.RotFriction;
                            Halo2Function pPhysCollFric = pPhysics.CollFriction;
                            Halo2Function pPhysCollElas = pPhysics.CollElasticity;
                            TagPath defaultPhysics = TagPath.FromPathAndExtension("fx\\physics_templates\\_default", "particle_physics");

                            ((TagFieldReference)tagFile.SelectField($"{emitter}/Struct:particle movement/Reference:template")).Path = defaultPhysics;
                            ((TagFieldFlags)tagFile.SelectField($"{emitter}/Struct:particle movement/Flags:flags")).RawValue = pPhysics.Flags;

                            // Gravity
                            ((TagFieldBlock)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements")).AddElement();
                            ((TagFieldBlock)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters")).AddElement();
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[0]/Struct:property/CharEnum:Input Variable")).Value = pPhysGrav.InputVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[0]/Struct:property/CharEnum:Range Variable")).Value = pPhysGrav.RangeVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[0]/Struct:property/CharEnum:Output Modifier")).Value = pPhysGrav.OutputMod;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[0]/Struct:property/CharEnum:Output Modifier Input")).Value = pPhysGrav.OutputModInput;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.ClampRangeMin = pPhysGrav.BaseVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.ClampRangeMax = pPhysGrav.RangeVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.IsRanged = pPhysGrav.UseRange;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[pPhysGrav.FuncType];
                            WriteFunctionCurve(tagFile, pPhysGrav, $"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[0]/Struct:property", pPhysGrav.UseRange);

                            // Air Friction
                            ((TagFieldBlock)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters")).AddElement();
                            ((TagFieldElementInteger)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/LongInteger:parameter id")).Data = 1;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/CharEnum:Input Variable")).Value = pPhysAirFric.InputVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/CharEnum:Range Variable")).Value = pPhysAirFric.RangeVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/CharEnum:Output Modifier")).Value = pPhysAirFric.OutputMod;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/CharEnum:Output Modifier Input")).Value = pPhysAirFric.OutputModInput;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.ClampRangeMin = pPhysAirFric.BaseVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.ClampRangeMax = pPhysAirFric.RangeVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.IsRanged = pPhysAirFric.UseRange;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[pPhysAirFric.FuncType];
                            WriteFunctionCurve(tagFile, pPhysAirFric, $"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property", pPhysAirFric.UseRange);

                            // Rotational Friction
                            ((TagFieldBlock)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters")).AddElement();
                            ((TagFieldElementInteger)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/LongInteger:parameter id")).Data = 2;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/CharEnum:Input Variable")).Value = pPhysRotFric.InputVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/CharEnum:Range Variable")).Value = pPhysRotFric.RangeVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/CharEnum:Output Modifier")).Value = pPhysRotFric.OutputMod;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/CharEnum:Output Modifier Input")).Value = pPhysRotFric.OutputModInput;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/Custom:Mapping")).Value.ClampRangeMin = pPhysRotFric.BaseVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/Custom:Mapping")).Value.ClampRangeMax = pPhysRotFric.RangeVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/Custom:Mapping")).Value.IsRanged = pPhysRotFric.UseRange;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[pPhysRotFric.FuncType];
                            WriteFunctionCurve(tagFile, pPhysRotFric, $"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property", pPhysRotFric.UseRange);

                            if (pPhysCollFric != null)
                            {
                                // Collision Friction
                                ((TagFieldBlock)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements")).AddElement();
                                ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/ShortEnum:type")).Value = 1;
                                ((TagFieldBlock)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters")).AddElement();
                                ((TagFieldElementInteger)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/LongInteger:parameter id")).Data = 0;
                                ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/Struct:property/CharEnum:Input Variable")).Value = pPhysCollFric.InputVar;
                                ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/Struct:property/CharEnum:Range Variable")).Value = pPhysCollFric.RangeVar;
                                ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/Struct:property/CharEnum:Output Modifier")).Value = pPhysCollFric.OutputMod;
                                ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/Struct:property/CharEnum:Output Modifier Input")).Value = pPhysCollFric.OutputModInput;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.ClampRangeMin = pPhysCollFric.BaseVal;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.ClampRangeMax = pPhysCollFric.RangeVal;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.IsRanged = pPhysCollFric.UseRange;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[pPhysCollFric.FuncType];
                                WriteFunctionCurve(tagFile, pPhysCollFric, $"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/Struct:property", pPhysCollFric.UseRange);

                                // Collision Elasticity
                                ((TagFieldBlock)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters")).AddElement();
                                ((TagFieldElementInteger)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/LongInteger:parameter id")).Data = 1;
                                ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/CharEnum:Input Variable")).Value = pPhysCollElas.InputVar;
                                ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/CharEnum:Range Variable")).Value = pPhysCollElas.RangeVar;
                                ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/CharEnum:Output Modifier")).Value = pPhysCollElas.OutputMod;
                                ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/CharEnum:Output Modifier Input")).Value = pPhysCollElas.OutputModInput;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.ClampRangeMin = pPhysCollElas.BaseVal;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.ClampRangeMax = pPhysCollElas.RangeVal;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.IsRanged = pPhysCollElas.UseRange;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[pPhysCollElas.FuncType];
                                WriteFunctionCurve(tagFile, pPhysCollElas, $"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property", pPhysCollElas.UseRange);
                            }
                        }

                        // Particle Self-Acceleration
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle self-acceleration/Custom:Mapping")).Value.ClampRangeMin = 0;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle self-acceleration/Custom:Mapping")).Value.ClampRangeMax = 0;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle self-acceleration/Custom:Mapping")).Value.IsRanged = false;

                        // Particle Velocity
                        Halo2Function particleVel = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].Velocity;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle initial velocity/CharEnum:Input Variable")).Value = particleVel.InputVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle initial velocity/CharEnum:Range Variable")).Value = particleVel.RangeVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle initial velocity/CharEnum:Output Modifier")).Value = particleVel.OutputMod;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle initial velocity/CharEnum:Output Modifier Input")).Value = particleVel.OutputModInput;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle initial velocity/Custom:Mapping")).Value.ClampRangeMin = particleVel.BaseVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle initial velocity/Custom:Mapping")).Value.ClampRangeMax = particleVel.RangeVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle initial velocity/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[particleVel.FuncType];
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle initial velocity/Custom:Mapping")).Value.IsRanged = particleVel.UseRange;
                        WriteFunctionCurve(tagFile, particleVel, $"{emitter}/Struct:particle initial velocity", particleVel.UseRange);

                        // Particle Angular Velocity
                        Halo2Function particleAngVel = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].AngularVelocity;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle initial rotation rate/CharEnum:Input Variable")).Value = particleAngVel.InputVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle initial rotation rate/CharEnum:Range Variable")).Value = particleAngVel.RangeVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle initial rotation rate/CharEnum:Output Modifier")).Value = particleAngVel.OutputMod;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle initial rotation rate/CharEnum:Output Modifier Input")).Value = particleAngVel.OutputModInput;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle initial rotation rate/Custom:Mapping")).Value.ClampRangeMin = particleAngVel.BaseVal / 360.0f; // Degrees per second -> 360 degree rotations per second
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle initial rotation rate/Custom:Mapping")).Value.ClampRangeMax = particleAngVel.RangeVal / 360.0f; // Degrees per second -> 360 degree rotations per second
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle initial rotation rate/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[particleAngVel.FuncType];
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle initial rotation rate/Custom:Mapping")).Value.IsRanged = particleAngVel.UseRange;
                        WriteFunctionCurve(tagFile, particleAngVel, $"{emitter}/Struct:particle initial rotation rate", particleAngVel.UseRange);

                        // Particle Rotation
                        if (effect.Events[eventIndex].ParticleSystems[pSysIndex].Particle != null)
                        {
                            Halo2Function particleRotation = effect.Events[eventIndex].ParticleSystems[pSysIndex].Particle.Rotation;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle rotation/CharEnum:Input Variable")).Value = particleRotation.InputVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle rotation/CharEnum:Range Variable")).Value = particleRotation.RangeVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle rotation/CharEnum:Output Modifier")).Value = particleRotation.OutputMod;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle rotation/CharEnum:Output Modifier Input")).Value = particleRotation.OutputModInput;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle rotation/Custom:Mapping")).Value.ClampRangeMin = particleRotation.BaseVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle rotation/Custom:Mapping")).Value.ClampRangeMax = particleRotation.RangeVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle rotation/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[particleRotation.FuncType];
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle rotation/Custom:Mapping")).Value.IsRanged = particleRotation.UseRange;
                            WriteFunctionCurve(tagFile, particleRotation, $"{emitter}/Struct:particle rotation", particleRotation.UseRange);
                        }

                        // Particle Size
                        Halo2Function particleSize = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].Size;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle size/CharEnum:Input Variable")).Value = particleSize.InputVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle size/CharEnum:Range Variable")).Value = particleSize.RangeVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle size/CharEnum:Output Modifier")).Value = particleSize.OutputMod;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle size/CharEnum:Output Modifier Input")).Value = particleSize.OutputModInput;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle size/Custom:Mapping")).Value.ClampRangeMin = particleSize.BaseVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle size/Custom:Mapping")).Value.ClampRangeMax = particleSize.RangeVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle size/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[particleSize.FuncType];
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle size/Custom:Mapping")).Value.IsRanged = particleSize.UseRange;
                        WriteFunctionCurve(tagFile, particleSize, $"{emitter}/Struct:particle size", particleSize.UseRange);

                        // Particle Scale
                        if (effect.Events[eventIndex].ParticleSystems[pSysIndex].Particle != null)
                        {
                            Halo2Function particleScale = effect.Events[eventIndex].ParticleSystems[pSysIndex].Particle.Scale;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle scale/CharEnum:Input Variable")).Value = particleScale.InputVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle scale/CharEnum:Range Variable")).Value = particleScale.RangeVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle scale/CharEnum:Output Modifier")).Value = particleScale.OutputMod;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle scale/CharEnum:Output Modifier Input")).Value = particleScale.OutputModInput;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle scale/Custom:Mapping")).Value.ClampRangeMin = particleScale.BaseVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle scale/Custom:Mapping")).Value.ClampRangeMax = particleScale.RangeVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle scale/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[particleScale.FuncType];
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle scale/Custom:Mapping")).Value.IsRanged = particleScale.UseRange;
                            WriteFunctionCurve(tagFile, particleScale, $"{emitter}/Struct:particle scale", particleScale.UseRange);
                        }

                        // Particle Tint
                        Halo2Function particleTint = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].Tint;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle tint/CharEnum:Input Variable")).Value = particleTint.InputVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle tint/CharEnum:Range Variable")).Value = particleTint.RangeVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle tint/CharEnum:Output Modifier")).Value = particleTint.OutputMod;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle tint/CharEnum:Output Modifier Input")).Value = particleTint.OutputModInput;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle tint/Custom:Mapping")).Value.IsRanged = particleTint.UseRange;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle tint/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[particleTint.FuncType];
                        if (particleTint.Colour1 != null)
                        {
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle tint/Custom:Mapping")).Value.SetColor(0, particleTint.Colour1);
                        }
                        else
                        {
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle tint/Custom:Mapping")).Value.SetColor(0, GameColor.FromRgb(1, 1, 1));
                        }

                        if (particleTint.ColourType == 2)
                        {
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle tint/Custom:Mapping")).Value.ColorGraphType = FunctionEditorColorGraphType.TwoColor;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle tint/Custom:Mapping")).Value.SetColor(1, particleTint.Colour2);
                        }
                        else if (particleTint.ColourType == 3)
                        {
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle tint/Custom:Mapping")).Value.ColorGraphType = FunctionEditorColorGraphType.ThreeColor;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle tint/Custom:Mapping")).Value.SetColor(1, particleTint.Colour2);
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle tint/Custom:Mapping")).Value.SetColor(2, particleTint.Colour3);
                        }
                        else if (particleTint.ColourType == 4)
                        {
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle tint/Custom:Mapping")).Value.ColorGraphType = FunctionEditorColorGraphType.FourColor;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle tint/Custom:Mapping")).Value.SetColor(1, particleTint.Colour2);
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle tint/Custom:Mapping")).Value.SetColor(2, particleTint.Colour3);
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle tint/Custom:Mapping")).Value.SetColor(3, particleTint.Colour4);
                        }
                        WriteFunctionCurve(tagFile, particleTint, $"{emitter}/Struct:particle tint", particleTint.UseRange);


                        // Particle Alpha
                        Halo2Function particleAlpha = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].Alpha;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle alpha/CharEnum:Input Variable")).Value = particleAlpha.InputVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle alpha/CharEnum:Range Variable")).Value = particleAlpha.RangeVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle alpha/CharEnum:Output Modifier")).Value = particleAlpha.OutputMod;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle alpha/CharEnum:Output Modifier Input")).Value = particleAlpha.OutputModInput;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle alpha/Custom:Mapping")).Value.ClampRangeMin = particleAlpha.BaseVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle alpha/Custom:Mapping")).Value.ClampRangeMax = particleAlpha.RangeVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle alpha/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[particleAlpha.FuncType];
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle alpha/Custom:Mapping")).Value.IsRanged = particleAlpha.UseRange;
                        WriteFunctionCurve(tagFile, particleAlpha, $"{emitter}/Struct:particle alpha", particleAlpha.UseRange);
                    }
                }
            }
        }

        public static void WriteH3Particle(string h3ekPath, string outputPath, Particle particle)
        {
            EnsureProjectInitialised(h3ekPath);

            // Another effect converted earlier may have created this already
            if (File.Exists(GetH3TagFilePath(h3ekPath, outputPath, "particle"))) return;

            TagPath outputTagPath = TagPath.FromPathAndExtension(outputPath, "particle");
            TagFile tagFile = new TagFile();
            try
            {
                tagFile.New(outputTagPath);
                WriteParticleData(tagFile, particle);
                tagFile.Save();
            }
            finally
            {
                tagFile.Dispose();
            }
        }

        private static void WriteParticleData(TagFile tagFile, Particle particle)
        {
            uint mainFlagsVal = 0, appearFlagsVal = 0, animFlagsVal = 0;
            if (particle.Flags[0] != "0")
            {
                for (int i = 1; i < particle.Flags.Length; i++) // Start at one to skip the value and go straight to the actual flags
                {
                    if (Utils.particleMainFlagValues.ContainsKey(particle.Flags[i]))
                    {
                        mainFlagsVal += Utils.particleMainFlagValues[particle.Flags[i]];
                    }
                    else if (Utils.particleAppearFlagValues.ContainsKey(particle.Flags[i]))
                    {
                        appearFlagsVal += Utils.particleAppearFlagValues[particle.Flags[i]];
                    }
                    else if (Utils.particleAnimFlagValues.ContainsKey(particle.Flags[i]))
                    {
                        animFlagsVal += Utils.particleAnimFlagValues[particle.Flags[i]];
                    }
                }
            }

            ((TagFieldFlags)tagFile.SelectField($"Flags:main flags")).RawValue = mainFlagsVal;
            ((TagFieldFlags)tagFile.SelectField($"Flags:appearance flags")).RawValue = appearFlagsVal;
            ((TagFieldFlags)tagFile.SelectField($"Flags:animation flags")).RawValue = animFlagsVal;
            ((TagFieldEnum)tagFile.SelectField($"ShortEnum:particle billboard style")).Value = Utils.billboardTypeMapping[particle.Billboard];
            ((TagFieldElementInteger)tagFile.SelectField($"ShortInteger:first sequence index")).Data = particle.FirstSeqIndex;
            ((TagFieldElementInteger)tagFile.SelectField($"ShortInteger:sequence count")).Data = particle.SeqCount;

            // Shader
            ((TagFieldBlock)tagFile.SelectField($"Struct:actual shader?/Block:parameters")).AddElement();
            ((TagFieldElementStringID)tagFile.SelectField($"Struct:actual shader?/Block:parameters[0]/StringId:parameter name")).Data = "base_map";
            ((TagFieldEnum)tagFile.SelectField($"Struct:actual shader?/Block:parameters[0]/LongEnum:parameter type")).Value = 0;
            if (particle.Bitmap != null)
            {
                ((TagFieldReference)tagFile.SelectField($"Struct:actual shader?/Block:parameters[0]/Reference:bitmap")).Path = TagPath.FromPathAndExtension(Path.Combine("halo_2\\", particle.Bitmap.Path), "bitmap");
            }
            int alphaMode = Utils.shaderTemplateBlendMode[Path.GetFileName(particle.ShaderTemplate)];
            if (alphaMode == 50)
            {
                // Plasma ? disable alpha for now
                ((TagFieldElementInteger)tagFile.SelectField($"Struct:actual shader?/Block:options[1]/ShortInteger:short")).Data = 0;
            }
            else if (alphaMode == 99)
            {
                // Alpha test
                // Don't have alpha test for H3 particles - use alpha blend?
                ((TagFieldElementInteger)tagFile.SelectField($"Struct:actual shader?/Block:options[1]/ShortInteger:short")).Data = 3;
            }
            else
            {
                // Blend mode
                ((TagFieldElementInteger)tagFile.SelectField($"Struct:actual shader?/Block:options[1]/ShortInteger:short")).Data = alphaMode;
            }

            // Colour
            ((TagFieldEnum)tagFile.SelectField($"Struct:color/CharEnum:Input Variable")).Value = particle.Colour.InputVar;
            ((TagFieldEnum)tagFile.SelectField($"Struct:color/CharEnum:Range Variable")).Value = particle.Colour.RangeVar;
            ((TagFieldEnum)tagFile.SelectField($"Struct:color/CharEnum:Output Modifier")).Value = particle.Colour.OutputMod;
            ((TagFieldEnum)tagFile.SelectField($"Struct:color/CharEnum:Output Modifier Input")).Value = particle.Colour.OutputModInput;
            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:color/Custom:Mapping")).Value.IsRanged = particle.Colour.UseRange;
            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:color/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[particle.Colour.FuncType];
            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:color/Custom:Mapping")).Value.SetColor(0, particle.Colour.Colour1);
            if (particle.Colour.ColourType == 2)
            {
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:color/Custom:Mapping")).Value.ColorGraphType = FunctionEditorColorGraphType.TwoColor;
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:color/Custom:Mapping")).Value.SetColor(1, particle.Colour.Colour2);
            }
            else if (particle.Colour.ColourType == 3)
            {
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:color/Custom:Mapping")).Value.ColorGraphType = FunctionEditorColorGraphType.ThreeColor;
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:color/Custom:Mapping")).Value.SetColor(1, particle.Colour.Colour2);
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:color/Custom:Mapping")).Value.SetColor(2, particle.Colour.Colour3);
            }
            else if (particle.Colour.ColourType == 4)
            {
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:color/Custom:Mapping")).Value.ColorGraphType = FunctionEditorColorGraphType.FourColor;
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:color/Custom:Mapping")).Value.SetColor(1, particle.Colour.Colour2);
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:color/Custom:Mapping")).Value.SetColor(2, particle.Colour.Colour3);
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:color/Custom:Mapping")).Value.SetColor(3, particle.Colour.Colour4);
            }
            WriteFunctionCurve(tagFile, particle.Colour, "Struct:color", particle.Colour.UseRange);

            // Alpha
            ((TagFieldEnum)tagFile.SelectField($"Struct:alpha/CharEnum:Input Variable")).Value = particle.Alpha.InputVar;
            ((TagFieldEnum)tagFile.SelectField($"Struct:alpha/CharEnum:Range Variable")).Value = particle.Alpha.RangeVar;
            ((TagFieldEnum)tagFile.SelectField($"Struct:alpha/CharEnum:Output Modifier")).Value = particle.Alpha.OutputMod;
            ((TagFieldEnum)tagFile.SelectField($"Struct:alpha/CharEnum:Output Modifier Input")).Value = particle.Alpha.OutputModInput;
            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:alpha/Custom:Mapping")).Value.ClampRangeMin = (particle.Alpha.BaseVal <= 0.0f) ? 1.0f : particle.Alpha.BaseVal;
            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:alpha/Custom:Mapping")).Value.ClampRangeMax = (particle.Alpha.RangeVal <= 0.0f) ? 1.0f : particle.Alpha.RangeVal;
            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:alpha/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[particle.Alpha.FuncType];
            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:alpha/Custom:Mapping")).Value.IsRanged = particle.Alpha.UseRange;
            WriteFunctionCurve(tagFile, particle.Alpha, "Struct:alpha", particle.Alpha.UseRange);

            // Frame Index
            ((TagFieldEnum)tagFile.SelectField($"Struct:frame index/CharEnum:Input Variable")).Value = particle.FrameIndex.InputVar;
            ((TagFieldEnum)tagFile.SelectField($"Struct:frame index/CharEnum:Range Variable")).Value = particle.FrameIndex.RangeVar;
            ((TagFieldEnum)tagFile.SelectField($"Struct:frame index/CharEnum:Output Modifier")).Value = particle.FrameIndex.OutputMod;
            ((TagFieldEnum)tagFile.SelectField($"Struct:frame index/CharEnum:Output Modifier Input")).Value = particle.FrameIndex.OutputModInput;
            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:frame index/Custom:Mapping")).Value.ClampRangeMin = particle.FrameIndex.BaseVal;
            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:frame index/Custom:Mapping")).Value.ClampRangeMax = particle.FrameIndex.RangeVal;
            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:frame index/Custom:Mapping")).Value.MasterType = Utils.funcTypeMapping[particle.FrameIndex.FuncType];
            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Struct:frame index/Custom:Mapping")).Value.IsRanged = particle.FrameIndex.UseRange;
            WriteFunctionCurve(tagFile, particle.FrameIndex, "Struct:frame index", particle.FrameIndex.UseRange);
        }
    
        public static void WriteFunctionCurve(TagFile tagFile, Halo2Function func, string fieldString, bool useRange)
        {
            if (Utils.funcTypeMapping[func.FuncType] == FunctionEditorMasterType.Curve)
            {
                if (func.CurvePoints.IsLinearKey)
                {
                    // Linear key
                    GamePoint2d baseY0 = new GamePoint2d(0, func.CurvePoints.BaseY0);
                    GamePoint2d baseY1 = new GamePoint2d((float)func.CurvePoints.BaseX1, func.CurvePoints.BaseY1);
                    GamePoint2d baseY2 = new GamePoint2d((float)func.CurvePoints.BaseX2, (float)func.CurvePoints.BaseY2);
                    GamePoint2d baseY3 = new GamePoint2d(1, (float)func.CurvePoints.BaseY3);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.InsertControlPoint(0, baseY1.X);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.InsertControlPoint(0, baseY2.X);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(0, 0, baseY0);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(0, 1, baseY1);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(0, 2, baseY2);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(0, 3, baseY3);

                    if (useRange)
                    {
                        GamePoint2d rangeY0 = new GamePoint2d(0, func.CurvePoints.RangeY0);
                        GamePoint2d rangeY1 = new GamePoint2d((float)func.CurvePoints.RangeX1, func.CurvePoints.RangeY1);
                        GamePoint2d rangeY2 = new GamePoint2d((float)func.CurvePoints.RangeX2, (float)func.CurvePoints.RangeY2);
                        GamePoint2d rangeY3 = new GamePoint2d(1, (float)func.CurvePoints.RangeY3);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.InsertControlPoint(1, rangeY1.X);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.InsertControlPoint(1, rangeY2.X);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(1, 0, rangeY0);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(1, 1, rangeY1);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(1, 2, rangeY2);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(1, 3, rangeY3);
                    }
                }
                else if (func.CurvePoints.IsSpline1 || func.CurvePoints.IsSpline2)
                {
                    // Spline

                    FunctionEditorSegmentType splineType = FunctionEditorSegmentType.Spline;

                    if (func.CurvePoints.IsSpline2)
                    {
                        splineType = FunctionEditorSegmentType.Spline2;
                    }

                    GamePoint2d startY = new GamePoint2d(0, func.CurvePoints.BaseY0);
                    GamePoint2d endY = new GamePoint2d(1, func.CurvePoints.BaseY1);
                    GamePoint2d handle1 = new GamePoint2d(func.CurvePoints.BaseHandle1X, func.CurvePoints.BaseHandle1Y);
                    GamePoint2d handle2 = new GamePoint2d(func.CurvePoints.BaseHandle2X, func.CurvePoints.BaseHandle2Y);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetSegmentType(0, 0, splineType);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(0, 0, startY);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(0, 1, handle1);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(0, 3, endY);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(0, 2, handle2);

                    if (useRange)
                    {
                        GamePoint2d rangeStartY = new GamePoint2d(0, func.CurvePoints.RangeY0);
                        GamePoint2d rangeEndY = new GamePoint2d(1, func.CurvePoints.RangeY1);
                        GamePoint2d rangeHandle1 = new GamePoint2d(func.CurvePoints.RangeHandle1X, func.CurvePoints.RangeHandle1Y);
                        GamePoint2d rangeHandle2 = new GamePoint2d(func.CurvePoints.RangeHandle2X, func.CurvePoints.RangeHandle2Y);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetSegmentType(1, 0, splineType);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(1, 0, rangeStartY);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(1, 1, rangeHandle1);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(1, 2, rangeHandle2);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(1, 3, rangeEndY);
                    }
                }
                else
                {
                    // Linear
                    GamePoint2d baseY0 = new GamePoint2d(0, func.CurvePoints.BaseY0);
                    GamePoint2d baseY1 = new GamePoint2d(1, func.CurvePoints.BaseY1);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(0, 0, baseY0);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(0, 1, baseY1);

                    if (useRange)
                    {
                        GamePoint2d rangeY0 = new GamePoint2d(0, func.CurvePoints.RangeY0);
                        GamePoint2d rangeY1 = new GamePoint2d(1, func.CurvePoints.RangeY1);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(1, 0, rangeY0);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetControlPoint(1, 1, rangeY1);
                    }
                }
            }
            else if (Utils.funcTypeMapping[func.FuncType] == FunctionEditorMasterType.Exponent)
            {
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMin(0, func.CurvePoints.BaseMin);
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMax(0, func.CurvePoints.BaseMax);
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetExponent(0, func.CurvePoints.BaseExponent);

                if (useRange)
                {
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMin(1, (float)func.CurvePoints.RangeMin);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMax(1, (float)func.CurvePoints.RangeMax);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetExponent(1, (float)func.CurvePoints.RangeExponent);
                }
            }
            else if (Utils.funcTypeMapping[func.FuncType] == FunctionEditorMasterType.Periodic)
            {
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMin(0, func.CurvePoints.BaseMin);
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMax(0, func.CurvePoints.BaseMax);
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetFrequency(0, func.CurvePoints.BaseFreq);
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetPhase(0, func.CurvePoints.BasePhase);
                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetFunctionIndex(0, (byte)func.CurvePoints.BaseGraphFunc);

                if (useRange)
                {
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMin(1, (float)func.CurvePoints.RangeMin);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMax(1, (float)func.CurvePoints.RangeMax);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetFrequency(1, (float)func.CurvePoints.RangeFreq);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetPhase(1, (float)func.CurvePoints.RangePhase);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetFunctionIndex(1, (byte)func.CurvePoints.RangeGraphFunc);
                }
            }
            else if (Utils.funcTypeMapping[func.FuncType] == FunctionEditorMasterType.Transition)
            {
                if (func.FuncType == 0)
                {
                    // "Basic" transition function
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMin(0, 0);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMax(0, 1);

                    if (useRange)
                    {
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMin(1, 0);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMax(1, 1);
                    }
                }
                else
                {
                    // Normal transition function
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMin(0, func.CurvePoints.BaseMin);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMax(0, func.CurvePoints.BaseMax);
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetFunctionIndex(0, (byte)func.CurvePoints.BaseGraphFunc);

                    if (useRange)
                    {
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMin(1, (float)func.CurvePoints.RangeMin);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetAmplitudeMax(1, (float)func.CurvePoints.RangeMax);
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{fieldString}/Custom:Mapping")).Value.SetFunctionIndex(1, (byte)func.CurvePoints.RangeGraphFunc);
                    }
                }
            }
        }
    }
}
