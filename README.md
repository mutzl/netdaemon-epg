# netdaemon-epg
EPG TV program guide for Homeassistant using NetDaemon

Provides homeassistant sensors for current tv show based on https://www.hoerzu.de/text/tv-programm/sender.php or https://tv.orf.at/.
You can even use your own data provider by implementing the `IDataProviderService`. Please have a look at the `SampleDataProviderService`.

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
      refresh_times:
        - "06:15"
        - "9:15"
        - "12:15"
        - "15:15"
        - "17:15"
        - "19:15"
        - "20:05"
        - "21:05"
        - "22:05"
        - "23:15"
        - "00:15"
    - fullname: "Mutzl.Homeassistant.HoerzuDataProviderService"
      stations:
        - "ORF 1"
        - "ORF 2"
        - "ORF 3"
    - fullname: "Mutzl.Homeassistant.SampleDataProviderService"
      stations:
        - "TV One"
   
```

For every station in every data provider you will find a sensor will be created with the following naming pattern: `sensor.epg_[dataprovider]_[station]`.

<img src="https://user-images.githubusercontent.com/2855185/116915630-8d1e5780-ac4c-11eb-9cce-fa1033b60ba3.png" width="50%">

The sensors provide also attributes like `BeginTime` and `Duration`, which can be the basis for building a clientside progressbar for the current TV show.
Another attribute contains the title of the upcoming show.

<img src="https://user-images.githubusercontent.com/2855185/116916147-3b2a0180-ac4d-11eb-9260-7c29b91dc23b.png" width="50%">

It is recommended, to disable the recording for those sensors, as it doesn't make much sense to keep history of all the tv guides. Especially since the description could become quite large.

```yaml
recorder:
  exclude:
    entity_globs:
      - sensor.epg_*
```

Using a markdown card, you can get a nice visual representation of the current show - maybe within a popup of the [browser-mod](https://github.com/thomasloven/hass-browser_mod)

<img src="https://user-images.githubusercontent.com/2855185/116916831-1eda9480-ac4e-11eb-9206-d021f9e3c780.png" width="50%">


## Internals
The integration reads the epg data for the current and the next day at startup and then once every day at 6:30am (to minimize the load on the 3rd party server).
In case your data provider changes more often than once in a day, you can also configure more refresh times.
It's also possible to refresh the epg data manually by calling the service `netdaemon.epg_refreshepgdata`.

Based on this data, it calculates what's currently on TV based on the (local) time of your homeassistant installation and updates the sensors `sensor.epg_XXX` whenever a new tv show starts according to this already downloaded data.
