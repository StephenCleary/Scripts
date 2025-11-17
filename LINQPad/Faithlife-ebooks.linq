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
	var output = new DumpContainer().Dump();

	cookieContainer.Add(new Cookie("viewportWidth", "1920", "/", "www.logos.com"));
	cookieContainer.Add(new Cookie("faithlifesites.com", Util.GetPassword("faithlifesites.com cookie for www.logos.com"), "/", "www.logos.com"));

	cookieContainer.Add(new Cookie("viewportWidth", "1920", "/", "ebooks.faithlife.com"));
	cookieContainer.Add(new Cookie("faithlifesites.com", Util.GetPassword("faithlifesites.com cookie for ebooks.faithlife.com"), "/", "ebooks.faithlife.com"));

//	cookieContainer.Add(new Cookie("viewportWidth", "1920", "/", "faithlife.com"));
//	cookieContainer.Add(new Cookie("faithlifesites.com", Util.GetPassword("faithlifesites.com cookie for faithlife.com"), "/", "faithlife.com"));
//
//	cookieContainer.Add(new Cookie("viewportWidth", "1920", "/", "curriculum.faithlife.com"));
//	cookieContainer.Add(new Cookie("faithlifesites.com", Util.GetPassword("faithlifesites.com cookie for curriculum.faithlife.com"), "/", "curriculum.faithlife.com"));

	await BuyAllTheThingsAsync(client, output, new LogosCartSystem(), new FaithlifeEbooksCartSystem());//, new FaithlifeStoreCartSystem()); //, new CurriculumCartSystem());
}

async Task BuyAllTheThingsAsync(HttpClient client, DumpContainer output, params ICartSystem[] carts)
{
	var counts = new CartCounts();

	foreach (var ownership in new[] { /*"partiallyOwned", "unowned",*/ "all" })
	{
		foreach (var cart in carts)
			cart.Ownership = ownership;
		
		var engines = carts.Select(cart => new Engine { Client = client, Counts = counts, Cart = cart }).ToList();
		while (true)
		{
			output.Content = new { Cart = counts.EbooksCartSize + counts.LogosCartSize, Ownership = ownership, Engines = engines.Select(x => $"{x.Cart}: {x.Page}/{x.PageCount}") };

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

//			await Task.Delay(100);
		}
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
		async Task<string> TryGetPageAsync(string url)
		{
			try
			{
				return await Client.GetStringAsync(url);
			}
			catch (HttpRequestException ex) when (ex.Message.Contains("429") || ex.Message.Contains("500") || ex.Message.Contains("504"))
			{
				await Task.Delay(2000);
				return null;
			}
		}
		
		if (Done)
			return;
		
		var url = Cart.SearchUrl(Page);
		var str = await TryGetPageAsync(url);
		if (str == null)
			return;
		var json = JObject.Parse(str);
		var pageCount = (int)json["pageCount"];
		if (pageCount == 0)
			return; //throw new Exception($"Authentication issue when accessing {url}; please remove cookie passwords for this site and retry.");
		PageCount = pageCount;
		foreach (var product in json["productsViewModel"]["products"])
		{
			if ((string)product["usdPrice"] != "$0.00")
				continue;
			var productPageUrl = product["productPageUrl"]?.ToString();
			if (Constants.Blacklist.Any(x => productPageUrl.Contains($"/{x}/")))
				continue;
				
			if ((bool)product["isOwnedByUser"])
				continue;
				
			var addToCartUrl = Cart.AddToCartUrl((string)product["routes"]["AddToCart"]);
			Counts.Increment(addToCartUrl);

			try { await Client.GetStringAsync(addToCartUrl); }
			catch (Exception ex) { (addToCartUrl, ex).Dump(); }

			new
			{
				Title = product["titleHtml"]?.ToString(),
				Author = product["authors"]?.ToString(),
				AddToCartUrl = new Hyperlinq(addToCartUrl),
				ProductPageUrl = new Hyperlinq(product["productPageUrl"]?.ToString()),
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
	//public int StoreCartSize { get; private set; }
	public int UnknownCartSize { get; private set; }
	
	public void Increment(string url)
	{
		lock (_mutex)
		{
			if (url.StartsWith("https://ebooks.faithlife.com/"))
				++EbooksCartSize;
			else if (url.StartsWith("https://logos.com/"))
				++LogosCartSize;
			//else if (url.StartsWith("https://faithlife.com/store"))
			//	++StoreCartSize;
			else
			{
				$"Unknown cart for URL {url}".Dump();
				++UnknownCartSize;
			}
		}
	}

	public bool HasProducts()
	{
		lock (_mutex)
		
			return EbooksCartSize != 0 || LogosCartSize != 0;// || StoreCartSize != 0;
	}
	public bool NeedsCheckout()
	{
		lock (_mutex)
			return EbooksCartSize >= 100 || LogosCartSize >= 100;// || StoreCartSize == 100;
	}
	public void Reset()
	{
		lock (_mutex)
			EbooksCartSize = LogosCartSize = 0;// = StoreCartSize = 0;
	}
}

public interface ICartSystem
{
	string Ownership { get; set; }
	string SearchUrl(int page);
	string AddToCartUrl(string url);
}

public sealed class FaithlifeEbooksCartSystem : ICartSystem
{
	public string Ownership { get; set; }
	public bool AllLanguages { get; set; }
	public bool Oldest { get; set; }
	private string SortBy => Oldest ? "Oldest" : "Newest";
	public string SearchUrl(int page) => (AllLanguages, Ownership) switch
	{
		(true, "all") => $"https://ebooks.faithlife.com/search?sortBy={SortBy}&limit=60&page={page}&ownership=all&geographicAvailability=availableToMe&isAjax=true",
		(false, "all") => $"https://ebooks.faithlife.com/search?sortBy={SortBy}&limit=60&page={page}&filters=language-english_Language&ownership=all&geographicAvailability=availableToMe&isAjax=true",
		(true, "unowned") => $"https://ebooks.faithlife.com/search?sortBy={SortBy}&limit=60&page={page}&ownership=unowned&geographicAvailability=availableToMe&isAjax=true",
		(false, "unowned") => $"https://ebooks.faithlife.com/search?sortBy={SortBy}&limit=60&page={page}&filters=language-english_Language&ownership=unowned&geographicAvailability=availableToMe&isAjax=true",
		(true, "partiallyOwned") => $"https://ebooks.faithlife.com/search?sortBy={SortBy}&limit=60&page={page}&ownership=partiallyOwned&geographicAvailability=availableToMe&isAjax=true",
		(false, "partiallyOwned") => $"https://ebooks.faithlife.com/search?sortBy={SortBy}&limit=60&page={page}&filters=language-english_Language&ownership=partiallyOwned&geographicAvailability=availableToMe&isAjax=true",
		_ => throw new InvalidOperationException($"Unknown ownership {Ownership}"),
	};
	public string AddToCartUrl(string url) => url.StartsWith("http") ?
		url :
		$"https://ebooks.faithlife.com{url}";
	public override string ToString() => "ebooks.faithlife.com";
}

public sealed class LogosCartSystem : ICartSystem
{
	public string Ownership { get; set; }
	public bool AllLanguages { get; set; }
	public bool Oldest { get; set; }
	private string SortBy => Oldest ? "Oldest" : "Newest";
	public string SearchUrl(int page) => (AllLanguages, Ownership) switch
	{
		(true, "all") => $"https://www.logos.com/search?limit=60&page={page}&ownership=all&geographicAvailability=availableToMe&sortBy={SortBy}&autoFacets=0&isAjax=true",
		(false, "all") => $"https://www.logos.com/search?filters=%2Blanguage-english_Language&limit=60&page={page}&ownership=all&geographicAvailability=availableToMe&sortBy={SortBy}&autoFacets=0&isAjax=true",
		(true, "unowned") => $"https://www.logos.com/search?limit=60&page={page}&ownership=unowned&geographicAvailability=availableToMe&sortBy={SortBy}&autoFacets=0&isAjax=true",
		(false, "unowned") => $"https://www.logos.com/search?filters=%2Blanguage-english_Language&limit=60&page={page}&ownership=unowned&geographicAvailability=availableToMe&sortBy={SortBy}&autoFacets=0&isAjax=true",
		(true, "partiallyOwned") => $"https://www.logos.com/search?limit=60&page={page}&ownership=partiallyOwned&geographicAvailability=availableToMe&sortBy={SortBy}&autoFacets=0&isAjax=true",
		(false, "partiallyOwned") => $"https://www.logos.com/search?filters=%2Blanguage-english_Language&limit=60&page={page}&ownership=partiallyOwned&geographicAvailability=availableToMe&sortBy={SortBy}&autoFacets=0&isAjax=true",
		_ => throw new InvalidOperationException($"Unknown ownership {Ownership}"),
	};
	public string AddToCartUrl(string url) => url.StartsWith("http") ?
		url :
		$"https://logos.com{url}";
	public override string ToString() => "logos.com";
}

public sealed class FaithlifeStoreCartSystem : ICartSystem
{
	public string Ownership { get; set; }
	public bool AllLanguages { get; set; }
	public bool Oldest { get; set; }
	private string SortBy => Oldest ? "Oldest" : "Newest";
	public string SearchUrl(int page) => (AllLanguages, Ownership) switch
	{
		(true, "unowned") => $"https://faithlife.com/store/search?sortBy={SortBy}&limit=60&page={page}&ownership=unowned&geographicAvailability=availableToMe&isAjax=true",
		(false, "unowned") => $"https://faithlife.com/store/search?sortBy={SortBy}&limit=60&page={page}&filters=language-english_Language&ownership=unowned&geographicAvailability=availableToMe&isAjax=true",
		(true, "partiallyOwned") => $"https://faithlife.com/store/search?sortBy={SortBy}&limit=60&page={page}&ownership=partiallyOwned&geographicAvailability=availableToMe&isAjax=true",
		(false, "partiallyOwned") => $"https://faithlife.com/store/search?sortBy={SortBy}&limit=60&page={page}&filters=language-english_Language&ownership=partiallyOwned&geographicAvailability=availableToMe&isAjax=true",
		_ => throw new InvalidOperationException($"Unknown ownership {Ownership}"),
	};
	public string AddToCartUrl(string url) => url.StartsWith("http") ?
		url :
		$"https://faithlife.com{url}";
	public override string ToString() => "faithlife.com/store";
}

public sealed class CurriculumCartSystem : ICartSystem
{
	public string Ownership { get; set; }
	public bool AllLanguages { get; set; }
	public bool Oldest { get; set; }
	private string SortBy => Oldest ? "Oldest" : "Newest";
	public string SearchUrl(int page) => (AllLanguages, Ownership) switch
	{
		(true, "unowned") => $"https://curriculum.faithlife.com/search?sortBy={SortBy}&limit=60&page={page}&ownership=unowned&geographicAvailability=all&isAjax=true",
		(false, "unowned") => $"https://curriculum.faithlife.com/search?sortBy={SortBy}&limit=60&page={page}&filters=language-english_Language&ownership=unowned&geographicAvailability=all&isAjax=true",
		(true, "partiallyOwned") => $"https://curriculum.faithlife.com/search?sortBy={SortBy}&limit=60&page={page}&ownership=partiallyOwned&geographicAvailability=all&isAjax=true",
		(false, "partiallyOwned") => $"https://curriculum.faithlife.com/search?sortBy={SortBy}&limit=60&page={page}&filters=language-english_Language&ownership=partiallyOwned&geographicAvailability=all&isAjax=true",
		_ => throw new InvalidOperationException($"Unknown ownership {Ownership}"),
	};
	public string AddToCartUrl(string url) => url.StartsWith("http") ?
		url :
		$"https://curriculum.faithlife.com{url}";
	public override string ToString() => "curriculum.faithlife.com";
}

public static class Constants
{
	// Products with a zero price that won't let us buy them.
	public static HashSet<string> Blacklist { get; } = new()
	{
		"195561", // verbum-academic-basic
		"184220", // verbum-sacred-art-media-archive
		"204928", // annotated-bibliography-of-rc-sprouls-works

		// Preordered; they just keep showing up in search results anyway...
		"247564", // the-bible-project-videos
		"186576", // what-is-predestination
		"186575", // why-should-i-join-a-church
		"186519", // does-god-exist
		"186518", // how-does-gods-law-apply-to-me
		"195703", // pugtato-finds-a-thing-curriculum-guide
		"192756", // salty-bitter-sweet-educators-guide
	};
}
