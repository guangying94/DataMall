# Singapore Accident Heat Map

This project ingest accident data from [Data Mall](https://www.mytransport.sg/content/mytransport/home/dataMall.html) and visual in [Power BI](https://powerbi.microsoft.com/en-us/).

## Procedure
### Data Ingestion
First, obtain API Key from the website. With that, we can query the information via HTTP Call. The data is in JSON format, and here's a sample response:

```json
{
    "odata.metadata": "http://datamall2.mytransport.sg/ltaodataservice/$metadata#IncidentSet",
    "value": [
        {
            "Type": "Roadwork",
            "Latitude": 1.3941791748135355,
            "Longitude": 103.81937144760715,
            "Message": "(1/9)15:06 Roadworks on SLE (towards CTE) after Upper Thomson Rd Exit."
        },
        {
            "Type": "Roadwork",
            "Latitude": 1.3262639066951878,
            "Longitude": 103.66582980791914,
            "Message": "(1/9)14:43 Roadworks on PIE (towards Tuas) after AYE Exit. Avoid lane 3."
        },
        {
            "Type": "Roadwork",
            "Latitude": 1.3177456388082662,
            "Longitude": 103.84977096388371,
            "Message": "(1/9)14:06 Roadworks on CTE (towards AYE) after Moulmein Rd Exit. Avoid lane 5."
        },
        {
            "Type": "Roadwork",
            "Latitude": 1.3445577544298277,
            "Longitude": 103.68855003798117,
            "Message": "(1/9)13:46 Roadworks on PIE (towards Changi Airport) before Pioneer Rd Nth. Avoid lane 4."
        }
    ]
}
```

Here, we use Azure function and set timer, to run every 2 minutes. The Azure function code can be found in this repository. This Azure function perform HTTP GET every 2 minutes, and store the data in table storage.

Here's how the table storage looks like:

![Table](https://smsjwg-dm2306.files.1drv.com/y4m6NH5VJEXsAkaVTEhDvu7LrzRbRm3SX02IsZctCOnxxQoZoaJhHg9Amk0gTOaZMO5hyMAv4j7yd8nB9heWXBUbvBGNnFBxjhicoLELUkPgqkq6-WQWhW8mq2SD_JXcT-q_tyEa9I1UePMaX5FF-jUxgJhXSIEMAtB92gLJvQrKsUaFAaQDwZoxXhvBKuisUrcR8igsNXKPa-2FGcNVrWLtQ?width=1024&height=550&cropmode=none)

### Data Visualization

From Power BI, import data source using Table Storage. From there, perform some transformation such as
1. Remove duplicate row
1. Split column and extract the date from messages
1. Others

The visualization used includes:
1. ArcGIS Map (Heat Map)
1. Slicer

Here's how it looks like:
![Power BI](https://7fqcnq-dm2306.files.1drv.com/y4mo4EZQUnLJdh132gckK9h17gN9KoHJkxxmmlXakXFt9VNFE_TwxKXW5Dkzv-rm10siQ45GWS78blTxPhPib4YGza7P8KfDdL7KvfmZMGkW33X5zTrdYVjk-BwQG2_gdjqe1fZlA51BAnkP6m985cTygfmJxd0CuIEtXOvm7RRkGKp_l9ucT6ZIkIFxzp0xOqhx0ae9dURNIjkx1VmAKCFPQ?width=1600&height=860&cropmode=none)
