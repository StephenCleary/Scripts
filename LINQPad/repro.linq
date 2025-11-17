<Query Kind="Program">
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

void Main()
{
}

private async void button1_Click(object sender, EventArgs e)
{
	var dataSource = new DataFactory();

	IAsyncEnumerable<int> data = dataSource.GetData();

	await foreach (var item in data.ConfigureAwait(false))
	{
		Console.WriteLine(item);
		DataFactory.PrintContext("Data - item: " + item);
	}
}

public class DataFactory
{
	public async IAsyncEnumerable<int> GetData()
	{
		for (int i = 0; i < 10; i++)
		{
			await Task.Delay(250).ConfigureAwait(true);
			PrintContext("Generating Data: " + i);
			yield return i;
		}
	}

	public static void PrintContext(string message)
	{
		Debug.WriteLine($"{message}, TID={Environment.CurrentManagedThreadId}, SyncContext.Current={(SynchronizationContext.Current?.GetType().Name?.ToString() ?? "null")}");
	}
}
