<Query Kind="Program" />

void Main()
{
	decimal usd = 119.76M;
	decimal totalShares = 1.0668M;

	// The number of shares before the transfer.
	decimal oldVestedShares = 554.2412M;
	decimal oldMatchingShares = 30.2128M;

	// The new balances.
	decimal newVestedShares = 555.2531M;
	decimal newMatchingShares = 30.2677M;

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