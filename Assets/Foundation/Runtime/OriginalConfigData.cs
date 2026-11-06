// Generated data structures from original metadata and verified native FlatBuffer accessors.
// Values are stored in decoded JSON; this file contains no reconstructed gameplay methods.
using System;
namespace Whitebox.Foundation.ConfigData
{
    [Serializable]
    public sealed class FBlindsGroupData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x191354C
        public int Width;
        // vtable 6; native accessor 0x1913590
        public FMagnetPoint[] MagnetCells;
    }
    [Serializable]
    public sealed class FBoardGoalSaveData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1913A0C
        public short[] GoalType;
        // vtable 6; native accessor 0x1913B24
        public short[] LeftCount;
        // vtable 8; native accessor 0x1913C3C
        public short[] TargetCount;
    }
    [Serializable]
    public sealed class FBoardSaveData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x191436C
        public FCellGridSaveData GridData;
        // vtable 6; native accessor 0x191445C
        public FBoardGoalSaveData[] Goals;
        // vtable 8; native accessor 0x1914560
        public FItemModelCreatorSaveData ItemModelCreatorSaveData;
    }
    [Serializable]
    public sealed class FCellGridSaveData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1914990
        public FCellSaveData[] Grid;
        // vtable 6; native accessor 0x1914ACC
        public int Width;
        // vtable 8; native accessor 0x1914B10
        public int Height;
        // vtable 10; native accessor 0x1914B54
        public int[] ShifterData;
    }
    [Serializable]
    public sealed class FCellGroupData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1915164
        public FPoint[] Cells;
    }
    [Serializable]
    public sealed class FCellSaveData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1915578
        public FItemSaveData CurrentItem;
        // vtable 6; native accessor 0x1915668
        public FStaticItemSaveData[] StaticItems;
    }
    [Serializable]
    public sealed class FCryptexData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1915AB4
        public int Id;
        // vtable 6; native accessor 0x1915AF8
        public int[] ColorId;
        // vtable 8; native accessor 0x1915C10
        public int Order;
        // vtable 10; native accessor 0x1915C54
        public int[] Target;
        // vtable 12; native accessor 0x1915D6C
        public int CellCount;
        // vtable 14; native accessor 0x1915DB0
        public int Width;
        // vtable 16; native accessor 0x1915DF4
        public int Height;
        // vtable 18; native accessor 0x1915E38
        public int MinX;
        // vtable 20; native accessor 0x1915E7C
        public int MinY;
    }
    [Serializable]
    public sealed class FCurtainData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x19164C0
        public int Id;
        // vtable 6; native accessor 0x1916504
        public int ColorId;
        // vtable 8; native accessor 0x1916548
        public int Order;
        // vtable 10; native accessor 0x191658C
        public int Target;
        // vtable 12; native accessor 0x19165D0
        public int CellCount;
        // vtable 14; native accessor 0x1916614
        public int Width;
        // vtable 16; native accessor 0x1916658
        public int Height;
        // vtable 18; native accessor 0x191669C
        public int MinX;
        // vtable 20; native accessor 0x19166E0
        public int MinY;
    }
    [Serializable]
    public sealed class FDirectedPoint
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1916A84
        public FPoint Point;
        // vtable 6; native accessor 0x1916B3C
        public int Direction;
    }
    [Serializable]
    public sealed class FDirectedPointGroupData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1916D70
        public FDirectedPoint[] Cells;
    }
    [Serializable]
    public sealed class FDrillData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1917184
        public int ColorId;
        // vtable 6; native accessor 0x19171C8
        public int MasterTileId;
        // vtable 8; native accessor 0x191720C
        public FPoint MasterPoint;
        // vtable 10; native accessor 0x19172C4
        public int Order;
        // vtable 12; native accessor 0x1917308
        public int Target;
    }
    [Serializable]
    public sealed class FGoalData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x19175DC
        public int Goal;
        // vtable 6; native accessor 0x1917620
        public int Count;
        // vtable 8; native accessor 0x1917664
        public bool IsFromSettings;
        // vtable 10; native accessor 0x19176AC
        public int BoardId;
    }
    [Serializable]
    public sealed class FIntIntPair
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1917948
        public int Key;
        // vtable 6; native accessor 0x191798C
        public int Value;
    }
    [Serializable]
    public sealed class FItemCount
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1917BC0
        public int Type;
        // vtable 6; native accessor 0x1917C04
        public int Count;
        // vtable 8; native accessor 0x1917C48
        public int BoardId;
    }
    [Serializable]
    public sealed class FItemCurtainData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1917EB4
        public int Id;
        // vtable 6; native accessor 0x1917EF8
        public int Order;
        // vtable 8; native accessor 0x1917F3C
        public int[] Goals;
        // vtable 10; native accessor 0x1918054
        public int[] Targets;
        // vtable 12; native accessor 0x191816C
        public int CellCount;
        // vtable 14; native accessor 0x19181B0
        public int Width;
        // vtable 16; native accessor 0x19181F4
        public int Height;
        // vtable 18; native accessor 0x1918238
        public int MinX;
        // vtable 20; native accessor 0x191827C
        public int MinY;
    }
    [Serializable]
    public sealed class FItemModelCreatorSaveData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1918888
        public FIntIntPair[] CreatedSetItemCountExceptMatchItem;
        // vtable 6; native accessor 0x191898C
        public FPredefinedItemsSaveData PredefinedItemsSaveData;
    }
    [Serializable]
    public sealed class FItemModelQueueSaveData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1918DBC
        public FItemSaveData[] Items;
    }
    [Serializable]
    public sealed class FItemSaveData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1919198
        public short TiledId;
        // vtable 6; native accessor 0x19191DC
        public short Data;
    }
    [Serializable]
    public sealed class FLevelSaveData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1919410
        public FValidationSaveData ValidationSaveData;
        // vtable 6; native accessor 0x19194D0
        public FBoardSaveData BoardSaveData;
        // vtable 8; native accessor 0x1919588
        public long[] GameStateSaveData;
        // vtable 10; native accessor 0x19196A4
        public long[] BoosterSaveData;
        // vtable 12; native accessor 0x19197C0
        public long[] LevelStatisticsSaveData;
        // vtable 14; native accessor 0x19198DC
        public long[] EventSaveData;
    }
    [Serializable]
    public sealed class FLimitData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x191A1F8
        public int Limit;
        // vtable 6; native accessor 0x191A23C
        public int Count;
        // vtable 8; native accessor 0x191A280
        public int BoardId;
    }
    [Serializable]
    public sealed class FMagnetGroupData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x191A4EC
        public FMagnetPoint[] MagnetCells;
    }
    [Serializable]
    public sealed class FMagnetPoint
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x191A8C8
        public FPoint Cell;
        // vtable 6; native accessor 0x191A980
        public bool IsHead;
        // vtable 8; native accessor 0x191A9C8
        public int DirectionTiledId;
    }
    [Serializable]
    public sealed class FMetalPlateGroupData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x191AC34
        public int Width;
        // vtable 6; native accessor 0x191AC78
        public FMagnetPoint[] MagnetCells;
    }
    [Serializable]
    public sealed class FPairData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x191B0BC
        public int Symbol;
        // vtable 6; native accessor 0x191B100
        public int ColorId;
        // vtable 8; native accessor 0x191B144
        public int FirstMinX;
        // vtable 10; native accessor 0x191B188
        public int FirstMinY;
        // vtable 12; native accessor 0x191B1CC
        public int SecondMinX;
        // vtable 14; native accessor 0x191B210
        public int SecondMinY;
    }
    [Serializable]
    public sealed class FPoint
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x191B4DC
        public int X;
        // vtable 6; native accessor 0x191B520
        public int Y;
    }
    [Serializable]
    public sealed class FPotionColor
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x191B754
        public int[] Colors;
    }
    [Serializable]
    public sealed class FPouchConfig
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x191BB7C
        public bool IsAvailableFalling;
        // vtable 6; native accessor 0x191BBC4
        public int LightballRatio;
        // vtable 8; native accessor 0x191BC08
        public int TntRatio;
        // vtable 10; native accessor 0x191BC4C
        public int RocketHorizontalRatio;
        // vtable 12; native accessor 0x191BC90
        public int RocketVerticalRatio;
        // vtable 14; native accessor 0x191BCD4
        public int PropellerRatio;
    }
    [Serializable]
    public sealed class FPredefinedItemsSaveData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x191BFA0
        public FItemModelQueueSaveData[] Queues;
    }
    [Serializable]
    public sealed class FRoyalCapsuleConfig
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x161F09C
        public int[] Items;
        // vtable 6; native accessor 0x161F1B4
        public int MinX;
        // vtable 8; native accessor 0x161F1F8
        public int MinY;
    }
    [Serializable]
    public sealed class FShifterData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x161F5B4
        public FPoint[] ItemCells;
        // vtable 6; native accessor 0x161F6C0
        public int StartMasterIndex;
        // vtable 8; native accessor 0x161F704
        public int EndMasterIndex;
        // vtable 10; native accessor 0x161F748
        public FDirectedPoint Start;
        // vtable 12; native accessor 0x161F808
        public FDirectedPoint TurnPoint;
        // vtable 14; native accessor 0x161F8C8
        public FDirectedPoint End;
    }
    [Serializable]
    public sealed class FSlotConfig
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x161FD98
        public int StartIndex;
        // vtable 6; native accessor 0x161FDDC
        public bool HasLightball;
        // vtable 8; native accessor 0x161FE24
        public bool HasRocket;
        // vtable 10; native accessor 0x161FE6C
        public bool HasTnt;
        // vtable 12; native accessor 0x161FEB4
        public bool HasPropeller;
    }
    [Serializable]
    public sealed class FStaticItemSaveData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x162018C
        public short TiledId;
        // vtable 6; native accessor 0x16201D0
        public short Data;
    }
    [Serializable]
    public sealed class FTiledCell
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1620404
        public int FillType;
        // vtable 6; native accessor 0x1620448
        public bool IsPredefined;
        // vtable 8; native accessor 0x1620490
        public int Grass;
        // vtable 10; native accessor 0x16204D4
        public int Honey;
        // vtable 12; native accessor 0x1620518
        public int Curtain;
        // vtable 14; native accessor 0x162055C
        public int Chain;
        // vtable 16; native accessor 0x16205A0
        public int Roof;
        // vtable 18; native accessor 0x16205E4
        public int Jelly;
        // vtable 20; native accessor 0x1620628
        public int Treasuremap;
        // vtable 22; native accessor 0x162066C
        public int Ceramic;
        // vtable 24; native accessor 0x16206B0
        public int MushroomSoil;
        // vtable 26; native accessor 0x16206F4
        public int Blinds;
        // vtable 28; native accessor 0x1620738
        public int WaterSoil;
        // vtable 30; native accessor 0x162077C
        public int ForceField;
        // vtable 32; native accessor 0x16207C0
        public int Cryptex;
        // vtable 34; native accessor 0x1620804
        public int IceBorder;
        // vtable 36; native accessor 0x1620848
        public int MetalPlates;
        // vtable 38; native accessor 0x162088C
        public int ItemCurtain;
    }
    [Serializable]
    public sealed class FTiledGrid
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1620DE4
        public int[] Items;
        // vtable 6; native accessor 0x1620EFC
        public FTiledCell[] Cells;
        // vtable 8; native accessor 0x1621000
        public int Width;
        // vtable 10; native accessor 0x1621044
        public int Height;
    }
    [Serializable]
    public sealed class FTiledLevel
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1621580
        public string Name;
        // vtable 6; native accessor 0x1621640
        public int Move;
        // vtable 8; native accessor 0x1621684
        public FGoalData[] Goals;
        // vtable 10; native accessor 0x1621790
        public FTiledGrid Grid;
        // vtable 12; native accessor 0x1621848
        public FTiledPredefined[] Predefined;
        // vtable 14; native accessor 0x1621984
        public FTiledSet[] Sets;
        // vtable 16; native accessor 0x1621AC0
        public int[] Colors;
        // vtable 18; native accessor 0x1621BD8
        public FItemCount[] Counts;
        // vtable 20; native accessor 0x1621CE4
        public FPotionColor[] PotionColors;
        // vtable 22; native accessor 0x1621DF0
        public int[] LowMatch;
        // vtable 24; native accessor 0x1621F08
        public FCurtainData[] Curtains;
        // vtable 26; native accessor 0x1622014
        public FCellGroupData[] CupShelves;
        // vtable 28; native accessor 0x1622120
        public FCellGroupData[] SoilGroups;
        // vtable 30; native accessor 0x162222C
        public FDrillData[] Drills;
        // vtable 32; native accessor 0x1622338
        public int[] LightBulbColorOrder;
        // vtable 34; native accessor 0x1622450
        public int ChainCount;
        // vtable 36; native accessor 0x1622494
        public bool PropellerIgnoresChain;
        // vtable 38; native accessor 0x16224DC
        public FPouchConfig PouchConfig;
        // vtable 40; native accessor 0x162259C
        public FLimitData[] Limits;
        // vtable 42; native accessor 0x16226A8
        public bool PropellerIgnoresDrill;
        // vtable 44; native accessor 0x16226F0
        public int JellyReceivableCellCount;
        // vtable 46; native accessor 0x1622734
        public bool HasHoleInGrid;
        // vtable 48; native accessor 0x162277C
        public FCellGroupData[] Treasuremaps;
        // vtable 50; native accessor 0x1622888
        public FSlotConfig[] Slots;
        // vtable 52; native accessor 0x162298C
        public int CageCount;
        // vtable 54; native accessor 0x16229D0
        public FMagnetGroupData[] MagnetGroups;
        // vtable 56; native accessor 0x1622ADC
        public bool Scroll;
        // vtable 58; native accessor 0x1622B24
        public FMagnetGroupData[] ConveyorBelts;
        // vtable 60; native accessor 0x1622C30
        public FCurtainData[] Coils;
        // vtable 62; native accessor 0x1622D3C
        public FMagnetGroupData[] CandyCaneGroups;
        // vtable 64; native accessor 0x1622E48
        public FCellGroupData[] TabletItemGroups;
        // vtable 66; native accessor 0x1622F54
        public FMagnetGroupData[] IvyItemGroups;
        // vtable 68; native accessor 0x1623060
        public FBlindsGroupData[] BlindsGroups;
        // vtable 70; native accessor 0x162316C
        public FWaterData WaterData;
        // vtable 72; native accessor 0x162325C
        public FShifterData[] ShifterGrids;
        // vtable 74; native accessor 0x1623360
        public FDirectedPointGroupData[] PorcelainGroups;
        // vtable 76; native accessor 0x162346C
        public FCellGroupData[] MagicGem;
        // vtable 78; native accessor 0x1623578
        public int[] MagicGemColorOrder;
        // vtable 80; native accessor 0x1623690
        public FCurtainData[] AncientVaults;
        // vtable 82; native accessor 0x162379C
        public FCellGroupData[] ForceFieldGroups;
        // vtable 84; native accessor 0x16238A8
        public FCurtainData[] Logs;
        // vtable 86; native accessor 0x16239B4
        public bool PropellerIgnoresLog;
        // vtable 88; native accessor 0x16239FC
        public FCellGroupData[] PinGroups;
        // vtable 90; native accessor 0x1623B08
        public FCryptexData[] Cryptexes;
        // vtable 92; native accessor 0x1623C14
        public FCellGroupData[] PowerCubeGroups;
        // vtable 94; native accessor 0x1623D20
        public FDirectedPointGroupData[] PotionTubeGroups;
        // vtable 96; native accessor 0x1623E2C
        public FCellGroupData[] CuckooGroups;
        // vtable 98; native accessor 0x1623F38
        public FCurtainData[] Sapphires;
        // vtable 100; native accessor 0x1624044
        public FRoyalCapsuleConfig[] RoyalCapsules;
        // vtable 102; native accessor 0x1624148
        public FPouchConfig CapsuleConfig;
        // vtable 104; native accessor 0x1624208
        public FMagnetGroupData[] BowlingRacks;
        // vtable 106; native accessor 0x1624314
        public FPoint[] MagicLantern;
        // vtable 108; native accessor 0x1624420
        public FPairData[] Pairs;
        // vtable 110; native accessor 0x162452C
        public FMetalPlateGroupData[] MetalPlateGroups;
        // vtable 112; native accessor 0x1624638
        public FCurtainData[] ColorMixers;
        // vtable 114; native accessor 0x1624744
        public bool SkipMoveReduction;
        // vtable 116; native accessor 0x162478C
        public FItemCurtainData[] ItemCurtains;
    }
    [Serializable]
    public sealed class FTiledPredefined
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1628D30
        public int X;
        // vtable 6; native accessor 0x1628D74
        public int Y;
        // vtable 8; native accessor 0x1628DB8
        public int[] Ids;
    }
    [Serializable]
    public sealed class FTiledSet
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x1629210
        public string Name;
        // vtable 6; native accessor 0x16292D0
        public FTiledSetElement[] Elements;
        // vtable 8; native accessor 0x162940C
        public bool CanFall;
        // vtable 10; native accessor 0x1629454
        public int CreateRatio;
        // vtable 12; native accessor 0x1629498
        public int[] TargetColumns;
        // vtable 14; native accessor 0x16295B0
        public int BirdMax;
        // vtable 16; native accessor 0x16295F4
        public int FrogMax;
        // vtable 18; native accessor 0x1629638
        public int[] TargetFills;
        // vtable 20; native accessor 0x1629750
        public int[] TargetFillRatios;
        // vtable 22; native accessor 0x1629868
        public FTiledSetMaxItemCount[] MaxItemCounts;
    }
    [Serializable]
    public sealed class FTiledSetElement
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x162A388
        public int Id;
        // vtable 6; native accessor 0x162A3CC
        public int CreateRatio;
    }
    [Serializable]
    public sealed class FTiledSetMaxItemCount
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x162A5C8
        public int Id;
        // vtable 6; native accessor 0x162A60C
        public int MaxCount;
    }
    [Serializable]
    public sealed class FValidationSaveData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x162A840
        public string LevelName;
        // vtable 6; native accessor 0x162A900
        public long Checksum;
        // vtable 8; native accessor 0x162A948
        public int AppVersion;
    }
    [Serializable]
    public sealed class FWaterData
    {
        public string[] _absent_fields;
        // vtable 4; native accessor 0x162AB7C
        public FCellGroupData EnterPoints;
        // vtable 6; native accessor 0x162AC3C
        public FCellGroupData WaterPoints;
        // vtable 8; native accessor 0x162ACFC
        public FCellGroupData ExitPoints;
        // vtable 10; native accessor 0x162ADBC
        public int WaterReceivableCellCount;
        // vtable 12; native accessor 0x162AE00
        public int BoardCount;
        // vtable 14; native accessor 0x162AE44
        public int SandCellCount;
    }
}