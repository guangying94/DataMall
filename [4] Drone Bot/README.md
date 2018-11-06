#  Drone Assistant Bot
This project helps the users to find out which location in Singapore is allowed to fly drone, and at the same time, guide users on what kind of permission is needed depending on their drone.

## Data Source / API used
1. [Civil Aviation Authority of Singapore Drone Guideline](https://www.caas.gov.sg/public-passengers/unmanned-aircraft-systems)
2. [National Environmental Authority Weather API](http://www.weather.gov.sg/weather-forecast-2hrnowcast-2/)
3. [Singapore Land Authority OneMap](https://docs.onemap.sg/)

## Technology used
1. [Microsoft Bot Framework](https://dev.botframework.com/)
2. [Azure Cosmos DB](https://azure.microsoft.com/en-us/services/cosmos-db/)
3. [Azure Machine Learning Studio](https://studio.azureml.net/)
4. [Microsoft Power BI](https://powerbi.microsoft.com/en-us/)
5. [Azure Cognitive Services - Custom Vision](https://customvision.ai/)
6. [Azure Cognitive Services - LUIS](https://luis.ai)
5. Microsoft Cortana
6. Facebook Messenger

## Conversation Flow
In general, this bot can break into 3 actions, drone classification, query flying zone, and guidance on permit.

### Drone Classification
This is straightforward, identical to previous project, where we are using Cognitive Services, Custom Vision to train a unique model to classify drone, as shown below.
![custom vision.png](https://datamallsga48a.blob.core.windows.net/hdb-powerbi/Drone%20image.png)

Once done, you will have an endpoint that can query the specific drone type.

### Query Flying Zone
This requires additional work. As there's no accurate/specific coordinates that we can obtain, we are using the reference from CAAS [Unmanned Aircraft Systems Area Limit](https://www.caas.gov.sg/public-passengers/unmanned-aircraft-systems/area-limits). Note that there's 2 restrictions here, areas and points.

For areas, we can draw polygon based on estimation, wheras for points (location), we can get specific location. Once the coordinates are obtained, we can input these coordinates into Azure Cosmos DB as geospatial objects. 

Here's an example on how to create a polygon using C#:
```csharp
RestrictionZone location1 = new RestrictionZone
{
	Id = "polygon 1",
    Name = "Changi Naval Base",
    RestrictionType = "Protected Area",
    Location = new Polygon(new[]
    {
    	new LinearRing(new[]
        {
        	new Position(104.022724, 1.324918), new Position(104.015921, 1.319603), new Position(104.016212, 1.310464 ), new Position(104.035749, 1.312319), new Position(104.033982, 1.327046), new Position(104.022724, 1.324918 )
         })
      })
};

await this.CreateGeospatialZoneIfNotExists("GeospatialDB", "GeospatialCollection", location1);
```

And this is how you create a point:
```csharp
RestrictionPoint point1 = new RestrictionPoint
{
	Id = "point1",
	Name = "Changi Airport Point",
	RestrictionType = "Airbase",
	Location = new Point(new Position(103.987992, 1.355888))
};

await this.CreateGeospatialPointIfNotExists("GeospatialDB", "GeospatialCollection", point1);
```

Once all coordinates are inserted into Cosmos DB correctly, your collection should looks something like this:
![cosmos db](https://datamallsga48a.blob.core.windows.net/hdb-powerbi/drone%20map.png)

Then, we can leverage on geospatial queries of Azure Cosmos DB to find out if the coordinate is inside the polygon, or within 5km of a single point. Please refer to [Cosmos DB Geospatial](https://docs.microsoft.com/en-us/azure/cosmos-db/geospatial) for reference. 

Here's an example on querying if the coordinate is within the polygon:
```csharp
public static bool CheckFlyingZone(string lat, string lon)
{
	string SQLQuery = "SELECT c.Name,c.RestrictionType FROM c WHERE ST_WITHIN({'type': 'Point', 'coordinates':[" + lon + ", " + lat + "]}, c.location) AND c.RestrictionType = 'Protected Area'";
DocumentClient client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);

	IQueryable<RestrictionZone> restrictionQueryInSql = client.CreateDocumentQuery<RestrictionZone>(
		UriFactory.CreateDocumentCollectionUri("GeospatialDB", "GeospatialCollection"),
		SQLQuery, new FeedOptions { EnableScanInQuery = true });

	int count = 0;

	foreach (RestrictionZone zone in restrictionQueryInSql)
	{
		count++;
	}

	if (count == 0)
	{
		return true;
	}
	else
	{
		return false;
	}
}
```
One additional item that I implemented here is the weather forecast, based on the queried location, this can help users to plan if they are going to fly drone in that specific area based on 2-hour weather forecast from NEA.

### Guidance on Drone Permit
The decision tree provided in the website is actually straightforward, but for illustration purpose, we are using Azure Machine Learning Studio to output the right permit based on the selection. However, this is useful for more complex scenario.

First, we construct the sample response in a csv file, which can be found [here](https://datamallsga48a.blob.core.windows.net/hdb-powerbi/Drone%20Permit.csv). Then, we use Azure Machine Learning Studio to train the model, and it will generate a REST endpoint for us to consume.

![aml.png](https://datamallsga48a.blob.core.windows.net/hdb-powerbi/aml.png)

### Bonus (Real-time Dashboard Reporting)
This is an additional function, to generate a user heatmap based on their query. The bot will collect the coordinate and send it to Power BI directly, then plot the coordinates on map.

![drone map.png](https://datamallsga48a.blob.core.windows.net/hdb-powerbi/drone%20power%20bi.png)