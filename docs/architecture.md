# Mimari notları

## Amaç

Windows masaüstü arayüzünden, kullanıcının yapılandırdığı LAN içindeki `llama.cpp` sunucularını ve RPC worker'larını izlemek.

## Katmanlar

- **Desktop:** WPF pencereleri, kullanıcı etkileşimi ve görünüm modelleri.
- **Core:** Alan modelleri, servis arayüzleri ve uygulama kuralları. WPF veya ağ kütüphanelerine bağımlı olmamalı.
- **Infrastructure:** Core arayüzlerinin HTTP, LAN keşfi, yapılandırma saklama ve loglama gibi somut uygulamaları.
- **Core.Tests:** Core kurallarının birim testleri.

## İlkeler

- Önce salt-okunur sağlık izleme; görev yönlendirme daha sonraki bir aşama.
- Ağ işlemleri zaman aşımı ve anlaşılır hata raporlaması içermeli.
- Kullanıcı tarafından verilen sunucu adresleri varsayılan olarak güvenilir kabul edilmemeli; erişim kontrolleri ayrıca ele alınmalı.
- LAN keşfi, açık portlar ve uzaktan komut çalıştırma gibi özellikler ancak ihtiyaç ve güvenlik tasarımı netleştikten sonra eklenmeli.
