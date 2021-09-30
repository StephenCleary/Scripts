<Query Kind="Program" />

void Main()
{
	decimal usd = 142.59M;
	decimal totalShares = 1.1077M;

	// The number of shares before the transfer.
	decimal oldVestedShares = 576.6717M;
	decimal oldMatchingShares = 32.4096M;

	// The new balances.
	decimal newVestedShares = 577.7204M;
	decimal newMatchingShares = 32.4686M;

	decimal vestedShares = newVestedShares - oldVestedShares;
	decimal matchingShares = newMatchingShares - oldMatchingShares;

	if (vestedShares + matchingShares != totalShares)
		throw new InvalidOperationException("Doesn't add up.");

	var (vestedUsd, matchingUsd) = Share(vestedShares, matchingShares, usd, 2);

	new {
		Title = "Vested",
		Shares = vestedShares,
		USD = vestedUsd,
	}.Dump();

	new
	{
		Title = "Matching",
		Shares = matchingShares,
		USD = matchingUsd,
	}.Dump();
}

(decimal a, decimal b) Share(decimal totalA, decimal totalB, decimal transfer, int decimalPlaces)
{
	var combined = totalA + totalB;
	var a = Math.Round(transfer * totalA / combined, decimalPlaces);
	return (a, transfer - a);
}