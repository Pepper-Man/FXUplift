using Bungie;
using Bungie.Tags;
using System;
using System.Collections.Generic;
using System.Linq;

namespace H2ToH3EffectConverter
{
    using Emitter = ReadFromH2.Emitter;
    using Halo2Function = ReadFromH2.Halo2Function;
    using Effect = ReadFromH2.Effect;
    using ParticlePhysics = ReadFromH2.ParticlePhysics;

    internal class WriteToH3
    {
        public static void WriteH3Effect(string h3ekPath, string outputPath, Effect effect)
        {
            ManagedBlamSystem.InitializeProject(InitializationType.TagsOnly, h3ekPath);
            TagPath outputTagPath = TagPath.FromPathAndExtension(outputPath, "effect");
            TagFile tagFile = new TagFile();
            tagFile.New(outputTagPath);

            Console.WriteLine("Writing tag data");

            // MARKERS ----------------------------------------------------------------------------------------
            int markerCount = effect.Markers.Count();
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
                    ((TagFieldFlags)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/WordFlags:flags")).RawValue= effect.Events[eventIndex].Parts[partsIndex].Flags;
                    ((TagFieldReference)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/Reference:type")).Path = TagPath.FromPathAndType(effect.Events[eventIndex].Parts[partsIndex].TagRef.Path, effect.Events[eventIndex].Parts[partsIndex].TagRef.Extension);
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/RealBounds:velocity bounds")).Data = effect.Events[eventIndex].Parts[partsIndex].VelocityBounds;
                    ((TagFieldElementSingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/Angle:velocity cone angle")).Data = effect.Events[eventIndex].Parts[partsIndex].VelocityConeAngle;
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/AngleBounds:angular velocity bounds")).Data = effect.Events[eventIndex].Parts[partsIndex].AngularVelocityBounds;
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/RealBounds:radius modifier bounds")).Data = effect.Events[eventIndex].Parts[partsIndex].RadiusModifierBounds;
                    ((TagFieldFlags)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/Flags:A scales values")).RawValue = effect.Events[eventIndex].Parts[partsIndex].AScalesVals;
                    ((TagFieldFlags)tagFile.SelectField($"Block:events[{eventIndex}]/Block:parts[{partsIndex}]/Flags:B scales values")).RawValue = effect.Events[eventIndex].Parts[partsIndex].BScalesVals;
                }

                // PARTICLE SYSTEMS ----------------------------------------------------------------------------
                int particleSysCount = effect.Events[eventIndex].ParticleSystems.Count();
                for (int pSysIndex = 0;  pSysIndex < particleSysCount; pSysIndex++)
                {
                    ((TagFieldBlock)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems")).AddElement();
                    ((TagFieldReference)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/Reference:particle")).Path = TagPath.FromPathAndType(effect.Events[eventIndex].ParticleSystems[pSysIndex].ParticleRef.Path, effect.Events[eventIndex].ParticleSystems[pSysIndex].ParticleRef.Extension);
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
                    for (int emitterIndex  = 0; emitterIndex < emitterCount; emitterIndex++)
                    {
                        string emitter = $"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/Block:emitters[{emitterIndex}]";
                        ((TagFieldBlock)tagFile.SelectField($"Block:events[{eventIndex}]/Block:particle systems[{pSysIndex}]/Block:emitters")).AddElement();
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/CharEnum:emission shape")).Value = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].EmissionShape;
                        Emitter emitterData = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex];

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
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission radius/Custom:Mapping")).Value.ClampRangeMin = emissionRadius.baseVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission radius/Custom:Mapping")).Value.ClampRangeMax = emissionRadius.rangeVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission radius/Custom:Mapping")).Value.IsRanged = emissionRadius.useRange;

                        // Emission Angle
                        Halo2Function emissionAngle = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].Angle;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:emission angle/CharEnum:Input Variable")).Value = emissionAngle.InputVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:emission angle/CharEnum:Range Variable")).Value = emissionAngle.RangeVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:emission angle/CharEnum:Output Modifier")).Value = emissionAngle.OutputMod;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:emission angle/CharEnum:Output Modifier Input")).Value = emissionAngle.OutputModInput;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission angle/Custom:Mapping")).Value.ClampRangeMin = emissionAngle.baseVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission angle/Custom:Mapping")).Value.ClampRangeMax = emissionAngle.rangeVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:emission angle/Custom:Mapping")).Value.IsRanged = emissionAngle.useRange;

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
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle emission rate/Custom:Mapping")).Value.ClampRangeMin = emissionRate.baseVal * 30; // particles per tick -> particles per second
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle emission rate/Custom:Mapping")).Value.ClampRangeMax = emissionRate.rangeVal * 30; // particles per tick -> particles per second
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle emission rate/Custom:Mapping")).Value.IsRanged = emissionRate.useRange;

                        // Particle Lifespan
                        Halo2Function lifespan = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].Lifespan;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle lifespan/CharEnum:Input Variable")).Value = lifespan.InputVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle lifespan/CharEnum:Range Variable")).Value = lifespan.RangeVar;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle lifespan/CharEnum:Output Modifier")).Value = lifespan.OutputMod;
                        ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle lifespan/CharEnum:Output Modifier Input")).Value = lifespan.OutputModInput;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle lifespan/Custom:Mapping")).Value.ClampRangeMin = lifespan.baseVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle lifespan/Custom:Mapping")).Value.ClampRangeMax = lifespan.rangeVal;
                        ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle lifespan/Custom:Mapping")).Value.IsRanged = lifespan.useRange;

                        // Particle Physics
                        ParticlePhysics pPhysics = effect.Events[eventIndex].ParticleSystems[pSysIndex].Emitters[emitterIndex].ParticlePhys;

                        if (pPhysics != null)
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
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.ClampRangeMin = pPhysGrav.baseVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.ClampRangeMax = pPhysGrav.rangeVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.IsRanged = pPhysGrav.useRange;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.MasterType = 0;

                            // Air Friction
                            ((TagFieldBlock)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters")).AddElement();
                            ((TagFieldElementInteger)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/LongInteger:parameter id")).Data = 1;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/CharEnum:Input Variable")).Value = pPhysAirFric.InputVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/CharEnum:Range Variable")).Value = pPhysAirFric.RangeVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/CharEnum:Output Modifier")).Value = pPhysAirFric.OutputMod;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/CharEnum:Output Modifier Input")).Value = pPhysAirFric.OutputModInput;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.ClampRangeMin = pPhysAirFric.baseVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.ClampRangeMax = pPhysAirFric.rangeVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.IsRanged = pPhysAirFric.useRange;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.MasterType = 0;

                            // Rotational Friction
                            ((TagFieldBlock)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters")).AddElement();
                            ((TagFieldElementInteger)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/LongInteger:parameter id")).Data = 2;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/CharEnum:Input Variable")).Value = pPhysRotFric.InputVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/CharEnum:Range Variable")).Value = pPhysRotFric.RangeVar;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/CharEnum:Output Modifier")).Value = pPhysRotFric.OutputMod;
                            ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/CharEnum:Output Modifier Input")).Value = pPhysRotFric.OutputModInput;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/Custom:Mapping")).Value.ClampRangeMin = pPhysRotFric.baseVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/Custom:Mapping")).Value.ClampRangeMax = pPhysRotFric.rangeVal;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/Custom:Mapping")).Value.IsRanged = pPhysRotFric.useRange;
                            ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[0]/Block:parameters[2]/Struct:property/Custom:Mapping")).Value.MasterType = 0;

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
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.ClampRangeMin = pPhysCollFric.baseVal;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.ClampRangeMax = pPhysCollFric.rangeVal;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.IsRanged = pPhysCollFric.useRange;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[0]/Struct:property/Custom:Mapping")).Value.MasterType = 0;

                                // Collision Elasticity
                                ((TagFieldBlock)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters")).AddElement();
                                ((TagFieldElementInteger)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/LongInteger:parameter id")).Data = 1;
                                ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/CharEnum:Input Variable")).Value = pPhysCollElas.InputVar;
                                ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/CharEnum:Range Variable")).Value = pPhysCollElas.RangeVar;
                                ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/CharEnum:Output Modifier")).Value = pPhysCollElas.OutputMod;
                                ((TagFieldEnum)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/CharEnum:Output Modifier Input")).Value = pPhysCollElas.OutputModInput;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.ClampRangeMin = pPhysCollElas.baseVal;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.ClampRangeMax = pPhysCollElas.rangeVal;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.IsRanged = pPhysCollElas.useRange;
                                ((TagFieldCustomFunctionEditor)tagFile.SelectField($"{emitter}/Struct:particle movement/Block:movements[1]/Block:parameters[1]/Struct:property/Custom:Mapping")).Value.MasterType = 0;
                            }
                            
                        }
                    }
                }
            }


            tagFile.Save();
        }
    }
}
