using HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Driver;

class Program
{
    static async Task Main()
    {
        await GetChampionsLeagueDataAsync();

        // Yerel Kupası varsa aç
        //await GetLocalCupDataAsync();

        await GetTeamsDataAsync();

        await MarkActiveTeamsAsync();

        await WriteLeagueDataToMongoDBAsync();
    }

    static async Task GetChampionsLeagueDataAsync()
    {
        string url = "https://arsiv.mackolik.com/AjaxHandlers/CupHandler.aspx?command=tab&id=182&season=2023&type=6"; 
        //**********************************************************************************************************************************************************************
        // belirlenen lige göre değişen url...
        string referer = "https://arsiv.mackolik.com/Standings/Default.aspx?sId=63860";
        string cookie = "_ga=GA1.2.1943581525.1647842297; ASP.NET_SessionId=5ziee3p2yzomyx4brgdknf4p";

        string htmlContent = await GetHtmlContentAsync(url, referer, cookie);

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("test");
        var championsCollection = database.GetCollection<YourDataModel>("sampiyonluklar");
        var goalKingsCollection = database.GetCollection<YourDataModel>("golKrallari");

        await WriteToMongoDBAsync(championsCollection, goalKingsCollection, doc);
        await WriteGoalKingsToMongoDBAsync(goalKingsCollection, doc);
    }

    static async Task GetLocalCupDataAsync()
    {
        string url = "https://arsiv.mackolik.com/AjaxHandlers/CupHandler.aspx?command=tab&id=7&season=2023/2024&type=6";  
        //**********************************************************************************************************************************************************************
        // belirlenen lige göre değişen url...

        string htmlContent = await GetHtmlContentAsync(url);

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("test");
        var collection = database.GetCollection<YourCupDataModel>("localCup");

        await WriteCupWinnersToMongoDBAsync(collection, doc);
    }

    static async Task GetTeamsDataAsync()
    {
        string url = "https://www.transfermarkt.com.tr/afc-asian-cup-2023/ewigeTabelle/pokalwettbewerb/AM23";
        //**********************************************************************************************************************************************************************
        // belirlenen lige göre değişen url...

        string cookie = "TMSESSID=d861c9d7c9e13f1c85fd9193c9e0e60d";
        string htmlContent = await GetHtmlContentAsync(url, null, cookie);

        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("test"); //playerDB
        var collection = database.GetCollection<YourTeamDataModel>("takimlar"); //turkiyeLigVerileri

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        await WriteTeamsToMongoDBAsync(collection, doc);
    }

    static async Task MarkActiveTeamsAsync()
    {
        string url = "https://www.transfermarkt.com.tr/afc-asian-cup-2023/teilnehmer/pokalwettbewerb/AM23/saison_id/2022";
        //**********************************************************************************************************************************************************************
        // belirlenen lige göre değişen url...

        string cookie = "TMSESSID=d861c9d7c9e13f1c85fd9193c9e0e60d";
        string htmlContent = await GetHtmlContentAsync(url, null, cookie);

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("test");
        var collection = database.GetCollection<YourTeamDataModel>("activeTeam");

        await MarkActiveTeamsToMongoDBAsync(collection, doc);
    }

    

    static async Task<string> GetHtmlContentAsync(string url, string referer = null, string cookie = null)
    {
        using (HttpClient client = new HttpClient())
        {
            if (!string.IsNullOrEmpty(referer))
                client.DefaultRequestHeaders.Add("Referer", referer);

            if (!string.IsNullOrEmpty(cookie))
                client.DefaultRequestHeaders.Add("Cookie", cookie);

            if (referer == null && cookie != null)
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                client.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
                client.DefaultRequestHeaders.Add("Referer", "https://www.transfermarkt.com.tr/afc-asian-cup-2023/ewigeTabelle/pokalwettbewerb/AM23");
                // belirlenen lige göre değişen url...
                //**********************************************************************************************************************************************************************
                client.DefaultRequestHeaders.Add("Origin", "https://www.transfermarkt.com.tr");
                client.DefaultRequestHeaders.Add("Cookie", "_sp_v1_p=503; consentUUID=a4a783fe-0e89-45cf-8fcf-499dc3dbb8f3_21_25_26; ...");

            }

            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new HttpRequestException($"HTTP isteği başarısız oldu. Hata kodu: {response.StatusCode}");
            }
        }
    }

    static async Task WriteLeagueDataToMongoDBAsync()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database1 = client.GetDatabase("test");
        var database2 = client.GetDatabase("test");  // playerDB
        // belirlenen lige göre değişen url...
        //**********************************************************************************************************************************************************************
        var collection = database2.GetCollection<LeagueDataModel>("ligVerileri");

        var activeTeamsCollection = database1.GetCollection<YourTeamDataModel>("activeTeam");
        var goalKingsCollection = database1.GetCollection<YourDataModel>("golKrallari");
        var localCupWinnersCollection = database1.GetCollection<YourCupDataModel>("localCup");
        var championsCollection = database1.GetCollection<YourDataModel>("sampiyonluklar");
        var teamsCollection = database1.GetCollection<YourTeamDataModel>("takimlar");

        var leagueData = new LeagueDataModel
        {
            LeagueSortCountry = "FIFA",
            LeagueCountry = "FIFA",
            LeagueName = "FIFA AFC Asian Cup",
            //LocalCupName = "Fransa Coupe de France",
            // belirlenen lige göre değişen url...
            //**********************************************************************************************************************************************************************
            ActiveTeams = await activeTeamsCollection.Find(_ => true).ToListAsync(),
            GoalKings = await goalKingsCollection.Find(_ => true).ToListAsync(),
            LocalCupWinners = await localCupWinnersCollection.Find(_ => true).ToListAsync(),
            Champions = await championsCollection.Find(_ => true).ToListAsync(),
            Teams = await teamsCollection.Find(_ => true).ToListAsync()
        };

        await collection.InsertOneAsync(leagueData);

        await activeTeamsCollection.DeleteManyAsync(_ => true);
        await goalKingsCollection.DeleteManyAsync(_ => true);
        await localCupWinnersCollection.DeleteManyAsync(_ => true);
        await championsCollection.DeleteManyAsync(_ => true);
        await teamsCollection.DeleteManyAsync(_ => true);
    }

    static string CleanString(string input)
    {
        return input.Replace("\r", "").Replace("\n", "").Replace(" ", "");
    }

    static async Task WriteToMongoDBAsync(IMongoCollection<YourDataModel> championsCollection, IMongoCollection<YourDataModel> goalKingsCollection, HtmlDocument doc)
    {
        var championsTable = doc.DocumentNode.SelectSingleNode("//table[1]");
        var championsRows = championsTable.SelectNodes("tr").Skip(1); 

        foreach (var row in championsRows)
        {
            var columns = row.SelectNodes("td");
            string season = columns[0].InnerText.Trim();
            string teamLogo = columns[1].SelectSingleNode("img").GetAttributeValue("src", "");
            string teamName = columns[1].SelectSingleNode("a").InnerText.Trim();
            string playerFlag = columns[2].SelectSingleNode("img").GetAttributeValue("src", "");

            var championData = new YourDataModel
            {
                Season = season,
                TeamLogo = teamLogo,
                TeamName = teamName,
                PlayerFlag = playerFlag
            };

            await championsCollection.InsertOneAsync(championData);
        }
    }

    static async Task WriteGoalKingsToMongoDBAsync(IMongoCollection<YourDataModel> goalKingsCollection, HtmlDocument doc)
    {
        var goalKingsTable = doc.DocumentNode.SelectSingleNode("//table[@id='tblSeasons']");
        var goalKingsRows = goalKingsTable.SelectNodes("tr").Skip(1); 

        foreach (var row in goalKingsRows)
        {
            var columns = row.SelectNodes("td");

            HtmlNode seasonNode = columns[0].SelectSingleNode("a");
            string season = seasonNode != null ? CleanString(seasonNode.InnerText.Trim()) : "";
            string playerFlag = columns[1].SelectSingleNode("img").GetAttributeValue("title", "");
            string playerName = columns[1].SelectSingleNode("a").InnerText.Trim();
            string teamLogo = columns[2].SelectSingleNode("img").GetAttributeValue("title", "");
            string goals = columns[3].InnerText.Trim();
            string matches = columns[4].InnerText.Trim();

            var goalKingData = new YourDataModel
            {
                Season = season,
                PlayerFlag = playerFlag,
                PlayerName = playerName,
                TeamLogo = teamLogo,
                Goals = goals,
                Matches = matches
            };

            await goalKingsCollection.InsertOneAsync(goalKingData);
        }
    }


    static async Task WriteCupWinnersToMongoDBAsync(IMongoCollection<YourCupDataModel> collection, HtmlDocument doc)
    {
        var cupWinnersTable = doc.DocumentNode.SelectSingleNode("//table[@class='list-table']");
        var cupWinnersRows = cupWinnersTable.SelectNodes("tr").Skip(1);

        foreach (var row in cupWinnersRows)
        {
            var columns = row.SelectNodes("td");
            string season = columns[0].InnerText.Trim();
            string teamLogo = columns[1].SelectSingleNode("img").GetAttributeValue("src", "");
            string teamName = columns[1].SelectSingleNode("a").InnerText.Trim();

            var cupWinnerData = new YourCupDataModel
            {
                Season = season,
                TeamLogo = teamLogo,
                TeamName = teamName
            };

            await collection.InsertOneAsync(cupWinnerData);
        }
    }

    static async Task WriteTeamsToMongoDBAsync(IMongoCollection<YourTeamDataModel> collection, HtmlDocument doc)
    {
        var teamsTable = doc.DocumentNode.SelectSingleNode("//table[@class='items']");
        var teamsRows = teamsTable.SelectNodes("tbody/tr");

        foreach (var row in teamsRows)
        {
            var columns = row.SelectNodes("td");
            string position = columns[0].InnerText.Trim();
            var teamLogoNode = columns[1].SelectSingleNode("img");
            string teamLogo = teamLogoNode != null ? teamLogoNode.GetAttributeValue("src", "") : "";
            string teamName = columns[2].SelectSingleNode("a").InnerText.Trim();
            string matches = columns[3].InnerText.Trim();
            string wins = columns[4].InnerText.Trim();
            string draws = columns[5].InnerText.Trim();
            string losses = columns[6].InnerText.Trim();
            string goalDifference = columns[7].InnerText.Trim();
            string points = columns[8].InnerText.Trim();

            var teamData = new YourTeamDataModel
            {
                Position = position,
                TeamLogo = teamLogo,
                TeamName = teamName,
                Matches = matches,
                Wins = wins,
                Draws = draws,
                Losses = losses,
                GoalDifference = goalDifference,
                Points = points
            };

            await collection.InsertOneAsync(teamData);
        }
    }

    static async Task MarkActiveTeamsToMongoDBAsync(IMongoCollection<YourTeamDataModel> collection, HtmlDocument doc)
    {
        var teamsTable = doc.DocumentNode.SelectSingleNode("//table[@class='items']");
        var teamsRows = teamsTable.SelectNodes("tbody/tr");

        foreach (var row in teamsRows)
        {
            var columns = row.SelectNodes("td");
            var teamLogoNode = columns[0].SelectSingleNode("img");
            string teamLogo = teamLogoNode != null ? teamLogoNode.GetAttributeValue("src", "") : "";
            string teamName = columns[1].SelectSingleNode("a").InnerText.Trim();

            var teamData = new YourTeamDataModel
            {
                TeamLogo = teamLogo,
                TeamName = teamName,
                IsActive = true
            };

            await collection.InsertOneAsync(teamData);
        }
    }

    


}

class YourCupDataModel
{
    public ObjectId Id { get; set; }
    public string Season { get; set; }
    public string TeamLogo { get; set; }
    public string TeamName { get; set; }
}

class YourDataModel
{
    public ObjectId Id { get; set; }
    public string Season { get; set; }
    public string TeamLogo { get; set; }
    public string TeamName { get; set; }
    public string Matches { get; set; }
    public string Gp { get; set; }
    public string AvgPoints { get; set; }
    public string TotalPoints { get; set; }
    public string PlayerFlag { get; set; }
    public string PlayerName { get; set; }
    public string Goals { get; set; }
}

class YourTeamDataModel
{
    public ObjectId Id { get; set; }
    public string Position { get; set; }
    public string TeamLogo { get; set; }
    public string TeamName { get; set; }
    public string Matches { get; set; }
    public string Wins { get; set; }
    public string Draws { get; set; }
    public string Losses { get; set; }
    public string GoalDifference { get; set; }
    public string Points { get; set; }
    public bool IsActive { get; set; }
}

class LeagueDataModel
{
    public ObjectId Id { get; set; }

    public string LeagueSortCountry { get; set; }
    public string LeagueCountry { get; set; }
    public string LeagueName { get; set; }
    public string LocalCupName { get; set; }

    public List<YourTeamDataModel> ActiveTeams { get; set; }
    public List<YourDataModel> GoalKings { get; set; }
    public List<YourCupDataModel> LocalCupWinners { get; set; }
    public List<YourDataModel> Champions { get; set; }
    public List<YourTeamDataModel> Teams { get; set; }
}
