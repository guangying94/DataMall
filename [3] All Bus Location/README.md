# API to get all bus location

This Azure Function takes 2 input, bus service number and direction, as shown below.

```json
{
    "ServiceNo": "10",
    "Direction": 1
}
```

It will output a json file that contains all bus location of this particular service number and direction.

Sample response:

```json
{
    "ServiceNo": "10",
    "Direction": 1,
    "BusCoordinate": [{
        "Latitude": "1.2744808333333333",
        "Longitude": "103.79670366666667"
    }, {
        "Latitude": "1.3072650000000001",
        "Longitude": "103.89544833333333"
    }, {
        "Latitude": "1.2724601666666666",
        "Longitude": "103.84432916666667"
    }, {
        "Latitude": "1.3116246666666667",
        "Longitude": "103.9222005"
    }, {
        "Latitude": "1.3374775",
        "Longitude": "103.95049633333333"
    }]
}
```

## To be continue
The key project for this API is to monitor bus location in near real time. Work in progress...
