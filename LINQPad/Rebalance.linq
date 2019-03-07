<Query Kind="Program" />

void Main()
{
	decimal usd = 53.84M;
	decimal aShares = 0.5652M;
	decimal bShares = -0.7013M;

	// The number of shares before the transfer.
	decimal oldMatchingAShares = 3.7749M;
	decimal oldMatchingBShares = 4.7525M;
	decimal oldVestedAShares = 77.3825M;
	decimal oldVestedBShares = 97.3545M;

	// The new balances.
	decimal newVestedAShares = 77.9202M;
	decimal newVestedBShares = 96.6873M;
	decimal newMatchingAShares = 3.8024M;
	decimal newMatchingBShares = 4.7184M;

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