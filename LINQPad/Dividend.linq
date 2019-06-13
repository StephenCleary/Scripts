<Query Kind="Program" />

void Main()
{
	decimal usd = 63.96M;
	decimal totalShares = 0.6366M;

	// The number of shares before the transfer.
	decimal oldVestedShares = 106.7517M;
	decimal oldMatchingShares = 4.6759M;

	// The new balances.
	decimal newVestedShares = 107.3616M;
	decimal newMatchingShares = 4.7026M;

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