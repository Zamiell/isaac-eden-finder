<Query Kind="Program">
  <IncludeUncapsulator>false</IncludeUncapsulator>
</Query>

// isaac-eden-finder Script
// For The Binding of Isaac: Repentance v1.7.5
// (with Mom's Ring as the final collectible)

// Created by Blade
// Originally modified from:
// https://gist.github.com/bladecoding/5fcc1356bfb0cf26555b0ade7c4fedca

// https://moddingofisaac.com/docs/rep/enums/CollectibleType.html
public static class CollectibleType
{
  public const int COLLECTIBLE_DIPLOPIA = 347;
	public const int COLLECTIBLE_CARD_READING = 660;
	public const int COLLECTIBLE_MOMS_RING = 732;

	public const int MAX_VANILLA = COLLECTIBLE_MOMS_RING;
}

// https://moddingofisaac.com/docs/rep/enums/Card.html
public static class Card
{
	public const int CARD_EMPEROR = 5;
	public const int CARD_CHAOS = 42;
	public const int CARD_SOUL_JACOB = 97;

	public const int MAX_VANILLA = CARD_SOUL_JACOB;
}

// https://moddingofisaac.com/docs/rep/enums/LevelStage.html
public static class LevelStage
{
	public const int NUM_STAGES = 14;
}

void Main()
{
	var rand = new Random();
	var seeds = new List<uint>();
	for(uint i = 0; i < uint.MaxValue; i++)
	{
		var startSeed = (uint)i;
		var dropSeed = CalculatePlayerSeed(startSeed);
		var items = CalculateEdenItems(dropSeed);

		if (
			items.Active == CollectibleType.COLLECTIBLE_DIPLOPIA
			&& items.Passive == CollectibleType.COLLECTIBLE_CARD_READING
			&& items.Card == Card.CARD_CHAOS
		)
		{
			seeds.Add(startSeed);
			var seedString = SeedToString(startSeed);
			var formattedSeedString = $"{seedString.Substring(0, 4)} {seedString.Substring(4)}";
			formattedSeedString.Dump();
		}

		if (i % 100000000 == 0)
		{
			("On numerical seed: " + i).Dump();
		}
	}
	seeds.Select(s => SeedToString(s)).ToArray().Dump();
}

public static EdenItems CalculateEdenItems(uint dropSeed, int itemCount = CollectibleType.MAX_VANILLA)
{
	var rng = new Rng(dropSeed, 0x1, 0x5, 0x13);

	var trinket = 0;
	var card = 0;
	var pill = 0;
	var hearts = 0;
	var soulHearts = 0;
	if ((rng.Next() % 3) == 0)
	{
		// Trinket
	}
	else if ((rng.Next() & 1) == 0)
	{
		if ((rng.Next() & 1) == 0)
		{
			card = GetCard(rng.Next());
		}
		else
		{
			// Pill
			var pillSeed = rng.Next();
		}
	}

	var activeId = 0;
	var passiveId = 0;
	for (var i = 0; i < 100; i++)
	{
		int itemId = (int)(rng.Next() % itemCount) + 1;

		// Black-list
		if (itemId > CollectibleType.MAX_VANILLA)
		{
			continue;
		}

		switch (itemId) {
			// These are all of the gaps in vanilla items
			// https://bindingofisaacrebirth.fandom.com/wiki/Items#Item_ID_Gaps
			case 43:
			case 61:
			case 235:
			case 587:
			case 613:
			case 620:
			case 630:
			case 648:
			case 662:
			case 666:
			case 718:
				continue;

			// These are the items that Eden cannot get, which are defined in the "items_metadata.xml"
			// file with a tag of "noeden"
			case 59: // Book of Belial (Birthright)
			case 238: // Key Piece 1
			case 239: // Key Piece 2
			case 327: // Polaroid
			case 328: // Negative
			case 550: // Broken Shovel 1
			case 551: // Broken Shovel 2
			case 552: // Mom's Shovel
			case 626: // Knife Piece 1
			case 627: // Knife Piece 2
			case 633: // Dogma
			case 668: // Dad's Note
			case 714: // Recall
			case 715: // Hold
				continue;

			default:
				break;
		}

		var itemType = ItemConfig[itemId];
		if (itemType == ItemType.Active)
		{
			activeId = activeId != 0 ? activeId : itemId;
		}
		else if (itemType == ItemType.Passive || itemType == ItemType.Familiar)
		{
			passiveId = passiveId != 0 ? passiveId : itemId;
		}

		// Isaac doesn't exit early :thinking:
		if (activeId != 0 && passiveId != 0)
		{
			break;
		}
	}

	// Hearts and SoulHearts are actually done in Player::Init
	var healthRng = new Rng(dropSeed, 0x1, 0x5, 0x13);
	var halfHearts = (int)healthRng.Next() & 3;
	hearts = halfHearts * 2;
	soulHearts = ((int)healthRng.Next() % (4 - halfHearts)) * 2;
	if (hearts == 0 && soulHearts < 4)
	{
	 	soulHearts = 4;
	}

	return new EdenItems(hearts, soulHearts, activeId, passiveId, trinket, card, pill);
}
public static int GetCard(uint seed, bool playing = false)
{
	var cardRng = new Rng(seed, 0x5, 0x9, 0x7);
	if (cardRng.Next() % 25 == 0)
	{
		return (int)(cardRng.Next() % 13) + 42;
	}
	if (cardRng.Next() % 10 == 0)
	{
		// Rune
	}
	if (cardRng.Next() % 5 == 0 && playing)
	{
		// Playing card
		return (int)(cardRng.Next() % 9) + 23;
	}

	return (int)(cardRng.Next() % 22) + 1;
}

public static Rng[] CalculateCollectibleSeeds(uint startSeed, int itemCount = CollectibleType.MAX_VANILLA)
{
	var playerInitSeed = CalculatePlayerInitSeed(startSeed);

	var playerInitRng = new Rng(playerInitSeed, 0x1, 0xB, 0x10);
	var collRng = new Rng(playerInitRng.Next(), 0x1, 0x13, 0x3);
	var seeds = new Rng[itemCount];
	for(var i = 0; i < seeds.Length; i++) {
		seeds[i] = new Rng(collRng.Next(), 5, 9, 7);
	}

	return seeds;
}

public static Rng[] CalculateCardSeeds(uint startSeed, int cardCount = Card.MAX_VANILLA)
{
	var playerInitSeed = CalculatePlayerInitSeed(startSeed);

	var playerInitRng = new Rng(playerInitSeed, 0x1, 0xB, 0x10);
	playerInitRng.Next();
	playerInitRng.Next();
	playerInitRng.Next();
	var cardsRng = new Rng(playerInitRng.Next(), 0x2, 0x5, 0xf);
	var seeds = new Rng[cardCount];
	for (var i = 0; i < seeds.Length; i++) {
		seeds[i] = new Rng(cardsRng.Next(), 5, 9, 7);
	}

	return seeds;
}

public static uint CalculatePlayerInitSeed(uint startSeed)
{
	var startRng = new Rng(startSeed, 0x3, 0x17, 0x19);

	// Stage Seeds
	for (var i = 0; i < LevelStage.NUM_STAGES; i++) {
		startRng.Next();
	}

	return startRng.Next(); // Seeds::PlayerInitSeed
}

public static uint CalculatePlayerSeed(uint startSeed)
{
	var playerInitSeed = CalculatePlayerInitSeed(startSeed);

	// These happen inside Player::Init
	var playerInitRng = new Rng(playerInitSeed, 0x1, 0xB, 0x10);
	playerInitRng.Next();
	playerInitRng.Next();
	playerInitRng.Next();
	playerInitRng.Next();

	return playerInitRng.Next(); // Entity::DropSeed
}

public class EdenItems
{
	public int Hearts;
	public int SoulHearts;
	public int Active;
	public int Passive;
	public int Trinket;
	public int Card;
	public int Pill;
	public EdenItems(int hearts, int soulHearts, int active, int passive, int trinket, int card, int pill)
	{
		Hearts = hearts;
		SoulHearts = soulHearts;
		Active = active;
		Passive = passive;
		Trinket = trinket;
		Card = card;
		Pill = pill;
	}
}

static string SeedToString(uint num)
{
	const string chars = "ABCDEFGHJKLMNPQRSTWXYZ01234V6789";
	byte x = 0;
	var tNum = num;
	while (tNum != 0)
	{
		x += ((byte)tNum);
		x += (byte)(x + (x >> 7));
		tNum >>= 5;
	}
	num ^= 0x0FEF7FFD;
	tNum = (num) << 8 | x;

	var ret = new char[8];
	for (int i = 0; i < 6; i++)
	{
		ret[i] = chars[(int)(num >> (27 - (i * 5)) & 0x1F)];
	}
	ret[6] = chars[(int)(tNum >> 5 & 0x1F)];
	ret[7] = chars[(int)(tNum & 0x1F)];

	return new string(ret);
}

public class Rng
{
	public uint Seed;
	public int Shift1;
	public int Shift2;
	public int Shift3;
	public uint Next()
	{
		var num = Seed;
		num ^= num >> Shift1;
		num ^= num << Shift2;
		num ^= num >> Shift3;
		Seed = num;
		return num;
	}

	public Rng(uint seed, int s1, int s2, int s3) {
		this.Seed = seed;
		Shift1 = s1;
		Shift2 = s2;
		Shift3 = s3;
	}
};

public enum ItemType {
	Null = 0,
	Passive = 1,
	Trinket = 2,
	Active = 3,
	Familiar = 4,
}

public static ItemType[] ItemConfig = GetItemConfig();
public static ItemType[] GetItemConfig() {
	var itemConfig = new ItemType[CollectibleType.MAX_VANILLA + 1];

	itemConfig[1] = ItemType.Passive;
	itemConfig[2] = ItemType.Passive;
	itemConfig[3] = ItemType.Passive;
	itemConfig[4] = ItemType.Passive;
	itemConfig[5] = ItemType.Passive;
	itemConfig[6] = ItemType.Passive;
	itemConfig[7] = ItemType.Passive;
	itemConfig[8] = ItemType.Familiar;
	itemConfig[9] = ItemType.Passive;
	itemConfig[10] = ItemType.Familiar;
	itemConfig[11] = ItemType.Familiar;
	itemConfig[12] = ItemType.Passive;
	itemConfig[13] = ItemType.Passive;
	itemConfig[14] = ItemType.Passive;
	itemConfig[15] = ItemType.Passive;
	itemConfig[16] = ItemType.Passive;
	itemConfig[17] = ItemType.Passive;
	itemConfig[18] = ItemType.Passive;
	itemConfig[19] = ItemType.Passive;
	itemConfig[20] = ItemType.Passive;
	itemConfig[21] = ItemType.Passive;
	itemConfig[22] = ItemType.Passive;
	itemConfig[23] = ItemType.Passive;
	itemConfig[24] = ItemType.Passive;
	itemConfig[25] = ItemType.Passive;
	itemConfig[26] = ItemType.Passive;
	itemConfig[27] = ItemType.Passive;
	itemConfig[28] = ItemType.Passive;
	itemConfig[29] = ItemType.Passive;
	itemConfig[30] = ItemType.Passive;
	itemConfig[31] = ItemType.Passive;
	itemConfig[32] = ItemType.Passive;
	itemConfig[33] = ItemType.Active;
	itemConfig[34] = ItemType.Active;
	itemConfig[35] = ItemType.Active;
	itemConfig[36] = ItemType.Active;
	itemConfig[37] = ItemType.Active;
	itemConfig[38] = ItemType.Active;
	itemConfig[39] = ItemType.Active;
	itemConfig[40] = ItemType.Active;
	itemConfig[41] = ItemType.Active;
	itemConfig[42] = ItemType.Active;
	itemConfig[44] = ItemType.Active;
	itemConfig[45] = ItemType.Active;
	itemConfig[46] = ItemType.Passive;
	itemConfig[47] = ItemType.Active;
	itemConfig[48] = ItemType.Passive;
	itemConfig[49] = ItemType.Active;
	itemConfig[50] = ItemType.Passive;
	itemConfig[51] = ItemType.Passive;
	itemConfig[52] = ItemType.Passive;
	itemConfig[53] = ItemType.Passive;
	itemConfig[54] = ItemType.Passive;
	itemConfig[55] = ItemType.Passive;
	itemConfig[56] = ItemType.Active;
	itemConfig[57] = ItemType.Familiar;
	itemConfig[58] = ItemType.Active;
	itemConfig[59] = ItemType.Active;
	itemConfig[60] = ItemType.Passive;
	itemConfig[62] = ItemType.Passive;
	itemConfig[63] = ItemType.Passive;
	itemConfig[64] = ItemType.Passive;
	itemConfig[65] = ItemType.Active;
	itemConfig[66] = ItemType.Active;
	itemConfig[67] = ItemType.Familiar;
	itemConfig[68] = ItemType.Passive;
	itemConfig[69] = ItemType.Passive;
	itemConfig[70] = ItemType.Passive;
	itemConfig[71] = ItemType.Passive;
	itemConfig[72] = ItemType.Passive;
	itemConfig[73] = ItemType.Familiar;
	itemConfig[74] = ItemType.Passive;
	itemConfig[75] = ItemType.Passive;
	itemConfig[76] = ItemType.Passive;
	itemConfig[77] = ItemType.Active;
	itemConfig[78] = ItemType.Active;
	itemConfig[79] = ItemType.Passive;
	itemConfig[80] = ItemType.Passive;
	itemConfig[81] = ItemType.Familiar;
	itemConfig[82] = ItemType.Passive;
	itemConfig[83] = ItemType.Active;
	itemConfig[84] = ItemType.Active;
	itemConfig[85] = ItemType.Active;
	itemConfig[86] = ItemType.Active;
	itemConfig[87] = ItemType.Passive;
	itemConfig[88] = ItemType.Familiar;
	itemConfig[89] = ItemType.Passive;
	itemConfig[90] = ItemType.Passive;
	itemConfig[91] = ItemType.Passive;
	itemConfig[92] = ItemType.Passive;
	itemConfig[93] = ItemType.Active;
	itemConfig[94] = ItemType.Familiar;
	itemConfig[95] = ItemType.Familiar;
	itemConfig[96] = ItemType.Familiar;
	itemConfig[97] = ItemType.Active;
	itemConfig[98] = ItemType.Familiar;
	itemConfig[99] = ItemType.Familiar;
	itemConfig[100] = ItemType.Familiar;
	itemConfig[101] = ItemType.Passive;
	itemConfig[102] = ItemType.Active;
	itemConfig[103] = ItemType.Passive;
	itemConfig[104] = ItemType.Passive;
	itemConfig[105] = ItemType.Active;
	itemConfig[106] = ItemType.Passive;
	itemConfig[107] = ItemType.Active;
	itemConfig[108] = ItemType.Passive;
	itemConfig[109] = ItemType.Passive;
	itemConfig[110] = ItemType.Passive;
	itemConfig[111] = ItemType.Active;
	itemConfig[112] = ItemType.Familiar;
	itemConfig[113] = ItemType.Familiar;
	itemConfig[114] = ItemType.Passive;
	itemConfig[115] = ItemType.Passive;
	itemConfig[116] = ItemType.Passive;
	itemConfig[117] = ItemType.Passive;
	itemConfig[118] = ItemType.Passive;
	itemConfig[119] = ItemType.Passive;
	itemConfig[120] = ItemType.Passive;
	itemConfig[121] = ItemType.Passive;
	itemConfig[122] = ItemType.Passive;
	itemConfig[123] = ItemType.Active;
	itemConfig[124] = ItemType.Active;
	itemConfig[125] = ItemType.Passive;
	itemConfig[126] = ItemType.Active;
	itemConfig[127] = ItemType.Active;
	itemConfig[128] = ItemType.Familiar;
	itemConfig[129] = ItemType.Passive;
	itemConfig[130] = ItemType.Active;
	itemConfig[131] = ItemType.Familiar;
	itemConfig[132] = ItemType.Passive;
	itemConfig[133] = ItemType.Active;
	itemConfig[134] = ItemType.Passive;
	itemConfig[135] = ItemType.Active;
	itemConfig[136] = ItemType.Active;
	itemConfig[137] = ItemType.Active;
	itemConfig[138] = ItemType.Passive;
	itemConfig[139] = ItemType.Passive;
	itemConfig[140] = ItemType.Passive;
	itemConfig[141] = ItemType.Passive;
	itemConfig[142] = ItemType.Passive;
	itemConfig[143] = ItemType.Passive;
	itemConfig[144] = ItemType.Familiar;
	itemConfig[145] = ItemType.Active;
	itemConfig[146] = ItemType.Active;
	itemConfig[147] = ItemType.Active;
	itemConfig[148] = ItemType.Passive;
	itemConfig[149] = ItemType.Passive;
	itemConfig[150] = ItemType.Passive;
	itemConfig[151] = ItemType.Passive;
	itemConfig[152] = ItemType.Passive;
	itemConfig[153] = ItemType.Passive;
	itemConfig[154] = ItemType.Passive;
	itemConfig[155] = ItemType.Familiar;
	itemConfig[156] = ItemType.Passive;
	itemConfig[157] = ItemType.Passive;
	itemConfig[158] = ItemType.Active;
	itemConfig[159] = ItemType.Passive;
	itemConfig[160] = ItemType.Active;
	itemConfig[161] = ItemType.Passive;
	itemConfig[162] = ItemType.Passive;
	itemConfig[163] = ItemType.Familiar;
	itemConfig[164] = ItemType.Active;
	itemConfig[165] = ItemType.Passive;
	itemConfig[166] = ItemType.Active;
	itemConfig[167] = ItemType.Familiar;
	itemConfig[168] = ItemType.Passive;
	itemConfig[169] = ItemType.Passive;
	itemConfig[170] = ItemType.Familiar;
	itemConfig[171] = ItemType.Active;
	itemConfig[172] = ItemType.Familiar;
	itemConfig[173] = ItemType.Passive;
	itemConfig[174] = ItemType.Familiar;
	itemConfig[175] = ItemType.Active;
	itemConfig[176] = ItemType.Passive;
	itemConfig[177] = ItemType.Active;
	itemConfig[178] = ItemType.Familiar;
	itemConfig[179] = ItemType.Passive;
	itemConfig[180] = ItemType.Passive;
	itemConfig[181] = ItemType.Active;
	itemConfig[182] = ItemType.Passive;
	itemConfig[183] = ItemType.Passive;
	itemConfig[184] = ItemType.Passive;
	itemConfig[185] = ItemType.Passive;
	itemConfig[186] = ItemType.Active;
	itemConfig[187] = ItemType.Familiar;
	itemConfig[188] = ItemType.Familiar;
	itemConfig[189] = ItemType.Passive;
	itemConfig[190] = ItemType.Passive;
	itemConfig[191] = ItemType.Passive;
	itemConfig[192] = ItemType.Active;
	itemConfig[193] = ItemType.Passive;
	itemConfig[194] = ItemType.Passive;
	itemConfig[195] = ItemType.Passive;
	itemConfig[196] = ItemType.Passive;
	itemConfig[197] = ItemType.Passive;
	itemConfig[198] = ItemType.Passive;
	itemConfig[199] = ItemType.Passive;
	itemConfig[200] = ItemType.Passive;
	itemConfig[201] = ItemType.Passive;
	itemConfig[202] = ItemType.Passive;
	itemConfig[203] = ItemType.Passive;
	itemConfig[204] = ItemType.Passive;
	itemConfig[205] = ItemType.Passive;
	itemConfig[206] = ItemType.Familiar;
	itemConfig[207] = ItemType.Familiar;
	itemConfig[208] = ItemType.Passive;
	itemConfig[209] = ItemType.Passive;
	itemConfig[210] = ItemType.Passive;
	itemConfig[211] = ItemType.Passive;
	itemConfig[212] = ItemType.Passive;
	itemConfig[213] = ItemType.Passive;
	itemConfig[214] = ItemType.Passive;
	itemConfig[215] = ItemType.Passive;
	itemConfig[216] = ItemType.Passive;
	itemConfig[217] = ItemType.Passive;
	itemConfig[218] = ItemType.Passive;
	itemConfig[219] = ItemType.Passive;
	itemConfig[220] = ItemType.Passive;
	itemConfig[221] = ItemType.Passive;
	itemConfig[222] = ItemType.Passive;
	itemConfig[223] = ItemType.Passive;
	itemConfig[224] = ItemType.Passive;
	itemConfig[225] = ItemType.Passive;
	itemConfig[226] = ItemType.Passive;
	itemConfig[227] = ItemType.Passive;
	itemConfig[228] = ItemType.Passive;
	itemConfig[229] = ItemType.Passive;
	itemConfig[230] = ItemType.Passive;
	itemConfig[231] = ItemType.Passive;
	itemConfig[232] = ItemType.Passive;
	itemConfig[233] = ItemType.Passive;
	itemConfig[234] = ItemType.Passive;
	itemConfig[236] = ItemType.Passive;
	itemConfig[237] = ItemType.Passive;
	itemConfig[238] = ItemType.Familiar;
	itemConfig[239] = ItemType.Familiar;
	itemConfig[240] = ItemType.Passive;
	itemConfig[241] = ItemType.Passive;
	itemConfig[242] = ItemType.Passive;
	itemConfig[243] = ItemType.Passive;
	itemConfig[244] = ItemType.Passive;
	itemConfig[245] = ItemType.Passive;
	itemConfig[246] = ItemType.Passive;
	itemConfig[247] = ItemType.Passive;
	itemConfig[248] = ItemType.Passive;
	itemConfig[249] = ItemType.Passive;
	itemConfig[250] = ItemType.Passive;
	itemConfig[251] = ItemType.Passive;
	itemConfig[252] = ItemType.Passive;
	itemConfig[253] = ItemType.Passive;
	itemConfig[254] = ItemType.Passive;
	itemConfig[255] = ItemType.Passive;
	itemConfig[256] = ItemType.Passive;
	itemConfig[257] = ItemType.Passive;
	itemConfig[258] = ItemType.Passive;
	itemConfig[259] = ItemType.Passive;
	itemConfig[260] = ItemType.Passive;
	itemConfig[261] = ItemType.Passive;
	itemConfig[262] = ItemType.Passive;
	itemConfig[263] = ItemType.Active;
	itemConfig[264] = ItemType.Familiar;
	itemConfig[265] = ItemType.Familiar;
	itemConfig[266] = ItemType.Familiar;
	itemConfig[267] = ItemType.Familiar;
	itemConfig[268] = ItemType.Familiar;
	itemConfig[269] = ItemType.Familiar;
	itemConfig[270] = ItemType.Familiar;
	itemConfig[271] = ItemType.Familiar;
	itemConfig[272] = ItemType.Familiar;
	itemConfig[273] = ItemType.Familiar;
	itemConfig[274] = ItemType.Familiar;
	itemConfig[275] = ItemType.Familiar;
	itemConfig[276] = ItemType.Familiar;
	itemConfig[277] = ItemType.Familiar;
	itemConfig[278] = ItemType.Familiar;
	itemConfig[279] = ItemType.Familiar;
	itemConfig[280] = ItemType.Familiar;
	itemConfig[281] = ItemType.Familiar;
	itemConfig[282] = ItemType.Active;
	itemConfig[283] = ItemType.Active;
	itemConfig[284] = ItemType.Active;
	itemConfig[285] = ItemType.Active;
	itemConfig[286] = ItemType.Active;
	itemConfig[287] = ItemType.Active;
	itemConfig[288] = ItemType.Active;
	itemConfig[289] = ItemType.Active;
	itemConfig[290] = ItemType.Active;
	itemConfig[291] = ItemType.Active;
	itemConfig[292] = ItemType.Active;
	itemConfig[293] = ItemType.Active;
	itemConfig[294] = ItemType.Active;
	itemConfig[295] = ItemType.Active;
	itemConfig[296] = ItemType.Active;
	itemConfig[297] = ItemType.Active;
	itemConfig[298] = ItemType.Active;
	itemConfig[299] = ItemType.Passive;
	itemConfig[300] = ItemType.Passive;
	itemConfig[301] = ItemType.Passive;
	itemConfig[302] = ItemType.Passive;
	itemConfig[303] = ItemType.Passive;
	itemConfig[304] = ItemType.Passive;
	itemConfig[305] = ItemType.Passive;
	itemConfig[306] = ItemType.Passive;
	itemConfig[307] = ItemType.Passive;
	itemConfig[308] = ItemType.Passive;
	itemConfig[309] = ItemType.Passive;
	itemConfig[310] = ItemType.Passive;
	itemConfig[311] = ItemType.Passive;
	itemConfig[312] = ItemType.Passive;
	itemConfig[313] = ItemType.Passive;
	itemConfig[314] = ItemType.Passive;
	itemConfig[315] = ItemType.Passive;
	itemConfig[316] = ItemType.Passive;
	itemConfig[317] = ItemType.Passive;
	itemConfig[318] = ItemType.Familiar;
	itemConfig[319] = ItemType.Familiar;
	itemConfig[320] = ItemType.Familiar;
	itemConfig[321] = ItemType.Familiar;
	itemConfig[322] = ItemType.Familiar;
	itemConfig[323] = ItemType.Active;
	itemConfig[324] = ItemType.Active;
	itemConfig[325] = ItemType.Active;
	itemConfig[326] = ItemType.Active;
	itemConfig[327] = ItemType.Passive;
	itemConfig[328] = ItemType.Passive;
	itemConfig[329] = ItemType.Passive;
	itemConfig[330] = ItemType.Passive;
	itemConfig[331] = ItemType.Passive;
	itemConfig[332] = ItemType.Passive;
	itemConfig[333] = ItemType.Passive;
	itemConfig[334] = ItemType.Passive;
	itemConfig[335] = ItemType.Passive;
	itemConfig[336] = ItemType.Passive;
	itemConfig[337] = ItemType.Passive;
	itemConfig[338] = ItemType.Active;
	itemConfig[339] = ItemType.Passive;
	itemConfig[340] = ItemType.Passive;
	itemConfig[341] = ItemType.Passive;
	itemConfig[342] = ItemType.Passive;
	itemConfig[343] = ItemType.Passive;
	itemConfig[344] = ItemType.Passive;
	itemConfig[345] = ItemType.Passive;
	itemConfig[346] = ItemType.Passive;
	itemConfig[347] = ItemType.Active;
	itemConfig[348] = ItemType.Active;
	itemConfig[349] = ItemType.Active;
	itemConfig[350] = ItemType.Passive;
	itemConfig[351] = ItemType.Active;
	itemConfig[352] = ItemType.Active;
	itemConfig[353] = ItemType.Passive;
	itemConfig[354] = ItemType.Passive;
	itemConfig[355] = ItemType.Passive;
	itemConfig[356] = ItemType.Passive;
	itemConfig[357] = ItemType.Active;
	itemConfig[358] = ItemType.Passive;
	itemConfig[359] = ItemType.Passive;
	itemConfig[360] = ItemType.Familiar;
	itemConfig[361] = ItemType.Familiar;
	itemConfig[362] = ItemType.Familiar;
	itemConfig[363] = ItemType.Familiar;
	itemConfig[364] = ItemType.Familiar;
	itemConfig[365] = ItemType.Familiar;
	itemConfig[366] = ItemType.Passive;
	itemConfig[367] = ItemType.Passive;
	itemConfig[368] = ItemType.Passive;
	itemConfig[369] = ItemType.Passive;
	itemConfig[370] = ItemType.Passive;
	itemConfig[371] = ItemType.Passive;
	itemConfig[372] = ItemType.Familiar;
	itemConfig[373] = ItemType.Passive;
	itemConfig[374] = ItemType.Passive;
	itemConfig[375] = ItemType.Passive;
	itemConfig[376] = ItemType.Passive;
	itemConfig[377] = ItemType.Passive;
	itemConfig[378] = ItemType.Passive;
	itemConfig[379] = ItemType.Passive;
	itemConfig[380] = ItemType.Passive;
	itemConfig[381] = ItemType.Passive;
	itemConfig[382] = ItemType.Active;
	itemConfig[383] = ItemType.Active;
	itemConfig[384] = ItemType.Familiar;
	itemConfig[385] = ItemType.Familiar;
	itemConfig[386] = ItemType.Active;
	itemConfig[387] = ItemType.Familiar;
	itemConfig[388] = ItemType.Familiar;
	itemConfig[389] = ItemType.Familiar;
	itemConfig[390] = ItemType.Familiar;
	itemConfig[391] = ItemType.Passive;
	itemConfig[392] = ItemType.Passive;
	itemConfig[393] = ItemType.Passive;
	itemConfig[394] = ItemType.Passive;
	itemConfig[395] = ItemType.Passive;
	itemConfig[396] = ItemType.Active;
	itemConfig[397] = ItemType.Passive;
	itemConfig[398] = ItemType.Passive;
	itemConfig[399] = ItemType.Passive;
	itemConfig[400] = ItemType.Passive;
	itemConfig[401] = ItemType.Passive;
	itemConfig[402] = ItemType.Passive;
	itemConfig[403] = ItemType.Familiar;
	itemConfig[404] = ItemType.Familiar;
	itemConfig[405] = ItemType.Familiar;
	itemConfig[406] = ItemType.Active;
	itemConfig[407] = ItemType.Passive;
	itemConfig[408] = ItemType.Passive;
	itemConfig[409] = ItemType.Passive;
	itemConfig[410] = ItemType.Passive;
	itemConfig[411] = ItemType.Passive;
	itemConfig[412] = ItemType.Passive;
	itemConfig[413] = ItemType.Passive;
	itemConfig[414] = ItemType.Passive;
	itemConfig[415] = ItemType.Passive;
	itemConfig[416] = ItemType.Passive;
	itemConfig[417] = ItemType.Familiar;
	itemConfig[418] = ItemType.Passive;
	itemConfig[419] = ItemType.Active;
	itemConfig[420] = ItemType.Passive;
	itemConfig[421] = ItemType.Active;
	itemConfig[422] = ItemType.Active;
	itemConfig[423] = ItemType.Passive;
	itemConfig[424] = ItemType.Passive;
	itemConfig[425] = ItemType.Passive;
	itemConfig[426] = ItemType.Familiar;
	itemConfig[427] = ItemType.Active;
	itemConfig[428] = ItemType.Passive;
	itemConfig[429] = ItemType.Passive;
	itemConfig[430] = ItemType.Familiar;
	itemConfig[431] = ItemType.Familiar;
	itemConfig[432] = ItemType.Passive;
	itemConfig[433] = ItemType.Passive;
	itemConfig[434] = ItemType.Active;
	itemConfig[435] = ItemType.Familiar;
	itemConfig[436] = ItemType.Familiar;
	itemConfig[437] = ItemType.Active;
	itemConfig[438] = ItemType.Passive;
	itemConfig[439] = ItemType.Active;
	itemConfig[440] = ItemType.Passive;
	itemConfig[441] = ItemType.Active;
	itemConfig[442] = ItemType.Passive;
	itemConfig[443] = ItemType.Passive;
	itemConfig[444] = ItemType.Passive;
	itemConfig[445] = ItemType.Passive;
	itemConfig[446] = ItemType.Passive;
	itemConfig[447] = ItemType.Passive;
	itemConfig[448] = ItemType.Passive;
	itemConfig[449] = ItemType.Passive;
	itemConfig[450] = ItemType.Passive;
	itemConfig[451] = ItemType.Passive;
	itemConfig[452] = ItemType.Passive;
	itemConfig[453] = ItemType.Passive;
	itemConfig[454] = ItemType.Passive;
	itemConfig[455] = ItemType.Passive;
	itemConfig[456] = ItemType.Passive;
	itemConfig[457] = ItemType.Passive;
	itemConfig[458] = ItemType.Passive;
	itemConfig[459] = ItemType.Passive;
	itemConfig[460] = ItemType.Passive;
	itemConfig[461] = ItemType.Passive;
	itemConfig[462] = ItemType.Passive;
	itemConfig[463] = ItemType.Passive;
	itemConfig[464] = ItemType.Passive;
	itemConfig[465] = ItemType.Passive;
	itemConfig[466] = ItemType.Passive;
	itemConfig[467] = ItemType.Familiar;
	itemConfig[468] = ItemType.Familiar;
	itemConfig[469] = ItemType.Familiar;
	itemConfig[470] = ItemType.Familiar;
	itemConfig[471] = ItemType.Familiar;
	itemConfig[472] = ItemType.Familiar;
	itemConfig[473] = ItemType.Familiar;
	itemConfig[474] = ItemType.Familiar;
	itemConfig[475] = ItemType.Active;
	itemConfig[476] = ItemType.Active;
	itemConfig[477] = ItemType.Active;
	itemConfig[478] = ItemType.Active;
	itemConfig[479] = ItemType.Active;
	itemConfig[480] = ItemType.Active;
	itemConfig[481] = ItemType.Active;
	itemConfig[482] = ItemType.Active;
	itemConfig[483] = ItemType.Active;
	itemConfig[484] = ItemType.Active;
	itemConfig[485] = ItemType.Active;
	itemConfig[486] = ItemType.Active;
	itemConfig[487] = ItemType.Active;
	itemConfig[488] = ItemType.Active;
	itemConfig[489] = ItemType.Active;
	itemConfig[490] = ItemType.Active;
	itemConfig[491] = ItemType.Familiar;
	itemConfig[492] = ItemType.Familiar;
	itemConfig[493] = ItemType.Passive;
	itemConfig[494] = ItemType.Passive;
	itemConfig[495] = ItemType.Passive;
	itemConfig[496] = ItemType.Passive;
	itemConfig[497] = ItemType.Passive;
	itemConfig[498] = ItemType.Passive;
	itemConfig[499] = ItemType.Passive;
	itemConfig[500] = ItemType.Familiar;
	itemConfig[501] = ItemType.Passive;
	itemConfig[502] = ItemType.Passive;
	itemConfig[503] = ItemType.Passive;
	itemConfig[504] = ItemType.Active;
	itemConfig[505] = ItemType.Passive;
	itemConfig[506] = ItemType.Passive;
	itemConfig[507] = ItemType.Active;
	itemConfig[508] = ItemType.Familiar;
	itemConfig[509] = ItemType.Familiar;
	itemConfig[510] = ItemType.Active;
	itemConfig[511] = ItemType.Familiar;
	itemConfig[512] = ItemType.Active;
	itemConfig[513] = ItemType.Passive;
	itemConfig[514] = ItemType.Passive;
	itemConfig[515] = ItemType.Active;
	itemConfig[516] = ItemType.Active;
	itemConfig[517] = ItemType.Passive;
	itemConfig[518] = ItemType.Familiar;
	itemConfig[519] = ItemType.Familiar;
	itemConfig[520] = ItemType.Passive;
	itemConfig[521] = ItemType.Active;
	itemConfig[522] = ItemType.Active;
	itemConfig[523] = ItemType.Active;
	itemConfig[524] = ItemType.Passive;
	itemConfig[525] = ItemType.Familiar;
	itemConfig[526] = ItemType.Familiar;
	itemConfig[527] = ItemType.Active;
	itemConfig[528] = ItemType.Familiar;
	itemConfig[529] = ItemType.Passive;
	itemConfig[530] = ItemType.Passive;
	itemConfig[531] = ItemType.Passive;
	itemConfig[532] = ItemType.Passive;
	itemConfig[533] = ItemType.Passive;
	itemConfig[534] = ItemType.Passive;
	itemConfig[535] = ItemType.Passive;
	itemConfig[536] = ItemType.Active;
	itemConfig[537] = ItemType.Familiar;
	itemConfig[538] = ItemType.Passive;
	itemConfig[539] = ItemType.Familiar;
	itemConfig[540] = ItemType.Passive;
	itemConfig[541] = ItemType.Passive;
	itemConfig[542] = ItemType.Familiar;
	itemConfig[543] = ItemType.Familiar;
	itemConfig[544] = ItemType.Familiar;
	itemConfig[545] = ItemType.Active;
	itemConfig[546] = ItemType.Passive;
	itemConfig[547] = ItemType.Passive;
	itemConfig[548] = ItemType.Familiar;
	itemConfig[549] = ItemType.Passive;
	itemConfig[550] = ItemType.Active;
	itemConfig[551] = ItemType.Passive;
	itemConfig[552] = ItemType.Active;
	itemConfig[553] = ItemType.Passive;
	itemConfig[554] = ItemType.Passive;
	itemConfig[555] = ItemType.Active;
	itemConfig[556] = ItemType.Active;
	itemConfig[557] = ItemType.Active;
	itemConfig[558] = ItemType.Passive;
	itemConfig[559] = ItemType.Passive;
	itemConfig[560] = ItemType.Passive;
	itemConfig[561] = ItemType.Passive;
	itemConfig[562] = ItemType.Passive;
	itemConfig[563] = ItemType.Passive;
	itemConfig[564] = ItemType.Passive;
	itemConfig[565] = ItemType.Familiar;
	itemConfig[566] = ItemType.Passive;
	itemConfig[567] = ItemType.Familiar;
	itemConfig[568] = ItemType.Passive;
	itemConfig[569] = ItemType.Familiar;
	itemConfig[570] = ItemType.Passive;
	itemConfig[571] = ItemType.Passive;
	itemConfig[572] = ItemType.Passive;
	itemConfig[573] = ItemType.Passive;
	itemConfig[574] = ItemType.Passive;
	itemConfig[575] = ItemType.Familiar;
	itemConfig[576] = ItemType.Passive;
	itemConfig[577] = ItemType.Active;
	itemConfig[578] = ItemType.Active;
	itemConfig[579] = ItemType.Passive;
	itemConfig[580] = ItemType.Active;
	itemConfig[581] = ItemType.Familiar;
	itemConfig[582] = ItemType.Active;
	itemConfig[583] = ItemType.Passive;
	itemConfig[584] = ItemType.Passive;
	itemConfig[585] = ItemType.Active;
	itemConfig[586] = ItemType.Passive;
	itemConfig[588] = ItemType.Passive;
	itemConfig[589] = ItemType.Passive;
	itemConfig[590] = ItemType.Passive;
	itemConfig[591] = ItemType.Passive;
	itemConfig[592] = ItemType.Passive;
	itemConfig[593] = ItemType.Passive;
	itemConfig[594] = ItemType.Passive;
	itemConfig[595] = ItemType.Passive;
	itemConfig[596] = ItemType.Passive;
	itemConfig[597] = ItemType.Passive;
	itemConfig[598] = ItemType.Passive;
	itemConfig[599] = ItemType.Passive;
	itemConfig[600] = ItemType.Passive;
	itemConfig[601] = ItemType.Passive;
	itemConfig[602] = ItemType.Passive;
	itemConfig[603] = ItemType.Passive;
	itemConfig[604] = ItemType.Active;
	itemConfig[605] = ItemType.Active;
	itemConfig[606] = ItemType.Passive;
	itemConfig[607] = ItemType.Familiar;
	itemConfig[608] = ItemType.Familiar;
	itemConfig[609] = ItemType.Active;
	itemConfig[610] = ItemType.Familiar;
	itemConfig[611] = ItemType.Active;
	itemConfig[612] = ItemType.Familiar;
	itemConfig[614] = ItemType.Passive;
	itemConfig[615] = ItemType.Familiar;
	itemConfig[616] = ItemType.Passive;
	itemConfig[617] = ItemType.Passive;
	itemConfig[618] = ItemType.Passive;
	itemConfig[619] = ItemType.Passive;
	itemConfig[621] = ItemType.Passive;
	itemConfig[622] = ItemType.Active;
	itemConfig[623] = ItemType.Active;
	itemConfig[624] = ItemType.Passive;
	itemConfig[625] = ItemType.Active;
	itemConfig[626] = ItemType.Familiar;
	itemConfig[627] = ItemType.Familiar;
	itemConfig[628] = ItemType.Active;
	itemConfig[629] = ItemType.Familiar;
	itemConfig[631] = ItemType.Active;
	itemConfig[632] = ItemType.Passive;
	itemConfig[633] = ItemType.Passive;
	itemConfig[634] = ItemType.Passive;
	itemConfig[635] = ItemType.Active;
	itemConfig[636] = ItemType.Active;
	itemConfig[637] = ItemType.Passive;
	itemConfig[638] = ItemType.Active;
	itemConfig[639] = ItemType.Active;
	itemConfig[640] = ItemType.Active;
	itemConfig[641] = ItemType.Passive;
	itemConfig[642] = ItemType.Active;
	itemConfig[643] = ItemType.Passive;
	itemConfig[644] = ItemType.Passive;
	itemConfig[645] = ItemType.Familiar;
	itemConfig[646] = ItemType.Passive;
	itemConfig[647] = ItemType.Passive;
	itemConfig[649] = ItemType.Familiar;
	itemConfig[650] = ItemType.Active;
	itemConfig[651] = ItemType.Familiar;
	itemConfig[652] = ItemType.Familiar;
	itemConfig[653] = ItemType.Active;
	itemConfig[654] = ItemType.Passive;
	itemConfig[655] = ItemType.Active;
	itemConfig[656] = ItemType.Familiar;
	itemConfig[657] = ItemType.Passive;
	itemConfig[658] = ItemType.Passive;
	itemConfig[659] = ItemType.Passive;
	itemConfig[660] = ItemType.Passive;
	itemConfig[661] = ItemType.Passive;
	itemConfig[663] = ItemType.Passive;
	itemConfig[664] = ItemType.Passive;
	itemConfig[665] = ItemType.Passive;
	itemConfig[667] = ItemType.Familiar;
	itemConfig[668] = ItemType.Passive;
	itemConfig[669] = ItemType.Passive;
	itemConfig[670] = ItemType.Passive;
	itemConfig[671] = ItemType.Passive;
	itemConfig[672] = ItemType.Passive;
	itemConfig[673] = ItemType.Passive;
	itemConfig[674] = ItemType.Passive;
	itemConfig[675] = ItemType.Passive;
	itemConfig[676] = ItemType.Passive;
	itemConfig[677] = ItemType.Passive;
	itemConfig[678] = ItemType.Passive;
	itemConfig[679] = ItemType.Familiar;
	itemConfig[680] = ItemType.Passive;
	itemConfig[681] = ItemType.Familiar;
	itemConfig[682] = ItemType.Familiar;
	itemConfig[683] = ItemType.Passive;
	itemConfig[684] = ItemType.Passive;
	itemConfig[685] = ItemType.Active;
	itemConfig[686] = ItemType.Passive;
	itemConfig[687] = ItemType.Active;
	itemConfig[688] = ItemType.Passive;
	itemConfig[689] = ItemType.Passive;
	itemConfig[690] = ItemType.Passive;
	itemConfig[691] = ItemType.Passive;
	itemConfig[692] = ItemType.Passive;
	itemConfig[693] = ItemType.Passive;
	itemConfig[694] = ItemType.Passive;
	itemConfig[695] = ItemType.Passive;
	itemConfig[696] = ItemType.Passive;
	itemConfig[697] = ItemType.Familiar;
	itemConfig[698] = ItemType.Familiar;
	itemConfig[699] = ItemType.Passive;
	itemConfig[700] = ItemType.Passive;
	itemConfig[701] = ItemType.Passive;
	itemConfig[702] = ItemType.Passive;
	itemConfig[703] = ItemType.Active;
	itemConfig[704] = ItemType.Active;
	itemConfig[705] = ItemType.Active;
	itemConfig[706] = ItemType.Active;
	itemConfig[707] = ItemType.Passive;
	itemConfig[708] = ItemType.Passive;
	itemConfig[709] = ItemType.Active;
	itemConfig[710] = ItemType.Active;
	itemConfig[711] = ItemType.Active;
	itemConfig[712] = ItemType.Active;
	itemConfig[713] = ItemType.Active;
	itemConfig[714] = ItemType.Active;
	itemConfig[715] = ItemType.Active;
	itemConfig[716] = ItemType.Passive;
	itemConfig[717] = ItemType.Passive;
	itemConfig[719] = ItemType.Active;
	itemConfig[720] = ItemType.Active;
	itemConfig[721] = ItemType.Passive;
	itemConfig[722] = ItemType.Active;
	itemConfig[723] = ItemType.Active;
	itemConfig[724] = ItemType.Passive;
	itemConfig[725] = ItemType.Passive;
	itemConfig[726] = ItemType.Passive;
	itemConfig[727] = ItemType.Passive;
	itemConfig[728] = ItemType.Active;
	itemConfig[729] = ItemType.Active;
	itemConfig[730] = ItemType.Passive;
	itemConfig[731] = ItemType.Passive;
	itemConfig[732] = ItemType.Passive;

	return itemConfig;
}
