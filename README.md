# netdaemon-epg
EPG TV program guide for Homeassistant using NetDaemon

Provides homeassistant sensors for current tv show based on https://www.hoerzu.de/text/tv-programm/sender.php

## Config
```yaml
epg:
  class: Mutzl.Homeassistant.Epg
  refreshrate_in_seconds: 10
  sender:
    - "ORF 1"
    - "ORF 2"
    - "3sat"
```

![image](https://user-images.githubusercontent.com/2855185/116314245-23ff9580-a7af-11eb-9f68-b76f0982bc56.png)

The sensors provide also attributes like `BeginTime` and `Duration`, which can be the basis for building a clientside progressbar for the current TV show.
Another attribute contains the title of the upcoming show.

![image](https://user-images.githubusercontent.com/2855185/116314350-442f5480-a7af-11eb-9817-7dffe1461738.png)

## Internals
The integration reads the epg data for the current and the next day at startup and then once every day at 6:30am (to minimize the load on the 3rd party server).
It's also possible to refresh the epg data manually by calling the service `netdaemon.epg_refreshepgdata`.

Based on this data, it calculates what's currently on TV based on the (local) time of your homeassistant installation and updates the sensors `sensor.epg_XXX` whenever a new tv show starts according to this already downloaded data.
