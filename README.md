# SmartEnergy

Edge-prototype for energistyring i hjem/bygg.  
Lytter på sensordata via **MQTT**, tar beslutninger (enkle regler nå – ML senere) og sender **kommandoer** tilbake på MQTT.

---

## ✨ What you get (MVP)

- **Mosquitto** MQTT broker (Docker)
- **.NET Worker** (Docker) som:
  - kobler til broker
  - **abonnerer** på `home/stue/temp`
  - **publiserer** kommando `home/stue/heater/cmd` = `ON` / `OFF`
  - **heartbeat** til `home/demo/heartbeat` hvert 15. sekund
- Alt kjører via **docker compose**
- Enkle testkommandoer for å simulere sensorer og observere kommandoer

---

## 📦 Project structure

```
SmartEnergy/
├─ SmartEnergy.sln
├─ docker-compose.yml           # starter mosquitto + worker
├─ Dockerfile                   # bygger .NET worker image
├─ appsettings.json             # konfig for worker (MQTT, intervall, osv.)
├─ Program.cs                   # DI + hosting
├─ Worker.cs                    # MQTT-klient, regler, heartbeat
├─ Settings.cs                  # MqttSettings & LoopSettings (Options)
└─ infra/
   └─ mosquitto/
      ├─ mosquitto.conf        # broker-config
      ├─ data/                 # (persistent data, ignorér i git)
      └─ log/                  # (logs, ignorér i git)

```



## 🧰 Requirements

- Docker Desktop (Windows/Mac/Linux)

- Git

- (Utvikling) Visual Studio 2022 eller dotnet 8 SDK




## 🚀 Quick start

### Clone
```
git clone <your-repo-url>
cd SmartEnergy
```

### Start alt i bakgrunnen
```
docker compose up -d --build
```

### Sjekk at det kjører
```
docker ps
```

# skal vise "mosquitto" og "smartenergy" som Up


Se logger fra appen
```
docker compose logs -f smartenergy
# Forventet:
# MQTT connected to mosquitto:1883
# Subscribed to home/stue/temp
# TX home/demo/heartbeat => 2025-...
```

## 🧪 Test (uten ekte sensorer)

### Åpne to terminaler i repo-mappa.

# Terminal A – abonner på alt (eller bare kommando-topic):

##### alt:
```
docker exec -it mosquitto sh -c "mosquitto_sub -t '#' -v"
```
# kun kommando fra worker:
```
docker exec -it mosquitto sh -c "mosquitto_sub -t 'home/stue/heater/cmd' -v"
```

# Terminal B – simuler temperaturer:
```
docker exec -it mosquitto sh -c "mosquitto_pub -t 'home/stue/temp' -m '20.5'"
docker exec -it mosquitto sh -c "mosquitto_pub -t 'home/stue/temp' -m '22.2'"
docker exec -it mosquitto sh -c "mosquitto_pub -t 'home/stue/temp' -m '19.8'"
```

Forventet oppførsel (enkel regel i Worker.cs):

 * temp < 21.0 → home/stue/heater/cmd = ON

* temp >= 21.0 → home/stue/heater/cmd = OFF

Merk: MQTT sender ikke historikk. Start subscriber før du publiserer testmeldinger.

## 🗂 MQTT topic-konvensjon (MVP)

#### Sensorer publiserer

- home/<rom>/temp          # f.eks. home/stue/temp


#### Worker publiserer kommandoer

- home/<rom>/<device>/cmd  # f.eks. home/stue/heater/cmd


#### System-pulse

- home/demo/heartbeat      # ISO-tid hvert 15. sekund


##### Senere utvidelser (forslag): occupancy, price, setpoint, mode (Comfort/Saver).

## ⚙️ Configuration

 - appsettings.json
```
{
  "Mqtt": {
    "Host": "mosquitto",
    "Port": 1883,
    "ClientId": "edge-control",
    "User": "",
    "Pass": ""
  },
  "Loop": { "IntervalSeconds": 15 }
}
```

- Host = mosquitto (tjenestenavnet i docker compose-nettverket)

- ClientId kan endres per enhet

- Loop.IntervalSeconds styrer heartbeat-frekvens

- Mosquitto-konfig (infra/mosquitto/mosquitto.conf)

- listener 1883
- allow_anonymous true
- persistence true
- persistence_location /mosquitto/data/



# 🔁 Dev-workflow

## Når du endrer C#-koden:

### rask bygg + restart bare appen
```
docker compose up -d --build smartenergy
```

### helt rent bygg (om noe henger)
```
docker compose down
docker compose build --no-cache smartenergy
docker compose up -d
```

### Se logger
```
docker compose logs -f smartenergy
```

### Stopp alt
```
docker compose down
```


# 🧭 Arkitektur (enkelt)
```
[Temp-sensor (simulert)]
        |
        |  MQTT:  home/<rom>/temp   (f.eks. home/stue/temp)
        v
+------------------+
|  Mosquitto       |  (Docker, port 1883)
|  MQTT broker     |
+------------------+
        |
        |  MQTT-sub: home/stue/temp
        |  MQTT-pub: home/stue/heater/cmd, home/demo/heartbeat
        v
+---------------------------+
| SmartEnergy Worker (.NET) |
| - Leser appsettings.json  |
| - Kobler til MQTT         |
| - Enkel regelmotor        |
|   (temp < 21 => ON, ellers OFF)
| - Heartbeat hvert 15s     |
| - Console-logging         |
+---------------------------+
        |
        |  MQTT: home/stue/heater/cmd = ON/OFF
        v
[Varmeovn (simulert/kommende)]
```
# Viktige detaljer

- Deploy: docker-compose.yml starter mosquitto + smartenergy i samme nettverk.

- Konfig: appsettings.json (host=mosquitto, port=1883, clientId, intervall).

- Topics (MVP):

  - Sensor → Broker: home/<rom>/temp

  - Worker → Aktuator: home/<rom>/<device>/cmd (eks. home/stue/heater/cmd)

  - **Heartbeat: home/demo/heartbeat

- Observability: Console-logger (via docker compose logs -f smartenergy).

- State/persistens: Ingen applikasjonsdatabase (mosquitto har vedvarende kø via infra/mosquitto/data/).

- Sikkerhet: Anonym MQTT i dev (enkelt å bytte til brukernavn/pass/TLS senere).







# Framtidig arkitektur (trinnvis utvidbar)

### Mål: Skaler fra én regel i én worker → til robust edge + sky-lag, læring, dashboards og administrasjon.
```
                (Flere hjem / bygg / rom)
+------------------+      +------------------+
| Edge Node A      |      | Edge Node B      |   ... (én pr. lokasjon)
| - Mosquitto      |      | - Mosquitto      |
| - Edge Worker    |      | - Edge Worker    |
| - Local DB (SQL) |      | - Local DB (SQL) |
+---------+--------+      +---------+--------+
          |                         |
          | (MQTT over TLS / VPN)   |
          +-----------+-------------+
                      |
                (Valgt sky / sentral)
                      v
        +-----------------------------+
        | Inntak/Message-bus          |
        |  - MQTT bridge / EMQX       |
        |  - eller Kafka/NATS         |
        +--------------+--------------+
                       |
         +-------------+-------------+
         |                           |
+--------------------+      +--------------------------+
| Time-series DB     |      | API / Backend            |
| (Influx/Timescale) |      | (REST/GraphQL, auth, RBAC|
+---------+----------+      +-----------+--------------+
          |                                 |
          |                                 |
   +------+-------+                  +------+-----------------+
   | Dashboards   |                  | ML/Analytics Services |
   | (Grafana)    |                  | - Python (FastAPI)    |
   +--------------+                  | - Trening & prediksjon|
                                     | - Feature store       |
                                     +-----------+-----------+
                                                 |
                                     +-----------v-----------+
                                     | Policy/Rules Engine   |
                                     | (beslutning + mål)    |
                                     +-----------+-----------+
                                                 |
                                       (Nedlink via MQTT)
                                                 |
                                   +-------------v-------------+
                                   | Edge Worker (Command exec)|
                                   | - Kjører lokalt uansett   |
                                   |   nett (offline-first)    |
                                   +---------------------------+

```
