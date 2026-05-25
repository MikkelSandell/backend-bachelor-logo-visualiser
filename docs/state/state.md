# State Diagram — Product Configuration Lifecycle

Paste into **mermaid.live**

```mermaid
stateDiagram-v2
    [*] --> Oprettet : Admin opretter produkt

    Oprettet : Oprettet
    Oprettet : Ingen printzoner endnu

    ZonerTilfojet : Zoner tilføjet
    ZonerTilfojet : Har zoner men mangler\nfuldstændig metadata

    FuldtKonfigureret : Fuldt konfigureret
    FuldtKonfigureret : Alle zoner har navn,\nmm-mål og teknikker

    Oprettet --> ZonerTilfojet : Admin tegner zone på canvas
    ZonerTilfojet --> Oprettet : Alle zoner slettes

    ZonerTilfojet --> ZonerTilfojet : Zone tilføjet, redigeret\neller slettet

    ZonerTilfojet --> FuldtKonfigureret : Gem ændringer med\nkomplette zone-metadata

    FuldtKonfigureret --> ZonerTilfojet : Zone slettes eller\nmetadata ufuldstændig

    FuldtKonfigureret --> FuldtKonfigureret : Zone opdateret\nog gemt
```
