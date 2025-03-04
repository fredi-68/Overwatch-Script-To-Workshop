﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Elements
{
    [AttributeUsage(AttributeTargets.Enum)]
    public class EnumParameter : Attribute
    {
        public EnumParameter() 
        { 

        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class EnumOverride : Attribute
    {
        public EnumOverride(string codeName, string workshopName)
        {
            CodeName = codeName;
            WorkshopName = workshopName;
        }
        public string CodeName { get; private set; }
        public string WorkshopName { get; private set; }
    }

    public class EnumData
    {
        private static EnumData[] AllEnums = null;

        private static EnumData[] GetEnumData()
        {
            if (AllEnums == null)
            {
                Type[] enums = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<EnumParameter>() != null).ToArray();
                AllEnums = new EnumData[enums.Length];

                for (int i = 0; i < AllEnums.Length; i++)
                    AllEnums[i] = new EnumData(enums[i]);
            }
            return AllEnums;
        }

        public static bool IsEnum(string codeName)
        {
            if (codeName == null)
                return false;
            return GetEnum(codeName) != null;
        }

        public static EnumData GetEnum(string codeName)
        {
            return GetEnumData().FirstOrDefault(e => e.CodeName == codeName);
        }

        public static EnumData GetEnum(Type type)
        {
            return GetEnumData().FirstOrDefault(e => e.Type == type);
        }

        public static EnumData GetEnum<T>()
        {
            return GetEnum(typeof(T));
        }

        public static EnumMember GetEnumValue(string enumCodeName, string valueCodeName)
        {
            return GetEnum(enumCodeName)?.GetEnumMember(valueCodeName);
        }

        public static EnumMember GetEnumValue(object enumValue)
        {
            return GetEnum(enumValue.GetType()).GetEnumMember(enumValue.ToString());
        }

        public static CompletionItem[] GetAllEnumCompletion()
        {
            return GetEnumData().Select(e => new CompletionItem(e.CodeName) { kind = CompletionItem.Enum }).ToArray();
        }

        public static Element Special(EnumMember enumMember)
        {
            // This converts enums with special properties to an Element.

            switch(enumMember.Enum.CodeName)
            {
                case "Hero":
                    return Element.Part<V_HeroVar>(enumMember);
                
                case "Map":
                    return new V_Number((int)enumMember.UnderlyingValue);

                default: return null;
            }
        }

        public string CodeName { get; private set; }
        public EnumMember[] Members { get; private set; }
        public Type Type { get; private set; }
        public Type UnderlyingType { get; private set; }

        public EnumData(Type type)
        {
            EnumOverride data = type.GetCustomAttribute<EnumOverride>();
            CodeName = data?.CodeName ?? type.Name;

            Type = type;
            UnderlyingType = Enum.GetUnderlyingType(type);

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            Members = new EnumMember[fields.Length];
            var values = Enum.GetValues(type);

            for (int v = 0; v < Members.Length; v++)
            {
                EnumOverride fieldData = fields[v].GetCustomAttribute<EnumOverride>();
                string fieldCodeName     = fieldData?.CodeName     ?? fields[v].Name;
                string fieldWorkshopName = fieldData?.WorkshopName ?? Extras.AddSpacesToSentence(fields[v].Name, false);

                Members[v] = new EnumMember(this, fieldCodeName, fieldWorkshopName, values.GetValue(v));
            }
        }

        public bool IsEnumMember(string codeName)
        {
            return GetEnumMember(codeName) != null;
        }

        public EnumMember GetEnumMember(string codeName)
        {
            return Members.FirstOrDefault(m => m.CodeName == codeName);
        }

        public CompletionItem[] GetCompletion()
        {
            return Members.Select(value =>
                new CompletionItem(value.CodeName) { kind = CompletionItem.EnumMember }
            ).ToArray();
        }
    }

    public class EnumMember : IWorkshopTree
    {
        public EnumData @Enum { get; private set; }
        public string CodeName { get; private set; }
        public string WorkshopName { get; private set; }
        public object UnderlyingValue { get; private set; }

        public EnumMember(EnumData @enum, string codeName, string workshopName, object value)
        {
            @Enum = @enum;
            CodeName = codeName;
            WorkshopName = workshopName;
            UnderlyingValue = System.Convert.ChangeType(value, @Enum.UnderlyingType);
        }

        public string ToWorkshop()
        {
            return WorkshopName;
        }

        public void DebugPrint(Log log, int depth)
        {
            log.Write(LogLevel.Verbose, Extras.Indent(depth, false) + WorkshopName);
        }
    }

    [EnumParameter]
    public enum RuleEvent
    {
        [EnumOverride(null, "Ongoing - Global")]
        OngoingGlobal,
        [EnumOverride(null, "Ongoing - Each Player")]
        OngoingPlayer,

        [EnumOverride(null, "Player earned elimination")]
        OnElimination,
        [EnumOverride(null, "Player dealt final blow")]
        OnFinalBlow,

        [EnumOverride(null, "Player dealt damage")]
        OnDamageDealt,
        [EnumOverride(null, "Player took damage")]
        OnDamageTaken,

        [EnumOverride(null, "Player died")]
        OnDeath
    }

    [EnumParameter]
    public enum PlayerSelector
    {
        All,
        Slot0,
        Slot1,
        Slot2,
        Slot3,
        Slot4,
        Slot5,
        Slot6,
        Slot7,
        Slot8,
        Slot9,
        Slot10,
        Slot11,
        // Why isn't it alphabetical? we will never know.
        Reaper,
        Tracer,
        Mercy,
        Hanzo,
        Torbjorn,
        Reinhardt,
        Pharah,
        Winston,
        Widowmaker,
        Bastion,
        Symmetra,
        Zenyatta,
        Gengi,
        Roadhog,
        Mccree,
        Junkrat,
        Zarya,
        [EnumOverride(null, "Soldier: 76")]
        Soldier76,
        Lucio,
        Dva,
        Mei,
        Sombra,
        Doomfist,
        Ana,
        Orisa,
        Brigitte,
        Moira,
        WreckingBall,
        Ashe,
        Baptiste
    }

    [EnumParameter]
    public enum Operators
    {
        [EnumOverride(null, "==")]
        Equal,
        [EnumOverride(null, "!=")]
        NotEqual,
        [EnumOverride(null, "<")]
        LessThan,
        [EnumOverride(null, "<=")]
        LessThanOrEqual,
        [EnumOverride(null, ">")]
        GreaterThan,
        [EnumOverride(null, ">=")]
        GreaterThanOrEqual
    }

    [EnumParameter]
    public enum Variable
    {
        A,
        B,
        C,
        D,
        E,
        F,
        G,
        H,
        I,
        J,
        K,
        L,
        M,
        N,
        O,
        P,
        Q,
        R,
        S,
        T,
        U,
        V,
        W,
        X,
        Y,
        Z
    }

    [EnumParameter]
    public enum Operation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        RaiseToPower,
        Min,
        Max,
        AppendToArray,
        RemoveFromArrayByValue,
        RemoveFromArrayByIndex
    }

    [EnumParameter]
    public enum Button
    {
        PrimaryFire,
        SecondaryFire,
        Ability1,
        Ability2,
        Ultimate,
        Interact,
        Jump,
        Crouch
    }

    [EnumParameter]
    public enum Relative
    {
        ToWorld,
        ToPlayer
    }

    [EnumParameter]
    public enum ContraryMotion
    {
        [EnumOverride(null, "Cancel Contrary Motion")]
        Cancel,
        [EnumOverride(null, "Incorporate Contrary Motion")]
        Incorporate
    }

    [EnumParameter]
    public enum RateChaseReevaluation
    {
        DestinationAndRate,
        None
    }

    [EnumParameter]
    public enum TimeChaseReevaluation
    {
        DestinationAndDuration,
        None
    }

    [EnumParameter]
    public enum Status
    {
        Hacked,
        Burning,
        KnockedDown,
        Asleep,
        Frozen,
        Unkillable,
        Invincible,
        PhasedOut,
        Rooted,
        Stunned
    }

    [EnumParameter]
    public enum TeamSelector
    {
        All,
        Team1,
        Team2,
    }

    [EnumParameter]
    public enum WaitBehavior
    {
        IgnoreCondition,
        AbortWhenFalse,
        RestartWhenTrue
    }

    [EnumParameter]
    public enum Effects
    {
        Sphere,
        LightShaft,
        Orb,
        Ring,
        Cloud,
        Sparkles,
        GoodAura,
        BadAura,
        EnergySound,
        PickupSound,
        GoodAuraSound,
        BadAuraSound,
        SparklesSound,
        SmokeSound,
        DecalSound,
        BeaconSound
    }

    [EnumParameter]
    public enum Color
    {
        White,
        Yellow,
        Green,
        Purple,
        Red,
        Blue,
        Team1,
        Team2
    }

    [EnumParameter]
    public enum EffectRev
    {
        VisibleToPositionAndRadius,
        PositionAndRadius,
        VisibleTo,
        None
    }

    [EnumParameter]
    public enum Rounding
    {
        Up,
        Down,
        Nearest
    }

    [EnumParameter]
    public enum Communication
    {
        VoiceLineUp,
        VoiceLineLeft,
        VoiceLineRight,
        VoiceLineDown,
        EmoteUp,
        EmoteLeft,
        EmoteRight,
        EmoteDown,
        UltimateStatus,
        Hello,
        NeedHealing,
        GroupUp,
        Thanks,
        Acknowledge
    }

    [EnumParameter]
    public enum Location
    {
        Left,
        Top,
        Right
    }

    [EnumParameter]
    public enum StringRev
    {
        VisibleToAndString,
        String
    }

    [EnumParameter]
    public enum Icon
    {
        [EnumOverride(null, "Arrow: Down")]
        ArrowDown,
        [EnumOverride(null, "Arrow: Left")]
        ArrowLeft,
        [EnumOverride(null, "Arrow: Right")]
        ArrowRight,
        [EnumOverride(null, "Arrow: Up")]
        ArrowUp,
        Aterisk,
        Bolt,
        Checkmark,
        Circle,
        Club,
        Diamond,
        Dizzy,
        ExclamationMark,
        Eye,
        Fire,
        Flag,
        Halo,
        Happy,
        Heart,
        Moon,
        No,
        Plus,
        Poison1,
        Poison2,
        QuestionMark,
        Radioactive,
        Recycle,
        RingThick,
        RingThin,
        Sad,
        Skull,
        Spade,
        Spiral,
        Stop,
        Trashcan,
        Warning,
        X
    }

    [EnumParameter]
    public enum IconRev
    {
        VisibleToAndPosition,
        Position,
        VisibleTo,
        None
    }

    [EnumParameter]
    public enum PlayEffects
    {
        GoodExplosion,
        BadExplosion,
        RingExplosion,
        GoodPickupEffect,
        BadPickupEffect,
        DebuffImpactSound,
        BuffImpactSound,
        RingExplosionSound,
        BuffExplosionSound,
        ExplosionSound
    }

    [EnumParameter]
    public enum Hero
    {
        Ana,
        Ashe,
        Baptiste,
        Bastion,
        Brigitte,
        Dva,
        Doomfist,
        Genji,
        Hanzo,
        Junkrat,
        Lucio,
        Mccree,
        Mei,
        Mercy,
        Moira,
        Orisa,
        Pharah,
        Reaper,
        Reinhardt,
        Roadhog,
        [EnumOverride(null, "Soldier: 76")]
        Soldier76,
        Sombra,
        Symmetra,
        Torbjorn,
        Tracer,
        Widowmaker,
        Winston,
        WreckingBall,
        Zarya,
        Zenyatta
    }

    [EnumParameter]
    public enum InvisibleTo
    {
        All,
        Enemies,
        None
    }

    [EnumParameter]
    public enum AccelerateRev
    {
        DirectionRateAndMaxSpeed,
        None
    }

    [EnumParameter]
    public enum ModRev
    {
        ReceiversDamagersAndDamagePercent,
        ReceiversAndDamagers,
        None
    }

    [EnumParameter]
    public enum FacingRev
    {
        DirectionAndTurnRate,
        None
    }

    [EnumParameter]
    public enum BarrierLOS
    {
        [EnumOverride(null, "Barriers Do Not Block LOS")]
        NoBarriersBlock,
        [EnumOverride(null, "Enemy Barriers Block LOS")]
        EnemyBarriersBlock,
        [EnumOverride(null, "All Barriers Block LOS")]
        AllBarriersBlock
    }

    [EnumParameter]
    public enum Transformation
    {
        Rotation,
        RotationAndTranslation
    }

    [EnumParameter]
    public enum RadiusLOS
    {
        Off,
        Surfaces,
        SurfacesAndEnemyBarriers,
        SurfacesAndAllBarriers
    }

    [EnumParameter]
    public enum LocalVector
    {
        Rotation,
        RotationAndTranslation
    }

    [EnumParameter]
    public enum Clipping
    {
        ClipAgainstSurfaces,
        DoNotClip
    }

    [EnumParameter]
    public enum InworldTextRev
    {
        VisibleToPositionAndString,
        VisibleToAndString,
        String
    }

    [EnumParameter]
    public enum Map
    {
        Black_Forest = 0,
        Blizzard_World = 1,
        Busan = 2,
        Castillo = 3,
        Chateau_Guillard = 4,
        Dorado = 5,
        Ecopoint_Antarctica = 6,
        Eichenwalde = 7,
        Hanamura = 8,
        Havana = 9,
        Hollywood = 10,
        Horizon_Lunar_Colony = 11,
        Ilios = 12,
        Junkertown = 13,
        Kings_Row = 14,
        Lijiang_Tower = 15,
        Necropolis = 16,
        Nepal = 17,
        Numbani = 18,
        Oasis = 19,
        Paris = 20,
        Petra = 21,
        Rialto = 22,
        Route_66 = 23,
        Temple_of_Anubis = 24,
        Volskaya_Industries = 25,
        Watchpoint_Gibraltar = 26,
        Ayutthaya = 27,
        Busan_Downtown = 28,
        Busan_Sanctuary = 29,
        Ilios_Lighthouse = 30,
        Ilios_Ruins = 31,
        Ilios_Well = 32,
        Lijiang_Control_Center = 33,
        Lijiang_Garden = 34,
        Lijiang_Night_Market = 35,
        Nepal_Sanctum = 36,
        Nepal_Shrine = 37,
        Nepal_Village = 38,
        Oasis_City_Center = 39,
        Oasis_Gardens = 40,
        Oasis_University = 41
    }
}
