# Worker protokolü

## Hedef

Worker cihazlar koordinatöre kendi kaynak durumunu, model uyumluluğunu ve sağlık bilgisini bildirir. Cihazlar eklenip çıkarılabildiği için worker API stateless ve heartbeat odaklı olmalıdır.

## Temel endpoint'ler

- `POST /api/worker/register`
- `POST /api/worker/heartbeat`
- `POST /api/worker/status`
- `POST /api/worker/unregister`

## Register örneği

```json
{
  "deviceId": "device-001",
  "name": "Laptop-01",
  "platform": "windows",
  "network": "ethernet",
  "capabilities": {
    "supportsLlamaCpp": true,
    "supportsRpc": true,
    "maxConcurrency": 2,
    "ramGb": 32
  },
  "trusted": true
}
```

## Status örneği

```json
{
  "deviceId": "device-001",
  "timestampUtc": "2026-11-10T06:45:00Z",
  "state": "idle",
  "memoryUsagePercent": 55,
  "cpuPercent": 18,
  "gpuPercent": 22,
  "temperatureC": 61,
  "runningModels": []
}
```

## Öncelik kuralları

- `ethernet` cihazlar genellikle daha yüksek öncelik alır.
- `wifi` cihazlar kullanılabilir ama daha güvensiz kabul edilir.
- `phone` cihazlar yalnızca hedeflenen mobil worker desteği için kullanılır.
- Çalışan iş varsa cihazın çalışma kapasitesi uygun şekilde azaltılır.

## Güvenlik

- Device ID güvenli şekilde saklanır.
- Worker ve koordinatör arasında kimlik doğrulama gerekir.
- Uygulama yalnızca güvenilir LAN içinde kullanılacak şekilde düşünülür.
