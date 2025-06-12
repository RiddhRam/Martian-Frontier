using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;

public class LeaderboardResults : MonoBehaviour
{
    // Player leaderboard score
    public BigInteger playerLS;
    private LeaderboardPlayer player;

    // IMPORTANT FOR GENERATING LEADERBOARD DATA
    // When the tournament ends (part of seed)
    private DateTime endTime;
    // Unique integer for the user (part of seed) so each user has a different leaderboard, even with the same endTime
    public int uniqueUserInt;
    const int averageOresMinedPerRound = 160;
    const int minutesPerRound = 2;
    private const int totalTournamentSeconds = 172_800; // 2 days
    System.Random rng;

    // TREAT THIS AS A VERY LARGE ARRAY (thousands of names)
    private readonly string[] botNames = {
        "Crimson", "Rusty", "Lunar", "Solar", "Astro", "Quantum",
        "Nova", "Phantom", "Obsidian", "Cobalt", "Plasma", "Ironclad",
        "Zephyr", "Void", "Gritty", "Vortex", "Redshift", "Orbital",
        "Radiant", "Pyro", "Blazing", "Silent", "Nebula", "Electric",
        "Shadow", "Frozen", "Glitchy", "Titan", "Infernal", "Chrome",
        "Echo", "Warped", "Venomous", "Hazard", "Stellar", "Jaded",
        "Atomic", "Grim", "Pixelated", "Blistered", "Cyber", "Fractal",
        "Miner", "Driller", "Hauler", "Rover", "Seeker",
        "Pioneer", "Scout", "Prospector", "Nomad", "Quaker", "Astronaut",
        "Digger", "Crawler", "Core", "Reactor", "Golem", "Harvester",
        "Excavator", "Sentinel", "Warden", "Breaker", "Scraper", "Forager",
        "Smelter", "Raider", "Assembler", "Tracer", "Forgemaster", "Grinder",
        "Operator", "Runner", "Chiseler", "Refiner", "Surveyor", "Plunderer",
        "Reclaimer", "Destructor", "Engineer", "Drifter", "Observer", "Stalker",
        "M1ner", "D1gg3r", "R0verX", "B00t404", "Dr1ll3r", "Nom4d",
        "Qw4ker", "H4ulr", "S3eker", "Gr1nder", "Ph4ntom", "Xpl0rer",
        "AstroN0m", "Obsid1an", "R3dsh1ft", "C0reBrkr", "Fr4ct4l",
        "Harv3ster", "D1v3r", "R0gueX", "W4rpdriv3", "G1itch", "V01dX",
        "Tr4cer77", "Bl1zz4rd", "Chrom3X", "Re4ct0r9", "V0rt3x99", "P1x3l",
        "Ember", "Tectonic", "Pulsar", "Nebulark", "Solaris", "Quasar",
        "Turbine", "Maelstrom", "Flux", "Oblivion", "Rift", "Singularity",
        "Eclipse", "Pyronova", "Thunderstrike", "Sentient", "Overdrive", "Chrono",
        "Blitz", "Warpcore", "Circuit", "Voltage", "Nanite", "Zenith",
        "Helion", "Omicron", "Catalyst", "Dynamo", "Onyx", "Phazer",
        "Reverberate", "Cryo", "Mecha", "Spectron", "Monolith", "Ether",
        "Gyrator", "Vanguard", "Titanium", "Mach", "Overseer", "Resonance",
        "Serrator", "Pulsewave", "Sonic", "Forgecore", "Amplify", "Kinetix",
        "Neutron", "Plasmonic", "Metron", "Ionic", "Havoc", "Zenon",
        "Stratos", "Hyperion", "Synthetix", "Photon", "Spectra", "Fusion",
        "Aegis", "Kryptron", "Shredder", "OblivionX", "Syphon", "Hydron",
        "Nexon", "Xenotron", "Etherion", "Velocitron", "Vortexus", "Catalyx",
        "Synthar", "Axion", "Dimensia", "Polaron", "HorizonX", "Ruptor",
        "Exotron", "Silicore", "Nocturn", "Halcyon", "Excalibur", "Typhon",
        "EchoCore", "NeonEdge", "ChronoX", "Celestus", "Pyrevolt", "Stormdrift",
        "Dreadnought", "Evolvion", "Voltar", "Strikron", "Roguewave", "Cipheron",
        "Glacion", "HyperCore", "Tesseract", "Omniflare", "Infernix", "Mechara",
        "Nucleon", "Skybreaker", "Voidstorm", "Cyclonix", "Oblivix", "Fission",
        "T3rra", "D3f1ler", "M3chX", "Crat3r", "Bl4st0ff", "F1ss10n", "Ast3r01d99",
        "T0rqu3", "Xen0nX", "Hydr0Ph4z3", "P3rmafrost", "L4vafl0w", "N3bularX",
        "Cyb3rnaut", "Quantum99", "R3nd3rX", "Z3r0P01nt", "Excav8r", "Thr4sh3r",
        "T1m3warp", "S0n1cX", "W4rpg4te", "Ragnar0k", "Pyr0x", "Dr4g0nB0t",
        "BlackH0l3", "V3l0c1tr0n", "R4d10act1v3", "Synth3X", "Turb0C0r3", "H3llfir3",
        "Gl4c14l", "Xpl01t3r", "M0chafl3re", "N1ghtSh4d3", "D3m0n1cX", "Puls4r99",
        "C0sm0tr0n", "D4t4M1n3r", "T3kn0M4ncer", "X-t3rmin8r", "Crypt0X", "F1r3wallX",
        "Bl1tzkr13g", "Str4t0blast", "C3l3st14lX", "Ph0t0nDr1ft", "T0rment0r", "V0rax",
        "Ex0G3n", "Lun4rM3ch", "D3struct0rX", "Havoc99", "M0n0l1thX", "Solara", "Nebulite",
        "Quantara", "Eclipseon", "Marsforge", "Orbitron",  "Fizzcrank",
        "Gravion", "Celestior", "Voidcore", "Exohelm", "Starforge", "Zenithal",
        "Planetrak", "Lunaris", "Orbex", "Pulsanova", "Aerion", "Astrogon",
        "Thrustar", "Galaxior", "Scrapline", "Steeljaw", "Rustclank", "Coregrind",
        "Drillbit", "Rockmaw", "Depthcrawler", "Ironbore", "Gritforge", "Shaftwalker",
        "Coremauler", "Deepgrip", "Crustpiercer", "Burrower", "Gunkrake",
        "Tunneljaw", "Maghammer", "Coregnaw", "Crushunit", "Codeburn", "SyntaxX",
        "Bytevoid", "Hackbit", "Packetstorm", "Glitchphase", "Compilr", "Databurn",
        "Cryptron", "0vercrank", "Err0rUnit", "Ramcore", "Debugga", "Fragloop",
        "S3gm3ntX", "Nullwave", "Cr4shdr1ll", "M4lfunct", "Bitm4sk", "C0d3Wr3ck",
        "M1n3R4g3", "Dr1ftX99", "Cl4wB0t", "R0ck3tM1n3", "N0D3cr4ck", "T3rr4Dr1ll",
        "R3kt0r", "S1l1c0nX", "V01dR1pp3r", "D34dB00t", "Gr1mF1nn", "Sh4d0wC0r3",
        "Sp1nDr1ll", "R4v3nM3ch", "M1n3flare", "Carb0nCr0wl", "Xcv8Roid", "D3pthC0de",
        "PlasmGr1nd", "T1nkrX", "Thudbrick", "Vantabot", "Blortok", "Greeblor",
        "Hexnut", "Plink99", "Drubbler", "Torqueleech", "Yttrion", "Ogranik",
        "Drossel", "Splinewalker", "Bronzorb", "Nokturne", "Frakspur", "Grundlebot",
        "Screevix", "Dustwhirl", "Squirmatron", "Bytequake", "CryoCore", "SynthForge",
        "Mechaweld", "Nanoflux", "Voxcircuit", "ExoShaft", "Rustpulse", "W4rpOre",
        "Zerobit", "V1rusPrime", "Pulsebyte", "Nanonite", "GhostProc", "Bitrend",
        "LagSpike", "HaxxorX", "KernelDrill", "C0d3spl1n3", "Crypt0burn", "Datawr4ck",
        "Glitchnode", "NullForge", "Shaftcore", "Quakejaw", "Tunnelgrind", "Orevex",
        "Drillquake", "Grindjaw", "Loadcore", "Tectodrill", "Ravencore", "Dredgebot",
        "Ironpick", "Corelatch", "Shovax", "Graveldent", "Deepburrow", "Substrator",
        "Excavix", "Pitburner", "Stratavore", "Chasmwalker", "Corehound", "Smeltlock",
        "Subcore", "Drillstorm", "Tremorclank", "Dustcrank", "Blastjaw", "Forgeleech",
        "Pickshard", "Hackminr", "Tunnellite", "Gravshift", "Bytecrack", "Frackbyte",
        "Nodeclank", "Burrowcore", "Voidminer", "EchoDrill", "Bitshredder", "Rubble",
        "Gritunit", "Shattercore", "Digipick", "Lodebyte", "Orebit", "Rocktide",
        "Downbit", "Clangbo", "Breachcore", "Furnix", "V1b3drill", "Fractanite",
        "SubnetX", "Blazebore", "TunnelByte", "Plasmcore", "Thumpjaw", "Ramshard",
        "Forkbit", "MagForge", "Crackrake", "Xtractron", "Shardflux", "Corefuse",
        "DrillNova", "orock", "Wrenchbit", "Datadent", "TremorX", "Krushbyte",
        "Burstrak", "Shov3lr", "Coalbolt", "Plasmweld", "Nodeflare", "Pitshift",
        "Drivax", "Drillgeist", "Voidclank", "Orecrank", "Dustprobe", "Mineweld",
        "Shatter", "Warpforge", "EchoNode", "Cryptforge", "Crackbit", "V0ltbore",
        "Dr1llwr4th", "Fract0byte", "DataSp1ke", "Br3achdrill", "Gl1tchr1ft", "Excavatr0n",
        "Sm3ltByte", "Foragron", "Weldshift", "Dr3dgex", "Coreb1t3", "Bitcr4wl", "LavaCoreX",
        "Downshard", "Sp1kegr1nd", "Ramgrind", "Tunnelbit", "Nodebreaker", "Shockcrank",
        "Ch4smX", "Cr4ck3r", "Grittr0n", "Boltjaw", "Ironcrawl", "Spl1nt3r", "Orekn1ght",
        "Zer0core", "Nodev1per", "Tunnelclaw", "SubterraX", "Obsidrax", "Nanogrind",
        "Slic3o", "Strataclaw", "Hackjaw", "Borift", "Crustmole", "GravforgeX",
        "Excavat3r77", "B1tburn", "CryoD1g", "Rockbyte", "Gnashunit", "S0ilburst",
        "Corebl1tz", "Faultbt", "D3pthshock", "B0rex", "R1ftbit", "VoidripperX",
        "Downclank", "Sh3arot", "Orew1zard", "D3epFl4re", "Smeltbyte", "Tunn3lDr0id",
        "Wrenchflux", "Gritslicer", "VoidB0t99", "F1ssot", "Axebot", "Depthbore",
        "Rockgr1nd", "B1tm4gnet", "Tect0buster", "Tr3morphase", "Furnacrusher", "OreSurge",
        "Chasmf1re", "Frackture", "Spl1nto", "Downbit3", "Mechdenter", "Loadsplit",
        "Magdriller", "Voltb0re", "Voidgrindr", "CorejawX", "B0tM1ner", "Thrashtr0n",
        "Subb0tX", "Tectonizer", "R0ckBr34k3r", "M1nerCl4nk", "NullM1ner", "Shaftb1t",
        "Dr1llburst", "Burrowb1t3", "MineR0v3", "Blastshaft", "Sk1ncr4per", "StrataSp1n",
        "OreJawz", "L0wbit", "Trenchcore", "Krankforge", "RubbleClank", "Chiselcore",
        "Fragmentor", "IroncoreX", "Digiz3r0", "Faultline", "Coreshock", "Tectogrid",
        "Draggat", "Voltrench", "Crackcore", "Cl4nkb1t", "Tunn3lJaw", "Xpl0drill",
        "Rustw0rm", "Tremorbolt", "Rubblebyte", "Voidtick", "Crushblitz", "Gritchurn",
        "Corequake", "B1tf0ssil", "Chasmflare", "Crush99", "Sh4ftsm4sh", "Xdr1ll3r",
        "Depthfury", "Gr1tcr4wl", "Frackbl4st", "Dr1llsp1n", "Cryoclaw", "Loadgr1p",
        "Tunnelcrack", "R1ftzone", "Slagot", "B0r3storm", "Oreblitz", "Magcrush",
        "Shatterminr", "Mechfract", "Digg0tron", "Pulverizer", "StrataX", "Downsplit",
        "Clunkbyte", "Subm3ch", "Gritstream", "Slicebore", "Gouget", "Blastknuckle",
        "Corepit", "BurnboX", "Shaftb1nder", "Flaregr1nd", "Fr4ckstorm", "WeldcoreX",
        "D3pthslicer", "LoadbitX", "Corecl4w", "Plungebit", "H4mmerJaw", "Moltenr1ft",
        "ExoCore", "Grimjaw", "Mineflare", "Cl4nkburst", "Nullshred", "Bytefracture",
        "Excavatik", "Tunnelcrusher", "Ironshifter", "Forgebyte", "Faultbreaker",
        "MinecrushX", "LavaChurn", "Blasterclank", "Shaftstrike", "Tect0flare",
        "Tunn3lshock", "Pickblitz", "Rustquake", "Coalstorm", "Fractunit", "Splint3rdrill",
        "Pitblast", "Shockvane", "D1ggrX", "OverburdenX", "Smashbit", "VoidScrap",
        "Oregrinder", "Deepbit", "Voltagrind", "Ironburst", "ShardbX", "Nodeblaster",
        "Bitstrike", "Rocksplit", "Drillblitz", "Nulljaw", "Fraybt", "Orelock",
        "Tunnelthrasher", "QuakeSp1n", "Depthjaw", "Datashard", "Gritpulse", "Burrowburn",
        "Oreburst", "M1n3sl4sh"
    };

    // The scores
    static readonly List<LeaderboardPlayer> leaderboardPlayerScores = new();

    public LeaderboardResults(BigInteger playerLS, DateTime endTime, int uniqueUserInt)
    {
        this.playerLS = playerLS;
        this.endTime = endTime;

        this.uniqueUserInt = uniqueUserInt;

        // If it hasn't been set yet, then set it
        if (this.uniqueUserInt == 0)
        {
            this.uniqueUserInt = new System.Random().Next(1, int.MaxValue);

            PlayerPrefs.SetInt("Unique User ID", this.uniqueUserInt);
        }

        rng = new((int)(endTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds + this.uniqueUserInt);

        InitializeLeaderboard();
    }

    private void InitializeLeaderboard()
    {
        player = new(playerLS, "You", "Player");
        leaderboardPlayerScores.Add(player);

        string[] chosenNames = botNames.OrderBy(_ => rng.Next()).Take(29).ToArray();

        for (int i = 0; i != chosenNames.Length; i++)
        {
            LeaderboardPlayer bot = new(100, chosenNames[i], Guid.NewGuid().ToString());
            leaderboardPlayerScores.Add(bot);
        }
    }

    public List<LeaderboardPlayer> GetLeaderboardScores()
    {
        SortBoard();
        return leaderboardPlayerScores;
    }

    private void SortBoard()
    {
        leaderboardPlayerScores.Sort(
            (a, b) => b.GetScore().CompareTo(a.GetScore()));
    }

    public void AddPlayerScore(BigInteger scoreToAdd)
    {
        playerLS += scoreToAdd;
        player.AddScore(scoreToAdd);
    }
}

public class LeaderboardPlayer
{
    private BigInteger score;
    private string playerName;
    private string uuid;

    public LeaderboardPlayer(BigInteger initialScore, string playerName, string uuid)
    {
        this.score = initialScore;
        this.playerName = playerName;
        this.uuid = uuid;
    }

    public void AddScore(BigInteger scoreToAdd)
    {
        score += scoreToAdd;
    }

    public BigInteger GetScore()
    {
        return score;
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public string GetUUID()
    {
        return uuid;
    }

}

// Keep a session’s timetable & score budget
public class PlaySession
{
    public DateTime startUtc;
    public DateTime endUtc;
    // Points to earn over this session
    public BigInteger target;          
}

/*
Populate the leaderboardPlayerScores list with 30 players. Give them each a unique name for botNames (no duplicates), a uuid and an initial score. I will explain how to determine the score in a second.

One of these players should be the player themselves. Set the player's uuid to be "Player", and their initial score to be 0. Then order the list from highest score to lowest.

The scores should be calculated by using averageOresMinedPerRound and minutesPerRound, and a float: randomNumberOfMinutes, for each player. Basically generate a random float of minutes the player plays, and use the average number of ores mined and minutes per round to determine their score.

A tricky part is this: the scores should change as endTime - DateTime.UtcNow gets lower. Basically the closer the tournament is to ending, the higher the scores should go. And they should not go up linearly.

You should treat it as players playing throughout multiple sessions. 2-4 sessions for each player, over two days. endTime - DateTime.UtcNow returns an integer from 0 - 172800 because the tournament is 2 days long.

I reccommend you calculate the final score of the player first, then treat it as them earning the score equally throughout each of their sessions. If they have a score of 6k and player 3 session, then they have 2k after each session. 

The sessions should be timed randomly throughout the 2 days. Also the score should not popup at once. Throughout each session, the scores should slowly change every 30 seconds. 

For example, you should not see a huge spike in a score. The score should slowly rise over several minutes until that session is over, then it stops until the next session or the touranment ends.
*/