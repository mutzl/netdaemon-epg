# netdaemon-epg
EPG TV program guide for Homeassistant using NetDaemon

Provides homeassistant sensors for current tv show based on https://www.hoerzu.de/text/tv-programm/sender.php or https://tv.orf.at/.
You can even use your own data provider by implementing the `IDataProviderService`.

## Config
```yaml
epg:
  class: Mutzl.Homeassistant.Epg
  refreshrate_in_seconds: 10
  data_providers:
    - fullname: "Mutzl.Homeassistant.OrfDataProviderService"
      stations:
        - "ORF 1"
        - "ORF 2"
        - "ORF 3"
    - fullname: "Mutzl.Homeassistant.HoerzuDataProviderService"
      stations:
        - "ORF 1"
        - "ORF 2"
        - "3sat"
```

![image](https://user-images.githubusercontent.com/2855185/116489742-f6414c00-a895-11eb-8ea7-5781499ad136.png)

The sensors provide also attributes like `BeginTime` and `Duration`, which can be the basis for building a clientside progressbar for the current TV show.
Another attribute contains the title of the upcoming show.

![image](https://user-images.githubusercontent.com/2855185/116489793-140eb100-a896-11eb-80dc-db47f58e881e.png)

By calling the service `netdaemon.epg_getdescription` and passing the `entity_id` of the desired TV show, the EPG writes the description into the `description` attribute of the `sensor.epg_desc`. Attribute was used because state has a 255 char size limit in Homeassistant.
The description can by used in the markdown card for example.

![image](https://user-images.githubusercontent.com/2855185/116490130-eaa25500-a896-11eb-88c8-71a69520045a.png)

## Internals
The integration reads the epg data for the current and the next day at startup and then once every day at 6:30am (to minimize the load on the 3rd party server).
It's also possible to refresh the epg data manually by calling the service `netdaemon.epg_refreshepgdata`.

Based on this data, it calculates what's currently on TV based on the (local) time of your homeassistant installation and updates the sensors `sensor.epg_XXX` whenever a new tv show starts according to this already downloaded data.
