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

![image](https://user-images.githubusercontent.com/2855185/116299908-8b611980-a79e-11eb-963a-472091a30c05.png)

The sensors provide also attributes like `BeginTime` and `Duration`, which can be the basis for building a clientside progressbar for the current TV Show.

![image](https://user-images.githubusercontent.com/2855185/116300113-cbc09780-a79e-11eb-95db-a45faa44a006.png)

## Internals
The integration reads the epg data for the current and the next day at startup and then once every day at 6:30am (to minimize the load on the 3rd party server).
Based on this data, it calculates what's currently on TV based on the (local) time of your homeassistant installation and updates the sensors `sensor.epg_XXX` whenever a new tv show starts according to this already downloaded data.
