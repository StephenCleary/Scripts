<Query Kind="Program" />

void Main()
{
	decimal usd = 15873.48M;
	decimal aShares = 183.7421M;
	decimal bShares = -151.7251M;

	// The number of shares before the transfer.
	decimal oldMatchingAShares = 8.8772M;
	decimal oldMatchingBShares = 7.3165M;
	decimal oldVestedAShares = 175.3323M;
	decimal oldVestedBShares = 144.4086M;

	// The new balances.
	decimal newVestedAShares = 350.2140M;
	decimal newVestedBShares = 0M;
	decimal newMatchingAShares = 17.7376M;
	decimal newMatchingBShares = 0M;

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