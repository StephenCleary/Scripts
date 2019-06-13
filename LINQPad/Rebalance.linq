<Query Kind="Program" />

void Main()
{
	decimal usd = 97.71M;
	decimal aShares = 0.9743M;
	decimal bShares = -1.1835M;

	// The number of shares before the transfer.
	decimal oldMatchingAShares = 5.1284M;
	decimal oldMatchingBShares = 6.3315M;
	decimal oldVestedAShares = 121.6866M;
	decimal oldVestedBShares = 150.0846M;

	// The new balances.
	decimal newVestedAShares = 122.619M;
	decimal newVestedBShares = 148.952M;
	decimal newMatchingAShares = 5.1703M;
	decimal newMatchingBShares = 6.2806M;

	decimal transferredVestedAShares = newVestedAShares - oldVestedAShares;
	decimal transferredVestedBShares = newVestedBShares - oldVestedBShares;
	decimal transferredMatchingAShares = newMatchingAShares - oldMatchingAShares;
	decimal transferredMatchingBShares = newMatchingBShares - oldMatchingBShares;
	
	if (transferredVestedAShares + transferredMatchingAShares != aShares)
		throw new InvalidOperationException("Doesn't add up.");
	if (transferredVestedBShares + transferredMatchingBShares != bShares)
		throw new InvalidOperationException("Doesn't add up.");

	var (vestedAusd, matchingAusd) = Share(transferredVestedAShares, transferredMatchingAShares, usd, 2);
	var (vestedBusd, matchingBusd) = Share(transferredVestedBShares, transferredMatchingBShares, usd, 2);

	if (vestedAusd != vestedBusd)
		throw new InvalidOperationException("Doesn't add up.");
	if (matchingAusd != matchingBusd)
		throw new InvalidOperationException("Doesn't add up.");

	new {
		Title = "Vested",
		AShares = transferredVestedAShares,
		USD = vestedAusd,
		BShares = transferredVestedBShares,
	}.Dump();

	new
	{
		Title = "Matching",
		AShares = transferredMatchingAShares,
		USD = matchingAusd,
		BShares = transferredMatchingBShares,
	}.Dump();
}

(decimal a, decimal b) Share(decimal totalA, decimal totalB, decimal transfer, int decimalPlaces)
{
	var combined = totalA + totalB;
	var a = Math.Round(transfer * totalA / combined, decimalPlaces);
	return (a, transfer - a);
}