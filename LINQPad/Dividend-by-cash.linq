<Query Kind="Program" />

void Main()
{
	decimal usd = 35.51M;
	decimal totalShares = 0.4099M;

	// The number of shares before the transfer.
	decimal oldVestedShares = 174.9419M;
	decimal oldMatchingShares = 8.8577M;

	// The earnings.
	decimal vestedUsd = 33.82M;
	decimal matchingUsd = 1.69M;

	if (vestedUsd + matchingUsd != usd)
		throw new InvalidOperationException("Doesn't add up.");

	var (vestedShares, matchingShares) = Share(vestedUsd, matchingUsd, totalShares, 4);

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