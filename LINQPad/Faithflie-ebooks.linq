<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net</Namespace>
</Query>

async Task Main()
{
	var cookieContainer = new CookieContainer();
	using var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
	using var client = new HttpClient(handler);

	cookieContainer.Add(new Cookie("viewportWidth", "1920", "/", "www.logos.com"));
	cookieContainer.Add(new Cookie("faithlifesites.com", Util.GetPassword("faithlifesites.com cookie for www.logos.com"), "/", "www.logos.com"));

	cookieContainer.Add(new Cookie("viewportWidth", "1920", "/", "ebooks.faithlife.com"));
	cookieContainer.Add(new Cookie("faithlifesites.com", Util.GetPassword("faithlifesites.com cookie for ebooks.faithlife.com"), "/", "ebooks.faithlife.com"));

	cookieContainer.Add(new Cookie("viewportWidth", "1920", "/", "faithlife.com"));
	cookieContainer.Add(new Cookie("faithlifesites.com", Util.GetPassword("faithlifesites.com cookie for faithlife.com"), "/", "faithlife.com"));

	await BuyAllTheThingsAsync(client, new FaithlifeStoreCartSystem(), new LogosCartSystem(), new FaithlifeEbooksCartSystem());
}

async Task BuyAllTheThingsAsync(HttpClient client, params ICartSystem[] carts)
{
	var counts = new CartCounts();
	var engines = carts.Select(cart => new Engine { Client = client, Counts = counts, Cart = cart }).ToList();
	while (true)
	{
		new { Cart = counts, Engines = engines.Select(x => new { Name = x.Cart.ToString(), Page = x.Page, Total = x.PageCount }) }.Dump($"NextPage");

		await Task.WhenAll(engines.Select(x => x.ProcessPageAsync()));
		if (counts.NeedsCheckout())
		{
			Util.ReadLine($"Cart has 100 items. Checkout all carts.");
			counts.Reset();
			foreach (var engine in engines)
				engine.Backup();
		}
		
		if (engines.All(x => x.Done))
			break;
			
		await Task.Delay(500);
	}
	
	if (counts.HasProducts())
	{
		Util.ReadLine($"Cart has items. Checkout all carts.");
	}
}

public sealed class Engine
{
	public HttpClient Client { get; init; }
	public CartCounts Counts { get; init; }
	public ICartSystem Cart { get; init; }
	public bool Done { get; private set; }
	public int Page { get; private set; } = 1;
	public int PageCount { get; private set; }
	
	public void Backup()
	{
		Done = false;
		Page -= 4; // back up three pages (180 complete entries), plus an extra one because the page was incremented.
	}
	
	public async Task ProcessPageAsync()
	{
		if (Done)
			return;
		
		var url = Cart.SearchUrl(Page);
		var str = await Client.GetStringAsync(url);
		var json = JObject.Parse(str);
		var pageCount = (int)json["pageCount"];
		if (pageCount == 0)
			throw new Exception($"Authentication issue when accessing {url}; please remove cookie passwords for this site and retry.");
		PageCount = pageCount;
		foreach (var product in json["productsViewModel"]["products"])
		{
			if ((string)product["usdPrice"] != "$0.00")
				continue;
			var addToCartUrl = Cart.AddToCartUrl((string)product["routes"]["AddToCart"]);
			Counts.Increment(addToCartUrl);
			await Client.GetStringAsync(addToCartUrl);

			new
			{
				Title = product["titleHtml"]?.ToString(),
				Author = product["authors"]?.ToString(),
				AddToCartUrl = new Hyperlinq(addToCartUrl),
				CartSize = Counts,
				Page = Page,
				PageCount = pageCount,
			}.Dump("Added to cart");
		}

		if (Page == pageCount)
			Done = true;
		++Page;
	}
}

public sealed class CartCounts
{
	private object _mutex = new();
	public int EbooksCartSize { get; private set; }
	public int LogosCartSize { get; private set; }
	public int StoreCartSize { get; private set; }
	public int UnknownCartSize { get; private set; }
	
	public void Increment(string url)
	{
		lock (_mutex)
		{
			if (url.StartsWith("https://ebooks.faithlife.com/"))
				++EbooksCartSize;
			else if (url.StartsWith("https://logos.com/"))
				++LogosCartSize;
			else if (url.StartsWith("https://faithlife.com/store"))
				++StoreCartSize;
			else
				++UnknownCartSize;
		}
	}

	public bool HasProducts()
	{
		lock (_mutex)
			return EbooksCartSize != 0 || LogosCartSize != 0 || StoreCartSize != 0;
	}
	public bool NeedsCheckout()
	{
		lock (_mutex)
			return EbooksCartSize == 100 || LogosCartSize == 100 || StoreCartSize == 100;
	}
	public void Reset()
	{
		lock (_mutex)
			EbooksCartSize = LogosCartSize = StoreCartSize = 0;
	}
}

public interface ICartSystem
{
	string SearchUrl(int page);
	string AddToCartUrl(string url);
}

public sealed class FaithlifeEbooksCartSystem : ICartSystem
{
	public bool AllLanguages { get; set; }
	public bool Oldest { get; set; }
	private string SortBy => Oldest ? "Oldest" : "Newest";
	public string SearchUrl(int page) => AllLanguages ?
		$"https://ebooks.faithlife.com/search?sortBy={SortBy}&limit=60&page={page}&filters=status-live_Status&ownership=unowned&geographicAvailability=availableToMe&isAjax=true" :
		$"https://ebooks.faithlife.com/search?sortBy={SortBy}&limit=60&page={page}&filters=status-live_Status%2Blanguage-english_Language&ownership=unowned&geographicAvailability=availableToMe&isAjax=true";
	public string AddToCartUrl(string url) => url.StartsWith("http") ?
		url :
		$"https://ebooks.faithlife.com{url}";
	public override string ToString() => "ebooks.faithlife.com";
}

public sealed class LogosCartSystem : ICartSystem
{
	public bool AllLanguages { get; set; }
	public bool Oldest { get; set; }
	private string SortBy => Oldest ? "Oldest" : "Newest";
	public string SearchUrl(int page) => AllLanguages ?
		$"https://www.logos.com/search?filters=%2Bstatus-live_Status&limit=60&page={page}&ownership=unowned&geographicAvailability=availableToMe&sortBy={SortBy}&autoFacets=0&isAjax=true" :
		$"https://www.logos.com/search?filters=%2Bstatus-live_Status%2Blanguage-english_Language&limit=60&page={page}&ownership=unowned&geographicAvailability=availableToMe&sortBy={SortBy}&autoFacets=0&isAjax=true";
	public string AddToCartUrl(string url) => url.StartsWith("http") ?
		url :
		$"https://logos.com{url}";
	public override string ToString() => "logos.com";
}

public sealed class FaithlifeStoreCartSystem : ICartSystem
{
	public bool AllLanguages { get; set; }
	public bool Oldest { get; set; }
	private string SortBy => Oldest ? "Oldest" : "Newest";
	public string SearchUrl(int page) => AllLanguages ?
		$"https://faithlife.com/store/search?sortBy={SortBy}&limit=60&page={page}&filters=status-live_Status&ownership=unowned&geographicAvailability=availableToMe&isAjax=true" :
		$"https://faithlife.com/store/search?sortBy={SortBy}&limit=60&page={page}&filters=status-live_Status%2Blanguage-english_Language&ownership=unowned&geographicAvailability=availableToMe&isAjax=true";
	public string AddToCartUrl(string url) => url.StartsWith("http") ?
		url :
		$"https://faithlife.com{url}";
	public override string ToString() => "faithlife.com/store";
}
