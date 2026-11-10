# Cihaz yönetimi ve worker protokolü

## Amaç

Koordinatör, LAN içindeki cihazları güvenli şekilde tanır, izler ve iş yükünü dağıtır. Cihazlar çalışma sırasında eklenip çıkarılabilir; ekleme/kaldırma sırasında çalışan işler güvenli şekilde yönetilir.

## Temel prensipler

- Kablolu ve kablosuz cihazlar desteklenir.
- Ethernet, daha düşük gecikme ve istikrar nedeniyle tercih edilir.
- Telefonlar ve laptoplar ayrı iş yükleri için kullanılabilir; telefonlar için pil ve arka plan kısıtları dikkate alınır.
- Tek büyük modelin birden fazla cihaza bölünmesi erken aşama hedefi değildir; ilk etapta tek iş / tek uygun worker dağıtımı yapılır.
- Cihazlar kullanıcı onayı ile eklenir; cihaz kaydı çıkarılabilir ve güvenilirlik seviyeleri değiştirilebilir.

## Cihaz ekleme ve çıkarma akışı

1. Kullanıcı cihazı koordinatöre ekler.
2. Cihaz bir kimlik doğrulama/başlatma isteği gönderir.
3. Koordinatör cihaz özelliklerini doğrular.
4. Cihaz sisteme kayıt olur ve heartbeat başlatılır.
5. Koordinatör cihazı "online"/"busy"/"offline" olarak izler.
6. Kullanıcı cihazı kaldırdığında koordinatör:
   - mevcut işlerin tamamlama durumunu kontrol eder
   - çalışan iş varsa güvenli şekilde bitirir veya başka worker'a atar
   - cihazı offline olarak işaretler

## Cihaz profili

```json
{
  "deviceId": "device-001",
  "name": "Laptop-01",
  "type": "laptop",
  "platform": "windows",
  "network": "ethernet",
  "status": "online",
  "lastSeenUtc": "2026-11-10T06:45:00Z",
  "capabilities": {
    "cpuCores": 12,
    "ramGb": 32,
    "vramGb": 0,
    "gpu": "NVIDIA RTX 4070",
    "supportsLlamaCpp": true,
    "supportsRpc": true,
    "maxConcurrency": 2
  },
  "trustLevel": "trusted",
  "maintenanceMode": false
}
```

## Worker heartbeat örneği

```json
{
  "deviceId": "device-001",
  "timestampUtc": "2026-11-10T06:45:00Z",
  "status": "online",
  "load": {
    "cpuPercent": 35,
    "ramPercent": 55,
    "gpuPercent": 42,
    "temperatureC": 62
  },
  "models": [
    {
      "name": "qwen2.5-coder-7b",
      "format": "gguf",
      "loaded": false,
      "memoryMb": 7000,
      "compatible": true
    }
  ]
}
```

## Görev atama önceliği

Bir iş atanmadan önce koordinatör aşağıdaki kriterleri kontrol eder:

1. Model uyumluluğu
2. Açık bellek / VRAM durumu
3. CPU / GPU yükü
4. Ağırlık ve gecikme
5. Cihaz güveni ve izin seviyesi
6. Ağ bağlantısı tipleri (Ethernet > Wi‑Fi > mobil)

## Ağırlıklandırma yaklaşımı

- Ethernet / güvenli LAN cihazları daha yüksek öncelik alır.
- Mobil cihazlar ancak uygunluk ve pil kısıtları doğrulandıktan sonra kullanılabilir.
- Gecikme ve başarısız iş sayısı arttıkça aday cihazların önceliği düşer.

## Sonraki aşama için not

Bu protokol, tek iş / tek worker dağıtımı için tasarlanmıştır. Model parçalaması ayrı teknik ve güvenlik sorunları yaratır; bunun için backend, bellek ve ağ ölçümleri netleşmeden geçilmemelidir.
