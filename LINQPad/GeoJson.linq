<Query Kind="Statements">
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
</Query>

var json = File.ReadAllText(@"C:\Users\steph\Downloads\takeout-20211226T222328Z-001\Takeout\Location History\Location History.json");

var file = JsonSerializer.Deserialize<LocationHistory>(json);

file.Dump();

sealed class LocationHistory
{
	public List<Location> locations { get; set; }
}

sealed class Location
{
	public string timestampMs { get; set; }
	public long latitudeE7 { get; set; }
	public long longitudeE7 { get; set; }
	public int accuracy { get; set; }
	public int altitude { get; set; }
	public int verticalAccuracy { get; set; }
}