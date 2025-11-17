<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
</Query>

async Task Main()
{
	using var client = new HttpClient();

	// OAuth oauth_consumer_key=""49A0531F71FFC15B35E66B354174DDA26767BBEC"",oauth_signature=""E312CE14173A4AB9EF7E977618FA835F98EA2DE6%266CA93CD9874573325841EC26BC9DD108B827533C"",oauth_signature_method=""PLAINTEXT"",oauth_version=""1.0"",oauth_token=""C439B5AB4E536262353C47E0C248E644EB643B67""
	client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", @"oauth_consumer_key=""49A0531F71FFC15B35E66B354174DDA26767BBEC"",oauth_signature=""E312CE14173A4AB9EF7E977618FA835F98EA2DE6%266CA93CD9874573325841EC26BC9DD108B827533C"",oauth_signature_method=""PLAINTEXT"",oauth_version=""1.0"",oauth_token=""C439B5AB4E536262353C47E0C248E644EB643B67""");

	var response = await client.SendAsync(CallLibraryApi3());
	response.StatusCode.Dump();
	var json = await response.Content.ReadAsStringAsync();
	json.Dump();
}

HttpRequestMessage CallLibraryApi()
{
	return new HttpRequestMessage()
	{
		// Replace ':' with '$'
		RequestUri = new Uri("https://libapi.logos.com/v1/metadata/lls$cs101framevideos.json"),
	};
}

HttpRequestMessage CallLibraryApi2()
{
	return new HttpRequestMessage()
	{
		// Replace ':' with '$'
		RequestUri = new Uri("https://libapi.logos.com/v1/tocIndex/lls$cs101framevideos.json"),
	};
}

HttpRequestMessage CallLibraryApi3()
{
	return new HttpRequestMessage()
	{
		// Replace ':' with '$'
		RequestUri = new Uri("https://libapi.logos.com/v1/bundle/lls$cs101framevideos.zip"),
	};
}

HttpRequestMessage CallAccountsApi()
{
	return new HttpRequestMessage()
	{
		RequestUri = new Uri("https://accountsapi.logos.com/v2/users/me"),
	};
}

HttpRequestMessage CallResourcesApi()
{
	return new HttpRequestMessage()
	{
		Method = HttpMethod.Post,
		RequestUri = new Uri("https://resourcesapi.logos.com/v5/files"), // ?next=djN8CKmTChISCRNxou718EJJEbfygMCyf1ho&fields=islastpage%2Cnext%2Cresourcefileinfos.resourceid%2Cresourcefileinfos.version%2Cresourcefileinfos.location%2Cresourcefileinfos.size",
		Content = new StringContent("{}", Encoding.UTF8, "application/json"),
	};
}