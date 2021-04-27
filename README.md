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

