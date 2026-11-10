# Geliştirme planı

## Aşama 1 — Temel koordinatör

- cihaz kaydı
- heartbeat
- cihaz listesi
- online/offline durumu
- temel health payload
- görev dağıtım aracı

## Aşama 2 — Çoklu laptop desteği

- iki veya daha fazla laptop kayıt olursa koordinasyon
- uygunluk kontrolü ve işçeşitli skorlaması
- failover ve retry
- çalışan işlerin izlendiği arayüz

## Aşama 3 — Mobil destek

- Android telefon worker prototipi
- pil / sıcaklık / arka plan sınırlamaları
- kullanıcı onayı ve güvenli eşleştirme
- mobil cihazlarda sınırlı görev işlemesi

## Aşama 4 — Gelişmiş dağıtık inference

- model parallelism deneme aşaması
- uygun backend doğrulaması
- ağ ve hafıza gereksinimleri ölçümü
- yalnızca teknik olarak uygun olduğunda etkinleştirme

## İlke

Daha büyük ve daha karmaşık dağıtık iş yükü, sistemin güvenli ve canlı olarak çalışmasını gerektirir. Bu yüzden ilk sürümde en kritik konular: güvenilir cihaz kaydı, heartbeat ve sağlam failover mekanizmasıdır.
