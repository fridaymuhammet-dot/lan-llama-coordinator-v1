# Ağ API tasarımı (v1 taslak)

Bu belge Coordinator–Worker arasında uygulanması planlanan HTTP API sözleşmesini tanımlar. **Tasarım dokümanıdır; endpoint'ler henüz uygulamada yoktur.** İlk gerçek ağ prototipi yalnızca tek Coordinator ve onaylanmış Windows Worker'ı hedefler.

## Tasarım kararları

- Worker bağlantıyı başlatır; Coordinator Worker'a inbound bağlantı açmaz. Böylece her Worker cihazında dinleyen bir servis/port gerekmez.
- API JSON/HTTPS ve `/api/v1` sürüm öneki kullanır.
- Her istekte kimlik doğrulaması ve TLS gerekir; “aynı LAN'da” olmak güven kanıtı değildir.
- Worker kaydı kullanıcı onayı olmadan güven oluşturmaz.
- İşler Worker tarafından Coordinator'dan alınır (pull). Protokol genel amaçlı uzaktan komut/terminal/PowerShell çalıştırma içermez.
- Inference backend'i, Coordinator'ın prompt verisini Worker'a iletmesini gerektirebilir. UI, veri gönderilmeden önce hedef cihazı ve yerel ağ aktarımını kullanıcıya göstermelidir.

## API uçları

| Method | Path | Kimlik doğrulama | Amaç |
|---|---|---|---|
| `POST` | `/api/v1/pairing-requests` | Tek kullanımlık eşleştirme kodu + HTTPS | Onay bekleyen Worker kaydı oluşturur |
| `GET` | `/api/v1/pairing-requests/{requestId}` | Kayıt isteğinde oluşturulan istek anahtarı | Worker, onay/ret sonucunu öğrenir; onay sonrası credential'ı bir kez alır |
| `POST` | `/api/v1/worker/heartbeat` | Worker credential | Sağlık ve yetenek durumunu gönderir |
| `POST` | `/api/v1/worker/tasks/claim` | Worker credential | Uygun bir işi atomik olarak alır; boşsa `204` döner |
| `POST` | `/api/v1/worker/tasks/{taskId}/result` | Worker credential | İş sonucunu veya sınırlı hata bilgisini gönderir |
| `POST` | `/api/v1/worker/unregister` | Worker credential | Worker'ı kontrollü biçimde çevrimdışı yapar |
| `POST` | `/api/v1/coordinator/workers/{deviceId}/revoke` | Yalnızca Coordinator'ın yerel yönetim arayüzü | Worker credential'ını iptal eder; bekleyen işleri yeniden kuyruğa alır |

Bilinmeyen sürüm, endpoint veya protokol değeri reddedilir. Mutasyon uçlarında uygun `Idempotency-Key` desteklenir. Kayıt, kimlik doğrulama ve kullanıcı onayı tamamlanmadan heartbeat/iş uçları kullanılamaz.

## Coordinator adresi ve TLS

İlk eşleştirmede kullanıcı Worker'a Coordinator'ın HTTPS adresini ve ekranda gösterilen sertifika parmak izini/QR bilgisini elle verir. Worker parmak izini doğrulamadan eşleştirme kodu veya cihaz verisi göndermez. Coordinator varsayılan olarak yalnızca kullanıcının seçtiği özel LAN arayüzünde dinler; loopback veya özel ağ dışında dinleme kapalıdır. İşletim sistemi güvenlik duvarı sadece gerekli özel ağ erişimine izin verir.

Sertifika değişimi sessizce kabul edilmez: UI yeniden doğrulama ister. Kısa ömürlü yerel sertifika için güvenli bir bootstrap/yenileme yolu uygulama aşamasında belirlenmeli; TLS doğrulamasını devre dışı bırakmak yasaktır.

## Worker kayıt isteği

Worker yerel olarak sabit bir `deviceId` ve eşleştirme isteği anahtarı üretir. `requestKey` rastgele, yüksek entropili ve Worker'da tutulur; Coordinator'a yalnızca güvenli biçimde doğrulanabilir materyal gönderilir. Örnek alanlar:

```json
{
  "protocolVersion": 1,
  "deviceId": "a3b15b80-8d16-4f34-9b76-234ea6dbe0b1",
  "name": "Laptop-01",
  "deviceType": "laptop",
  "platform": "windows",
  "networkType": "ethernet",
  "capabilities": {
    "cpuCores": 12,
    "ramBytes": 34359738368,
    "gpu": [{ "name": "GPU name", "vramBytes": 12884901888 }],
    "backends": ["llama.cpp"],
    "modelFormats": ["gguf"],
    "maxConcurrency": 1
  },
  "workerPublicKey": "base64url-public-key"
}
```

Gerçek CPU/GPU değerleri Worker tarafından keşfedilip raporlanır; UI'da örnek donanım değerleri gerçekmiş gibi gösterilmez. Capability beyanı güvenilir ölçüm değildir ve tek başına iş uygunluğunu garanti etmez.

Coordinator isteği `202 Accepted` ve rastgele `requestId` ile `pending` yapar. Kullanıcı pending cihazı görür ve onaylar/reddeder. Pairing kodu kısa ömürlü, tek kullanımlık ve deneme sayısı sınırlıdır. Kod ve eşleştirme sırları loglara yazılmaz. Onaydan sonra Worker credential'ı yalnızca korumalı durum sorgusu üzerinden bir kez alır. Credential 256-bit rastgele bearer secret olmalı; yalnızca TLS üzerinden verilir, OS credential store'da saklanır ve loglarda/URL'de bulunmaz. Alternatif key-bound credential yöntemi, gerçek uygulamada token saklama riskini azaltmak için tercih edilebilir.

Örnek kayıt yanıtı:

```json
{
  "requestId": "pair-opaque-id",
  "status": "pending",
  "expiresAtUtc": "2026-11-10T07:20:00Z"
}
```

Onay bekleme sorgusu `pending`, `rejected` veya `approved` döner. `approved` yanıtında credential tek seferlik gösterilir; tekrar sorgular credential'ı tekrar açığa çıkarmaz. Kayıt anahtarı/credential iptali sonrası cihaz yeniden eşleştirilmelidir.

## Heartbeat payload

Worker yaklaşık 15 saniyede bir heartbeat yollar; Coordinator, 45 saniye heartbeat gelmezse Worker'ı offline işaretler. Süreler yapılandırılabilir. İstemci zaman damgası gözlem bilgisi içindir; timeout kararı Coordinator'ın kendi saatine göre verilir.

```json
{
  "protocolVersion": 1,
  "sequence": 42,
  "observedAtUtc": "2026-11-10T07:10:00Z",
  "status": "idle",
  "resources": {
    "cpuPercent": 21,
    "ramUsedBytes": 12884901888,
    "gpuPercent": 8,
    "temperatureCelsius": 58,
    "batteryPercent": null,
    "onExternalPower": null
  },
  "loadedModels": [
    { "name": "local-model", "format": "gguf", "backend": "llama.cpp", "loaded": true }
  ],
  "activeTaskIds": []
}
```

Coordinator `sequence` değerinin artmasını zorunlu tutar; eski/tekrar heartbeat sağlığı yenilemez. Yüzde, sıcaklık, alan uzunlukları ve model sayısı sınırlandırılır. Heartbeat yalnızca credential'ın bağlı olduğu `deviceId` için kabul edilir; istemci gövdesindeki kimlik yetki belirlemez. Mobil alanlar nullable'dır; mobil prototip gelene kadar kullanılmaz.

## İş alma ve gönderim payload'ı

Worker `POST /api/v1/worker/tasks/claim` ile boşta olduğunu bildirir. Coordinator worker yeteneklerini, güven durumunu, sağlık/kapasite durumunu ve model/backend uygunluğunu kontrol ederek en fazla bir işi atomik biçimde rezerve eder. Uygun iş yoksa `204 No Content`; iş varsa `200 OK`:

```json
{
  "taskId": "task-opaque-id",
  "leaseId": "lease-opaque-id",
  "leaseExpiresAtUtc": "2026-11-10T07:11:00Z",
  "model": { "name": "local-model", "format": "gguf", "backend": "llama.cpp" },
  "input": { "prompt": "Kullanıcının izin verdiği istek", "maxTokens": 256, "temperature": 0.7 }
}
```

Lease süresi dolarsa veya Worker offline olursa Coordinator işi `queued` durumuna alabilir. At-least-once teslim olasılığı bulunduğundan inference tekrarlanabilir kabul edilmelidir; iş kimliği ve lease ile eski/çift sonuçlar reddedilir. Büyük prompt/çıktılar için ilk sürümde sıkı boyut sınırı uygulanır; dosya/model transferi bu API kapsamına girmez.

Worker sonuç gönderimi:

```json
{
  "protocolVersion": 1,
  "leaseId": "lease-opaque-id",
  "status": "succeeded",
  "output": { "text": "Model çıktısı" },
  "usage": { "inputTokens": 24, "outputTokens": 96 },
  "error": null
}
```

Hata halinde `status: "failed"` ve makinece sınıflandırılmış, hassas prompt/stack trace içermeyen kısa bir hata kodu gönderilir. Coordinator `taskId`, `leaseId`, Worker sahipliği ve durum geçişini doğrular; çıktı ve prompt hassas veri olarak kabul edilir, varsayılan loglardan çıkarılır.

## Durum kodları

- `200`: heartbeat/sonuç kabul edildi veya eşleştirme durumu döndü.
- `202`: eşleştirme isteği onay bekliyor.
- `204`: iş yok veya unregister başarılı.
- `400`: hatalı payload / desteklenmeyen protokol sürümü.
- `401` / `403`: credential geçersiz/iptal edilmiş ya da cihaz onaysız.
- `404`: bilinmeyen kayıt/iş.
- `409`: tekrar/lease veya durum geçişi çakışması.
- `410`: eşleştirme isteği veya lease süresi doldu.
- `429`: hız sınırı.

## Güvenlik kontrol listesi

- LAN-only, tek başına güvenlik kontrolü değildir. TLS + sunucu doğrulaması + onaylı cihaz kimliği gerekir.
- Coordinator yalnızca özel LAN arayüzünde dinler; internet yönlendirme/port yönlendirme desteklenmez.
- Pairing kodları kısa ömürlü, tek kullanımlık ve hız sınırlı olmalıdır. Hatalı denemelerde gecikme/engelleme uygulanmalıdır.
- Worker credential'ları cihaz bazında, iptal edilebilir ve mümkünse anahtara bağlı olmalıdır; ortak tüm-cihaz sırrı kullanılmaz.
- Her endpoint rol/cihaz sahipliği kontrolü yapar. İstemci `deviceId` alanına güvenip yetki vermez.
- TLS, istek boyutu, hız sınırı, JSON derinliği, timeout, replay/sequence ve lease kontrolleri uygulanır.
- Worker yalnızca allowlist edilmiş inference backend'ine erişir; shell, arbitrary URL fetch, dosya sistemi veya genel RPC proxy sunmaz.
- `llama.cpp` RPC'nin güvenlik özellikleri deployment öncesi doğrulanmalı; desteklenmiyorsa doğrudan internet/LAN'a açılmaz ve host güvenlik duvarıyla sınırlandırılır.
- Prompt, model çıktısı, token ve credential loglanmaz. UI her iş için hedef Worker'ı gösterir; veri paylaşımı açık kullanıcı onayıyla yapılır.
- Worker kaldırma/iptal credential'ı geçersiz kılar ve aktif lease'leri iptal/yeniden kuyruğa alır.

## Uygulama sırası

1. Sözleşme DTO'ları ve serialization testleri.
2. Localhost üzerinde fake/simulated worker testi.
3. Coordinator endpoint ve kalıcı pairing/credential store.
4. HTTPS sertifika bootstrap'ı ve Worker outbound client.
5. Onaylı Worker heartbeat; WPF'de pending/online/offline ve revoke akışı.
6. Önceden başlatılmış, loopback'e bağlı inference backend'ine tek istek yönlendirme.

Bu sıradan önce LAN üzerinde gerçek worker portu açılmamalıdır.
