<Query Kind="Statements" />

var bitSize = 32768;

var maxSetSizeByNumberOfHashes = new SortedDictionary<int, int>();

for (var i = 256; i != 22714; ++i)
{
	var numberOfHashesDouble = (double)bitSize / i * Math.Log(2.0);
	var (numberOfHashesCeiling, numberOfHashesFloor) = ((int)Math.Ceiling(numberOfHashesDouble), (int)Math.Floor(numberOfHashesDouble));
	var (falseProbabilityCeiling, falseProbabilityFloor) = (FalseProbability(numberOfHashesCeiling, i), FalseProbability(numberOfHashesFloor, i));
	var (numberOfHashes, falseProbability) = falseProbabilityCeiling < falseProbabilityFloor ? (numberOfHashesCeiling, falseProbabilityCeiling) : (numberOfHashesFloor, falseProbabilityFloor);

	maxSetSizeByNumberOfHashes[numberOfHashes] = i;
}

maxSetSizeByNumberOfHashes.Dump();

foreach (var setSize in new[] { 512, 768, 1024, 1536, 2048, 3072, 4096, 10000, 15768, 15769, 22713, 22714, 65536, 80000, 100000, 131072, 1000000 })
{
	// TODO: ~cache~ precalculate number of hashes by set size.
	var numberOfHashesDouble = (double)bitSize / setSize * Math.Log(2.0);
	var (numberOfHashesCeiling, numberOfHashesFloor) = ((int)Math.Ceiling(numberOfHashesDouble), (int)Math.Floor(numberOfHashesDouble));
	var (falseProbabilityCeiling, falseProbabilityFloor) = (FalseProbability(numberOfHashesCeiling, setSize), FalseProbability(numberOfHashesFloor, setSize));
	var (numberOfHashes, falseProbability) = falseProbabilityCeiling < falseProbabilityFloor ? (numberOfHashesCeiling, falseProbabilityCeiling) : (numberOfHashesFloor, falseProbabilityFloor);
	new { setSize, numberOfHashes, falseProbability }.Dump();
	
	var quickCheckLow = FalseProbability(numberOfHashes - 1, setSize);
	var quickCheckHigh = FalseProbability(numberOfHashes + 1, setSize);
	if (quickCheckLow < falseProbability)
		new { setSize, BetterNumberOfHashes = numberOfHashes-1, falseProbability }.Dump();
	if (quickCheckHigh < falseProbability)
		new { setSize, BetterNumberOfHashes = numberOfHashes+1, falseProbability }.Dump();
}

double FalseProbability(int numberOfHashes, int setSize) => Math.Pow((1 - Math.Exp(-numberOfHashes * setSize / (double)bitSize)), numberOfHashes);