# LAN Llama Coordinator

Windows-first desktop app to coordinate llama.cpp servers and workers across a trusted LAN.

> **Status:** Early prototype. Core domain models, WPF screens and a transport-independent, in-memory worker protocol are present. Real HTTP/TLS communication and actual inference are not implemented yet.

The first release targets one Coordinator and multiple approved Workers. It routes each inference request to one compatible Worker; splitting a single model across devices is a later research item.

See [the network API design](docs/network-api-design.md) for the intended HTTPS contract and [the protocol prototype](docs/worker-protocol-prototype.md) for the current in-memory flow and limitations.
